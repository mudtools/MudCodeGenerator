using System.Collections.Immutable;

namespace Mud.ServiceCodeGenerator;

/// <summary>
/// Web API 源代码生成器基类
/// </summary>
/// <remarks>
/// 提供Web API相关的公共功能，包括HttpClient特性处理、HTTP方法验证等
/// </remarks>
public abstract class WebApiSourceGenerator : TransitiveCodeGenerator
{
    /// <summary>
    /// HttpClientApi特性名称数组
    /// </summary>
    private readonly string[] HttpClientApiAttributeName = ["HttpClientApiAttribute", "HttpClientApi"];

    /// <summary>
    /// 支持的HTTP方法名称数组
    /// </summary>
    protected static readonly string[] SupportedHttpMethods = ["Get", "GetAttribute", "Post", "PostAttribute", "Put", "PutAttribute", "Delete", "DeleteAttribute", "Patch", "PatchAttribute", "Head", "HeadAttribute", "Options", "OptionsAttribute"];

    /// <summary>
    /// 初始化源代码生成器
    /// </summary>
    /// <param name="context">初始化上下文</param>
    public override void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // 使用自定义方法查找标记了[HttpClientApi]的接口
        var interfaceDeclarations = GetClassDeclarationProvider<InterfaceDeclarationSyntax>(context, HttpClientApiAttributeName);

        // 组合编译和接口声明
        var compilationAndInterfaces = context.CompilationProvider.Combine(interfaceDeclarations);

        // 注册源生成
        context.RegisterSourceOutput(compilationAndInterfaces,
             (spc, source) => Execute(source.Left, source.Right, spc));
    }

    /// <summary>
    /// 执行源代码生成逻辑
    /// </summary>
    /// <param name="compilation">编译信息</param>
    /// <param name="interfaces">接口声明数组</param>
    /// <param name="context">源代码生成上下文</param>
    protected abstract void Execute(Compilation compilation, ImmutableArray<InterfaceDeclarationSyntax> interfaces, SourceProductionContext context);

    /// <summary>
    /// 根据接口名称获取实现类名称
    /// </summary>
    /// <param name="interfaceName">接口名称</param>
    /// <returns>实现类名称</returns>
    /// <remarks>
    /// 如果接口名称以"I"开头且第二个字符为大写，则移除"I"前缀；否则添加"Impl"后缀
    /// </remarks>
    protected string GetImplementationClassName(string interfaceName)
    {
        if (string.IsNullOrEmpty(interfaceName))
            return "NullOrEmptyInterfaceName";

        return interfaceName.StartsWith("I", StringComparison.Ordinal) && interfaceName.Length > 1 && char.IsUpper(interfaceName[1])
            ? interfaceName.Substring(1)
            : interfaceName + "Impl";
    }


    /// <summary>
    /// 获取包装接口名称
    /// </summary>
    protected string GetWrapInterfaceName(INamedTypeSymbol interfaceSymbol, AttributeData wrapAttribute)
    {
        // 检查特性参数中是否有指定的包装接口名称
        var wrapInterfaceArg = wrapAttribute.NamedArguments.FirstOrDefault(a => a.Key == "WrapInterface");
        if (!string.IsNullOrEmpty(wrapInterfaceArg.Value.Value?.ToString()))
        {
            return wrapInterfaceArg.Value.Value.ToString();
        }

        // 根据接口名称生成默认包装接口名称
        var interfaceName = interfaceSymbol.Name;
        if (interfaceName.EndsWith("Api", StringComparison.OrdinalIgnoreCase))
        {
            return interfaceName.Substring(0, interfaceName.Length - 3);
        }
        else if (interfaceName.StartsWith("I", StringComparison.OrdinalIgnoreCase) && interfaceName.Length > 1)
        {
            return interfaceName.Substring(1);
        }

        return interfaceName + "Wrap";
    }


    /// <summary>
    /// 获取HttpClientApi特性
    /// </summary>
    protected AttributeData? GetHttpClientApiAttribute(INamedTypeSymbol interfaceSymbol)
    {
        if (interfaceSymbol == null)
            return null;
        return interfaceSymbol.GetAttributes()
            .FirstOrDefault(a => HttpClientApiAttributeName.Contains(a.AttributeClass?.Name));
    }

    /// <summary>
    /// 从特性获取基地址
    /// </summary>
    protected string GetBaseUrlFromAttribute(AttributeData attribute)
    {
        if (attribute == null)
            return string.Empty;

        var baseUrlArg = attribute.ConstructorArguments.FirstOrDefault();
        return baseUrlArg.Value?.ToString() ?? string.Empty;
    }

    /// <summary>
    /// 从特性获取超时时间
    /// </summary>
    protected int GetTimeoutFromAttribute(AttributeData attribute)
    {
        if (attribute == null)
            return 100;
        var timeoutArg = attribute.NamedArguments.FirstOrDefault(a => a.Key == "Timeout");
        return timeoutArg.Value.Value is int value ? value : 100; // 默认100秒
    }



    /// <summary>
    /// 获取方法参数列表字符串
    /// </summary>
    protected string GetParameterList(IMethodSymbol methodSymbol)
    {
        if (methodSymbol == null)
            return string.Empty;

        return string.Join(", ", methodSymbol.Parameters.Select(p => $"{p.Type} {p.Name}"));
    }

    /// <summary>
    /// 检查方法是否有效
    /// </summary>
    protected bool IsValidMethod(IMethodSymbol method)
    {
        if (method == null)
            return false;
        return method.GetAttributes()
            .Any(attr => SupportedHttpMethods.Contains(attr.AttributeClass?.Name));
    }

    /// <summary>
    /// 验证接口是否包含有效的HTTP方法
    /// </summary>
    protected bool HasValidHttpMethods(INamedTypeSymbol interfaceSymbol)
    {
        if (interfaceSymbol == null)
            return false;

        return interfaceSymbol.GetMembers().OfType<IMethodSymbol>()
                              .Any(method => method.GetAttributes()
                              .Any(attr => SupportedHttpMethods.Contains(attr.AttributeClass?.Name)));
    }

    protected AttributeSyntax? FindHttpMethodAttribute(MethodDeclarationSyntax methodSyntax)
    {
        if (methodSyntax == null)
            return null;
        return methodSyntax.AttributeLists
            .SelectMany(a => a.Attributes)
            .FirstOrDefault(a => SupportedHttpMethods.Contains(a.Name.ToString()));
    }


    #region AnalyzeMethod

    protected MethodAnalysisResult AnalyzeMethod(Compilation compilation, IMethodSymbol methodSymbol, InterfaceDeclarationSyntax interfaceDecl)
    {
        var methodSyntax = FindMethodSyntax(compilation, methodSymbol, interfaceDecl);
        if (methodSyntax == null)
            return MethodAnalysisResult.Invalid;

        var httpMethodAttr = FindHttpMethodAttribute(methodSyntax);
        if (httpMethodAttr == null)
            return MethodAnalysisResult.Invalid;

        var httpMethod = httpMethodAttr.Name.ToString();
        var urlTemplate = GetAttributeArgumentValue(httpMethodAttr, 0)?.ToString().Trim('"') ?? "";

        var parameters = methodSymbol.Parameters.Select(p =>
        {
            var parameterInfo = new ParameterInfo
            {
                Name = p.Name,
                Type = p.Type.ToDisplayString(),
                Attributes = p.GetAttributes().Select(attr => new ParameterAttributeInfo
                {
                    Name = attr.AttributeClass?.Name ?? "",
                    Arguments = attr.ConstructorArguments.Select(arg => arg.Value).ToArray(),
                    NamedArguments = attr.NamedArguments.ToDictionary(na => na.Key, na => na.Value.Value)
                }).ToList(),
                HasDefaultValue = p.HasExplicitDefaultValue
            };

            if (p.HasExplicitDefaultValue)
            {
                parameterInfo.DefaultValue = p.ExplicitDefaultValue;
                parameterInfo.DefaultValueLiteral = GetDefaultValueLiteral(p.Type, p.ExplicitDefaultValue);
            }

            return parameterInfo;
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

    /// <summary>
    /// 获取参数默认值的字面量表示
    /// </summary>
    /// <param name="parameterType">参数类型</param>
    /// <param name="defaultValue">默认值</param>
    /// <returns>默认值的字面量表示</returns>
    private string GetDefaultValueLiteral(ITypeSymbol parameterType, object? defaultValue)
    {
        var typeName = parameterType.ToDisplayString();
        if (defaultValue == null && typeName == "System.Threading.CancellationToken")
            return "default";
        if (defaultValue == null)
            return "null";

        switch (typeName)
        {
            case "string":
                return $"\"{defaultValue}\"";
            case "bool":
                return defaultValue.ToString()?.ToLowerInvariant() ?? "false";
            case "char":
                return $"'{(char)defaultValue}'";
            case "int":
            case "long":
            case "short":
            case "byte":
            case "float":
            case "double":
            case "decimal":
                return defaultValue.ToString() ?? "0";
            default:
                // 处理枚举类型
                if (parameterType is INamedTypeSymbol namedType && namedType.TypeKind == TypeKind.Enum)
                {
                    var enumTypeName = namedType.ToDisplayString();

                    // 使用Roslyn符号系统获取枚举成员
                    var enumMembers = namedType.GetMembers()
                        .OfType<IFieldSymbol>()
                        .Where(f => f.IsConst && f.HasConstantValue && Equals(f.ConstantValue, defaultValue))
                        .ToList();

                    var enumValueName = enumMembers.Any()
                        ? enumMembers.First().Name
                        : defaultValue?.ToString() ?? "0";

                    return $"{enumTypeName}.{enumValueName}";
                }

                // 默认处理为字符串
                return $"\"{defaultValue}\"";
        }
    }

    #endregion
}



/// <summary>
/// 方法分析结果
/// </summary>
/// <remarks>
/// 用于存储接口方法的分析信息，包括 HTTP 方法、URL 模板、参数等。
/// </remarks>
public class MethodAnalysisResult
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
/// 存储方法参数的详细信息，包括参数名、类型、特性和默认值。
/// </remarks>
public class ParameterInfo
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

    /// <summary>
    /// 是否具有默认值
    /// </summary>
    public bool HasDefaultValue { get; set; }

    /// <summary>
    /// 默认值
    /// </summary>
    public object? DefaultValue { get; set; }

    /// <summary>
    /// 默认值的字面量表示
    /// </summary>
    public string? DefaultValueLiteral { get; set; }
}

/// <summary>
/// 参数特性信息
/// </summary>
/// <remarks>
/// 存储参数特性的详细信息，包括特性名称、构造函数参数和命名参数。
/// </remarks>
public class ParameterAttributeInfo
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
