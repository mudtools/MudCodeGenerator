// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.CodeGenerator 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis.Text;
using Mud.ServiceCodeGenerator.ApiWrap.Helpers;
using Mud.ServiceCodeGenerator.ApiWrap.Strategies;
using System.Collections.Immutable;
using System.Text;

namespace Mud.ServiceCodeGenerator.ApiWrap;

/// <summary>
/// HttpClient API 源生成器 - 重构版本
/// <para>基于 Roslyn 技术，自动为标记了 [HttpClientApi] 特性的接口生成 HttpClient 实现类。</para>
/// <para>支持 HTTP 方法：Get, Post, Put, Delete, Patch, Head, Options。</para>
/// </summary>
[Generator(LanguageNames.CSharp)]
public partial class HttpClientApiSourceGeneratorRefactored : WebApiSourceGenerator
{
    private readonly ParameterProcessorManager _parameterProcessorManager;
    private readonly MethodGenerationStrategyManager _methodStrategyManager;

    /// <summary>
    /// 初始化 HttpClient API 源生成器
    /// </summary>
    public HttpClientApiSourceGeneratorRefactored()
    {
        _parameterProcessorManager = new ParameterProcessorManager();
        _methodStrategyManager = new MethodGenerationStrategyManager();
    }

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

    /// <summary>
    /// 处理单个接口
    /// </summary>
    private void ProcessInterface(Compilation compilation, InterfaceDeclarationSyntax interfaceDecl, SourceProductionContext context)
    {
        HttpApiGenerationExceptionHandler.SafeExecute(() =>
        {
            var model = compilation.GetSemanticModel(interfaceDecl.SyntaxTree);
            if (model.GetDeclaredSymbol(interfaceDecl) is not INamedTypeSymbol interfaceSymbol)
                return false;

            // 验证接口配置
            if (!HttpApiConfigurationValidator.ValidateInterfaceConfiguration(interfaceSymbol, context))
                return false;

            if (!HasValidHttpMethods(interfaceSymbol))
            {
                HttpApiGenerationExceptionHandler.ReportWarning(context, interfaceDecl.Identifier.Text, "HTTPCLIENT002",
                    $"接口{interfaceDecl.Identifier.Text}不包含有效的HTTP方法特性，跳过生成");
                return false;
            }

            var sourceCode = GenerateImplementationClass(compilation, interfaceSymbol, interfaceDecl);
            var className = GetImplementationClassName(interfaceSymbol.Name);
            context.AddSource($"{className}.g.cs", SourceText.From(sourceCode, Encoding.UTF8));

            return true;
        }, interfaceDecl, context);
    }

    /// <summary>
    /// 生成实现类的完整代码
    /// </summary>
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

    /// <summary>
    /// 生成类结构
    /// </summary>
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

    /// <summary>
    /// 生成类的字段和构造函数
    /// </summary>
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

    /// <summary>
    /// 生成所有方法
    /// </summary>
    private void GenerateMethods(Compilation compilation, StringBuilder codeBuilder, INamedTypeSymbol interfaceSymbol, InterfaceDeclarationSyntax interfaceDecl)
    {
        GenerateClassPartialMethods(codeBuilder, interfaceSymbol);

        foreach (var methodSymbol in interfaceSymbol.GetMembers().OfType<IMethodSymbol>())
        {
            GenerateMethodImplementation(compilation, codeBuilder, methodSymbol, interfaceDecl);
        }
    }

    /// <summary>
    /// 生成方法实现
    /// </summary>
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

        // 使用重构后的代码生成方法
        GenerateMethodSignature(codeBuilder, methodInfo, methodSymbol);
        GenerateMethodBody(compilation, codeBuilder, methodInfo, hasTokenManager, hasAuthorizationHeader, hasAuthorizationQuery);
    }

    /// <summary>
    /// 生成方法签名
    /// </summary>
    private void GenerateMethodSignature(StringBuilder codeBuilder, MethodAnalysisResult methodInfo, IMethodSymbol methodSymbol)
    {
        codeBuilder.AppendLine();
        codeBuilder.AppendLine("        /// <summary>");
        codeBuilder.AppendLine($"        /// <inheritdoc cref=\"{methodInfo.InterfaceName}.{methodSymbol.Name}\"/>");
        codeBuilder.AppendLine("        /// </summary>");
        codeBuilder.AppendLine($"        {GeneratedCodeAttribute}");

        var asyncKeyword = methodInfo.IsAsyncMethod ? "async " : "";
        var parameters = string.Join(", ", methodSymbol.Parameters.Select(p => $"{p.Type} {p.Name}"));

        codeBuilder.AppendLine($"        public {asyncKeyword}{methodSymbol.ReturnType} {methodSymbol.Name}({parameters})");
        codeBuilder.AppendLine("        {");
    }

    /// <summary>
    /// 生成方法体
    /// </summary>
    private void GenerateMethodBody(Compilation compilation, StringBuilder codeBuilder, MethodAnalysisResult methodInfo, bool hasTokenManager, bool hasAuthorizationHeader, bool hasAuthorizationQuery)
    {
        // 如果需要Token管理器，获取access_token
        if (hasTokenManager && (hasAuthorizationHeader || hasAuthorizationQuery))
        {
            GenerateTokenAcquisition(codeBuilder, hasAuthorizationHeader, hasAuthorizationQuery, methodInfo);
        }

        GenerateRequestSetup(codeBuilder, methodInfo);
        GenerateParameterHandling(compilation, codeBuilder, methodInfo);

        // 添加Authorization header
        if (hasTokenManager && hasAuthorizationHeader)
        {
            GenerateAuthorizationHeader(codeBuilder, methodInfo);
        }

        GenerateRequestExecution(compilation, codeBuilder, methodInfo);

        codeBuilder.AppendLine("        }");
        codeBuilder.AppendLine();
    }

    /// <summary>
    /// 生成Token获取代码
    /// </summary>
    private static void GenerateTokenAcquisition(StringBuilder codeBuilder, bool hasAuthorizationHeader, bool hasAuthorizationQuery, MethodAnalysisResult methodInfo)
    {
        if (!hasAuthorizationHeader && !hasAuthorizationQuery) return;

        codeBuilder.AppendLine("            var access_token = await _tokenManager.GetTokenAsync();");
        codeBuilder.AppendLine("            if (string.IsNullOrEmpty(access_token))");
        codeBuilder.AppendLine("            {");
        codeBuilder.AppendLine("                throw new InvalidOperationException(\"无法获取访问令牌\");");
        codeBuilder.AppendLine("            }");
        codeBuilder.AppendLine();
    }

    /// <summary>
    /// 生成Authorization header
    /// </summary>
    private static void GenerateAuthorizationHeader(StringBuilder codeBuilder, MethodAnalysisResult methodInfo)
    {
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

    // 其余方法保持与原文件一致...
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
                // 这里需要实际的格式字符串处理逻辑
                interpolatedUrl = interpolatedUrl.Replace($"{{{param.Name}}}", $"{{{param.Name}}}");
            }
        }

        return $"            var url = $\"{interpolatedUrl}\";";
    }

    private void GenerateParameterHandling(Compilation compilation, StringBuilder codeBuilder, MethodAnalysisResult methodInfo)
    {
        var context = new ParameterGenerationContext
        {
            Compilation = compilation,
            IndentLevel = 3,
            HttpRequestVariable = "request",
            UrlVariable = "url",
            HasCancellationToken = methodInfo.Parameters.Any(p => p.Type.Contains("CancellationToken"))
        };

        var generatedCode = _parameterProcessorManager.ProcessParameters(methodInfo.Parameters, context);
        codeBuilder.Append(generatedCode);
    }

    private void GenerateRequestExecution(Compilation compilation, StringBuilder codeBuilder, MethodAnalysisResult methodInfo)
    {
        var (cancellationTokenArg, _) = GetCancellationTokenParams(methodInfo);
        var interfaceName = GetImplementationClassName(methodInfo.InterfaceName);

        codeBuilder.AppendLine("            try");
        codeBuilder.AppendLine("            {");
        GenerateRequestExecutionCore(compilation, codeBuilder, methodInfo, interfaceName, cancellationTokenArg);
        codeBuilder.AppendLine("            }");
        codeBuilder.AppendLine("            catch (System.Exception ex)");
        codeBuilder.AppendLine("            {");
        GenerateExceptionHandling(codeBuilder, methodInfo, interfaceName);
        codeBuilder.AppendLine("            }");
    }

    private void GenerateRequestExecutionCore(Compilation compilation, StringBuilder codeBuilder, MethodAnalysisResult methodInfo, string interfaceName, string cancellationTokenArg)
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

        GenerateResponseProcessing(compilation, codeBuilder, methodInfo, cancellationTokenArg);
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

    private void GenerateResponseProcessing(Compilation compilation, StringBuilder codeBuilder, MethodAnalysisResult methodInfo, string cancellationTokenArg)
    {
        var context = new MethodGenerationContext
        {
            Compilation = compilation,
            ClassName = GetImplementationClassName(methodInfo.InterfaceName),
            IndentLevel = 3,
            CancellationTokenParameter = GetCancellationTokenParameter(methodInfo),
            HasTokenManager = false,
            HasAuthorizationHeader = false,
            HasAuthorizationQuery = false
        };

        var generatedCode = _methodStrategyManager.GenerateResponseCode(methodInfo, context);
        codeBuilder.Append(generatedCode);
    }

    private void GenerateExceptionHandling(StringBuilder codeBuilder, MethodAnalysisResult methodInfo, string interfaceName)
    {
        codeBuilder.AppendLine("                _logger.LogError(ex, \"HTTP请求异常: {{Url}}\", url);");
        codeBuilder.AppendLine($"                On{StringExtensions.ConvertFunctionName(interfaceName, "Api", "RequestError")}(ex, url);");
        codeBuilder.AppendLine($"                On{StringExtensions.ConvertFunctionName(methodInfo.MethodName, "Error")}(ex, url);");
        codeBuilder.AppendLine("                throw;");
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

    private string GetCancellationTokenParameter(MethodAnalysisResult methodInfo)
    {
        var cancellationTokenParam = methodInfo.Parameters.FirstOrDefault(p => p.Type.Contains("CancellationToken"));
        return cancellationTokenParam?.Name ?? string.Empty;
    }
}