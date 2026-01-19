// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.CodeGenerator 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis.Text;
using Mud.ServiceCodeGenerator.HttpInvoke.Generators;
using Mud.ServiceCodeGenerator.HttpInvoke.Helpers;
using System.Text;

namespace Mud.ServiceCodeGenerator.HttpInvoke;


internal class InterfaceImpCodeGenerator
{
    public class GenerationContext
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

    public class Configuration
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
    private SemanticModel _semanticModel;
    private StringBuilder _codeBuilder;

    private ClassStructureGenerator? _classStructureGenerator;
    private ConstructorGenerator? _constructorGenerator;
    private RequestBuilder? _requestBuilder;

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
        // 预估容量：根据接口方法数量估算，平均每个方法约500-800字符
        var estimatedCapacity = EstimateCodeCapacity();
        _codeBuilder = new StringBuilder(estimatedCapacity);
    }

    /// <summary>
    /// 估算生成的代码容量
    /// </summary>
    private int EstimateCodeCapacity()
    {
        _semanticModel = _compilation.GetSemanticModel(_interfaceDecl.SyntaxTree);
        if (_semanticModel.GetDeclaredSymbol(_interfaceDecl) is not INamedTypeSymbol interfaceSymbol)
            return 5000; // 默认容量

        var methods = TypeSymbolHelper.GetAllMethods(interfaceSymbol, true);
        int methodCount = 0;
        foreach (var method in methods)
        {
            methodCount++;
        }

        // 基础容量（类结构、字段、构造函数等）约 2000 字符
        // 每个方法约 600-800 字符
        var estimatedCapacity = 2000 + (methodCount * 700);

        // 限制最大初始容量为 30000，避免过度分配
        return Math.Min(estimatedCapacity, 30000);
    }

    /// <summary>
    /// 生成代码入口，包含类结构和方法实现。
    /// </summary>
    public void GeneratorCode()
    {
        if (_semanticModel.GetDeclaredSymbol(_interfaceDecl) is not INamedTypeSymbol interfaceSymbolObj)
            return;
        _interfaceSymbol = interfaceSymbolObj;

        // 获取HttpClientApi特性中的属性值
        _httpClientApiAttribute = AttributeDataHelper.GetAttributeDataFromSymbol(_interfaceSymbol, HttpClientGeneratorConstants.HttpClientApiAttributeNames);
        _isAbstract = AttributeDataHelper.GetBoolValueFromAttribute(_httpClientApiAttribute, HttpClientGeneratorConstants.IsAbstractProperty);
        _inheritedFrom = AttributeDataHelper.GetStringValueFromAttribute(_httpClientApiAttribute, HttpClientGeneratorConstants.InheritedFromProperty);
        _tokenManage = AttributeDataHelper.GetStringValueFromAttribute(_httpClientApiAttribute, HttpClientGeneratorConstants.TokenManageProperty);

        // 验证基类（问题26）
        // if (!string.IsNullOrEmpty(_inheritedFrom))
        // {
        //     var validationResult = BaseClassValidator.ValidateBaseClass(
        //         _compilation,
        //         _inheritedFrom,
        //         !string.IsNullOrEmpty(_tokenManage),
        //         _interfaceSymbol.ContainingNamespace);

        //     if (!validationResult.IsValid)
        //     {
        //         _context.ReportDiagnostic(Diagnostic.Create(
        //             Diagnostics.HttpClientApiParameterError,
        //             _interfaceDecl.GetLocation(),
        //             _interfaceSymbol.Name,
        //             validationResult.ErrorMessage));
        //         return;
        //     }
        // }

        // 初始化各个生成器
        InitializeGenerators();

        // 检查同名类是否已存在（问题25）
        var className = TypeSymbolHelper.GetImplementationClassName(_interfaceSymbol.Name);
        var namespaceName = SyntaxHelper.GetNamespaceName(_interfaceDecl, "Internal");
        var fullClassName = $"{namespaceName}.{className}";

        // var existingSymbol = _compilation.GetTypeByMetadataName(fullClassName);
        // if (existingSymbol != null && existingSymbol.Locations.Length > 0)
        // {
        //     var location = existingSymbol.Locations[0];
        //     _context.ReportDiagnostic(Diagnostic.Create(
        //         Diagnostics.HttpClientApiParameterError,
        //         _interfaceDecl.GetLocation(),
        //         _interfaceSymbol.Name,
        //         $"生成的类 '{className}' 已存在于 {location.SourceTree?.FilePath ?? location.GetLineSpan().StartLinePosition.ToString()}"));
        //     return;
        // }

        GenerateImplementationClass();
    }

    /// <summary>
    /// 初始化各个专用生成器
    /// </summary>
    private void InitializeGenerators()
    {
        var config = ExtractConfigurationFromAttributes();
        var context = new GenerationContext(TypeSymbolHelper.GetImplementationClassName(_interfaceSymbol.Name), config);

        _classStructureGenerator = new ClassStructureGenerator(
            _httpInvokeClassSourceGenerator,
            _interfaceSymbol,
            _codeBuilder,
            _isAbstract,
            _inheritedFrom);

        _constructorGenerator = new ConstructorGenerator(_codeBuilder, context);
        _requestBuilder = new RequestBuilder();
    }

    private void GenerateImplementationClass()
    {
        var className = TypeSymbolHelper.GetImplementationClassName(_interfaceSymbol.Name);
        var namespaceName = SyntaxHelper.GetNamespaceName(_interfaceDecl, "Internal");

        // 使用专用生成器
        _classStructureGenerator!.Generate(className, namespaceName);
        _constructorGenerator!.Generate(className);
        GenerateMethods();

        _codeBuilder.AppendLine("    }");
        _codeBuilder.AppendLine("}");

        // 在这里再次使用 className
        _context.AddSource($"{className}.g.cs", SourceText.From(_codeBuilder.ToString(), Encoding.UTF8));
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
                ? TypeSymbolHelper.GetTypeAllDisplayString(_compilation, _tokenManage!)
                : null
        };
    }



    private void GenerateMethods()
    {
        // 根据IsAbstract和InheritedFrom决定是否包含父接口方法
        var includeParentInterfaces = GetIncludeParentInterfaces();

        var methodsToGenerate = TypeSymbolHelper.GetAllMethods(_interfaceSymbol, includeParentInterfaces);

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

        // 验证URL模板格式
        if (!string.IsNullOrEmpty(methodInfo.UrlTemplate) &&
            !CSharpCodeValidator.IsValidUrlTemplate(methodInfo.UrlTemplate, out var urlError))
        {
            // 报告URL模板错误
            _context.ReportDiagnostic(Diagnostic.Create(Diagnostics.HttpClientInvalidUrlTemplate, _interfaceDecl.GetLocation(), _interfaceDecl.Identifier.Text, methodInfo.UrlTemplate, urlError));
            return;
        }

        // 检查是否忽略生成实现
        if (methodInfo.IgnoreImplement) return;

        var hasTokenManager = !string.IsNullOrEmpty(_tokenManage);
        var tokenManagerType = hasTokenManager ? TypeSymbolHelper.GetTypeAllDisplayString(_compilation, _tokenManage!) : null;
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

        // 使用 ParameterValidationHelper 生成参数验证代码
        ParameterValidationHelper.GenerateParameterValidation(_codeBuilder, methodInfo.Parameters);

        _codeBuilder.AppendLine();

        // 使用 RequestBuilder 构建请求
        var urlCode = _requestBuilder!.BuildUrlString(methodInfo);
        _codeBuilder.AppendLine(urlCode);

        _requestBuilder.GenerateQueryParameters(_codeBuilder, methodInfo);
        _requestBuilder.GenerateRequestSetup(_codeBuilder, methodInfo);
        _requestBuilder.GenerateHeaderParameters(_codeBuilder, methodInfo);
        _codeBuilder.AppendLine();
        _requestBuilder.GenerateBodyParameter(_codeBuilder, methodInfo);

        // 添加Authorization header
        if (hasTokenManager && hasAuthorizationHeader)
        {
            // 从接口特性中获取实际的header名称
            var headerName = "Authorization";
            if (methodInfo.InterfaceAttributes?.Any() == true)
            {
                var headerAttr = methodInfo.InterfaceAttributes.FirstOrDefault(attr => attr.StartsWith("Header:", StringComparison.Ordinal));
                if (!string.IsNullOrEmpty(headerAttr))
                {
                    headerName = headerAttr.Substring(7); // 去掉"Header:"前缀
                }
            }

            _codeBuilder.AppendLine($"            request.Headers.Add(\"{headerName}\", access_token);");
        }

        // 添加接口上定义的所有Header特性（支持Replace功能）
        if (methodInfo.InterfaceHeaderAttributes?.Any() == true)
        {
            GenerateInterfaceHeaders(methodInfo);
        }

        var (cancellationTokenArg, _) = GetCancellationTokenParams(methodInfo);
        _requestBuilder.GenerateRequestExecution(_codeBuilder, methodInfo, cancellationTokenArg);

        _codeBuilder.AppendLine("        }");
        _codeBuilder.AppendLine();
    }


    private (string withComma, string withoutComma) GetCancellationTokenParams(MethodAnalysisResult methodInfo)
    {
        var cancellationTokenParam = methodInfo.Parameters.FirstOrDefault(p => p.Type.Contains("CancellationToken"));
        var paramValue = cancellationTokenParam?.Name;

        return (
            withComma: paramValue != null ? $", cancellationToken: {paramValue}" : "",
            withoutComma: paramValue ?? ""
        );
    }



    /// <summary>
    /// 生成接口定义的Header代码（支持Replace功能）
    /// </summary>
    private void GenerateInterfaceHeaders(MethodAnalysisResult methodInfo)
    {
        var hasTokenManager = !string.IsNullOrEmpty(_tokenManage);
        var hasAuthorizationHeader = TypeSymbolHelper.HasPropertyAttribute(_interfaceSymbol!, "Header", "Authorization");

        foreach (var interfaceHeader in methodInfo.InterfaceHeaderAttributes)
        {
            if (string.IsNullOrEmpty(interfaceHeader.Name))
                continue;

            // 跳过由Token管理器处理的Authorization Header
            if (hasTokenManager && hasAuthorizationHeader &&
                interfaceHeader.Name.Equals("Authorization", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // 跳过没有值的Header（由Token管理器处理或参数动态生成）
            if (interfaceHeader.Value == null)
            {
                continue;
            }

            var headerValue = interfaceHeader.Value?.ToString() ?? "null";

            if (interfaceHeader.Replace)
            {
                // 如果需要替换已存在的Header，先删除再添加
                _codeBuilder.AppendLine($"            // 替换接口定义的Header: {interfaceHeader.Name}");
                _codeBuilder.AppendLine($"            if (request.Headers.Contains(\"{interfaceHeader.Name}\"))");
                _codeBuilder.AppendLine($"                request.Headers.Remove(\"{interfaceHeader.Name}\");");
                _codeBuilder.AppendLine($"            request.Headers.Add(\"{interfaceHeader.Name}\", \"{headerValue}\");");
            }
            else
            {
                // 添加有固定值的Header
                _codeBuilder.AppendLine($"            // 添加接口定义的Header: {interfaceHeader.Name}");
                _codeBuilder.AppendLine($"            request.Headers.Add(\"{interfaceHeader.Name}\", \"{headerValue}\");");
            }
        }
    }
}