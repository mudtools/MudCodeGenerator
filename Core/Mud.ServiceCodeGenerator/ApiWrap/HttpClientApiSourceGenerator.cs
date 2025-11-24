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
        GenerateClassFieldsAndConstructor(codeBuilder, className, interfaceSymbol, compilation);
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
    }


    private void GenerateClassFieldsAndConstructor(StringBuilder codeBuilder, string className, INamedTypeSymbol interfaceSymbol, Compilation compilation)
    {
        codeBuilder.AppendLine("        private readonly HttpClient _httpClient;");
        codeBuilder.AppendLine($"        private readonly ILogger<{className}> _logger;");
        codeBuilder.AppendLine("        private readonly JsonSerializerOptions _jsonSerializerOptions;");

        // 从HttpClientApi特性获取配置
        var httpClientApiAttribute = GetHttpClientApiAttribute(interfaceSymbol);
        var defaultContentType = GetHttpClientApiContentTypeFromAttribute(httpClientApiAttribute);
        var timeout = GetHttpClientApiTimeoutFromAttribute(httpClientApiAttribute);
        var tokenManage = GetTokenManageFromAttribute(httpClientApiAttribute);

        // 检查是否需要Token管理器
        var hasTokenManager = !string.IsNullOrEmpty(tokenManage);
        var tokenManagerType = hasTokenManager ? GetTokenManagerType(compilation, tokenManage!) : null;

        if (hasTokenManager)
        {
            codeBuilder.AppendLine($"        private readonly {tokenManagerType} _tokenManager;");
        }

        codeBuilder.AppendLine($"        private readonly string _defaultContentType = \"{defaultContentType}\";");
        codeBuilder.AppendLine();
        codeBuilder.AppendLine("        /// <summary>");
        codeBuilder.AppendLine($"        /// 构建 <see cref = \"{className}\"/> 类的实例。");
        codeBuilder.AppendLine("        /// </summary>");
        codeBuilder.AppendLine("        /// <param name=\"httpClient\">HttpClient实例</param>");
        codeBuilder.AppendLine("        /// <param name=\"logger\">日志记录器</param>");
        codeBuilder.AppendLine("        /// <param name=\"option\">Json序列化参数</param>");

        if (hasTokenManager)
        {
            codeBuilder.AppendLine($"        /// <param name=\"tokenManager\">Token管理器</param>");
            codeBuilder.AppendLine($"        public {className}(HttpClient httpClient, ILogger<{className}> logger, IOptions<JsonSerializerOptions> option, {tokenManagerType} tokenManager)");
        }
        else
        {
            codeBuilder.AppendLine($"        public {className}(HttpClient httpClient, ILogger<{className}> logger, IOptions<JsonSerializerOptions> option)");
        }

        codeBuilder.AppendLine("        {");
        codeBuilder.AppendLine("            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));");
        codeBuilder.AppendLine("            _logger = logger ?? throw new ArgumentNullException(nameof(logger));");
        codeBuilder.AppendLine("            _jsonSerializerOptions = option.Value;");
        if (hasTokenManager)
        {
            codeBuilder.AppendLine("            _tokenManager = tokenManager ?? throw new ArgumentNullException(nameof(tokenManager));");
        }
        codeBuilder.AppendLine();
        codeBuilder.AppendLine($"            // 配置HttpClient超时时间");
        codeBuilder.AppendLine($"            _httpClient.Timeout = TimeSpan.FromSeconds({timeout});");
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

        // 检查是否忽略生成实现
        if (methodInfo.IgnoreImplement) return;

        // 获取接口符号以检查Token管理
        var model = compilation.GetSemanticModel(interfaceDecl.SyntaxTree);
        var interfaceSymbol = model.GetDeclaredSymbol(interfaceDecl) as INamedTypeSymbol;
        var httpClientApiAttribute = GetHttpClientApiAttribute(interfaceSymbol);
        var tokenManage = GetTokenManageFromAttribute(httpClientApiAttribute);
        var hasTokenManager = !string.IsNullOrEmpty(tokenManage);
        var tokenManagerType = hasTokenManager ? GetTokenManagerType(compilation, tokenManage!) : null;
        var hasAuthorizationHeader = HasInterfaceAttribute(interfaceSymbol!, "Header", "Authorization");
        var hasAuthorizationQuery = HasInterfaceAttribute(interfaceSymbol!, "Query", "Authorization");

        codeBuilder.AppendLine();
        codeBuilder.AppendLine($"        /// <summary>");
        codeBuilder.AppendLine($"        /// <inheritdoc cref=\"{methodInfo.InterfaceName}.{methodSymbol.Name} \"/>");
        codeBuilder.AppendLine($"        /// </summary>");
        codeBuilder.AppendLine($"        {GeneratedCodeAttribute}");
        // 根据方法返回类型决定是否添加 async 关键字
        var asyncKeyword = methodInfo.IsAsyncMethod ? "async " : "";
        codeBuilder.AppendLine($"        public {asyncKeyword}{methodSymbol.ReturnType} {methodSymbol.Name}({GetParameterList(methodSymbol)})");
        codeBuilder.AppendLine("        {");

        // 如果需要Token管理器，获取access_token
        if (hasTokenManager && (hasAuthorizationHeader || hasAuthorizationQuery))
        {
            codeBuilder.AppendLine($"            var access_token = await _tokenManager.GetTokenAsync();");
            codeBuilder.AppendLine($"            if (string.IsNullOrEmpty(access_token))");
            codeBuilder.AppendLine($"            {{");
            codeBuilder.AppendLine($"                throw new InvalidOperationException(\"无法获取访问令牌\");");
            codeBuilder.AppendLine($"            }}");
            codeBuilder.AppendLine();
        }

        GenerateRequestSetup(codeBuilder, methodInfo);
        GenerateParameterHandling(codeBuilder, methodInfo);

        // 添加Authorization header
        if (hasTokenManager && hasAuthorizationHeader)
        {
            // 从接口特性中获取实际的header名称
            var headerName = "Authorization";
            if (methodInfo.InterfaceAttributes?.Any() == true)
            {
                var headerAttr = methodInfo.InterfaceAttributes.FirstOrDefault(attr => attr.StartsWith("Header:"));
                if (!string.IsNullOrEmpty(headerAttr))
                {
                    headerName = headerAttr.Substring(7); // 去掉"Header:"前缀
                }
            }
            codeBuilder.AppendLine($"            // 添加Authorization header as {headerName}");
            codeBuilder.AppendLine($"            request.Headers.Add(\"{headerName}\", access_token);");
        }

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

        // 检查接口是否有[Query("Authorization")]特性（支持AliasAs）
        var hasAuthorizationQuery = methodInfo.InterfaceAttributes?.Any(attr => attr.StartsWith("Query:")) == true;

        if (!queryParams.Any() && !arrayQueryParams.Any() && !hasAuthorizationQuery)
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

        // 添加Authorization query参数
        if (hasAuthorizationQuery)
        {
            // 从接口特性中获取实际的query参数名称
            var queryName = "Authorization";
            if (methodInfo.InterfaceAttributes?.Any() == true)
            {
                var queryAttr = methodInfo.InterfaceAttributes.FirstOrDefault(attr => attr.StartsWith("Query:"));
                if (!string.IsNullOrEmpty(queryAttr))
                {
                    queryName = queryAttr.Substring(6); // 去掉"Query:"前缀
                }
            }
            codeBuilder.AppendLine($"            // 添加Authorization query参数 as {queryName}");
            codeBuilder.AppendLine($"            queryParams.Add(\"{queryName}\", access_token);");
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
        var contentType = GetBodyContentType(bodyAttr);

        // 检查参数是否明确指定了ContentType
        var hasExplicitContentType = bodyAttr.NamedArguments.ContainsKey("ContentType");
        var contentTypeExpression = hasExplicitContentType ? $"\"{contentType}\"" : "GetMediaType(_defaultContentType)";

        codeBuilder.AppendLine($"            if ({bodyParam.Name} != null)");
        codeBuilder.AppendLine("            {");

        if (useStringContent)
        {
            codeBuilder.AppendLine($"                request.Content = new StringContent({bodyParam.Name}.ToString() ?? \"\", Encoding.UTF8, {contentTypeExpression});");
        }
        else
        {
            codeBuilder.AppendLine($"                var jsonContent = JsonSerializer.Serialize({bodyParam.Name}, _jsonSerializerOptions);");
            codeBuilder.AppendLine($"                request.Content = new StringContent(jsonContent, Encoding.UTF8, {contentTypeExpression});");
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
        var (cancellationTokenArg, _) = GetCancellationTokenParams(methodInfo);
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
        var (_, cancellationTokenArgForRead) = GetCancellationTokenParams(methodInfo);
        codeBuilder.AppendLine($"                    On{StringExtensions.ConvertFunctionName(interfaceName, "Api", "RequestFail")}(response, url);");
        codeBuilder.AppendLine($"                    On{StringExtensions.ConvertFunctionName(methodInfo.MethodName, "Fail")}(response, url);");
        codeBuilder.AppendLine($"                    var errorContent = await response.Content.ReadAsStringAsync({cancellationTokenArgForRead});");
        codeBuilder.AppendLine("                    _logger.LogError(\"HTTP请求失败: {StatusCode}, 响应: {Response}\", (int)response.StatusCode, errorContent);");
        codeBuilder.AppendLine("                    throw new HttpRequestException($\"HTTP请求失败: {(int)response.StatusCode}\");");
    }

    private void GenerateResponseProcessing(StringBuilder codeBuilder, MethodAnalysisResult methodInfo, string cancellationTokenArg)
    {
        // 检查是否有 FilePath 参数，直接保存到文件
        var filePathParam = methodInfo.Parameters.FirstOrDefault(p => p.Attributes.Any(attr => attr.Name == GeneratorConstants.FilePathAttribute));
        var hasFilePathParam = filePathParam != null;

        // 检查是否为文件下载场景：异步方法且内部返回类型为 byte[]
        var isFileDownload = methodInfo.IsAsyncMethod &&
                             methodInfo.AsyncInnerReturnType.Equals("byte[]", StringComparison.OrdinalIgnoreCase);
        var (cancellationTokenArgForCopy, cancellationTokenArgForRead) = GetCancellationTokenParams(methodInfo);
        if (hasFilePathParam)
        {

            // FilePath 参数场景：直接保存到指定路径
            codeBuilder.AppendLine($"                using (var stream = await response.Content.ReadAsStreamAsync({cancellationTokenArgForRead}))");
            codeBuilder.AppendLine($"                using (var fileStream = File.Create({filePathParam.Name}))");
            codeBuilder.AppendLine("                {");

            // 从 FilePathAttribute 中读取 BufferSize 参数
            var filePathAttr = filePathParam.Attributes.First(a => a.Name == GeneratorConstants.FilePathAttribute);
            var bufferSize = GetBufferSizeFromAttribute(filePathAttr);

            codeBuilder.AppendLine($"                    await stream.CopyToAsync(fileStream, {bufferSize}{cancellationTokenArgForCopy});");
            codeBuilder.AppendLine("                }");

            // 对于有 FilePath 参数的方法，不返回任何值（void 或 Task）
            if (!methodInfo.IsAsyncMethod || (methodInfo.IsAsyncMethod && methodInfo.AsyncInnerReturnType.Equals("void", StringComparison.OrdinalIgnoreCase)))
            {
                codeBuilder.AppendLine("                return;");
            }
            else if (methodInfo.IsAsyncMethod && !string.IsNullOrEmpty(methodInfo.AsyncInnerReturnType) && !methodInfo.AsyncInnerReturnType.Equals("void", StringComparison.OrdinalIgnoreCase))
            {
                // 如果是异步方法且有非void返回类型，返回默认值
                codeBuilder.AppendLine("                return default;");
            }
            // 对于 Task 类型的异步方法，不需要 return 语句
        }
        else if (isFileDownload)
        {
            // 文件下载场景：直接读取为字节数组
            codeBuilder.AppendLine($"                byte[] fileBytes = await response.Content.ReadAsByteArrayAsync({cancellationTokenArgForRead});");
            codeBuilder.AppendLine("                return fileBytes;");
        }
        else
        {
            // 常规 JSON 反序列化场景
            codeBuilder.AppendLine($"                using var stream = await response.Content.ReadAsStreamAsync({cancellationTokenArgForRead});");
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

    private (string withComma, string withoutComma) GetCancellationTokenParams(MethodAnalysisResult methodInfo)
    {
        var cancellationTokenParam = methodInfo.Parameters.FirstOrDefault(p => p.Type.Contains("CancellationToken"));
        var paramValue = cancellationTokenParam?.Name;

        return (
            withComma: paramValue != null ? $", {paramValue}" : "",
            withoutComma: paramValue ?? ""
        );
    }

    /// <summary>
    /// 从 FilePathAttribute 中获取 BufferSize 参数
    /// </summary>
    /// <param name="filePathAttr">FilePath特性</param>
    /// <returns>缓冲区大小</returns>
    private int GetBufferSizeFromAttribute(ParameterAttributeInfo filePathAttr)
    {
        // 首先检查命名参数
        if (filePathAttr.NamedArguments.TryGetValue("BufferSize", out var bufferSizeValue))
        {
            if (int.TryParse(bufferSizeValue?.ToString(), out var bufferSize))
            {
                return bufferSize;
            }
        }

        // 然后检查构造函数参数
        if (filePathAttr.Arguments.Length > 0)
        {
            var firstArg = filePathAttr.Arguments[0];
            if (int.TryParse(firstArg?.ToString(), out var bufferSize))
            {
                return bufferSize;
            }
        }

        // 如果都没有设置，使用默认值 81920 (80KB)
        return 81920;
    }
}