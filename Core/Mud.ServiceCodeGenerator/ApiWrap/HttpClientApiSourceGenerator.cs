// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.CodeGenerator 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

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
    /// <inheritdoc/>
    protected override void ExecuteGenerator(Compilation compilation, ImmutableArray<InterfaceDeclarationSyntax> interfaces, SourceProductionContext context)
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

            if (!HasValidHttpMethods(interfaceSymbol))
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
        codeBuilder.AppendLine($"    /// <inheritdoc cref=\"{interfaceSymbol.Name}\"/>");
        codeBuilder.AppendLine($"    /// </summary>");
        codeBuilder.AppendLine($"    {CompilerGeneratedAttribute}");
        codeBuilder.AppendLine($"    {GeneratedCodeAttribute}");
        codeBuilder.AppendLine($"    internal partial class {className} : {interfaceSymbol.Name}");
        codeBuilder.AppendLine("    {");
        GenerateClassFieldsAndConstructor(codeBuilder, className, interfaceSymbol);
    }

    /// <summary>
    /// 从HttpClientApi特性获取内容类型
    /// </summary>
    /// <param name="httpClientApiAttribute">HttpClientApi特性</param>
    /// <returns>内容类型</returns>
    private string GetContentTypeFromAttribute(AttributeData? httpClientApiAttribute)
    {
        if (httpClientApiAttribute?.NamedArguments.FirstOrDefault(k => k.Key == "ContentType").Value.Value is string contentType)
            return contentType;

        return "application/json";
    }

    /// <summary>
    /// 从HttpClientApi特性获取超时设置
    /// </summary>
    /// <param name="httpClientApiAttribute">HttpClientApi特性</param>
    /// <returns>超时秒数</returns>
    private int GetTimeoutFromAttribute(AttributeData? httpClientApiAttribute)
    {
        if (httpClientApiAttribute?.NamedArguments.FirstOrDefault(k => k.Key == "TimeoutSeconds").Value.Value is int timeout)
            return timeout;

        return 30; // 默认30秒超时
    }
    private void GenerateClassFieldsAndConstructor(StringBuilder codeBuilder, string className, INamedTypeSymbol interfaceSymbol)
    {
        codeBuilder.AppendLine("        private readonly HttpClient _httpClient;");
        codeBuilder.AppendLine($"        private readonly ILogger<{className}> _logger;");
        codeBuilder.AppendLine("        private readonly JsonSerializerOptions _jsonSerializerOptions;");

        // 从HttpClientApi特性获取配置
        var httpClientApiAttribute = GetHttpClientApiAttribute(interfaceSymbol);
        var defaultContentType = "application/json";

        if (httpClientApiAttribute != null)
        {
            defaultContentType = GetContentTypeFromAttribute(httpClientApiAttribute);
        }

        codeBuilder.AppendLine($"        private readonly string _defaultContentType = \"{defaultContentType}\";");
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

        if (httpClientApiAttribute != null)
        {
            var timeout = GetTimeoutFromAttribute(httpClientApiAttribute);
            codeBuilder.AppendLine();
            codeBuilder.AppendLine($"            // 配置HttpClient超时时间");
            codeBuilder.AppendLine($"            _httpClient.Timeout = TimeSpan.FromSeconds({timeout});");
        }

        codeBuilder.AppendLine("        }");
        codeBuilder.AppendLine();

        // 添加辅助方法来获取媒体类型（去除字符集信息）
        codeBuilder.AppendLine("        /// <summary>");
        codeBuilder.AppendLine("        /// 从Content-Type字符串中提取媒体类型部分，去除字符集信息。");
        codeBuilder.AppendLine("        /// </summary>");
        codeBuilder.AppendLine("        /// <param name=\"contentType\">完整的Content-Type字符串</param>");
        codeBuilder.AppendLine("        /// <returns>媒体类型部分</returns>");
        codeBuilder.AppendLine("        private static string GetMediaType(string contentType)");
        codeBuilder.AppendLine("        {");
        codeBuilder.AppendLine("            if (string.IsNullOrEmpty(contentType))");
        codeBuilder.AppendLine("                return \"application/json\";");
        codeBuilder.AppendLine();
        codeBuilder.AppendLine("            // Content-Type可能包含字符集信息，如 \"application/json; charset=utf-8\"");
        codeBuilder.AppendLine("            // 需要分号前的媒体类型部分");
        codeBuilder.AppendLine("            var semicolonIndex = contentType.IndexOf(';');");
        codeBuilder.AppendLine("            if (semicolonIndex >= 0)");
        codeBuilder.AppendLine("            {");
        codeBuilder.AppendLine("                return contentType.Substring(0, semicolonIndex).Trim();");
        codeBuilder.AppendLine("            }");
        codeBuilder.AppendLine();
        codeBuilder.AppendLine("            return contentType.Trim();");
        codeBuilder.AppendLine("        }");
        codeBuilder.AppendLine();
    }

    private void GenerateMethods(Compilation compilation, StringBuilder codeBuilder, INamedTypeSymbol interfaceSymbol, InterfaceDeclarationSyntax interfaceDecl)
    {
        GenerateClassPartialMethods(codeBuilder, interfaceSymbol);

        foreach (var methodSymbol in interfaceSymbol.GetMembers().OfType<IMethodSymbol>())
        {
            GenerateMethodImplementation(compilation, codeBuilder, methodSymbol, interfaceDecl);
        }
    }

    /// <summary>
    /// <inheritdoc cref=""/>
    /// </summary>
    /// <param name="compilation"></param>
    /// <param name="codeBuilder"></param>
    /// <param name="methodSymbol"></param>
    /// <param name="interfaceDecl"></param>
    private void GenerateMethodImplementation(Compilation compilation, StringBuilder codeBuilder, IMethodSymbol methodSymbol, InterfaceDeclarationSyntax interfaceDecl)
    {
        var methodInfo = AnalyzeMethod(compilation, methodSymbol, interfaceDecl);
        if (!methodInfo.IsValid) return;

        codeBuilder.AppendLine();
        codeBuilder.AppendLine($"        /// <summary>");
        codeBuilder.AppendLine($"        /// <inheritdoc cref=\"{methodInfo.InterfaceName}.{methodSymbol.Name} \"/>");
        codeBuilder.AppendLine($"        /// </summary>");
        codeBuilder.AppendLine($"        {GeneratedCodeAttribute}");
        // 根据方法返回类型决定是否添加 async 关键字
        var asyncKeyword = methodInfo.IsAsyncMethod ? "async " : "";
        codeBuilder.AppendLine($"        public {asyncKeyword}{methodSymbol.ReturnType} {methodSymbol.Name}({GetParameterList(methodSymbol)})");
        codeBuilder.AppendLine("        {");

        GenerateRequestSetup(codeBuilder, methodInfo);
        GenerateParameterHandling(codeBuilder, methodInfo);
        GenerateRequestExecution(codeBuilder, methodInfo);

        codeBuilder.AppendLine("        }");
        codeBuilder.AppendLine();
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
            codeBuilder.AppendLine($"        {GeneratedCodeAttribute}");
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
            codeBuilder.AppendLine($"        {GeneratedCodeAttribute}");
            codeBuilder.AppendLine($"        partial void On{StringExtensions.ConvertFunctionName(interfaceName, "Api", eventType)}({parameter}, string url);");
        }
    }


    private void GenerateRequestSetup(StringBuilder codeBuilder, MethodAnalysisResult methodInfo)
    {
        var urlCode = BuildUrlString(methodInfo);
        codeBuilder.AppendLine(urlCode);
        codeBuilder.AppendLine($"            _logger.LogDebug(\"开始HTTP {methodInfo.HttpMethod}请求: {{Url}}\", url);");
        codeBuilder.AppendLine($"            using var request = new HttpRequestMessage(HttpMethod.{methodInfo.HttpMethod}, url);");
        //codeBuilder.AppendLine($"            request.Headers.Add(\"Content-Type\", _defaultContentType);");
    }

    private string BuildUrlString(MethodAnalysisResult methodInfo)
    {
        var pathParams = methodInfo.Parameters
            .Where(p => p.Attributes.Any(attr => GeneratorConstants.PathAttributes.Contains(attr.Name)))
            .ToList();

        if (!pathParams.Any())
            return $"            var url = \"{methodInfo.UrlTemplate}\";";

        var interpolatedUrl = methodInfo.UrlTemplate;
        foreach (var param in pathParams)
        {
            if (methodInfo.UrlTemplate.Contains($"{{{param.Name}}}"))
            {
                var formatString = GetFormatString(param.Attributes.First(a => GeneratorConstants.PathAttributes.Contains(a.Name)));
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
            .Where(p => p.Attributes.Any(attr => attr.Name == GeneratorConstants.QueryAttribute))
            .ToList();

        var arrayQueryParams = methodInfo.Parameters
            .Where(p => p.Attributes.Any(attr => attr.Name == GeneratorConstants.ArrayQueryAttribute))
            .ToList();

        if (!queryParams.Any() && !arrayQueryParams.Any())
            return;

        codeBuilder.AppendLine($"            var queryParams = HttpUtility.ParseQueryString(string.Empty);");

        foreach (var param in queryParams)
        {
            GenerateSingleQueryParameter(codeBuilder, param);
        }

        foreach (var param in arrayQueryParams)
        {
            GenerateArrayQueryParameter(codeBuilder, param);
        }

        codeBuilder.AppendLine("            if (queryParams.Count > 0)");
        codeBuilder.AppendLine("            {");
        codeBuilder.AppendLine("                url += \"?\" + queryParams.ToString();");
        codeBuilder.AppendLine("            }");
    }

    private void GenerateSingleQueryParameter(StringBuilder codeBuilder, ParameterInfo param)
    {
        var queryAttr = param.Attributes.First(a => a.Name == GeneratorConstants.QueryAttribute);
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

    private void GenerateArrayQueryParameter(StringBuilder codeBuilder, ParameterInfo param)
    {
        var arrayQueryAttr = param.Attributes.First(a => a.Name == GeneratorConstants.ArrayQueryAttribute);
        var paramName = GetQueryParameterName(arrayQueryAttr, param.Name);
        var separator = GetArrayQuerySeparator(arrayQueryAttr);

        codeBuilder.AppendLine($"            if ({param.Name} != null && {param.Name}.Length > 0)");
        codeBuilder.AppendLine("            {");

        if (string.IsNullOrEmpty(separator))
        {
            // 使用重复键名格式：user_ids=id0&user_ids=id1&user_ids=id2
            codeBuilder.AppendLine($"                foreach (var item in {param.Name})");
            codeBuilder.AppendLine("                {");
            codeBuilder.AppendLine($"                    if (item != null)");
            codeBuilder.AppendLine("                    {");
            codeBuilder.AppendLine($"                        var encodedValue = HttpUtility.UrlEncode(item.ToString());");
            codeBuilder.AppendLine($"                        queryParams.Add(\"{paramName}\", encodedValue);");
            codeBuilder.AppendLine("                    }");
            codeBuilder.AppendLine("                }");
        }
        else
        {
            // 使用分隔符连接格式：user_ids=id0;id1;id2
            codeBuilder.AppendLine($"                var joinedValues = string.Join(\"{separator}\", {param.Name}.Where(item => item != null).Select(item => HttpUtility.UrlEncode(item.ToString())));");
            codeBuilder.AppendLine($"                queryParams.Add(\"{paramName}\", joinedValues);");
        }

        codeBuilder.AppendLine("            }");
    }

    private void GenerateSimpleQueryParameter(StringBuilder codeBuilder, ParameterInfo param, string paramName, string? formatString)
    {
        if (IsArrayType(param.Type))
        {
            // 处理数组类型：使用默认分号分隔符格式
            codeBuilder.AppendLine($"            if ({param.Name} != null && {param.Name}.Length > 0)");
            codeBuilder.AppendLine("            {");
            codeBuilder.AppendLine($"                var joinedValues = string.Join(\";\", {param.Name}.Where(item => item != null).Select(item => HttpUtility.UrlEncode(item.ToString())));");
            codeBuilder.AppendLine($"                queryParams.Add(\"{paramName}\", joinedValues);");
            codeBuilder.AppendLine("            }");
        }
        else if (IsStringType(param.Type))
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
            .Where(p => p.Attributes.Any(attr => attr.Name == GeneratorConstants.HeaderAttribute))
            .ToList();

        foreach (var param in headerParams)
        {
            var headerAttr = param.Attributes.First(a => a.Name == GeneratorConstants.HeaderAttribute);
            var headerName = headerAttr.Arguments.FirstOrDefault()?.ToString() ?? param.Name;

            codeBuilder.AppendLine($"            if (!string.IsNullOrEmpty({param.Name}))");
            codeBuilder.AppendLine($"                request.Headers.Add(\"{headerName}\", {param.Name});");
        }
    }

    private void GenerateBodyParameter(StringBuilder codeBuilder, MethodAnalysisResult methodInfo)
    {
        var bodyParam = methodInfo.Parameters
            .FirstOrDefault(p => p.Attributes.Any(attr => attr.Name == GeneratorConstants.BodyAttribute));

        if (bodyParam == null)
            return;

        var bodyAttr = bodyParam.Attributes.First(a => a.Name == GeneratorConstants.BodyAttribute);
        var useStringContent = GetUseStringContentFlag(bodyAttr);

        // 优先使用参数级别的ContentType，如果没有设置则使用接口级别的默认ContentType
        var contentType = GetBodyContentType(bodyAttr);

        // 检查参数是否明确指定了ContentType
        var hasExplicitContentType = bodyAttr.NamedArguments.ContainsKey("ContentType");

        if (!hasExplicitContentType)
        {
            // 参数没有指定ContentType，使用接口级别的默认值
            // StringContent 的第三个参数只需要媒体类型，不需要字符集信息
            codeBuilder.AppendLine($"            if ({bodyParam.Name} != null)");
            codeBuilder.AppendLine("            {");

            if (useStringContent)
            {
                codeBuilder.AppendLine($"                request.Content = new StringContent({bodyParam.Name}.ToString() ?? \"\", Encoding.UTF8, GetMediaType(_defaultContentType));");
            }
            else
            {
                codeBuilder.AppendLine($"                var jsonContent = JsonSerializer.Serialize({bodyParam.Name}, _jsonSerializerOptions);");
                codeBuilder.AppendLine($"                request.Content = new StringContent(jsonContent, Encoding.UTF8, GetMediaType(_defaultContentType));");
            }

            codeBuilder.AppendLine("            }");
        }
        else
        {
            // 参数指定了ContentType，直接使用指定的值
            var contentTypeLiteral = $"\"{contentType}\"";
            codeBuilder.AppendLine($"            if ({bodyParam.Name} != null)");
            codeBuilder.AppendLine("            {");

            if (useStringContent)
            {
                codeBuilder.AppendLine($"                request.Content = new StringContent({bodyParam.Name}.ToString() ?? \"\", Encoding.UTF8, {contentTypeLiteral});");
            }
            else
            {
                codeBuilder.AppendLine($"                var jsonContent = JsonSerializer.Serialize({bodyParam.Name}, _jsonSerializerOptions);");
                codeBuilder.AppendLine($"                request.Content = new StringContent(jsonContent, Encoding.UTF8, {contentTypeLiteral});");
            }

            codeBuilder.AppendLine("            }");
        }
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

        // 对于异步方法，使用内部返回类型；对于同步方法，使用完整返回类型
        var deserializeType = methodInfo.IsAsyncMethod ? methodInfo.AsyncInnerReturnType : methodInfo.ReturnType;
        codeBuilder.AppendLine($"                var result = await JsonSerializer.DeserializeAsync<{deserializeType}>(stream, _jsonSerializerOptions{cancellationTokenArg});");
        codeBuilder.AppendLine("                return result;");
    }

    private void GenerateExceptionHandling(StringBuilder codeBuilder, MethodAnalysisResult methodInfo, string interfaceName)
    {
        codeBuilder.AppendLine("                _logger.LogError(ex, \"HTTP请求异常: {{Url}}\", url);");
        codeBuilder.AppendLine($"                On{StringExtensions.ConvertFunctionName(interfaceName, "Api", "RequestError")}(ex, url);");
        codeBuilder.AppendLine($"                On{StringExtensions.ConvertFunctionName(methodInfo.MethodName, "Error")}(ex, url);");
        codeBuilder.AppendLine("                throw;");
    }


    private bool IsSimpleType(string typeName)
    {
        var simpleTypes = new[] { "string", "int", "long", "float", "double", "decimal", "bool",
                                  "DateTime", "System.DateTime", "Guid", "System.Guid",
                                  "string[]", "int[]", "long[]", "float[]", "double[]", "decimal[]",
                                  "DateTime[]", "System.DateTime[]", "Guid[]", "System.Guid[]",};
        return simpleTypes.Contains(typeName) || typeName.EndsWith("?", StringComparison.OrdinalIgnoreCase) && simpleTypes.Contains(typeName.TrimEnd('?'));
    }

    private bool IsStringType(string typeName)
    {
        return typeName.Equals("string", StringComparison.OrdinalIgnoreCase) ||
               typeName.Equals("string?", StringComparison.OrdinalIgnoreCase);
    }

    private bool IsArrayType(string typeName)
    {
        return typeName.EndsWith("[]", StringComparison.OrdinalIgnoreCase) || typeName.EndsWith("[]?", StringComparison.OrdinalIgnoreCase);
    }

    private string? GetFormatString(ParameterAttributeInfo attribute)
    {
        // 检查构造函数参数
        if (attribute.Arguments.Length > 1)
        {
            return attribute.Arguments[1] as string;
        }
        else if (attribute.Arguments.Length == 1 && GeneratorConstants.PathAttributes.Contains(attribute.Name))
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

    private string? GetArrayQuerySeparator(ParameterAttributeInfo attribute)
    {
        // 检查构造函数参数
        if (attribute.Arguments.Length > 1)
        {
            return attribute.Arguments[1] as string;
        }

        // 检查命名参数
        return attribute.NamedArguments.TryGetValue("Separator", out var separator)
            ? separator as string
            : null;
    }

    private string GetCancellationTokenParam(MethodAnalysisResult methodInfo)
    {
        var cancellationTokenParam = methodInfo.Parameters.FirstOrDefault(p => p.Type.Contains("CancellationToken"));
        return cancellationTokenParam != null ? $", {cancellationTokenParam.Name}" : "";
    }
}