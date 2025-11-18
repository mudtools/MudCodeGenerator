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

    protected virtual string[] ApiWrapAttributeNames() => GeneratorConstants.HttpClientApiAttributeNames;

    /// <summary>
    /// 初始化源代码生成器
    /// </summary>
    /// <param name="context">初始化上下文</param>
    public override void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // 使用自定义方法查找标记了[HttpClientApi]的接口
        var interfaceDeclarations = GetClassDeclarationProvider<InterfaceDeclarationSyntax>(context, ApiWrapAttributeNames());

        // 组合编译和接口声明
        var compilationAndInterfaces = context.CompilationProvider.Combine(interfaceDeclarations);

        // 注册源生成
        context.RegisterSourceOutput(compilationAndInterfaces,
             (spc, source) => ExecuteGenerator(source.Left, source.Right, spc));
    }

    /// <summary>
    /// 执行源代码生成逻辑
    /// </summary>
    /// <param name="compilation">编译信息</param>
    /// <param name="interfaces">接口声明数组</param>
    /// <param name="context">源代码生成上下文</param>
    protected abstract void ExecuteGenerator(Compilation compilation, ImmutableArray<InterfaceDeclarationSyntax> interfaces, SourceProductionContext context);

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
    /// 获取包装类名称
    /// </summary>
    protected string GetWrapClassName(string wrapInterfaceName)
    {
        if (string.IsNullOrEmpty(wrapInterfaceName))
            return "NullOrEmptyWrapInterfaceName";

        if (wrapInterfaceName.StartsWith("I", StringComparison.Ordinal) && wrapInterfaceName.Length > 1)
        {
            return wrapInterfaceName.Substring(1);
        }
        return wrapInterfaceName + GeneratorConstants.DefaultWrapSuffix;
    }

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
    protected AttributeData? GetHttpClientApiAttribute(INamedTypeSymbol interfaceSymbol)
    {
        if (interfaceSymbol == null)
            return null;
        return interfaceSymbol.GetAttributes()
            .FirstOrDefault(a => GeneratorConstants.HttpClientApiAttributeNames.Contains(a.AttributeClass?.Name));
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
    /// 从特性获取ContentType
    /// </summary>
    protected string GetContentTypeFromAttribute(AttributeData attribute)
    {
        if (attribute == null)
            return "application/json; charset=utf-8";
        var contentTypeArg = attribute.NamedArguments.FirstOrDefault(a => a.Key == "ContentType");
        var contentType = contentTypeArg.Value.Value?.ToString();
        return string.IsNullOrEmpty(contentType) ? "application/json; charset=utf-8" : contentType;
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
            .Any(attr => GeneratorConstants.SupportedHttpMethods.Contains(attr.AttributeClass?.Name));
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
                              .Any(attr => GeneratorConstants.SupportedHttpMethods.Contains(attr.AttributeClass?.Name)));
    }

    protected AttributeSyntax? FindHttpMethodAttribute(MethodDeclarationSyntax methodSyntax)
    {
        if (methodSyntax == null)
            return null;
        return methodSyntax.AttributeLists
            .SelectMany(a => a.Attributes)
            .FirstOrDefault(a => GeneratorConstants.SupportedHttpMethods.Contains(a.Name.ToString()));
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
    /// 获取方法的声明语法
    /// </summary>
    protected MethodDeclarationSyntax? GetMethodDeclarationSyntax(IMethodSymbol method, InterfaceDeclarationSyntax interfaceDecl)
    {
        if (interfaceDecl == null)
            return null;

        if (method == null)
            return null;
        return interfaceDecl.Members
            .OfType<MethodDeclarationSyntax>()
            .FirstOrDefault(m => m.Identifier.ValueText == method.Name);
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
    #endregion

    #region AnalyzeMethod
    /// <summary>
    /// 分析函数符号，并返回<see cref="MethodAnalysisResult"/>分析结果。。
    /// </summary>
    /// <param name="compilation"></param>
    /// <param name="methodSymbol"></param>
    /// <param name="interfaceDecl"></param>
    /// <returns></returns>
    protected MethodAnalysisResult AnalyzeMethod(Compilation compilation, IMethodSymbol methodSymbol, InterfaceDeclarationSyntax interfaceDecl)
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
            var parameterInfo = new ParameterInfo
            {
                Name = p.Name,
                Type = p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
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

        return new MethodAnalysisResult
        {
            InterfaceName = interfaceDecl.Identifier.Text,
            IsValid = true,
            MethodName = methodSymbol.Name,
            HttpMethod = httpMethod,
            UrlTemplate = urlTemplate,
            ReturnType = GetReturnTypeDisplayString(methodSymbol.ReturnType),
            AsyncInnerReturnType = GetAsyncInnerReturnType(methodSymbol.ReturnType),
            IsAsyncMethod = IsAsyncReturnType(methodSymbol.ReturnType),
            Parameters = parameters
        };
    }

    protected MethodDeclarationSyntax? FindMethodSyntax(Compilation compilation, IMethodSymbol methodSymbol, InterfaceDeclarationSyntax interfaceDecl)
    {
        if (interfaceDecl == null || methodSymbol == null)
            return null;

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
        // 返回完整的原始返回类型
        return returnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    }

    /// <summary>
    /// 获取异步方法的内部返回类型
    /// </summary>
    private string GetAsyncInnerReturnType(ITypeSymbol returnType)
    {
        if (returnType is INamedTypeSymbol namedType && namedType.IsGenericType)
        {
            if (namedType.Name == "Task" && namedType.TypeArguments.Length == 1)
            {
                var genericType = namedType.TypeArguments[0];
                return genericType is INamedTypeSymbol genericNamedType &&
                       genericNamedType.IsGenericType &&
                       genericNamedType.Name == "Nullable"
                    ? $"{genericNamedType.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}?"
                    : genericType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            }
        }

        // 如果是 Task 而不是 Task<T>，返回 void
        if (returnType is INamedTypeSymbol taskNamedType &&
            taskNamedType.Name == "Task" &&
            taskNamedType.TypeArguments.Length == 0)
        {
            return "void";
        }

        return returnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
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
            .FirstOrDefault(attr => GeneratorConstants.TokenAttributeNames.Contains(attr.AttributeClass?.Name));

        if (tokenAttribute != null && tokenAttribute.ConstructorArguments.Length > 0)
        {
            var tokenTypeValue = tokenAttribute.ConstructorArguments[0].Value;
            if (tokenTypeValue != null)
            {
                // 处理枚举值转换
                var enumValue = Convert.ToInt32(tokenTypeValue);
                return enumValue switch
                {
                    0 => "TenantAccessToken",
                    1 => "UserAccessToken",
                    2 => "Both",
                    _ => "TenantAccessToken"
                };
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
        var valueRepresentation = matchingMember ?? Convert.ToInt64(defaultValue).ToString();

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

        #endregion
    }

}
