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


internal class InterfaceImpCodeGenerator
{
    private class GenerationContext
    {
        public string ClassName { get; }
        public Configuration Config { get; }
        public bool HasInheritedFrom => !string.IsNullOrEmpty(Config.InheritedFrom);
        public bool HasTokenManager => !string.IsNullOrEmpty(Config.TokenManager);
        public string FieldAccessibility { get; }
        public string LoggerType { get; }
        public string ConstructorLoggerType { get; }
        public string OptionsFieldName { get; }
        public string OptionsParameterName { get; }

        public GenerationContext(string className, Configuration config)
        {
            ClassName = className;
            Config = config;
            FieldAccessibility = config.IsAbstract ? "protected " : "private ";
            LoggerType = config.IsAbstract ? "ILogger" : $"ILogger<{className}>";
            ConstructorLoggerType = config.IsAbstract ? "ILogger" : $"ILogger<{className}>";
            OptionsFieldName = PrivateFieldNamingHelper.GeneratePrivateFieldName(Config.HttpClientOptionsName);
            OptionsParameterName = PrivateFieldNamingHelper.GeneratePrivateFieldName(Config.HttpClientOptionsName, FieldNamingStyle.PureCamel);
        }
    }

    private class Configuration
    {
        public string HttpClientOptionsName { get; set; } = "HttpClientOptions";
        public string DefaultContentType { get; set; } = "application/json";
        public int TimeoutFromAttribute { get; set; } = 100;
        public string? BaseAddressFromAttribute { get; set; }
        public bool IsAbstract { get; set; }
        public string? InheritedFrom { get; set; }
        public string? TokenManager { get; set; }
        public string? TokenManagerType { get; set; }
    }

    private string httpClientOptionsName = "HttpClientOptions";
    private Compilation _compilation;
    private InterfaceDeclarationSyntax _interfaceDecl;
    private SourceProductionContext _context;
    private HttpInvokeClassSourceGenerator _httpInvokeClassSourceGenerator;

    private AttributeData? _httpClientApiAttribute;
    private bool _isAbstract;
    private string? _inheritedFrom;
    private string? _tokenManage;
    private INamedTypeSymbol _interfaceSymbol;
    private StringBuilder _codeBuilder;

    public InterfaceImpCodeGenerator(
        HttpInvokeClassSourceGenerator httpInvokeClassSourceGenerator,
        Compilation compilation,
        InterfaceDeclarationSyntax interfaceDecl,
        SourceProductionContext context,
        string optionsName)
    {
        _httpInvokeClassSourceGenerator = httpInvokeClassSourceGenerator;
        httpClientOptionsName = optionsName;
        _compilation = compilation;
        _interfaceDecl = interfaceDecl;
        _context = context;
        _codeBuilder = new StringBuilder();
    }

    /// <summary>
    /// 生成代码入口，包含类结构和方法实现。
    /// </summary>
    public void GeneratorCode()
    {
        var model = _compilation.GetSemanticModel(_interfaceDecl.SyntaxTree);
        if (model.GetDeclaredSymbol(_interfaceDecl) is not INamedTypeSymbol interfaceSymbolObj)
            return;
        _interfaceSymbol = interfaceSymbolObj;
        // 获取HttpClientApi特性中的属性值
        _httpClientApiAttribute = AttributeDataHelper.GetAttributeDataFromSymbol(_interfaceSymbol, HttpClientGeneratorConstants.HttpClientApiAttributeNames);
        _isAbstract = AttributeDataHelper.GetBoolValueFromAttribute(_httpClientApiAttribute, HttpClientGeneratorConstants.IsAbstractProperty);
        _inheritedFrom = AttributeDataHelper.GetStringValueFromAttribute(_httpClientApiAttribute, HttpClientGeneratorConstants.InheritedFromProperty);
        _tokenManage = AttributeDataHelper.GetStringValueFromAttribute(_httpClientApiAttribute, HttpClientGeneratorConstants.TokenManageProperty);

        GenerateImplementationClass();

        var className = TypeSymbolHelper.GetImplementationClassName(_interfaceSymbol.Name);
        _context.AddSource($"{className}.g.cs", SourceText.From(_codeBuilder.ToString(), Encoding.UTF8));
    }

    private void GenerateImplementationClass()
    {
        var className = TypeSymbolHelper.GetImplementationClassName(_interfaceSymbol.Name);
        var namespaceName = SyntaxHelper.GetNamespaceName(_interfaceDecl);

        GenerateClassStructure(className, namespaceName);
        GenerateClassFieldsAndConstructor(className);
        GenerateMethods();

        _codeBuilder.AppendLine("    }");
        _codeBuilder.AppendLine("}");
    }


    private void GenerateClassStructure(string className, string namespaceName)
    {
        // 构建类声明
        var classKeyword = _isAbstract ? "abstract partial class" : "partial class";
        var inheritanceList = new List<string> { _interfaceSymbol.Name };

        if (!string.IsNullOrEmpty(_inheritedFrom))
        {
            inheritanceList.Insert(0, _inheritedFrom);
        }

        _httpInvokeClassSourceGenerator.GenerateFileHeader(_codeBuilder);

        _codeBuilder.AppendLine();
        _codeBuilder.AppendLine($"namespace {namespaceName}");
        _codeBuilder.AppendLine("{");
        _codeBuilder.AppendLine($"    /// <summary>");
        _codeBuilder.AppendLine($"    /// <inheritdoc cref=\"{_interfaceSymbol.Name}\"/>");
        _codeBuilder.AppendLine($"    /// </summary>");
        _codeBuilder.AppendLine($"    {GeneratedCodeConsts.CompilerGeneratedAttribute}");
        _codeBuilder.AppendLine($"    {GeneratedCodeConsts.GeneratedCodeAttribute}");
        _codeBuilder.AppendLine($"    internal {classKeyword} {className} : {string.Join(", ", inheritanceList)}");
        _codeBuilder.AppendLine("    {");
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

    private void GenerateClassFieldsAndConstructor(string className)
    {
        var config = ExtractConfigurationFromAttributes();
        var context = new GenerationContext(className, config);

        GenerateClassFields(context);
        GenerateConstructor(context);
        GenerateHelperMethods(context);
    }


    private Configuration ExtractConfigurationFromAttributes()
    {
        return new Configuration
        {
            HttpClientOptionsName = httpClientOptionsName,
            DefaultContentType = GetHttpClientApiContentTypeFromAttribute(_httpClientApiAttribute),
            TimeoutFromAttribute = AttributeDataHelper.GetIntValueFromAttribute(_httpClientApiAttribute, HttpClientGeneratorConstants.TimeoutProperty, 100),
            BaseAddressFromAttribute = AttributeDataHelper.GetStringValueFromAttributeConstructor(_httpClientApiAttribute, HttpClientGeneratorConstants.BaseAddressProperty),
            IsAbstract = _isAbstract,
            InheritedFrom = _inheritedFrom,
            TokenManager = _tokenManage,
            TokenManagerType = !string.IsNullOrEmpty(_tokenManage)
                ? TypeSymbolHelper.GetrTypeAllDisplayString(_compilation, _tokenManage!)
                : null
        };
    }

    private void GenerateClassFields(GenerationContext context)
    {
        if (context.HasInheritedFrom) return;

        _codeBuilder.AppendLine($"        {context.FieldAccessibility}readonly HttpClient _httpClient;");
        _codeBuilder.AppendLine($"        {context.FieldAccessibility}readonly {context.LoggerType} _logger;");
        _codeBuilder.AppendLine($"        {context.FieldAccessibility}readonly JsonSerializerOptions _jsonSerializerOptions;");
        _codeBuilder.AppendLine($"        {context.FieldAccessibility}readonly {context.Config.HttpClientOptionsName} {context.OptionsFieldName};");

        if (context.HasTokenManager)
        {
            _codeBuilder.AppendLine($"        {context.FieldAccessibility}readonly {context.Config.TokenManagerType} _tokenManager;");
        }
        _codeBuilder.AppendLine($"        {context.FieldAccessibility}readonly string _defaultContentType = \"{context.Config.DefaultContentType}\";");
        _codeBuilder.AppendLine();
    }

    private void GenerateConstructor(GenerationContext context)
    {
        GenerateConstructorDocumentation(context);
        GenerateConstructorSignature(context);
        GenerateConstructorBody(context);
    }

    private void GenerateConstructorDocumentation(GenerationContext context)
    {
        _codeBuilder.AppendLine("        /// <summary>");
        _codeBuilder.AppendLine($"        /// 构建 <see cref = \"{context.ClassName}\"/> 类的实例。");
        _codeBuilder.AppendLine("        /// </summary>");
        _codeBuilder.AppendLine("        /// <param name=\"httpClient\">HttpClient实例</param>");
        _codeBuilder.AppendLine("        /// <param name=\"logger\">日志记录器</param>");
        _codeBuilder.AppendLine("        /// <param name=\"option\">Json序列化参数</param>");
        _codeBuilder.AppendLine($"        /// <param name=\"{context.OptionsParameterName}\">飞书配置选项</param>");

        if (context.HasTokenManager)
        {
            _codeBuilder.AppendLine("        /// <param name=\"tokenManager\">Token管理器</param>");
        }
    }

    private void GenerateConstructorSignature(GenerationContext context)
    {
        var parameters = new List<string>
        {
            "HttpClient httpClient",
            $"{context.ConstructorLoggerType} logger",
            "IOptions<JsonSerializerOptions> option",
            $"IOptions<{context.Config.HttpClientOptionsName}> {context.OptionsParameterName}"
        };

        if (context.HasTokenManager)
        {
            parameters.Add($"{context.Config.TokenManagerType} tokenManager");
        }

        var signature = $"        public {context.ClassName}({string.Join(", ", parameters)})";
        _codeBuilder.Append(signature);

        if (context.HasInheritedFrom)
        {
            var baseParameters = new List<string>
            {
                "httpClient",
                "logger",
                "option",
                context.OptionsParameterName
            };
            if (context.HasTokenManager)
            {
                baseParameters.Add("tokenManager");
            }
            _codeBuilder.AppendLine($" : base({string.Join(", ", baseParameters)})");
        }
        else
        {
            _codeBuilder.AppendLine();
        }
    }

    private void GenerateConstructorBody(GenerationContext context)
    {
        _codeBuilder.AppendLine("        {");

        if (!context.HasInheritedFrom)
        {
            _codeBuilder.AppendLine("            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));");
            _codeBuilder.AppendLine("            _logger = logger ?? throw new ArgumentNullException(nameof(logger));");
            _codeBuilder.AppendLine("            _jsonSerializerOptions = option.Value;");
            _codeBuilder.AppendLine($"            {context.OptionsFieldName} = {context.OptionsParameterName}?.Value ?? throw new ArgumentNullException(nameof({context.OptionsParameterName}));");

            if (context.HasTokenManager)
            {
                _codeBuilder.AppendLine("            _tokenManager = tokenManager ?? throw new ArgumentNullException(nameof(tokenManager));");
            }
            _codeBuilder.AppendLine();
        }

        GenerateHttpClientConfiguration(context);
        _codeBuilder.AppendLine("        }");
        _codeBuilder.AppendLine();
    }

    private void GenerateHttpClientConfiguration(GenerationContext context)
    {
        if (context.Config.IsAbstract) return;

        _codeBuilder.AppendLine("            // 设置 HttpClient BaseAddress（用于相对路径请求）");
        _codeBuilder.AppendLine("            var finalBaseAddress = GetFinalBaseAddress();");
        _codeBuilder.AppendLine("            if (!string.IsNullOrEmpty(finalBaseAddress))");
        _codeBuilder.AppendLine("            {");
        _codeBuilder.AppendLine("                _httpClient.BaseAddress = new Uri(finalBaseAddress);");
        _codeBuilder.AppendLine("            }");
        _codeBuilder.AppendLine();

        _codeBuilder.AppendLine("            // 配置HttpClient超时时间");
        _codeBuilder.AppendLine("            var finalTimeout = GetFinalTimeout();");
        _codeBuilder.AppendLine("            _httpClient.Timeout = TimeSpan.FromSeconds(finalTimeout);");
    }

    private void GenerateHelperMethods(GenerationContext context)
    {
        if (!context.Config.IsAbstract)
        {
            GenerateConfigurationHelperMethods(context);
        }

        if (context.HasInheritedFrom) return;

        GenerateGetMediaTypeMethod(context);
    }

    private void GenerateGetMediaTypeMethod(GenerationContext context)
    {
        _codeBuilder.AppendLine("        /// <summary>");
        _codeBuilder.AppendLine("        /// 从Content-Type字符串中提取媒体类型部分，去除字符集信息。");
        _codeBuilder.AppendLine("        /// </summary>");
        _codeBuilder.AppendLine("        /// <param name=\"contentType\">完整的Content-Type字符串</param>");
        _codeBuilder.AppendLine("        /// <returns>媒体类型部分</returns>");
        _codeBuilder.AppendLine($"        {context.FieldAccessibility}string GetMediaType(string contentType)");
        _codeBuilder.AppendLine("        {");
        _codeBuilder.AppendLine("            if (string.IsNullOrEmpty(contentType))");
        _codeBuilder.AppendLine("                return \"application/json\";");
        _codeBuilder.AppendLine();
        _codeBuilder.AppendLine("            // Content-Type可能包含字符集信息，如 \"application/json; charset=utf-8\"");
        _codeBuilder.AppendLine("            // 需要分号前的媒体类型部分");
        _codeBuilder.AppendLine("            var semicolonIndex = contentType.IndexOf(';');");
        _codeBuilder.AppendLine("            if (semicolonIndex >= 0)");
        _codeBuilder.AppendLine("            {");
        _codeBuilder.AppendLine("                return contentType.Substring(0, semicolonIndex).Trim();");
        _codeBuilder.AppendLine("            }");
        _codeBuilder.AppendLine();
        _codeBuilder.AppendLine("            return contentType.Trim();");
        _codeBuilder.AppendLine("        }");
        _codeBuilder.AppendLine();
    }

    private void GenerateConfigurationHelperMethods(GenerationContext context)
    {
        GenerateGetFinalTimeoutMethod(context);
        GenerateGetFinalBaseAddressMethod(context);
    }

    private void GenerateGetFinalTimeoutMethod(GenerationContext context)
    {
        _codeBuilder.AppendLine("        /// <summary>");
        _codeBuilder.AppendLine($"        /// 获取最终的超时时间，优先使用 HttpClientApi 特性中的设置，否则使用 {context.Config.HttpClientOptionsName}.TimeOut");
        _codeBuilder.AppendLine("        /// </summary>");
        _codeBuilder.AppendLine("        /// <returns>超时秒数</returns>");
        _codeBuilder.AppendLine("        private int GetFinalTimeout()");
        _codeBuilder.AppendLine("        {");
        _codeBuilder.AppendLine($"            // 优先使用 HttpClientApi 特性中的超时设置");
        _codeBuilder.AppendLine($"            var attributeTimeout = {context.Config.TimeoutFromAttribute};");
        _codeBuilder.AppendLine($"            if (attributeTimeout > 0)");
        _codeBuilder.AppendLine($"                return attributeTimeout;");
        _codeBuilder.AppendLine();
        _codeBuilder.AppendLine($"            // 尝试使用 {context.Config.HttpClientOptionsName}.TimeOut");
        _codeBuilder.AppendLine($"            var optionsTimeout = {context.OptionsFieldName}.TimeOut;");
        _codeBuilder.AppendLine($"            return !string.IsNullOrEmpty(optionsTimeout) && int.TryParse(optionsTimeout, out var parsedTimeout)");
        _codeBuilder.AppendLine($"                ? parsedTimeout");
        _codeBuilder.AppendLine($"                : 60; // 默认60秒超时");
        _codeBuilder.AppendLine("        }");
        _codeBuilder.AppendLine();
    }

    private void GenerateGetFinalBaseAddressMethod(GenerationContext context)
    {
        _codeBuilder.AppendLine("        /// <summary>");
        _codeBuilder.AppendLine($"        /// 获取最终的 BaseAddress，优先使用 HttpClientApi 特性中的设置，否则使用 {context.Config.HttpClientOptionsName}.BaseUrl");
        _codeBuilder.AppendLine("        /// </summary>");
        _codeBuilder.AppendLine("        /// <returns>BaseAddress</returns>");
        _codeBuilder.AppendLine("        private string? GetFinalBaseAddress()");
        _codeBuilder.AppendLine("        {");
        _codeBuilder.AppendLine($"            // 优先使用 HttpClientApi 特性中的 BaseAddress");
        _codeBuilder.AppendLine($"            var attributeAddress = \"{context.Config.BaseAddressFromAttribute}\";");
        _codeBuilder.AppendLine($"            return !string.IsNullOrEmpty(attributeAddress)");
        _codeBuilder.AppendLine($"                ? attributeAddress");
        _codeBuilder.AppendLine($"                : {context.OptionsFieldName}.BaseUrl;");
        _codeBuilder.AppendLine("        }");
        _codeBuilder.AppendLine();
    }

    private void GenerateMethods()
    {
        GenerateClassPartialMethods();

        // 根据IsAbstract和InheritedFrom决定是否包含父接口方法
        var includeParentInterfaces = GetIncludeParentInterfaces();

        IEnumerable<IMethodSymbol> methodsToGenerate = TypeSymbolHelper.GetAllMethods(_interfaceSymbol, includeParentInterfaces);

        foreach (var methodSymbol in methodsToGenerate)
        {
            GenerateMethodImplementation(methodSymbol);
        }
    }

    private bool GetIncludeParentInterfaces()
    {
        if (_isAbstract)
            return false;
        if (!string.IsNullOrEmpty(_inheritedFrom))
            return false;
        return true;
    }


    /// <summary>
    /// 生成方法实现的代码。
    /// </summary>
    /// <param name="methodSymbol"></param>
    private void GenerateMethodImplementation(IMethodSymbol methodSymbol)
    {
        var methodInfo = MethodHelper.AnalyzeMethod(_compilation, methodSymbol, _interfaceDecl);
        if (!methodInfo.IsValid) return;

        // 检查是否忽略生成实现
        if (methodInfo.IgnoreImplement) return;

        // 获取接口符号以检查Token管理
        var model = _compilation.GetSemanticModel(_interfaceDecl.SyntaxTree);

        var hasTokenManager = !string.IsNullOrEmpty(_tokenManage);
        var tokenManagerType = hasTokenManager ? TypeSymbolHelper.GetrTypeAllDisplayString(_compilation, _tokenManage!) : null;
        var hasAuthorizationHeader = TypeSymbolHelper.HasPropertyAttribute(_interfaceSymbol!, "Header", "Authorization");
        var hasAuthorizationQuery = TypeSymbolHelper.HasPropertyAttribute(_interfaceSymbol!, "Query", "Authorization");

        _codeBuilder.AppendLine();
        _codeBuilder.AppendLine($"        /// <summary>");
        _codeBuilder.AppendLine($"        /// <inheritdoc />");
        _codeBuilder.AppendLine($"        /// </summary>");
        _codeBuilder.AppendLine($"        {GeneratedCodeConsts.GeneratedCodeAttribute}");
        // 根据方法返回类型决定是否添加 async 关键字
        var asyncKeyword = methodInfo.IsAsyncMethod ? "async " : "";
        _codeBuilder.AppendLine($"        public {asyncKeyword}{methodSymbol.ReturnType} {methodSymbol.Name}({TypeSymbolHelper.GetParameterList(methodSymbol)})");
        _codeBuilder.AppendLine("        {");

        // 如果需要Token管理器，获取access_token
        if (hasTokenManager && (hasAuthorizationHeader || hasAuthorizationQuery))
        {
            _codeBuilder.AppendLine($"            var access_token = await _tokenManager.GetTokenAsync();");
            _codeBuilder.AppendLine($"            if (string.IsNullOrEmpty(access_token))");
            _codeBuilder.AppendLine($"            {{");
            _codeBuilder.AppendLine($"                throw new InvalidOperationException(\"无法获取访问令牌\");");
            _codeBuilder.AppendLine($"            }}");
            _codeBuilder.AppendLine();
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
            _codeBuilder.AppendLine($"            // 添加Authorization header as {headerName}");
            _codeBuilder.AppendLine($"            request.Headers.Add(\"{headerName}\", access_token);");
        }

        // 添加接口上定义的所有Header特性（只添加有固定值的Header）
        if (methodInfo.InterfaceHeaderAttributes?.Any() == true)
        {
            var fixedValueHeaders = methodInfo.InterfaceHeaderAttributes
                .Where(h => !string.IsNullOrEmpty(h.Name) && h.Value != null)
                .ToList();

            if (fixedValueHeaders.Any())
            {
                _codeBuilder.AppendLine($"            // 添加接口定义的Header特性");
                foreach (var interfaceHeader in fixedValueHeaders)
                {
                    var headerValue = interfaceHeader.Value?.ToString() ?? "null";
                    _codeBuilder.AppendLine($"            request.Headers.Add(\"{interfaceHeader.Name}\", \"{headerValue}\");");
                }
            }
        }

        GenerateRequestExecution(methodInfo);

        _codeBuilder.AppendLine("        }");
        _codeBuilder.AppendLine();
    }

    private void GenerateClassPartialMethods()
    {
        var includeParentInterfaces = GetIncludeParentInterfaces();
        IEnumerable<IMethodSymbol> methodsToProcess = TypeSymbolHelper.GetAllMethods(_interfaceSymbol, includeParentInterfaces);

        var processedMethods = new HashSet<string>();

        foreach (var methodSymbol in methodsToProcess)
        {
            if (!processedMethods.Add(methodSymbol.Name))
                continue;

            GenerateMethodPartialMethods(methodSymbol.Name);
        }

        var interfaceName = TypeSymbolHelper.GetImplementationClassName(_interfaceSymbol.Name);
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
            _codeBuilder.AppendLine();
            _codeBuilder.AppendLine($"        /// <summary>");
            _codeBuilder.AppendLine($"        /// {methodName} {description}。");
            _codeBuilder.AppendLine($"        /// </summary>");
            _codeBuilder.AppendLine($"        {GeneratedCodeConsts.GeneratedCodeAttribute}");
            _codeBuilder.AppendLine($"        partial void On{StringExtensions.ConvertFunctionName(methodName, eventType)}({parameter}, string url);");
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
            _codeBuilder.AppendLine();
            _codeBuilder.AppendLine($"        /// <summary>");
            _codeBuilder.AppendLine($"        /// {interfaceName} {description}。");
            _codeBuilder.AppendLine($"        /// </summary>");
            _codeBuilder.AppendLine($"        {GeneratedCodeConsts.GeneratedCodeAttribute}");
            _codeBuilder.AppendLine($"        partial void On{StringExtensions.ConvertFunctionName(interfaceName, "Api", eventType)}({parameter}, string url);");
        }
    }


    private void GenerateRequestSetup(MethodAnalysisResult methodInfo)
    {
        var urlCode = BuildUrlString(methodInfo);
        _codeBuilder.AppendLine(urlCode);

        // 检查是否需要 BaseAddress（仅当 URL 为相对路径时）
        var isAbsoluteUrl = methodInfo.UrlTemplate.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                           methodInfo.UrlTemplate.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

        if (!isAbsoluteUrl)
        {
            _codeBuilder.AppendLine("            // 检查 BaseAddress 是否已设置（相对路径 URL 需要 BaseAddress）");
            _codeBuilder.AppendLine("            if (_httpClient.BaseAddress == null)");
            _codeBuilder.AppendLine("            {");
            _codeBuilder.AppendLine($"                throw new InvalidOperationException(\"BaseAddress 配置缺失，相对路径 URL 需要在 HttpClientApi 特性或 {httpClientOptionsName}.BaseUrl 中设置有效的基地址\");");
            _codeBuilder.AppendLine("            }");
        }

        _codeBuilder.AppendLine($"            if ({PrivateFieldNamingHelper.GeneratePrivateFieldName(httpClientOptionsName)}.EnableLogging)");
        _codeBuilder.AppendLine($"            {{");
        _codeBuilder.AppendLine($"                _logger.LogDebug(\"开始HTTP {methodInfo.HttpMethod}请求: {{Url}}\", url);");
        _codeBuilder.AppendLine($"            }}");
        _codeBuilder.AppendLine($"            using var request = new HttpRequestMessage(HttpMethod.{methodInfo.HttpMethod}, url);");
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

        _codeBuilder.AppendLine($"            var queryParams = HttpUtility.ParseQueryString(string.Empty);");

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
            _codeBuilder.AppendLine($"            // 添加Authorization query参数 as {queryName}");
            _codeBuilder.AppendLine($"            queryParams.Add(\"{queryName}\", access_token);");
        }

        _codeBuilder.AppendLine("            if (queryParams.Count > 0)");
        _codeBuilder.AppendLine("            {");
        _codeBuilder.AppendLine("                url += \"?\" + queryParams.ToString();");
        _codeBuilder.AppendLine("            }");
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

        _codeBuilder.AppendLine($"            if ({param.Name} != null && {param.Name}.Length > 0)");
        _codeBuilder.AppendLine("            {");

        if (string.IsNullOrEmpty(separator))
        {
            // 使用重复键名格式：user_ids=id0&user_ids=id1&user_ids=id2
            _codeBuilder.AppendLine($"                foreach (var item in {param.Name})");
            _codeBuilder.AppendLine("                {");
            _codeBuilder.AppendLine($"                    if (item != null)");
            _codeBuilder.AppendLine("                    {");
            _codeBuilder.AppendLine($"                        var encodedValue = HttpUtility.UrlEncode(item.ToString());");
            _codeBuilder.AppendLine($"                        queryParams.Add(\"{paramName}\", encodedValue);");
            _codeBuilder.AppendLine("                    }");
            _codeBuilder.AppendLine("                }");
        }
        else
        {
            // 使用分隔符连接格式：user_ids=id0;id1;id2
            _codeBuilder.AppendLine($"                var joinedValues = string.Join(\"{separator}\", {param.Name}.Where(item => item != null).Select(item => HttpUtility.UrlEncode(item.ToString())));");
            _codeBuilder.AppendLine($"                queryParams.Add(\"{paramName}\", joinedValues);");
        }

        _codeBuilder.AppendLine("            }");
    }

    private void GenerateSimpleQueryParameter(ParameterInfo param, string paramName, string? formatString)
    {
        if (IsArrayType(param.Type))
        {
            // 处理数组类型：使用默认分号分隔符格式
            _codeBuilder.AppendLine($"            if ({param.Name} != null && {param.Name}.Length > 0)");
            _codeBuilder.AppendLine("            {");
            _codeBuilder.AppendLine($"                var joinedValues = string.Join(\";\", {param.Name}.Where(item => item != null).Select(item => HttpUtility.UrlEncode(item.ToString())));");
            _codeBuilder.AppendLine($"                queryParams.Add(\"{paramName}\", joinedValues);");
            _codeBuilder.AppendLine("            }");
        }
        else if (IsStringType(param.Type))
        {
            _codeBuilder.AppendLine($"            if (!string.IsNullOrEmpty({param.Name}))");
            _codeBuilder.AppendLine("            {");
            _codeBuilder.AppendLine($"                var encodedValue = HttpUtility.UrlEncode({param.Name});");
            _codeBuilder.AppendLine($"                queryParams.Add(\"{paramName}\", encodedValue);");
            _codeBuilder.AppendLine("            }");
        }
        else
        {
            if (param.Name.EndsWith("?", StringComparison.Ordinal))
            {
                _codeBuilder.AppendLine($"            if ({param.Name} != null)");
                var formatExpression = !string.IsNullOrEmpty(formatString)
                   ? $".ToString(\"{formatString}\")"
                   : ".ToString()";
                _codeBuilder.AppendLine($"                queryParams.Add(\"{paramName}\", {param.Name}{formatExpression});");
            }
            else
            {
                var formatExpression = !string.IsNullOrEmpty(formatString)
                  ? $".ToString(\"{formatString}\")"
                  : ".ToString()";
                _codeBuilder.AppendLine($"            queryParams.Add(\"{paramName}\", {param.Name}{formatExpression});");
            }
        }
    }

    private void GenerateComplexQueryParameter(ParameterInfo param, string paramName)
    {
        _codeBuilder.AppendLine($"            if ({param.Name} != null)");
        _codeBuilder.AppendLine("            {");
        _codeBuilder.AppendLine($"                var properties = {param.Name}.GetType().GetProperties();");
        _codeBuilder.AppendLine("                foreach (var prop in properties)");
        _codeBuilder.AppendLine("                {");
        _codeBuilder.AppendLine($"                    var value = prop.GetValue({param.Name});");
        _codeBuilder.AppendLine("                    if (value != null)");
        _codeBuilder.AppendLine("                    {");
        _codeBuilder.AppendLine($"                        queryParams.Add(prop.Name, HttpUtility.UrlEncode(value.ToString()));");
        _codeBuilder.AppendLine("                    }");
        _codeBuilder.AppendLine("                }");
        _codeBuilder.AppendLine("            }");
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

            _codeBuilder.AppendLine($"            if (!string.IsNullOrEmpty({param.Name}))");
            _codeBuilder.AppendLine($"                request.Headers.Add(\"{headerName}\", {param.Name});");
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

        _codeBuilder.AppendLine($"            if ({bodyParam.Name} != null)");
        _codeBuilder.AppendLine("            {");

        if (useStringContent)
        {
            _codeBuilder.AppendLine($"                request.Content = new StringContent({bodyParam.Name}.ToString() ?? \"\", Encoding.UTF8, {contentTypeExpression});");
        }
        else
        {
            _codeBuilder.AppendLine($"                var jsonContent = JsonSerializer.Serialize({bodyParam.Name}, _jsonSerializerOptions);");
            _codeBuilder.AppendLine($"                request.Content = new StringContent(jsonContent, Encoding.UTF8, {contentTypeExpression});");
        }

        _codeBuilder.AppendLine("            }");
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
        var interfaceName = TypeSymbolHelper.GetImplementationClassName(methodInfo.CurrentInterfaceName);

        _codeBuilder.AppendLine("            try");
        _codeBuilder.AppendLine("            {");
        GenerateRequestExecutionCore(methodInfo, interfaceName, cancellationTokenArg);
        _codeBuilder.AppendLine("            }");
        _codeBuilder.AppendLine("            catch (System.Exception ex)");
        _codeBuilder.AppendLine("            {");
        GenerateExceptionHandling(methodInfo, interfaceName);
        _codeBuilder.AppendLine("            }");
    }

    private void GenerateRequestExecutionCore(MethodAnalysisResult methodInfo, string interfaceName, string cancellationTokenArg)
    {
        _codeBuilder.AppendLine($"                On{StringExtensions.ConvertFunctionName(interfaceName, "Api", "RequestBefore")}(request, url);");
        _codeBuilder.AppendLine($"                On{StringExtensions.ConvertFunctionName(methodInfo.MethodName, "Before")}(request, url);");
        _codeBuilder.AppendLine($"                using var response = await _httpClient.SendAsync(request{cancellationTokenArg});");
        _codeBuilder.AppendLine($"                if ({PrivateFieldNamingHelper.GeneratePrivateFieldName(httpClientOptionsName)}.EnableLogging)");
        _codeBuilder.AppendLine("                {");
        _codeBuilder.AppendLine("                    _logger.LogDebug(\"HTTP请求完成: {StatusCode}\", (int)response.StatusCode);");
        _codeBuilder.AppendLine("                }");
        _codeBuilder.AppendLine();
        _codeBuilder.AppendLine("                if (!response.IsSuccessStatusCode)");
        _codeBuilder.AppendLine("                {");
        GenerateErrorResponseHandling(methodInfo, interfaceName);
        _codeBuilder.AppendLine("                }");
        _codeBuilder.AppendLine($"                On{StringExtensions.ConvertFunctionName(interfaceName, "Api", "RequestAfter")}(response, url);");
        _codeBuilder.AppendLine($"                On{StringExtensions.ConvertFunctionName(methodInfo.MethodName, "After")}(response, url);");

        GenerateResponseProcessing(methodInfo, cancellationTokenArg);
    }

    private void GenerateErrorResponseHandling(MethodAnalysisResult methodInfo, string interfaceName)
    {
        var (_, cancellationTokenArgForRead) = GetCancellationTokenParams(methodInfo);
        _codeBuilder.AppendLine($"                    On{StringExtensions.ConvertFunctionName(interfaceName, "Api", "RequestFail")}(response, url);");
        _codeBuilder.AppendLine($"                    On{StringExtensions.ConvertFunctionName(methodInfo.MethodName, "Fail")}(response, url);");
        _codeBuilder.AppendLine($"                    var errorContent = await response.Content.ReadAsStringAsync({cancellationTokenArgForRead});");
        _codeBuilder.AppendLine($"                    if ({PrivateFieldNamingHelper.GeneratePrivateFieldName(httpClientOptionsName)}.EnableLogging)");
        _codeBuilder.AppendLine("                    {");
        _codeBuilder.AppendLine("                        _logger.LogError(\"HTTP请求失败: {StatusCode}, 响应: {Response}\", (int)response.StatusCode, errorContent);");
        _codeBuilder.AppendLine("                    }");
        _codeBuilder.AppendLine("                    throw new HttpRequestException($\"HTTP请求失败: {(int)response.StatusCode}\");");
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
            _codeBuilder.AppendLine($"                using (var stream = await response.Content.ReadAsStreamAsync({cancellationTokenArgForRead}))");
            _codeBuilder.AppendLine($"                using (var fileStream = File.Create({filePathParam.Name}))");
            _codeBuilder.AppendLine("                {");

            // 从 FilePathAttribute 中读取 BufferSize 参数
            var filePathAttr = filePathParam.Attributes.First(a => a.Name == HttpClientGeneratorConstants.FilePathAttribute);
            var bufferSize = GetBufferSizeFromAttribute(filePathAttr);

            _codeBuilder.AppendLine($"                    await stream.CopyToAsync(fileStream, {bufferSize}{cancellationTokenArgForCopy});");
            _codeBuilder.AppendLine("                }");

            // 对于有 FilePath 参数的方法，不返回任何值（void 或 Task）
            if (!methodInfo.IsAsyncMethod || (methodInfo.IsAsyncMethod && methodInfo.AsyncInnerReturnType.Equals("void", StringComparison.OrdinalIgnoreCase)))
            {
                _codeBuilder.AppendLine("                return;");
            }
            else if (methodInfo.IsAsyncMethod && !string.IsNullOrEmpty(methodInfo.AsyncInnerReturnType) && !methodInfo.AsyncInnerReturnType.Equals("void", StringComparison.OrdinalIgnoreCase))
            {
                // 如果是异步方法且有非void返回类型，返回默认值
                _codeBuilder.AppendLine("                return default;");
            }
            // 对于 Task 类型的异步方法，不需要 return 语句
        }
        else if (isFileDownload)
        {
            // 文件下载场景：直接读取为字节数组
            _codeBuilder.AppendLine($"                byte[] fileBytes = await response.Content.ReadAsByteArrayAsync({cancellationTokenArgForRead});");
            _codeBuilder.AppendLine("                return fileBytes;");
        }
        else
        {
            // 常规 JSON 反序列化场景
            _codeBuilder.AppendLine($"                using var stream = await response.Content.ReadAsStreamAsync({cancellationTokenArgForRead});");
            _codeBuilder.AppendLine();
            _codeBuilder.AppendLine("                if (stream.Length == 0)");
            _codeBuilder.AppendLine("                {");
            _codeBuilder.AppendLine("                    return default;");
            _codeBuilder.AppendLine("                }");
            _codeBuilder.AppendLine();

            // 对于异步方法，使用内部返回类型；对于同步方法，使用完整返回类型
            var deserializeType = methodInfo.IsAsyncMethod ? methodInfo.AsyncInnerReturnType : methodInfo.ReturnType;
            _codeBuilder.AppendLine($"                var result = await JsonSerializer.DeserializeAsync<{deserializeType}>(stream, _jsonSerializerOptions{cancellationTokenArg});");
            _codeBuilder.AppendLine("                return result;");
        }
    }

    private void GenerateExceptionHandling(MethodAnalysisResult methodInfo, string interfaceName)
    {
        _codeBuilder.AppendLine($"                if ({PrivateFieldNamingHelper.GeneratePrivateFieldName(httpClientOptionsName)}.EnableLogging)");
        _codeBuilder.AppendLine("                {");
        _codeBuilder.AppendLine("                    _logger.LogError(ex, \"HTTP请求异常: {{Url}}\", url);");
        _codeBuilder.AppendLine("                }");
        _codeBuilder.AppendLine($"                On{StringExtensions.ConvertFunctionName(interfaceName, "Api", "RequestError")}(ex, url);");
        _codeBuilder.AppendLine($"                On{StringExtensions.ConvertFunctionName(methodInfo.MethodName, "Error")}(ex, url);");
        _codeBuilder.AppendLine("                throw;");
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