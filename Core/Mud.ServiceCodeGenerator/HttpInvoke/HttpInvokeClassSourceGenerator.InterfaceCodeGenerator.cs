// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.CodeGenerator 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis.Text;
using Mud.CodeGenerator.Helper;
using System.Text;

namespace Mud.ServiceCodeGenerator.HttpInvoke;

internal class InterfaceCodeGenerator
{
    private string httpClientOptionsName = "HttpClientOptions";
    private Compilation compilation;
    private InterfaceDeclarationSyntax interfaceDecl;
    private SourceProductionContext context;
    private HttpInvokeClassSourceGenerator httpInvokeClassSourceGenerator;

    private AttributeData? httpClientApiAttribute = null;
    private bool isAbstract = false;
    private string? inheritedFrom = null;
    private string? tokenManage = null;
    private INamedTypeSymbol interfaceSymbol;
    private StringBuilder codeBuilder;

    public InterfaceCodeGenerator(
        Compilation compilation,
        InterfaceDeclarationSyntax interfaceDecl,
        SourceProductionContext context,
        HttpInvokeClassSourceGenerator httpInvokeClassSourceGenerator,
        string optionsName)
    {
        this.compilation = compilation;
        this.interfaceDecl = interfaceDecl;
        this.context = context;
        this.httpInvokeClassSourceGenerator = httpInvokeClassSourceGenerator;
        this.httpClientOptionsName = optionsName;
        this.codeBuilder = new StringBuilder();
    }

    public void Generator()
    {
        var model = compilation.GetSemanticModel(interfaceDecl.SyntaxTree);
        if (model.GetDeclaredSymbol(interfaceDecl) is not INamedTypeSymbol interfaceSymbolObj)
            return;
        interfaceSymbol = interfaceSymbolObj;
        // 获取HttpClientApi特性中的属性值
        httpClientApiAttribute = AttributeDataHelper.GetAttributeDataFromSymbol(interfaceSymbol, HttpClientGeneratorConstants.HttpClientApiAttributeNames);
        isAbstract = AttributeDataHelper.GetBoolValueFromAttribute(httpClientApiAttribute, HttpClientGeneratorConstants.IsAbstractProperty);
        inheritedFrom = AttributeDataHelper.GetStringValueFromAttribute(httpClientApiAttribute, HttpClientGeneratorConstants.InheritedFromProperty);
        tokenManage = AttributeDataHelper.GetStringValueFromAttribute(httpClientApiAttribute, HttpClientGeneratorConstants.TokenManageProperty);

        GenerateImplementationClass(interfaceDecl);
        var className = InterfaceHelper.GetImplementationClassName(interfaceSymbol.Name);
        context.AddSource($"{className}.g.cs", SourceText.From(codeBuilder.ToString(), Encoding.UTF8));

    }

    private void GenerateImplementationClass(InterfaceDeclarationSyntax interfaceDecl)
    {
        var className = InterfaceHelper.GetImplementationClassName(interfaceSymbol.Name);
        var namespaceName = SyntaxHelper.GetNamespaceName(interfaceDecl);

        GenerateClassStructure(className, namespaceName);
        GenerateClassFieldsAndConstructor(className, compilation);
        GenerateMethods(interfaceDecl);

        codeBuilder.AppendLine("    }");
        codeBuilder.AppendLine("}");
    }


    private void GenerateClassStructure(string className, string namespaceName)
    {
        // 构建类声明
        var classKeyword = isAbstract ? "abstract class" : "class";
        var inheritanceList = new List<string> { interfaceSymbol.Name };

        if (!string.IsNullOrEmpty(inheritedFrom))
        {
            inheritanceList.Insert(0, inheritedFrom);
        }

        httpInvokeClassSourceGenerator.GenerateFileHeader(codeBuilder);

        codeBuilder.AppendLine();
        codeBuilder.AppendLine($"namespace {namespaceName}");
        codeBuilder.AppendLine("{");
        codeBuilder.AppendLine($"    /// <summary>");
        codeBuilder.AppendLine($"    /// <inheritdoc cref=\"{interfaceSymbol.Name}\"/>");
        codeBuilder.AppendLine($"    /// </summary>");
        codeBuilder.AppendLine($"    {GeneratedCodeConsts.CompilerGeneratedAttribute}");
        codeBuilder.AppendLine($"    {GeneratedCodeConsts.GeneratedCodeAttribute}");
        codeBuilder.AppendLine($"    internal partial {classKeyword} {className} : {string.Join(", ", inheritanceList)}");
        codeBuilder.AppendLine("    {");
    }

    /// <summary>
    /// 从特性获取内容类型（专用方法，重载基础方法）
    /// </summary>
    /// <param name="httpClientApiAttribute">HttpClientApi特性</param>
    /// <returns>内容类型</returns>
    private string GetHttpClientApiContentTypeFromAttribute(AttributeData? attribute)
    {
        if (attribute == null)
            return "application/json";
        var contentTypeArg = attribute.NamedArguments.FirstOrDefault(a => a.Key == "ContentType");
        var contentType = contentTypeArg.Value.Value?.ToString();
        return string.IsNullOrEmpty(contentType) ? "application/json" : contentType;
    }

    private void GenerateClassFieldsAndConstructor(string className, Compilation compilation)
    {
        // 从HttpClientApi特性获取配置     
        var defaultContentType = GetHttpClientApiContentTypeFromAttribute(httpClientApiAttribute);
        var timeoutFromAttribute = AttributeDataHelper.GetIntValueFromAttribute(httpClientApiAttribute, HttpClientGeneratorConstants.TimeoutProperty, 100);
        var baseAddressFromAttribute = AttributeDataHelper.GetStringValueFromAttributeConstructor(httpClientApiAttribute, HttpClientGeneratorConstants.BaseAddressProperty);

        // 检查是否需要Token管理器
        var hasTokenManager = !string.IsNullOrEmpty(tokenManage);
        var tokenManagerType = hasTokenManager ? httpInvokeClassSourceGenerator.GetTokenManagerType(compilation, tokenManage!) : null;

        // 根据IsAbstract决定Logger类型
        var loggerType = isAbstract ? "ILogger" : $"ILogger<{className}>";

        codeBuilder.AppendLine("        private readonly HttpClient _httpClient;");
        codeBuilder.AppendLine($"        private readonly {loggerType} _logger;");
        codeBuilder.AppendLine("        private readonly JsonSerializerOptions _jsonSerializerOptions;");
        codeBuilder.AppendLine($"        private readonly {httpClientOptionsName} {PrivateFieldNamingHelper.GeneratePrivateFieldName(httpClientOptionsName)};");

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
        codeBuilder.AppendLine($"        /// <param name=\"{PrivateFieldNamingHelper.GeneratePrivateFieldName(httpClientOptionsName, FieldNamingStyle.PureCamel)}\">飞书配置选项</param>");

        // 构建构造函数参数列表
        var constructorLoggerType = isAbstract ? "ILogger" : $"ILogger<{className}>";

        if (hasTokenManager)
        {
            codeBuilder.AppendLine($"        /// <param name=\"tokenManager\">Token管理器</param>");
            codeBuilder.Append($"        public {className}(HttpClient httpClient, {constructorLoggerType} logger, IOptions<JsonSerializerOptions> option, IOptions<{httpClientOptionsName}> {PrivateFieldNamingHelper.GeneratePrivateFieldName(httpClientOptionsName, FieldNamingStyle.PureCamel)}, {tokenManagerType} tokenManager)");
        }
        else
        {
            codeBuilder.Append($"        public {className}(HttpClient httpClient, {constructorLoggerType} logger, IOptions<JsonSerializerOptions> option, IOptions<{httpClientOptionsName}> {PrivateFieldNamingHelper.GeneratePrivateFieldName(httpClientOptionsName, FieldNamingStyle.PureCamel)})");
        }

        if (!string.IsNullOrEmpty(inheritedFrom))
            codeBuilder.AppendLine($" : base(httpClient, logger, option, {PrivateFieldNamingHelper.GeneratePrivateFieldName(httpClientOptionsName, FieldNamingStyle.PureCamel)}{(hasTokenManager ? ", tokenManager" : "")})");
        else
            codeBuilder.AppendLine();
        codeBuilder.AppendLine("        {");
        codeBuilder.AppendLine("            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));");
        codeBuilder.AppendLine("            _logger = logger ?? throw new ArgumentNullException(nameof(logger));");
        codeBuilder.AppendLine("            _jsonSerializerOptions = option.Value;");
        codeBuilder.AppendLine($"            {PrivateFieldNamingHelper.GeneratePrivateFieldName(httpClientOptionsName)} = {PrivateFieldNamingHelper.GeneratePrivateFieldName(httpClientOptionsName, FieldNamingStyle.PureCamel)}?.Value ?? throw new ArgumentNullException(nameof({PrivateFieldNamingHelper.GeneratePrivateFieldName(httpClientOptionsName, FieldNamingStyle.PureCamel)}));");
        if (hasTokenManager)
        {
            codeBuilder.AppendLine("            _tokenManager = tokenManager ?? throw new ArgumentNullException(nameof(tokenManager));");
        }

        codeBuilder.AppendLine();

        // 根据IsAbstract和InheritedFrom决定是否设置BaseAddress和Timeout
        if (!isAbstract)
        {
            // 设置 BaseAddress
            codeBuilder.AppendLine("            // 设置 HttpClient BaseAddress（用于相对路径请求）");
            codeBuilder.AppendLine("            var finalBaseAddress = GetFinalBaseAddress();");
            codeBuilder.AppendLine("            if (!string.IsNullOrEmpty(finalBaseAddress))");
            codeBuilder.AppendLine("            {");
            codeBuilder.AppendLine("                _httpClient.BaseAddress = new Uri(finalBaseAddress);");
            codeBuilder.AppendLine("            }");
            codeBuilder.AppendLine();

            // 设置超时时间
            codeBuilder.AppendLine($"            // 配置HttpClient超时时间");
            codeBuilder.AppendLine($"            var finalTimeout = GetFinalTimeout();");
            codeBuilder.AppendLine($"            _httpClient.Timeout = TimeSpan.FromSeconds(finalTimeout);");
        }
        codeBuilder.AppendLine("        }");
        codeBuilder.AppendLine();

        // 根据IsAbstract和InheritedFrom决定是否生成辅助方法
        if (!isAbstract)
        {
            // 只为非抽象类和非继承类生成辅助方法
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
            codeBuilder.AppendLine("        /// <summary>");
            codeBuilder.AppendLine($"        /// 获取最终的超时时间，优先使用 HttpClientApi 特性中的设置，否则使用 {httpClientOptionsName}.TimeOut");
            codeBuilder.AppendLine("        /// </summary>");
            codeBuilder.AppendLine("        /// <returns>超时秒数</returns>");
            codeBuilder.AppendLine($"        private int GetFinalTimeout()");
            codeBuilder.AppendLine("        {");
            codeBuilder.AppendLine($"            // 优先使用 HttpClientApi 特性中的超时设置");
            codeBuilder.AppendLine($"            var attributeTimeout = {timeoutFromAttribute};");
            codeBuilder.AppendLine($"            if (attributeTimeout > 0)");
            codeBuilder.AppendLine($"                return attributeTimeout;");
            codeBuilder.AppendLine();
            codeBuilder.AppendLine($"            // 尝试使用 {httpClientOptionsName}.TimeOut");
            codeBuilder.AppendLine($"            var optionsTimeout = {PrivateFieldNamingHelper.GeneratePrivateFieldName(httpClientOptionsName)}.TimeOut;");
            codeBuilder.AppendLine($"            return !string.IsNullOrEmpty(optionsTimeout) && int.TryParse(optionsTimeout, out var parsedTimeout)");
            codeBuilder.AppendLine($"                ? parsedTimeout");
            codeBuilder.AppendLine($"                : 60; // 默认60秒超时");
            codeBuilder.AppendLine("        }");
            codeBuilder.AppendLine();
            codeBuilder.AppendLine("        /// <summary>");
            codeBuilder.AppendLine($"        /// 获取最终的 BaseAddress，优先使用 HttpClientApi 特性中的设置，否则使用 {httpClientOptionsName}.BaseUrl");
            codeBuilder.AppendLine("        /// </summary>");
            codeBuilder.AppendLine("        /// <returns>BaseAddress</returns>");
            codeBuilder.AppendLine($"        private string? GetFinalBaseAddress()");
            codeBuilder.AppendLine("        {");
            codeBuilder.AppendLine($"            // 优先使用 HttpClientApi 特性中的 BaseAddress");
            codeBuilder.AppendLine($"            var attributeAddress = \"{baseAddressFromAttribute}\";");
            codeBuilder.AppendLine($"            return !string.IsNullOrEmpty(attributeAddress)");
            codeBuilder.AppendLine($"                ? attributeAddress");
            codeBuilder.AppendLine($"                : {PrivateFieldNamingHelper.GeneratePrivateFieldName(httpClientOptionsName)}.BaseUrl;");
            codeBuilder.AppendLine("        }");
            codeBuilder.AppendLine();
        }
    }

    private void GenerateMethods(InterfaceDeclarationSyntax interfaceDecl)
    {
        GenerateClassPartialMethods(interfaceSymbol);

        // 根据IsAbstract和InheritedFrom决定是否包含父接口方法
        var includeParentInterfaces = GetIncludeParentInterfaces(interfaceSymbol);

        IEnumerable<IMethodSymbol> methodsToGenerate = InterfaceHelper.GetAllInterfaceMethods(interfaceSymbol, includeParentInterfaces);

        foreach (var methodSymbol in methodsToGenerate)
        {
            GenerateMethodImplementation(compilation, methodSymbol, interfaceDecl);
        }
    }

    private bool GetIncludeParentInterfaces(INamedTypeSymbol interfaceSymbol)
    {
        return !(isAbstract || !string.IsNullOrEmpty(inheritedFrom));
    }


    /// <summary>
    /// <inheritdoc cref=""/>
    /// </summary>
    /// <param name="compilation"></param>
    /// <param name="codeBuilder"></param>
    /// <param name="methodSymbol"></param>
    /// <param name="interfaceDecl"></param>
    private void GenerateMethodImplementation(Compilation compilation, IMethodSymbol methodSymbol, InterfaceDeclarationSyntax interfaceDecl)
    {
        var methodInfo = httpInvokeClassSourceGenerator.AnalyzeMethod(compilation, methodSymbol, interfaceDecl);
        if (!methodInfo.IsValid) return;

        // 检查是否忽略生成实现
        if (methodInfo.IgnoreImplement) return;

        // 获取接口符号以检查Token管理
        var model = compilation.GetSemanticModel(interfaceDecl.SyntaxTree);

        var hasTokenManager = !string.IsNullOrEmpty(tokenManage);
        var tokenManagerType = hasTokenManager ? httpInvokeClassSourceGenerator.GetTokenManagerType(compilation, tokenManage!) : null;
        var hasAuthorizationHeader = InterfaceHelper.HasInterfaceAttribute(interfaceSymbol!, "Header", "Authorization");
        var hasAuthorizationQuery = InterfaceHelper.HasInterfaceAttribute(interfaceSymbol!, "Query", "Authorization");

        codeBuilder.AppendLine();
        codeBuilder.AppendLine($"        /// <summary>");
        codeBuilder.AppendLine($"        /// <inheritdoc cref=\"{methodInfo.MethodOwnerInterfaceName}.{methodSymbol.Name} \"/>");
        codeBuilder.AppendLine($"        /// </summary>");
        codeBuilder.AppendLine($"        {GeneratedCodeConsts.GeneratedCodeAttribute}");
        // 根据方法返回类型决定是否添加 async 关键字
        var asyncKeyword = methodInfo.IsAsyncMethod ? "async " : "";
        codeBuilder.AppendLine($"        public {asyncKeyword}{methodSymbol.ReturnType} {methodSymbol.Name}({InterfaceHelper.GetParameterList(methodSymbol)})");
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

        GenerateRequestSetup(methodInfo);
        GenerateParameterHandling(methodInfo);

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

        // 添加接口上定义的所有Header特性（只添加有固定值的Header）
        if (methodInfo.InterfaceHeaderAttributes?.Any() == true)
        {
            var fixedValueHeaders = methodInfo.InterfaceHeaderAttributes
                .Where(h => !string.IsNullOrEmpty(h.Name) && h.Value != null)
                .ToList();

            if (fixedValueHeaders.Any())
            {
                codeBuilder.AppendLine($"            // 添加接口定义的Header特性");
                foreach (var interfaceHeader in fixedValueHeaders)
                {
                    var headerValue = interfaceHeader.Value?.ToString() ?? "null";
                    codeBuilder.AppendLine($"            request.Headers.Add(\"{interfaceHeader.Name}\", \"{headerValue}\");");
                }
            }
        }

        GenerateRequestExecution(methodInfo);

        codeBuilder.AppendLine("        }");
        codeBuilder.AppendLine();
    }

    private void GenerateClassPartialMethods(INamedTypeSymbol interfaceSymbol)
    {
        var includeParentInterfaces = GetIncludeParentInterfaces(interfaceSymbol);
        IEnumerable<IMethodSymbol> methodsToProcess = InterfaceHelper.GetAllInterfaceMethods(interfaceSymbol, includeParentInterfaces);

        var processedMethods = new HashSet<string>();

        foreach (var methodSymbol in methodsToProcess)
        {
            if (!processedMethods.Add(methodSymbol.Name))
                continue;

            GenerateMethodPartialMethods(methodSymbol.Name);
        }

        var interfaceName = InterfaceHelper.GetImplementationClassName(interfaceSymbol.Name);
        if (processedMethods.Add(interfaceName))
        {
            GenerateInterfacePartialMethods(interfaceName);
        }
    }

    private void GenerateMethodPartialMethods(string methodName)
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
            codeBuilder.AppendLine($"        {GeneratedCodeConsts.GeneratedCodeAttribute}");
            codeBuilder.AppendLine($"        partial void On{StringExtensions.ConvertFunctionName(methodName, eventType)}({parameter}, string url);");
        }
    }

    private void GenerateInterfacePartialMethods(string interfaceName)
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
            codeBuilder.AppendLine($"        {GeneratedCodeConsts.GeneratedCodeAttribute}");
            codeBuilder.AppendLine($"        partial void On{StringExtensions.ConvertFunctionName(interfaceName, "Api", eventType)}({parameter}, string url);");
        }
    }


    private void GenerateRequestSetup(MethodAnalysisResult methodInfo)
    {
        var urlCode = BuildUrlString(methodInfo);
        codeBuilder.AppendLine(urlCode);

        // 检查是否需要 BaseAddress（仅当 URL 为相对路径时）
        var isAbsoluteUrl = methodInfo.UrlTemplate.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                           methodInfo.UrlTemplate.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

        if (!isAbsoluteUrl)
        {
            codeBuilder.AppendLine("            // 检查 BaseAddress 是否已设置（相对路径 URL 需要 BaseAddress）");
            codeBuilder.AppendLine("            if (_httpClient.BaseAddress == null)");
            codeBuilder.AppendLine("            {");
            codeBuilder.AppendLine($"                throw new InvalidOperationException(\"BaseAddress 配置缺失，相对路径 URL 需要在 HttpClientApi 特性或 {httpClientOptionsName}.BaseUrl 中设置有效的基地址\");");
            codeBuilder.AppendLine("            }");
        }

        codeBuilder.AppendLine($"            if ({PrivateFieldNamingHelper.GeneratePrivateFieldName(httpClientOptionsName)}.EnableLogging)");
        codeBuilder.AppendLine($"            {{");
        codeBuilder.AppendLine($"                _logger.LogDebug(\"开始HTTP {methodInfo.HttpMethod}请求: {{Url}}\", url);");
        codeBuilder.AppendLine($"            }}");
        codeBuilder.AppendLine($"            using var request = new HttpRequestMessage(HttpMethod.{methodInfo.HttpMethod}, url);");
        //codeBuilder.AppendLine($"            request.Headers.Add(\"Content-Type\", _defaultContentType);");
    }

    private string BuildUrlString(MethodAnalysisResult methodInfo)
    {
        var pathParams = methodInfo.Parameters
            .Where(p => p.Attributes.Any(attr => HttpClientGeneratorConstants.PathAttributes.Contains(attr.Name)))
            .ToList();

        var urlTemplate = methodInfo.UrlTemplate;

        // 检查是否为绝对 URL
        var isAbsoluteUrl = urlTemplate.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                           urlTemplate.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

        string urlCode;

        if (!pathParams.Any())
        {
            if (isAbsoluteUrl)
            {
                // 绝对 URL 直接使用
                urlCode = $"            var url = \"{urlTemplate}\";";
            }
            else
            {
                // 相对 URL 需要与 BaseAddress 组合
                urlCode = $"            var url = \"{urlTemplate}\";";
            }
        }
        else
        {
            var interpolatedUrl = urlTemplate;
            foreach (var param in pathParams)
            {
                if (urlTemplate.Contains($"{{{param.Name}}}"))
                {
                    var formatString = GetFormatString(param.Attributes.First(a => HttpClientGeneratorConstants.PathAttributes.Contains(a.Name)));
                    interpolatedUrl = FormatUrlParameter(interpolatedUrl, param.Name, formatString);
                }
            }

            if (isAbsoluteUrl)
            {
                // 绝对 URL 直接使用插值结果
                urlCode = $"            var url = $\"{interpolatedUrl}\";";
            }
            else
            {
                // 相对 URL 需要与 BaseAddress 组合
                urlCode = $"            var url = $\"{interpolatedUrl}\";";
            }
        }

        return urlCode;
    }

    private string FormatUrlParameter(string url, string paramName, string? formatString)
    {
        return string.IsNullOrEmpty(formatString)
            ? url.Replace($"{{{paramName}}}", $"{{{paramName}}}")
            : url.Replace($"{{{paramName}}}", $"{{{paramName}.ToString(\"{formatString}\")}}");
    }

    private void GenerateParameterHandling(MethodAnalysisResult methodInfo)
    {
        GenerateQueryParameters(methodInfo);
        GenerateHeaderParameters(methodInfo);
        GenerateBodyParameter(methodInfo);
    }

    private void GenerateQueryParameters(MethodAnalysisResult methodInfo)
    {
        var queryParams = methodInfo.Parameters
            .Where(p => p.Attributes.Any(attr => attr.Name == HttpClientGeneratorConstants.QueryAttribute))
            .ToList();

        var arrayQueryParams = methodInfo.Parameters
            .Where(p => p.Attributes.Any(attr => attr.Name == HttpClientGeneratorConstants.ArrayQueryAttribute))
            .ToList();

        // 检查接口是否有[Query("Authorization")]特性（支持AliasAs）
        var hasAuthorizationQuery = methodInfo.InterfaceAttributes?.Any(attr => attr.StartsWith("Query:")) == true;

        if (!queryParams.Any() && !arrayQueryParams.Any() && !hasAuthorizationQuery)
            return;

        codeBuilder.AppendLine($"            var queryParams = HttpUtility.ParseQueryString(string.Empty);");

        foreach (var param in queryParams)
        {
            GenerateSingleQueryParameter(param);
        }

        foreach (var param in arrayQueryParams)
        {
            GenerateArrayQueryParameter(param);
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

    private void GenerateSingleQueryParameter(ParameterInfo param)
    {
        var queryAttr = param.Attributes.First(a => a.Name == HttpClientGeneratorConstants.QueryAttribute);
        var paramName = GetQueryParameterName(queryAttr, param.Name);
        var formatString = GetFormatString(queryAttr);

        if (IsSimpleType(param.Type))
        {
            GenerateSimpleQueryParameter(param, paramName, formatString);
        }
        else
        {
            GenerateComplexQueryParameter(param, paramName);
        }
    }

    private void GenerateArrayQueryParameter(ParameterInfo param)
    {
        var arrayQueryAttr = param.Attributes.First(a => a.Name == HttpClientGeneratorConstants.ArrayQueryAttribute);
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

    private void GenerateSimpleQueryParameter(ParameterInfo param, string paramName, string? formatString)
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

    private void GenerateComplexQueryParameter(ParameterInfo param, string paramName)
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

    private void GenerateHeaderParameters(MethodAnalysisResult methodInfo)
    {
        var headerParams = methodInfo.Parameters
            .Where(p => p.Attributes.Any(attr => attr.Name == HttpClientGeneratorConstants.HeaderAttribute))
            .ToList();

        foreach (var param in headerParams)
        {
            var headerAttr = param.Attributes.First(a => a.Name == HttpClientGeneratorConstants.HeaderAttribute);
            var headerName = headerAttr.Arguments.FirstOrDefault()?.ToString() ?? param.Name;

            codeBuilder.AppendLine($"            if (!string.IsNullOrEmpty({param.Name}))");
            codeBuilder.AppendLine($"                request.Headers.Add(\"{headerName}\", {param.Name});");
        }
    }

    private void GenerateBodyParameter(MethodAnalysisResult methodInfo)
    {
        var bodyParam = methodInfo.Parameters
            .FirstOrDefault(p => p.Attributes.Any(attr => attr.Name == HttpClientGeneratorConstants.BodyAttribute));

        if (bodyParam == null)
            return;

        var bodyAttr = bodyParam.Attributes.First(a => a.Name == HttpClientGeneratorConstants.BodyAttribute);
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

    private void GenerateRequestExecution(MethodAnalysisResult methodInfo)
    {
        var (cancellationTokenArg, _) = GetCancellationTokenParams(methodInfo);
        var interfaceName = InterfaceHelper.GetImplementationClassName(methodInfo.CurrentInterfaceName);

        codeBuilder.AppendLine("            try");
        codeBuilder.AppendLine("            {");
        GenerateRequestExecutionCore(methodInfo, interfaceName, cancellationTokenArg);
        codeBuilder.AppendLine("            }");
        codeBuilder.AppendLine("            catch (System.Exception ex)");
        codeBuilder.AppendLine("            {");
        GenerateExceptionHandling(methodInfo, interfaceName);
        codeBuilder.AppendLine("            }");
    }

    private void GenerateRequestExecutionCore(MethodAnalysisResult methodInfo, string interfaceName, string cancellationTokenArg)
    {
        codeBuilder.AppendLine($"                On{StringExtensions.ConvertFunctionName(interfaceName, "Api", "RequestBefore")}(request, url);");
        codeBuilder.AppendLine($"                On{StringExtensions.ConvertFunctionName(methodInfo.MethodName, "Before")}(request, url);");
        codeBuilder.AppendLine($"                using var response = await _httpClient.SendAsync(request{cancellationTokenArg});");
        codeBuilder.AppendLine($"                if ({PrivateFieldNamingHelper.GeneratePrivateFieldName(httpClientOptionsName)}.EnableLogging)");
        codeBuilder.AppendLine("                {");
        codeBuilder.AppendLine("                    _logger.LogDebug(\"HTTP请求完成: {StatusCode}\", (int)response.StatusCode);");
        codeBuilder.AppendLine("                }");
        codeBuilder.AppendLine();
        codeBuilder.AppendLine("                if (!response.IsSuccessStatusCode)");
        codeBuilder.AppendLine("                {");
        GenerateErrorResponseHandling(methodInfo, interfaceName);
        codeBuilder.AppendLine("                }");
        codeBuilder.AppendLine($"                On{StringExtensions.ConvertFunctionName(interfaceName, "Api", "RequestAfter")}(response, url);");
        codeBuilder.AppendLine($"                On{StringExtensions.ConvertFunctionName(methodInfo.MethodName, "After")}(response, url);");

        GenerateResponseProcessing(methodInfo, cancellationTokenArg);
    }

    private void GenerateErrorResponseHandling(MethodAnalysisResult methodInfo, string interfaceName)
    {
        var (_, cancellationTokenArgForRead) = GetCancellationTokenParams(methodInfo);
        codeBuilder.AppendLine($"                    On{StringExtensions.ConvertFunctionName(interfaceName, "Api", "RequestFail")}(response, url);");
        codeBuilder.AppendLine($"                    On{StringExtensions.ConvertFunctionName(methodInfo.MethodName, "Fail")}(response, url);");
        codeBuilder.AppendLine($"                    var errorContent = await response.Content.ReadAsStringAsync({cancellationTokenArgForRead});");
        codeBuilder.AppendLine($"                    if ({PrivateFieldNamingHelper.GeneratePrivateFieldName(httpClientOptionsName)}.EnableLogging)");
        codeBuilder.AppendLine("                    {");
        codeBuilder.AppendLine("                        _logger.LogError(\"HTTP请求失败: {StatusCode}, 响应: {Response}\", (int)response.StatusCode, errorContent);");
        codeBuilder.AppendLine("                    }");
        codeBuilder.AppendLine("                    throw new HttpRequestException($\"HTTP请求失败: {(int)response.StatusCode}\");");
    }

    private void GenerateResponseProcessing(MethodAnalysisResult methodInfo, string cancellationTokenArg)
    {
        // 检查是否有 FilePath 参数，直接保存到文件
        var filePathParam = methodInfo.Parameters.FirstOrDefault(p => p.Attributes.Any(attr => attr.Name == HttpClientGeneratorConstants.FilePathAttribute));
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
            var filePathAttr = filePathParam.Attributes.First(a => a.Name == HttpClientGeneratorConstants.FilePathAttribute);
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

    private void GenerateExceptionHandling(MethodAnalysisResult methodInfo, string interfaceName)
    {
        codeBuilder.AppendLine($"                if ({PrivateFieldNamingHelper.GeneratePrivateFieldName(httpClientOptionsName)}.EnableLogging)");
        codeBuilder.AppendLine("                {");
        codeBuilder.AppendLine("                    _logger.LogError(ex, \"HTTP请求异常: {{Url}}\", url);");
        codeBuilder.AppendLine("                }");
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
        else if (attribute.Arguments.Length == 1 && HttpClientGeneratorConstants.PathAttributes.Contains(attribute.Name))
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
