// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.CodeGenerator 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Globalization;

namespace Mud.ServiceCodeGenerator;

/// <summary>
/// 生成Http调用代码的源代码生成器基类
/// </summary>
/// <remarks>
/// 提供Web API相关的公共功能，包括HttpClient特性处理、HTTP方法验证等
/// </remarks>
public abstract class HttpInvokeBaseSourceGenerator : TransitiveCodeGenerator
{
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

    protected virtual string[] ApiWrapAttributeNames() => HttpClientGeneratorConstants.HttpClientApiAttributeNames;

    /// <summary>
    /// 初始化源代码生成器
    /// </summary>
    /// <param name="context">初始化上下文</param>
    public override void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // 查找标记了[HttpClientApi]的接口声明
        var httpClientApiInterfaces = GetClassDeclarationProvider<InterfaceDeclarationSyntax>(context, ApiWrapAttributeNames());

        // 获取编译信息和分析器配置选项
        var compilationWithOptions = context.CompilationProvider
            .Combine(context.AnalyzerConfigOptionsProvider);

        // 组合所有需要的数据：编译信息、接口声明、配置选项
        var completeDataProvider = compilationWithOptions.Combine(httpClientApiInterfaces);

        // 注册源代码生成器
        context.RegisterSourceOutput(completeDataProvider,
            (ctx, provider) => ExecuteGenerator(
                compilation: provider.Left.Left,
                interfaces: provider.Right,
                context: ctx,
                configOptionsProvider: provider.Left.Right));
    }

    /// <summary>
    /// 执行源代码生成逻辑
    /// </summary>
    /// <param name="compilation">编译信息</param>
    /// <param name="interfaces">接口声明数组</param>
    /// <param name="context">源代码生成上下文</param>
    protected abstract void ExecuteGenerator(
        Compilation compilation,
        ImmutableArray<InterfaceDeclarationSyntax> interfaces,
        SourceProductionContext context,
        AnalyzerConfigOptionsProvider configOptionsProvider);

    /// <summary>
    /// 获取包装接口名称
    /// </summary>
    protected string GetWrapInterfaceName(INamedTypeSymbol interfaceSymbol, AttributeData wrapAttribute)
    {
        string GetDefalultWrapInterfaceName(string interfaceName)
        {
            if (string.IsNullOrEmpty(interfaceName))
                return "NullOrEmptyWrapInterfaceName";

            if (interfaceName.EndsWith("api", StringComparison.OrdinalIgnoreCase))
                return interfaceName.Substring(0, interfaceName.Length - 3) + "Wrap";
            return interfaceName + "Wrap";
        }

        if (interfaceSymbol == null) return null;
        if (wrapAttribute == null) return GetDefalultWrapInterfaceName(interfaceSymbol.Name);

        // 检查特性参数中是否有指定的包装接口名称
        var wrapInterfaceArg = wrapAttribute.NamedArguments.FirstOrDefault(a => a.Key == "WrapInterface");
        if (!string.IsNullOrEmpty(wrapInterfaceArg.Value.Value?.ToString()))
        {
            return wrapInterfaceArg.Value.Value.ToString();
        }

        // 默认在接口名称后添加"Wrap"
        return GetDefalultWrapInterfaceName(interfaceSymbol.Name);
    }

    /// <summary>
    /// 获取HttpClientApi特性
    /// </summary>
    public AttributeData? GetHttpClientApiAttribute(INamedTypeSymbol interfaceSymbol)
    {
        if (interfaceSymbol == null)
            return null;
        return interfaceSymbol.GetAttributes()
            .FirstOrDefault(a => HttpClientGeneratorConstants.HttpClientApiAttributeNames.Contains(a.AttributeClass?.Name));
    }

    /// <summary>
    /// 从特性获取 BaseAddress
    /// </summary>
    /// <param name="httpClientApiAttribute">HttpClientApi特性</param>
    /// <returns>BaseAddress</returns>
    public string? GetBaseAddressFromAttribute(AttributeData? httpClientApiAttribute)
    {
        if (httpClientApiAttribute == null)
            return null;

        // 优先检查命名参数 BaseAddress
        var baseAddressArg = httpClientApiAttribute.NamedArguments.FirstOrDefault(a => a.Key == "BaseAddress");
        if (baseAddressArg.Value.Value is string namedBaseAddress && !string.IsNullOrEmpty(namedBaseAddress))
            return namedBaseAddress;

        // 检查构造函数参数（兼容旧版本）
        if (httpClientApiAttribute.ConstructorArguments.Length > 0 && httpClientApiAttribute.ConstructorArguments[0].Value is string baseAddress)
            return baseAddress;

        return null;
    }

    /// <summary>
    /// 从特性获取超时时间
    /// </summary>
    public int GetTimeoutFromAttribute(AttributeData attribute)
    {
        if (attribute == null)
            return 100;

        // 优先检查 TimeoutSeconds 命名参数
        var timeoutSecondsArg = attribute.NamedArguments.FirstOrDefault(a => a.Key == "TimeoutSeconds");
        if (timeoutSecondsArg.Value.Value is int timeoutSecondsValue)
            return timeoutSecondsValue;

        // 检查 Timeout 命名参数（兼容其他特性）
        var timeoutArg = attribute.NamedArguments.FirstOrDefault(a => a.Key == "Timeout");
        return timeoutArg.Value.Value is int value ? value : 100; // 默认100秒
    }

    /// <summary>
    /// 从特性获取超时时间（专用方法，查找TimeoutSeconds参数）
    /// </summary>
    /// <param name="httpClientApiAttribute">HttpClientApi特性</param>
    /// <returns>超时秒数</returns>
    protected int GetHttpClientApiTimeoutFromAttribute(AttributeData? httpClientApiAttribute)
    {
        if (httpClientApiAttribute?.NamedArguments.FirstOrDefault(k => k.Key == "TimeoutSeconds").Value.Value is int timeout)
            return timeout;

        return 30; // 默认30秒超时
    }


    /// <summary>
    /// 从特性获取注册组名称
    /// </summary>
    protected string? GetRegistryGroupNameFromAttribute(AttributeData attribute)
    {
        if (attribute == null)
            return null;
        var registryGroupNameArg = attribute.NamedArguments.FirstOrDefault(a => a.Key == "RegistryGroupName");
        return registryGroupNameArg.Value.Value?.ToString();
    }

    /// <summary>
    /// 从特性获取Token管理器接口名称
    /// </summary>
    /// <param name="httpClientApiAttribute">HttpClientApi特性</param>
    /// <returns>Token管理器接口名称</returns>
    public string? GetTokenManageFromAttribute(AttributeData? httpClientApiAttribute)
    {
        if (httpClientApiAttribute?.NamedArguments.FirstOrDefault(k => k.Key == "TokenManage").Value.Value is string tokenManage)
            return tokenManage;

        return null;
    }

    /// <summary>
    /// 从特性获取IsAbstract属性值
    /// </summary>
    /// <param name="httpClientApiAttribute">HttpClientApi特性</param>
    /// <returns>是否为抽象类</returns>
    public bool GetIsAbstractFromAttribute(AttributeData? httpClientApiAttribute)
    {
        if (httpClientApiAttribute?.NamedArguments.FirstOrDefault(k => k.Key == "IsAbstract").Value.Value is bool isAbstract)
            return isAbstract;

        return false; // 默认值为false
    }

    /// <summary>
    /// 从特性获取InheritedFrom属性值
    /// </summary>
    /// <param name="httpClientApiAttribute">HttpClientApi特性</param>
    /// <returns>父类名称</returns>
    public string? GetInheritedFromFromAttribute(AttributeData? httpClientApiAttribute)
    {
        if (httpClientApiAttribute?.NamedArguments.FirstOrDefault(k => k.Key == "InheritedFrom").Value.Value is string inheritedFrom)
            return inheritedFrom;

        return null; // 默认值为空字符串
    }

    /// <summary>
    /// 获取Token管理器接口的完整命名空间
    /// </summary>
    /// <param name="compilation">编译对象</param>
    /// <param name="tokenManageInterfaceName">Token管理器接口名称</param>
    /// <returns>Token管理器接口的完整类型名称</returns>
    public string GetTokenManagerType(Compilation compilation, string tokenManageInterfaceName)
    {
        if (compilation != null)
        {
            var tokenManagerSymbol = compilation.GetTypeByMetadataName(tokenManageInterfaceName);
            if (tokenManagerSymbol != null)
            {
                return tokenManagerSymbol.ToDisplayString();
            }
        }

        // 如果没有找到，返回原始名称
        return tokenManageInterfaceName;
    }

    /// <summary>
    /// 检查方法是否有效
    /// </summary>
    protected bool IsValidMethod(IMethodSymbol method)
    {
        if (method == null)
            return false;
        return method.GetAttributes()
            .Any(attr => HttpClientGeneratorConstants.SupportedHttpMethods.Contains(attr.AttributeClass?.Name));
    }

    protected AttributeSyntax? FindHttpMethodAttribute(MethodDeclarationSyntax methodSyntax)
    {
        if (methodSyntax == null)
            return null;
        return methodSyntax.AttributeLists
            .SelectMany(a => a.Attributes)
            .FirstOrDefault(a => HttpClientGeneratorConstants.SupportedHttpMethods.Contains(a.Name.ToString()));
    }

    #region Common Utility Methods
    /// <summary>
    /// 获取XML文档注释
    /// </summary>
    protected string GetXmlDocumentation(SyntaxNode syntaxNode)
    {
        if (syntaxNode == null)
            return string.Empty;

        var leadingTrivia = syntaxNode.GetLeadingTrivia();
        var xmlDocTrivia = leadingTrivia.FirstOrDefault(t => t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) ||
                                                             t.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia));

        if (xmlDocTrivia != default)
        {
            return xmlDocTrivia.ToFullString();
        }

        return string.Empty;
    }

    /// <summary>
    /// 检查参数是否具有指定的特性
    /// </summary>
    protected bool HasAttribute(ParameterInfo parameter, params string[] attributeNames)
    {
        if (parameter == null)
            return false;
        if (parameter.Attributes == null || parameter.Attributes.Count == 0)
            return false;
        return parameter.Attributes
            .Any(attr => attributeNames.Contains(attr.Name));
    }

    /// <summary>
    /// 根据特性名称过滤参数
    /// </summary>
    protected IReadOnlyList<ParameterInfo> FilterParametersByAttribute(IReadOnlyList<ParameterInfo> parameters, string[] attributeNames, bool exclude = false)
    {
        return exclude
            ? parameters.Where(p => !HasAttribute(p, attributeNames)).ToList()
            : parameters.Where(p => HasAttribute(p, attributeNames)).ToList();
    }

    /// <summary>
    /// 检查方法是否具有指定的特性
    /// </summary>
    protected bool HasMethodAttribute(IMethodSymbol methodSymbol, params string[] attributeNames)
    {
        if (methodSymbol == null)
            return false;

        return methodSymbol.GetAttributes()
            .Any(attr => attributeNames.Contains(attr.AttributeClass?.Name));
    }

    /// <summary>
    /// 生成方法参数列表字符串
    /// </summary>
    protected string GenerateParameterList(IReadOnlyList<ParameterInfo> parameters)
    {
        if (parameters == null || !parameters.Any())
            return string.Empty;

        var parameterStrings = parameters.Select(parameter =>
        {
            var parameterStr = $"{parameter.Type} {parameter.Name}";

            // 处理可选参数
            if (parameter.HasDefaultValue && !string.IsNullOrEmpty(parameter.DefaultValueLiteral))
            {
                parameterStr += $" = {parameter.DefaultValueLiteral}";
            }

            return parameterStr;
        });

        return string.Join(", ", parameterStrings);
    }

    /// <summary>
    /// 生成正确的参数调用列表，确保token参数替换掉原来标记了[Token]特性的参数位置
    /// </summary>
    protected IReadOnlyList<string> GenerateCorrectParameterCallList(IReadOnlyList<ParameterInfo> originalParameters, IReadOnlyList<ParameterInfo> filteredParameters, string tokenParameterName)
    {
        var callParameters = new List<string>();

        foreach (var originalParam in originalParameters)
        {
            // 检查当前参数是否是Token参数
            if (HasAttribute(originalParam, HttpClientGeneratorConstants.TokenAttributeNames))
            {
                // 如果是Token参数，用token参数替换
                callParameters.Add(tokenParameterName);
            }
            else
            {
                // 如果不是Token参数，检查是否在过滤后的参数列表中
                var matchingFilteredParam = filteredParameters.FirstOrDefault(p => p.Name == originalParam.Name);
                if (matchingFilteredParam != null)
                {
                    callParameters.Add(matchingFilteredParam.Name);
                }
            }
        }

        return callParameters;
    }
    #endregion

    #region AnalyzeMethod
    /// <summary>
    /// 分析函数符号，并返回<see cref="MethodAnalysisResult"/>分析结果。。
    /// </summary>
    /// <param name="compilation"></param>
    /// <param name="methodSymbol"></param>
    /// <param name="interfaceDecl"></param>
    /// <returns></returns>
    public MethodAnalysisResult AnalyzeMethod(Compilation compilation, IMethodSymbol methodSymbol, InterfaceDeclarationSyntax interfaceDecl)
    {
        var methodSyntax = FindMethodSyntax(compilation, methodSymbol, interfaceDecl);
        if (interfaceDecl == null || methodSyntax == null || methodSymbol == null)
            return MethodAnalysisResult.Invalid;

        var httpMethodAttr = FindHttpMethodAttribute(methodSyntax);
        if (httpMethodAttr == null)
            return MethodAnalysisResult.Invalid;

        var httpMethod = httpMethodAttr.Name.ToString();
        var urlTemplate = GetAttributeArgumentValue(httpMethodAttr, 0)?.ToString().Trim('"') ?? "";

        var parameters = methodSymbol.Parameters.Select(p =>
        {
            // 创建一个包含可空类型符号的显示格式
            var parameterFormat = new SymbolDisplayFormat(
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier
            );

            var parameterInfo = new ParameterInfo
            {
                Name = p.Name,
                Type = p.Type.ToDisplayString(parameterFormat),
                Attributes = p.GetAttributes().Select(attr => new ParameterAttributeInfo
                {
                    Name = attr.AttributeClass?.Name ?? "",
                    Arguments = attr.ConstructorArguments.Select(arg => arg.Value).ToArray(),
                    NamedArguments = attr.NamedArguments.ToDictionary(na => na.Key, na => na.Value.Value)
                }).ToList(),
                HasDefaultValue = p.HasExplicitDefaultValue,
                TokenType = GetTokenType(p)
            };

            if (p.HasExplicitDefaultValue)
            {
                parameterInfo.DefaultValue = p.ExplicitDefaultValue;
                parameterInfo.DefaultValueLiteral = GetDefaultValueLiteral(p.Type, p.ExplicitDefaultValue);
            }

            return parameterInfo;
        }).ToList();

        // 分析接口特性
        var interfaceSymbol = compilation.GetSemanticModel(interfaceDecl.SyntaxTree).GetDeclaredSymbol(interfaceDecl) as INamedTypeSymbol;
        var interfaceAttributes = new HashSet<string>();
        var interfaceHeaderAttributes = new List<InterfaceHeaderAttribute>();

        if (interfaceSymbol != null)
        {
            // 检查并处理所有[Header]特性
            var headerAttributes = interfaceSymbol.GetAttributes()
                .Where(attr => (attr.AttributeClass?.Name == "HeaderAttribute" || attr.AttributeClass?.Name == "Header"));

            foreach (var headerAttr in headerAttributes)
            {
                // 获取Header名称
                var headerName = GetHeaderName(headerAttr);

                // 创建接口Header特性信息
                var interfaceHeaderAttr = new InterfaceHeaderAttribute
                {
                    Name = headerName,
                    Value = GetHeaderValue(headerAttr),
                    Replace = GetHeaderReplace(headerAttr)
                };

                interfaceHeaderAttributes.Add(interfaceHeaderAttr);

                // 保持与现有逻辑的兼容性，如果是Authorization Header，继续添加到InterfaceAttributes中
                if (headerName == "Authorization")
                {
                    interfaceAttributes.Add($"Header:{headerName}");
                }
            }

            // 检查并处理[Query]特性
            var queryAttributes = interfaceSymbol.GetAttributes()
                .Where(attr => (attr.AttributeClass?.Name == "QueryAttribute" || attr.AttributeClass?.Name == "Query") &&
                               attr.ConstructorArguments.Length > 0 &&
                               attr.ConstructorArguments[0].Value?.ToString() == "Authorization");

            foreach (var queryAttr in queryAttributes)
            {
                // 优先使用AliasAs属性
                var aliasAs = queryAttr.NamedArguments.FirstOrDefault(arg => arg.Key == "AliasAs").Value.Value?.ToString();
                var queryName = string.IsNullOrEmpty(aliasAs) ? "Authorization" : aliasAs;
                interfaceAttributes.Add($"Query:{queryName}");
            }
        }

        return new MethodAnalysisResult
        {
            MethodOwnerInterfaceName = methodSymbol.ContainingType?.Name ?? interfaceDecl.Identifier.Text,
            CurrentInterfaceName = interfaceDecl.Identifier.Text,
            IsValid = true,
            MethodName = methodSymbol.Name,
            HttpMethod = httpMethod,
            UrlTemplate = urlTemplate,
            ReturnType = GetReturnTypeDisplayString(methodSymbol.ReturnType),
            AsyncInnerReturnType = GetAsyncInnerReturnType(methodSymbol.ReturnType),
            IsAsyncMethod = IsAsyncReturnType(methodSymbol.ReturnType),
            Parameters = parameters,
            IgnoreImplement = HasMethodAttribute(methodSymbol, HttpClientGeneratorConstants.IgnoreImplementAttributeNames),
            IgnoreWrapInterface = HasMethodAttribute(methodSymbol, HttpClientGeneratorConstants.IgnoreWrapInterfaceAttributeNames),
            InterfaceAttributes = interfaceAttributes,
            InterfaceHeaderAttributes = interfaceHeaderAttributes
        };
    }

    /// <summary>
    /// 查询方法的语法对象。
    /// </summary>
    /// <param name="compilation"></param>
    /// <param name="methodSymbol"></param>
    /// <param name="interfaceDecl"></param>
    /// <returns></returns>
    protected MethodDeclarationSyntax? FindMethodSyntax(Compilation compilation, IMethodSymbol methodSymbol, InterfaceDeclarationSyntax interfaceDecl)
    {
        if (interfaceDecl == null || methodSymbol == null || compilation == null)
            return null;

        // 获取当前接口及其所有基接口的语法节点
        var allInterfaces = GetAllBaseInterfaceSyntaxNodes(compilation, interfaceDecl);

        foreach (var interfaceSyntax in allInterfaces)
        {
            var method = interfaceSyntax.Members
                .OfType<MethodDeclarationSyntax>()
                .FirstOrDefault(m =>
                {
                    var model = compilation.GetSemanticModel(m.SyntaxTree);
                    var methodSymbolFromSyntax = model.GetDeclaredSymbol(m);
                    return methodSymbolFromSyntax?.Equals(methodSymbol, SymbolEqualityComparer.Default) == true;
                });

            if (method != null)
                return method;
        }

        return null;
    }

    /// <summary>
    /// 获取接口及其所有基接口的语法节点
    /// </summary>
    private IEnumerable<InterfaceDeclarationSyntax> GetAllBaseInterfaceSyntaxNodes(Compilation compilation, InterfaceDeclarationSyntax interfaceDecl)
    {
        yield return interfaceDecl;

        var semanticModel = compilation.GetSemanticModel(interfaceDecl.SyntaxTree);
        var interfaceSymbol = semanticModel.GetDeclaredSymbol(interfaceDecl);

        if (interfaceSymbol == null)
            yield break;

        foreach (var baseInterface in interfaceSymbol.Interfaces)
        {
            var baseInterfaceSyntax = GetInterfaceDeclarationSyntax(compilation, baseInterface);
            if (baseInterfaceSyntax != null)
            {
                yield return baseInterfaceSyntax;

                // 递归获取更深层的基接口
                foreach (var deeperBase in GetAllBaseInterfaceSyntaxNodes(compilation, baseInterfaceSyntax))
                {
                    yield return deeperBase;
                }
            }
        }
    }

    private InterfaceDeclarationSyntax? GetInterfaceDeclarationSyntax(Compilation compilation, INamedTypeSymbol interfaceSymbol)
    {
        foreach (var syntaxReference in interfaceSymbol.DeclaringSyntaxReferences)
        {
            var syntax = syntaxReference.GetSyntax();
            if (syntax is InterfaceDeclarationSyntax interfaceDecl)
            {
                return interfaceDecl;
            }
        }
        return null;
    }

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
        // 创建一个包含可空类型符号的显示格式
        var format = new SymbolDisplayFormat(
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier
        );

        return returnType.ToDisplayString(format);
    }

    /// <summary>
    /// 获取异步方法的内部返回类型
    /// </summary>
    private string GetAsyncInnerReturnType(ITypeSymbol returnType)
    {
        // 创建一个包含可空类型符号的显示格式
        var format = new SymbolDisplayFormat(
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier
        );

        if (returnType is INamedTypeSymbol namedType && namedType.IsGenericType)
        {
            if (namedType.Name == "Task" && namedType.TypeArguments.Length == 1)
            {
                var genericType = namedType.TypeArguments[0];
                return genericType is INamedTypeSymbol genericNamedType &&
                       genericNamedType.IsGenericType &&
                       genericNamedType.Name == "Nullable"
                    ? $"{genericNamedType.TypeArguments[0].ToDisplayString(format)}?"
                    : genericType.ToDisplayString(format);
            }
        }

        // 如果是 Task 而不是 Task<T>，返回 void
        if (returnType is INamedTypeSymbol taskNamedType &&
            taskNamedType.Name == "Task" &&
            taskNamedType.TypeArguments.Length == 0)
        {
            return "void";
        }

        return returnType.ToDisplayString(format);
    }

    /// <summary>
    /// 判断返回类型是否为异步类型（Task 或 Task<T>）
    /// </summary>
    private bool IsAsyncReturnType(ITypeSymbol returnType)
    {
        if (returnType is INamedTypeSymbol namedType)
        {
            return namedType.Name == "Task" &&
                   (namedType.TypeArguments.Length == 0 || namedType.TypeArguments.Length == 1);
        }
        return false;
    }

    /// <summary>
    /// 获取参数的 Token 类型
    /// </summary>
    private string GetTokenType(IParameterSymbol parameter)
    {
        var tokenAttribute = parameter.GetAttributes()
            .FirstOrDefault(attr => HttpClientGeneratorConstants.TokenAttributeNames.Contains(attr.AttributeClass?.Name));

        if (tokenAttribute == null)
            return "TenantAccessToken";

        // 首先检查命名参数 TokenType
        var namedTokenType = tokenAttribute.NamedArguments
            .FirstOrDefault(na => na.Key.Equals("TokenType", StringComparison.OrdinalIgnoreCase)).Value.Value;

        if (namedTokenType != null)
        {
            return ConvertTokenEnumValueToString(Convert.ToInt32(namedTokenType, CultureInfo.InvariantCulture));
        }

        // 然后检查构造函数参数
        if (tokenAttribute.ConstructorArguments.Length > 0)
        {
            var tokenTypeValue = tokenAttribute.ConstructorArguments[0].Value;
            if (tokenTypeValue != null)
            {
                return ConvertTokenEnumValueToString(Convert.ToInt32(tokenTypeValue, CultureInfo.InvariantCulture));
            }
        }

        // 默认返回 TenantAccessToken
        return "TenantAccessToken";
    }

    /// <summary>
    /// 获取参数默认值的字面量表示
    /// </summary>
    /// <param name="parameterType">参数类型</param>
    /// <param name="defaultValue">默认值</param>
    /// <returns>默认值的字面量表示</returns>
    private string GetDefaultValueLiteral(ITypeSymbol parameterType, object? defaultValue)
    {
        if (defaultValue == null)
        {
            return parameterType.ToDisplayString() == "System.Threading.CancellationToken"
                ? "default"
                : "null";
        }

        switch (parameterType.SpecialType)
        {
            case SpecialType.System_String:
                return $"\"{EscapeString(defaultValue.ToString()!)}\"";
            case SpecialType.System_Boolean:
                return defaultValue.ToString()!.ToLowerInvariant();
            case SpecialType.System_Char:
                return $"'{EscapeChar((char)defaultValue)}'";
            case SpecialType.System_Int16:
            case SpecialType.System_Int32:
            case SpecialType.System_Int64:
            case SpecialType.System_Byte:
            case SpecialType.System_Single:
            case SpecialType.System_Double:
            case SpecialType.System_Decimal:
                return defaultValue.ToString()!;
        }

        // 处理枚举类型
        if (parameterType is INamedTypeSymbol { TypeKind: TypeKind.Enum } namedType)
        {
            return GetEnumLiteral(namedType, defaultValue);
        }

        // 默认处理为字符串
        return $"\"{EscapeString(defaultValue.ToString()!)}\"";
    }

    /// <summary>
    /// 获取枚举值的字面量表示
    /// </summary>
    private static string GetEnumLiteral(INamedTypeSymbol enumType, object defaultValue)
    {
        var enumTypeName = enumType.ToDisplayString();

        // 查找匹配的枚举成员
        var matchingMember = enumType.GetMembers()
            .OfType<IFieldSymbol>()
            .Where(f => f.IsConst && f.HasConstantValue && Equals(f.ConstantValue, defaultValue))
            .Select(f => f.Name)
            .FirstOrDefault();

        // 如果找到匹配成员，使用成员名；否则使用数值
        var valueRepresentation = matchingMember ?? Convert.ToInt64(defaultValue, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);

        return $"{enumTypeName}.{valueRepresentation}";
    }

    /// <summary>
    /// 转义字符串中的特殊字符
    /// </summary>
    private static string EscapeString(string value)
    {
        return value.Replace("\\", "\\\\")
                    .Replace("\"", "\\\"")
                    .Replace("\0", "\\0")
                    .Replace("\a", "\\a")
                    .Replace("\b", "\\b")
                    .Replace("\f", "\\f")
                    .Replace("\n", "\\n")
                    .Replace("\r", "\\r")
                    .Replace("\t", "\\t")
                    .Replace("\v", "\\v");
    }

    /// <summary>
    /// 转义字符中的特殊字符
    /// </summary>
    private static string EscapeChar(char value)
    {
        return value switch
        {
            '\\' => "\\\\",
            '\'' => "\\'",
            '\0' => "\\0",
            '\a' => "\\a",
            '\b' => "\\b",
            '\f' => "\\f",
            '\n' => "\\n",
            '\r' => "\\r",
            '\t' => "\\t",
            '\v' => "\\v",
            _ => value.ToString()
        };
    }

    #endregion

    #region Header Attribute Helper Methods

    /// <summary>
    /// 从特性中获取字符串参数值，按优先级顺序检查命名参数、构造函数参数
    /// </summary>
    /// <param name="attribute">特性数据</param>
    /// <param name="namedParameterNames">命名参数名称列表（按优先级排序）</param>
    /// <param name="constructorParameterIndex">构造函数参数索引</param>
    /// <param name="defaultValue">默认值</param>
    /// <returns>参数值</returns>
    private static string? GetStringValueFromAttribute(AttributeData attribute, string[] namedParameterNames, int constructorParameterIndex = -1, string? defaultValue = null)
    {
        if (attribute == null)
            return defaultValue;

        // 检查命名参数
        foreach (var paramName in namedParameterNames)
        {
            var namedArg = attribute.NamedArguments.FirstOrDefault(arg => arg.Key.Equals(paramName, StringComparison.OrdinalIgnoreCase));
            if (namedArg.Key != null && namedArg.Value.Value?.ToString() is string namedValue && !string.IsNullOrEmpty(namedValue))
                return namedValue;
        }

        // 检查构造函数参数
        if (constructorParameterIndex >= 0 && attribute.ConstructorArguments.Length > constructorParameterIndex)
        {
            var constructorArg = attribute.ConstructorArguments[constructorParameterIndex].Value?.ToString();
            if (!string.IsNullOrEmpty(constructorArg))
                return constructorArg;
        }

        return defaultValue;
    }

    /// <summary>
    /// 将Token类型枚举值转换为字符串
    /// </summary>
    /// <param name="enumValue">枚举值</param>
    /// <returns>Token类型字符串</returns>
    private static string ConvertTokenEnumValueToString(int enumValue)
    {
        return enumValue switch
        {
            0 => "TenantAccessToken",
            1 => "UserAccessToken",
            2 => "Both",
            _ => "TenantAccessToken"
        };
    }

    /// <summary>
    /// 获取Header特性的名称
    /// </summary>
    /// <param name="headerAttr">Header特性</param>
    /// <returns>Header名称</returns>
    private string GetHeaderName(AttributeData headerAttr)
    {
        return GetStringValueFromAttribute(headerAttr, ["AliasAs", "Name"], 0, "Unknown") ?? "Unknown";
    }

    /// <summary>
    /// 获取Header特性的值
    /// </summary>
    /// <param name="headerAttr">Header特性</param>
    /// <returns>Header值</returns>
    private object? GetHeaderValue(AttributeData headerAttr)
    {
        // 优先检查Value命名参数
        var valueProperty = headerAttr.NamedArguments.FirstOrDefault(arg => arg.Key == "Value").Value.Value;
        if (valueProperty != null)
            return valueProperty;

        // 检查构造函数参数（第二个参数）
        if (headerAttr.ConstructorArguments.Length > 1)
        {
            return headerAttr.ConstructorArguments[1].Value;
        }

        return null;
    }

    /// <summary>
    /// 从特性中获取布尔值参数
    /// </summary>
    /// <param name="attribute">特性数据</param>
    /// <param name="parameterName">参数名称</param>
    /// <param name="defaultValue">默认值</param>
    /// <returns>布尔值</returns>
    private static bool GetBoolValueFromAttribute(AttributeData attribute, string parameterName, bool defaultValue = false)
    {
        if (attribute == null)
            return defaultValue;

        var namedArg = attribute.NamedArguments.FirstOrDefault(arg => arg.Key.Equals(parameterName, StringComparison.OrdinalIgnoreCase));
        if (namedArg.Key != null && namedArg.Value.Value != null)
        {
            return bool.TryParse(namedArg.Value.Value.ToString(), out var result) ? result : defaultValue;
        }

        return defaultValue;
    }

    /// <summary>
    /// 获取Header特性的Replace设置
    /// </summary>
    /// <param name="headerAttr">Header特性</param>
    /// <returns>是否替换已存在的Header</returns>
    private bool GetHeaderReplace(AttributeData headerAttr)
    {
        return GetBoolValueFromAttribute(headerAttr, "Replace", false);
    }

    #endregion
}