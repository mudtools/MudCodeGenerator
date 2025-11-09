using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Text;

namespace Mud.ServiceCodeGenerator;

/// <summary>
/// HttpClient API 源生成器
/// <para>基于 Roslyn 技术，自动为标记了 [HttpClientApi] 特性的接口生成 HttpClient 实现类。</para>
/// <para>支持 HTTP 方法：Get, Post, Put, Delete, Patch, Head, Options。</para>
/// </summary>
[Generator(LanguageNames.CSharp)]
public partial class HttpClientApiSourceGenerator : WebApiSourceGenerator
{
    private static readonly string[] PathAttributes = ["PathAttribute", "Path", "RouteAttribute", "Route"];
    private const string QueryAttribute = "QueryAttribute";
    private const string HeaderAttribute = "HeaderAttribute";
    private const string BodyAttribute = "BodyAttribute";

    /// <inheritdoc/>
    protected override System.Collections.ObjectModel.Collection<string> GetFileUsingNameSpaces()
    {
        return
        [
            "System",
            "System.Web",
            "System.Net.Http",
            "System.Text",
            "System.Text.Json",
            "System.Threading.Tasks",
            "System.Collections.Generic",
            "System.Linq",
            "Microsoft.Extensions.Logging",
            "Microsoft.Extensions.Options"
        ];
    }

    /// <inheritdoc/>
    protected override void Execute(Compilation compilation, ImmutableArray<InterfaceDeclarationSyntax> interfaces, SourceProductionContext context)
    {
        if (compilation == null || interfaces.IsDefaultOrEmpty)
            return;

        foreach (var interfaceDecl in interfaces)
        {
            ProcessInterface(compilation, interfaceDecl, context);
        }
    }

    private void ProcessInterface(Compilation compilation, InterfaceDeclarationSyntax interfaceDecl, SourceProductionContext context)
    {
        try
        {
            var model = compilation.GetSemanticModel(interfaceDecl.SyntaxTree);
            if (model.GetDeclaredSymbol(interfaceDecl) is not INamedTypeSymbol interfaceSymbol)
                return;

            if (!HasValidHttpMethods(interfaceSymbol, interfaceDecl))
            {
                ReportWarning(context, interfaceDecl.Identifier.Text, "HTTPCLIENT002",
                    $"接口{interfaceDecl.Identifier.Text}不包含有效的HTTP方法特性，跳过生成");
                return;
            }

            var sourceCode = GenerateImplementationClass(compilation, interfaceSymbol, interfaceDecl);
            var className = GetImplementationClassName(interfaceSymbol.Name);
            context.AddSource($"{className}.g.cs", SourceText.From(sourceCode, Encoding.UTF8));
        }
        catch (Exception ex)
        {
            HandleInterfaceProcessingException(ex, interfaceDecl, context);
        }
    }

    private void HandleInterfaceProcessingException(Exception ex, InterfaceDeclarationSyntax interfaceDecl, SourceProductionContext context)
    {
        var descriptor = ex switch
        {
            InvalidOperationException => new DiagnosticDescriptor(
                "HTTPCLIENT003", "HttpClient API语法错误",
                $"接口{interfaceDecl.Identifier.Text}的语法分析失败: {ex.Message}",
                "Generation", DiagnosticSeverity.Error, true),
            ArgumentException => new DiagnosticDescriptor(
                "HTTPCLIENT004", "HttpClient API参数错误",
                $"接口{interfaceDecl.Identifier.Text}的参数配置错误: {ex.Message}",
                "Generation", DiagnosticSeverity.Error, true),
            _ => new DiagnosticDescriptor(
                "HTTPCLIENT001", "HttpClient API生成错误",
                $"生成接口{interfaceDecl.Identifier.Text}的实现时发生错误: {ex.Message}",
                "Generation", DiagnosticSeverity.Error, true)
        };

        ReportErrorDiagnostic(context, descriptor, interfaceDecl.Identifier.Text, ex);
    }

    private void ReportWarning(SourceProductionContext context, string interfaceName, string id, string message)
    {
        var descriptor = new DiagnosticDescriptor(id, "HttpClient API警告", message, "Generation", DiagnosticSeverity.Warning, true);
        ReportWarningDiagnostic(context, descriptor, interfaceName);
    }

    private string GenerateImplementationClass(Compilation compilation, INamedTypeSymbol interfaceSymbol, InterfaceDeclarationSyntax interfaceDecl)
    {
        var className = GetImplementationClassName(interfaceSymbol.Name);
        var namespaceName = GetNamespaceName(interfaceDecl);

        var codeBuilder = new StringBuilder();
        GenerateClassStructure(codeBuilder, className, namespaceName, interfaceSymbol);
        GenerateMethods(compilation, codeBuilder, interfaceSymbol, interfaceDecl);

        codeBuilder.AppendLine("    }");
        codeBuilder.AppendLine("}");

        return codeBuilder.ToString();
    }

    private void GenerateClassStructure(StringBuilder codeBuilder, string className, string namespaceName, INamedTypeSymbol interfaceSymbol)
    {
        GenerateFileHeader(codeBuilder);

        codeBuilder.AppendLine();
        codeBuilder.AppendLine($"namespace {namespaceName}");
        codeBuilder.AppendLine("{");
        codeBuilder.AppendLine($"    /// <summary>");
        codeBuilder.AppendLine($"    /// {interfaceSymbol.Name}的HttpClient实现类");
        codeBuilder.AppendLine($"    /// </summary>");
        codeBuilder.AppendLine("    [global::System.Runtime.CompilerServices.CompilerGenerated]");
        codeBuilder.AppendLine($"    internal partial class {className} : {interfaceSymbol.Name}");
        codeBuilder.AppendLine("    {");
        GenerateClassFieldsAndConstructor(codeBuilder, className);
    }

    private void GenerateClassFieldsAndConstructor(StringBuilder codeBuilder, string className)
    {
        codeBuilder.AppendLine("        private readonly HttpClient _httpClient;");
        codeBuilder.AppendLine($"        private readonly ILogger<{className}> _logger;");
        codeBuilder.AppendLine("        private readonly JsonSerializerOptions _jsonSerializerOptions;");
        codeBuilder.AppendLine();
        codeBuilder.AppendLine("        /// <summary>");
        codeBuilder.AppendLine($"        /// 构建 <see cref = \"{className}\"/> 类的实例。");
        codeBuilder.AppendLine("        /// </summary>");
        codeBuilder.AppendLine("        /// <param name=\"httpClient\">HttpClient实例</param>");
        codeBuilder.AppendLine("        /// <param name=\"logger\">日志记录器</param>");
        codeBuilder.AppendLine("        /// <param name=\"option\">Json序列化参数</param>");
        codeBuilder.AppendLine($"        public {className}(HttpClient httpClient, ILogger<{className}> logger, IOptions<JsonSerializerOptions> option)");
        codeBuilder.AppendLine("        {");
        codeBuilder.AppendLine("            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));");
        codeBuilder.AppendLine("            _logger = logger ?? throw new ArgumentNullException(nameof(logger));");
        codeBuilder.AppendLine("            _jsonSerializerOptions = option.Value;");
        codeBuilder.AppendLine("        }");
    }

    private void GenerateMethods(Compilation compilation, StringBuilder codeBuilder, INamedTypeSymbol interfaceSymbol, InterfaceDeclarationSyntax interfaceDecl)
    {
        GenerateClassPartialMethods(codeBuilder, interfaceSymbol);

        foreach (var methodSymbol in interfaceSymbol.GetMembers().OfType<IMethodSymbol>())
        {
            GenerateMethodImplementation(compilation, codeBuilder, methodSymbol, interfaceDecl);
        }
    }

    private void GenerateMethodImplementation(Compilation compilation, StringBuilder codeBuilder, IMethodSymbol methodSymbol, InterfaceDeclarationSyntax interfaceDecl)
    {
        var methodInfo = AnalyzeMethod(compilation, methodSymbol, interfaceDecl);
        if (!methodInfo.IsValid) return;

        codeBuilder.AppendLine();
        codeBuilder.AppendLine($"        /// <summary>");
        codeBuilder.AppendLine($"        /// 实现 {methodSymbol.Name} 方法");
        codeBuilder.AppendLine($"        /// </summary>");
        codeBuilder.AppendLine("        [global::System.Runtime.CompilerServices.CompilerGenerated]");
        codeBuilder.AppendLine($"        public async {methodSymbol.ReturnType} {methodSymbol.Name}({GetParameterList(methodSymbol)})");
        codeBuilder.AppendLine("        {");

        GenerateRequestSetup(codeBuilder, methodInfo);
        GenerateParameterHandling(codeBuilder, methodInfo);
        GenerateRequestExecution(codeBuilder, methodInfo);

        codeBuilder.AppendLine("        }");
    }

    private void GenerateClassPartialMethods(StringBuilder codeBuilder, INamedTypeSymbol interfaceSymbol)
    {
        var methods = interfaceSymbol.GetMembers().OfType<IMethodSymbol>().ToList();
        var processedMethods = new HashSet<string>();

        foreach (var methodSymbol in methods)
        {
            if (!processedMethods.Add(methodSymbol.Name))
                continue;

            GenerateMethodPartialMethods(codeBuilder, methodSymbol.Name);
        }

        var interfaceName = GetImplementationClassName(interfaceSymbol.Name);
        if (processedMethods.Add(interfaceName))
        {
            GenerateInterfacePartialMethods(codeBuilder, interfaceName);
        }
    }

    private void GenerateMethodPartialMethods(StringBuilder codeBuilder, string methodName)
    {
        var events = new[]
        {
            ("Before", "方法调用之前", "HttpRequestMessage request"),
            ("After", "方法调用之后", "HttpResponseMessage response"),
            ("Fail", "方法调用失败", "HttpResponseMessage response"),
            ("Error", "方法调用发生错误", "Exception error")
        };

        foreach (var (eventType, description, parameter) in events)
        {
            codeBuilder.AppendLine();
            codeBuilder.AppendLine($"        /// <summary>");
            codeBuilder.AppendLine($"        /// {methodName} {description}。");
            codeBuilder.AppendLine($"        /// </summary>");
            codeBuilder.AppendLine("        [global::System.Runtime.CompilerServices.CompilerGenerated]");
            codeBuilder.AppendLine($"        partial void On{StringExtensions.ConvertFunctionName(methodName, eventType)}({parameter}, string url);");
        }
    }

    private void GenerateInterfacePartialMethods(StringBuilder codeBuilder, string interfaceName)
    {
        var events = new[]
        {
            ("RequestBefore", "方法调用之前", "HttpRequestMessage request"),
            ("RequestAfter", "方法调用之后", "HttpResponseMessage response"),
            ("RequestFail", "方法调用失败", "HttpResponseMessage response"),
            ("RequestError", "方法调用发生错误", "Exception error")
        };

        foreach (var (eventType, description, parameter) in events)
        {
            codeBuilder.AppendLine();
            codeBuilder.AppendLine($"        /// <summary>");
            codeBuilder.AppendLine($"        /// {interfaceName} {description}。");
            codeBuilder.AppendLine($"        /// </summary>");
            codeBuilder.AppendLine("        [global::System.Runtime.CompilerServices.CompilerGenerated]");
            codeBuilder.AppendLine($"        partial void On{StringExtensions.ConvertFunctionName(interfaceName, "Api", eventType)}({parameter}, string url);");
        }
    }

    private MethodAnalysisResult AnalyzeMethod(Compilation compilation, IMethodSymbol methodSymbol, InterfaceDeclarationSyntax interfaceDecl)
    {
        var methodSyntax = FindMethodSyntax(compilation, methodSymbol, interfaceDecl);
        if (methodSyntax == null)
            return MethodAnalysisResult.Invalid;

        var httpMethodAttr = FindHttpMethodAttribute(methodSyntax);
        if (httpMethodAttr == null)
            return MethodAnalysisResult.Invalid;

        var httpMethod = httpMethodAttr.Name.ToString();
        var urlTemplate = GetAttributeArgumentValue(httpMethodAttr, 0)?.ToString().Trim('"') ?? "";

        var parameters = methodSymbol.Parameters.Select(p => new ParameterInfo
        {
            Name = p.Name,
            Type = p.Type.ToDisplayString(),
            Attributes = p.GetAttributes().Select(attr => new ParameterAttributeInfo
            {
                Name = attr.AttributeClass?.Name ?? "",
                Arguments = attr.ConstructorArguments.Select(arg => arg.Value).ToArray(),
                NamedArguments = attr.NamedArguments.ToDictionary(na => na.Key, na => na.Value.Value)
            }).ToList()
        }).ToList();

        return new MethodAnalysisResult
        {
            InterfaceName = interfaceDecl.Identifier.Text,
            IsValid = true,
            MethodName = methodSymbol.Name,
            HttpMethod = httpMethod,
            UrlTemplate = urlTemplate,
            ReturnType = GetReturnTypeDisplayString(methodSymbol.ReturnType),
            Parameters = parameters
        };
    }

    private MethodDeclarationSyntax? FindMethodSyntax(Compilation compilation, IMethodSymbol methodSymbol, InterfaceDeclarationSyntax interfaceDecl)
    {
        return interfaceDecl.Members
            .OfType<MethodDeclarationSyntax>()
            .FirstOrDefault(m =>
            {
                var model = compilation.GetSemanticModel(m.SyntaxTree);
                var methodSymbolFromSyntax = model.GetDeclaredSymbol(m);
                return methodSymbolFromSyntax?.Equals(methodSymbol, SymbolEqualityComparer.Default) == true;
            });
    }

    private AttributeSyntax? FindHttpMethodAttribute(MethodDeclarationSyntax methodSyntax)
    {
        return methodSyntax.AttributeLists
            .SelectMany(a => a.Attributes)
            .FirstOrDefault(a => SupportedHttpMethods.Contains(a.Name.ToString()));
    }

    private void GenerateRequestSetup(StringBuilder codeBuilder, MethodAnalysisResult methodInfo)
    {
        var urlCode = BuildUrlString(methodInfo);
        codeBuilder.AppendLine(urlCode);
        codeBuilder.AppendLine($"            _logger.LogDebug(\"开始HTTP {methodInfo.HttpMethod}请求: {{Url}}\", url);");
        codeBuilder.AppendLine($"            using var request = new HttpRequestMessage(HttpMethod.{methodInfo.HttpMethod}, url);");
    }

    private string BuildUrlString(MethodAnalysisResult methodInfo)
    {
        var pathParams = methodInfo.Parameters
            .Where(p => p.Attributes.Any(attr => PathAttributes.Contains(attr.Name)))
            .ToList();

        if (!pathParams.Any())
            return $"            var url = \"{methodInfo.UrlTemplate}\";";

        var interpolatedUrl = methodInfo.UrlTemplate;
        foreach (var param in pathParams)
        {
            if (methodInfo.UrlTemplate.Contains($"{{{param.Name}}}"))
            {
                var formatString = GetFormatString(param.Attributes.First(a => PathAttributes.Contains(a.Name)));
                interpolatedUrl = FormatUrlParameter(interpolatedUrl, param.Name, formatString);
            }
        }

        return $"            var url = $\"{interpolatedUrl}\";";
    }

    private string FormatUrlParameter(string url, string paramName, string? formatString)
    {
        return string.IsNullOrEmpty(formatString)
            ? url.Replace($"{{{paramName}}}", $"{{{paramName}}}")
            : url.Replace($"{{{paramName}}}", $"{{{paramName}.ToString(\"{formatString}\")}}");
    }

    private void GenerateParameterHandling(StringBuilder codeBuilder, MethodAnalysisResult methodInfo)
    {
        GenerateQueryParameters(codeBuilder, methodInfo);
        GenerateHeaderParameters(codeBuilder, methodInfo);
        GenerateBodyParameter(codeBuilder, methodInfo);
    }

    private void GenerateQueryParameters(StringBuilder codeBuilder, MethodAnalysisResult methodInfo)
    {
        var queryParams = methodInfo.Parameters
            .Where(p => p.Attributes.Any(attr => attr.Name == QueryAttribute))
            .ToList();

        if (!queryParams.Any())
            return;

        codeBuilder.AppendLine($"            var queryParams = HttpUtility.ParseQueryString(string.Empty);");

        foreach (var param in queryParams)
        {
            GenerateSingleQueryParameter(codeBuilder, param);
        }

        codeBuilder.AppendLine("            if (queryParams.Count > 0)");
        codeBuilder.AppendLine("            {");
        codeBuilder.AppendLine("                url += \"?\" + queryParams.ToString();");
        codeBuilder.AppendLine("            }");
    }

    private void GenerateSingleQueryParameter(StringBuilder codeBuilder, ParameterInfo param)
    {
        var queryAttr = param.Attributes.First(a => a.Name == QueryAttribute);
        var paramName = GetQueryParameterName(queryAttr, param.Name);
        var formatString = GetFormatString(queryAttr);

        if (IsSimpleType(param.Type))
        {
            GenerateSimpleQueryParameter(codeBuilder, param, paramName, formatString);
        }
        else
        {
            GenerateComplexQueryParameter(codeBuilder, param, paramName);
        }
    }

    private void GenerateSimpleQueryParameter(StringBuilder codeBuilder, ParameterInfo param, string paramName, string? formatString)
    {
        if (IsStringType(param.Type))
        {
            codeBuilder.AppendLine($"            if (!string.IsNullOrEmpty({param.Name}))");
            codeBuilder.AppendLine("            {");
            codeBuilder.AppendLine($"                var encodedValue = HttpUtility.UrlEncode({param.Name});");
            codeBuilder.AppendLine($"                queryParams.Add(\"{paramName}\", encodedValue);");
            codeBuilder.AppendLine("            }");
        }
        else
        {
            codeBuilder.AppendLine($"            if ({param.Name} != null)");
            var formatExpression = !string.IsNullOrEmpty(formatString)
                ? $".ToString(\"{formatString}\")"
                : ".ToString()";
            codeBuilder.AppendLine($"                queryParams.Add(\"{paramName}\", {param.Name}{formatExpression});");
        }
    }

    private void GenerateComplexQueryParameter(StringBuilder codeBuilder, ParameterInfo param, string paramName)
    {
        codeBuilder.AppendLine($"            if ({param.Name} != null)");
        codeBuilder.AppendLine("            {");
        codeBuilder.AppendLine($"                var properties = {param.Name}.GetType().GetProperties();");
        codeBuilder.AppendLine("                foreach (var prop in properties)");
        codeBuilder.AppendLine("                {");
        codeBuilder.AppendLine($"                    var value = prop.GetValue({param.Name});");
        codeBuilder.AppendLine("                    if (value != null)");
        codeBuilder.AppendLine("                    {");
        codeBuilder.AppendLine($"                        queryParams.Add(prop.Name, HttpUtility.UrlEncode(value.ToString()));");
        codeBuilder.AppendLine("                    }");
        codeBuilder.AppendLine("                }");
        codeBuilder.AppendLine("            }");
    }

    private void GenerateHeaderParameters(StringBuilder codeBuilder, MethodAnalysisResult methodInfo)
    {
        var headerParams = methodInfo.Parameters
            .Where(p => p.Attributes.Any(attr => attr.Name == HeaderAttribute))
            .ToList();

        foreach (var param in headerParams)
        {
            var headerAttr = param.Attributes.First(a => a.Name == HeaderAttribute);
            var headerName = headerAttr.Arguments.FirstOrDefault()?.ToString() ?? param.Name;

            codeBuilder.AppendLine($"            if (!string.IsNullOrEmpty({param.Name}))");
            codeBuilder.AppendLine($"                request.Headers.Add(\"{headerName}\", {param.Name});");
        }
    }

    private void GenerateBodyParameter(StringBuilder codeBuilder, MethodAnalysisResult methodInfo)
    {
        var bodyParam = methodInfo.Parameters
            .FirstOrDefault(p => p.Attributes.Any(attr => attr.Name == BodyAttribute));

        if (bodyParam == null)
            return;

        var bodyAttr = bodyParam.Attributes.First(a => a.Name == BodyAttribute);
        var contentType = GetBodyContentType(bodyAttr);
        var useStringContent = GetUseStringContentFlag(bodyAttr);

        codeBuilder.AppendLine($"            if ({bodyParam.Name} != null)");
        codeBuilder.AppendLine("            {");

        if (useStringContent)
        {
            codeBuilder.AppendLine($"                request.Content = new StringContent({bodyParam.Name}.ToString() ?? \"\", Encoding.UTF8, \"{contentType}\");");
        }
        else
        {
            codeBuilder.AppendLine($"                var jsonContent = JsonSerializer.Serialize({bodyParam.Name}, _jsonSerializerOptions);");
            codeBuilder.AppendLine($"                request.Content = new StringContent(jsonContent, Encoding.UTF8, \"{contentType}\");");
        }

        codeBuilder.AppendLine("            }");
    }

    private string GetBodyContentType(ParameterAttributeInfo bodyAttr)
    {
        return bodyAttr.NamedArguments.TryGetValue("ContentType", out var contentTypeArg)
            ? (contentTypeArg?.ToString() ?? "application/json")
            : "application/json";
    }

    private bool GetUseStringContentFlag(ParameterAttributeInfo bodyAttr)
    {
        return bodyAttr.NamedArguments.TryGetValue("UseStringContent", out var useStringContentArg)
            && bool.Parse(useStringContentArg?.ToString() ?? "false");
    }

    private void GenerateRequestExecution(StringBuilder codeBuilder, MethodAnalysisResult methodInfo)
    {
        var cancellationTokenArg = GetCancellationTokenParam(methodInfo);
        var interfaceName = GetImplementationClassName(methodInfo.InterfaceName);

        codeBuilder.AppendLine("            try");
        codeBuilder.AppendLine("            {");
        GenerateRequestExecutionCore(codeBuilder, methodInfo, interfaceName, cancellationTokenArg);
        codeBuilder.AppendLine("            }");
        codeBuilder.AppendLine("            catch (System.Exception ex)");
        codeBuilder.AppendLine("            {");
        GenerateExceptionHandling(codeBuilder, methodInfo, interfaceName);
        codeBuilder.AppendLine("            }");
    }

    private void GenerateRequestExecutionCore(StringBuilder codeBuilder, MethodAnalysisResult methodInfo, string interfaceName, string cancellationTokenArg)
    {
        codeBuilder.AppendLine($"                On{StringExtensions.ConvertFunctionName(interfaceName, "Api", "RequestBefore")}(request, url);");
        codeBuilder.AppendLine($"                On{StringExtensions.ConvertFunctionName(methodInfo.MethodName, "Before")}(request, url);");
        codeBuilder.AppendLine($"                using var response = await _httpClient.SendAsync(request{cancellationTokenArg});");
        codeBuilder.AppendLine("                _logger.LogDebug(\"HTTP请求完成: {StatusCode}\", (int)response.StatusCode);");
        codeBuilder.AppendLine();
        codeBuilder.AppendLine("                if (!response.IsSuccessStatusCode)");
        codeBuilder.AppendLine("                {");
        GenerateErrorResponseHandling(codeBuilder, methodInfo, interfaceName);
        codeBuilder.AppendLine("                }");
        codeBuilder.AppendLine($"                On{StringExtensions.ConvertFunctionName(interfaceName, "Api", "RequestAfter")}(response, url);");
        codeBuilder.AppendLine($"                On{StringExtensions.ConvertFunctionName(methodInfo.MethodName, "After")}(response, url);");

        GenerateResponseProcessing(codeBuilder, methodInfo, cancellationTokenArg);
    }

    private void GenerateErrorResponseHandling(StringBuilder codeBuilder, MethodAnalysisResult methodInfo, string interfaceName)
    {
        codeBuilder.AppendLine($"                    On{StringExtensions.ConvertFunctionName(interfaceName, "Api", "RequestFail")}(response, url);");
        codeBuilder.AppendLine($"                    On{StringExtensions.ConvertFunctionName(methodInfo.MethodName, "Fail")}(response, url);");
        codeBuilder.AppendLine("                    var errorContent = await response.Content.ReadAsStringAsync();");
        codeBuilder.AppendLine("                    _logger.LogError(\"HTTP请求失败: {StatusCode}, 响应: {Response}\", (int)response.StatusCode, errorContent);");
        codeBuilder.AppendLine("                    throw new HttpRequestException($\"HTTP请求失败: {(int)response.StatusCode}\");");
    }

    private void GenerateResponseProcessing(StringBuilder codeBuilder, MethodAnalysisResult methodInfo, string cancellationTokenArg)
    {
        codeBuilder.AppendLine("                using var stream = await response.Content.ReadAsStreamAsync();");
        codeBuilder.AppendLine();
        codeBuilder.AppendLine("                if (stream.Length == 0)");
        codeBuilder.AppendLine("                {");
        codeBuilder.AppendLine("                    return default;");
        codeBuilder.AppendLine("                }");
        codeBuilder.AppendLine();
        codeBuilder.AppendLine($"                var result = await JsonSerializer.DeserializeAsync<{methodInfo.ReturnType}>(stream, _jsonSerializerOptions{cancellationTokenArg});");
        codeBuilder.AppendLine("                return result;");
    }

    private void GenerateExceptionHandling(StringBuilder codeBuilder, MethodAnalysisResult methodInfo, string interfaceName)
    {
        codeBuilder.AppendLine("                _logger.LogError(ex, \"HTTP请求异常: {{Url}}\", url);");
        codeBuilder.AppendLine($"                On{StringExtensions.ConvertFunctionName(interfaceName, "Api", "RequestError")}(ex, url);");
        codeBuilder.AppendLine($"                On{StringExtensions.ConvertFunctionName(methodInfo.MethodName, "Error")}(ex, url);");
        codeBuilder.AppendLine("                throw;");
    }

    // 辅助方法保持不变，但可以进一步优化...
    private object? GetAttributeArgumentValue(AttributeSyntax attribute, int index)
    {
        if (attribute.ArgumentList == null || index >= attribute.ArgumentList.Arguments.Count)
            return null;

        return attribute.ArgumentList.Arguments[index].Expression switch
        {
            LiteralExpressionSyntax literal => literal.Token.Value,
            _ => null
        };
    }

    private string GetReturnTypeDisplayString(ITypeSymbol returnType)
    {
        if (returnType is INamedTypeSymbol namedType && namedType.IsGenericType)
        {
            if (namedType.Name == "Task" && namedType.TypeArguments.Length == 1)
            {
                var genericType = namedType.TypeArguments[0];
                return genericType is INamedTypeSymbol genericNamedType &&
                       genericNamedType.IsGenericType &&
                       genericNamedType.Name == "Nullable"
                    ? $"{genericNamedType.TypeArguments[0].ToDisplayString()}?"
                    : genericType.ToDisplayString();
            }
        }

        return returnType.ToDisplayString();
    }

    private bool IsSimpleType(string typeName)
    {
        var simpleTypes = new[] { "string", "int", "long", "float", "double", "decimal", "bool", "DateTime", "System.DateTime", "Guid", "System.Guid" };
        return simpleTypes.Contains(typeName) || typeName.EndsWith("?") && simpleTypes.Contains(typeName.TrimEnd('?'));
    }

    private bool IsStringType(string typeName)
    {
        return typeName.Equals("string", StringComparison.OrdinalIgnoreCase) ||
               typeName.Equals("string?", StringComparison.OrdinalIgnoreCase);
    }

    private string? GetFormatString(ParameterAttributeInfo attribute)
    {
        // 检查构造函数参数
        if (attribute.Arguments.Length > 1)
        {
            return attribute.Arguments[1] as string;
        }
        else if (attribute.Arguments.Length == 1 && PathAttributes.Contains(attribute.Name))
        {
            return attribute.Arguments[0] as string;
        }

        // 检查命名参数
        return attribute.NamedArguments.TryGetValue("FormatString", out var formatString)
            ? formatString as string
            : null;
    }

    private string GetQueryParameterName(ParameterAttributeInfo attribute, string defaultName)
    {
        if (attribute.Arguments.Length > 0)
        {
            var nameArg = attribute.Arguments[0] as string;
            if (!string.IsNullOrEmpty(nameArg))
                return nameArg;
        }

        return attribute.NamedArguments.TryGetValue("Name", out var nameNamedArg)
            ? nameNamedArg as string ?? defaultName
            : defaultName;
    }

    private string GetCancellationTokenParam(MethodAnalysisResult methodInfo)
    {
        var cancellationTokenParam = methodInfo.Parameters.FirstOrDefault(p => p.Type.Contains("CancellationToken"));
        return cancellationTokenParam != null ? $", {cancellationTokenParam.Name}" : "";
    }


    /// <summary>
    /// 方法分析结果
    /// </summary>
    /// <remarks>
    /// 用于存储接口方法的分析信息，包括 HTTP 方法、URL 模板、参数等。
    /// </remarks>
    private class MethodAnalysisResult
    {
        /// <summary>
        /// 接口名称
        /// </summary>
        public string InterfaceName { get; set; } = string.Empty;

        /// <summary>
        /// 方法是否有效
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// 方法名称
        /// </summary>
        public string MethodName { get; set; } = string.Empty;

        /// <summary>
        /// HTTP 方法（GET, POST, PUT, DELETE, PATCH, HEAD, OPTIONS）
        /// </summary>
        public string HttpMethod { get; set; } = string.Empty;

        /// <summary>
        /// URL 模板，支持参数占位符
        /// </summary>
        public string UrlTemplate { get; set; } = string.Empty;

        /// <summary>
        /// 返回类型显示字符串
        /// </summary>
        public string ReturnType { get; set; } = string.Empty;

        /// <summary>
        /// 方法参数列表
        /// </summary>
        public List<ParameterInfo> Parameters { get; set; } = [];

        /// <summary>
        /// 无效的分析结果实例
        /// </summary>
        public static MethodAnalysisResult Invalid => new() { IsValid = false };
    }

    /// <summary>
    /// 参数信息
    /// </summary>
    /// <remarks>
    /// 存储方法参数的详细信息，包括参数名、类型和特性。
    /// </remarks>
    private class ParameterInfo
    {
        /// <summary>
        /// 参数名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 参数类型显示字符串
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// 参数特性列表
        /// </summary>
        public List<ParameterAttributeInfo> Attributes { get; set; } = [];
    }

    /// <summary>
    /// 参数特性信息
    /// </summary>
    /// <remarks>
    /// 存储参数特性的详细信息，包括特性名称、构造函数参数和命名参数。
    /// </remarks>
    private class ParameterAttributeInfo
    {
        /// <summary>
        /// 特性名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 构造函数参数数组
        /// </summary>
        public object?[] Arguments { get; set; } = [];

        /// <summary>
        /// 命名参数字典
        /// </summary>
        public Dictionary<string, object?> NamedArguments { get; set; } = [];
    }
}