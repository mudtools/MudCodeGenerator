// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.CodeGenerator 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Globalization;

namespace Mud.HttpUtils.HttpInvoke.Helpers;

/// <summary>
/// 函数帮助类
/// </summary>
internal sealed class MethodHelper
{
    #region AnalyzeMethod
    /// <summary>
    /// 分析函数符号，并返回<see cref="MethodAnalysisResult"/>分析结果。。
    /// </summary>
    /// <param name="compilation"></param>
    /// <param name="methodSymbol"></param>
    /// <param name="interfaceDecl"></param>
    /// <returns></returns>
    public static MethodAnalysisResult AnalyzeMethod(Compilation compilation, IMethodSymbol methodSymbol, InterfaceDeclarationSyntax interfaceDecl)
    {
        var methodSyntax = FindMethodSyntax(compilation, methodSymbol, interfaceDecl);
        if (interfaceDecl == null || methodSyntax == null || methodSymbol == null)
            return MethodAnalysisResult.Invalid;

        var httpMethodAttr = FindHttpMethodAttribute(methodSyntax);
        if (httpMethodAttr == null)
            return MethodAnalysisResult.Invalid;

        var httpMethod = httpMethodAttr.Name.ToString();
        var urlTemplate = GetAttributeArgumentValue(httpMethodAttr, 0)?.ToString().Trim('"') ?? "";

        // 获取方法级别的HttpContentType特性
        var methodContentType = GetHttpContentTypeFromSymbol(methodSymbol);

        var parameters = methodSymbol.Parameters.Select(p =>
        {
            var parameterInfo = new ParameterInfo
            {
                Name = p.Name,
                Type = TypeSymbolHelper.GetTypeFullName(p.Type),
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
        string? interfaceContentType = null;

        if (interfaceSymbol != null)
        {
            // 获取接口级别的HttpContentType特性
            interfaceContentType = GetHttpContentTypeFromSymbol(interfaceSymbol);

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

                // 检查是否为Authorization Header（无论使用AliasAs与否）
                var isAuthorizationHeader = AttributeDataHelper.GetStringValueFromAttribute(headerAttr, ["Name"], 0) == "Authorization";
                if (isAuthorizationHeader)
                {
                    // 保持与现有逻辑的兼容性，使用实际的headerName（可能包含AliasAs）
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
            ReturnType = TypeSymbolHelper.GetTypeFullName(methodSymbol.ReturnType),
            AsyncInnerReturnType = TypeSymbolHelper.ExtractAsyncInnerType(methodSymbol.ReturnType),
            IsAsyncMethod = TypeSymbolHelper.IsAsyncType(methodSymbol.ReturnType),
            Parameters = parameters,
            IgnoreImplement = HasMethodAttribute(methodSymbol, HttpClientGeneratorConstants.IgnoreImplementAttributeNames),
            IgnoreWrapInterface = HasMethodAttribute(methodSymbol, HttpClientGeneratorConstants.IgnoreWrapInterfaceAttributeNames),
            InterfaceAttributes = interfaceAttributes,
            InterfaceHeaderAttributes = interfaceHeaderAttributes,
            InterfaceContentType = interfaceContentType,
            MethodContentType = methodContentType
        };
    }
    private static AttributeSyntax? FindHttpMethodAttribute(MethodDeclarationSyntax methodSyntax)
    {
        if (methodSyntax == null)
            return null;

        // 尝试找到任何一个支持的HTTP方法特性
        foreach (var methodName in HttpClientGeneratorConstants.SupportedHttpMethods)
        {
            var attributes = AttributeSyntaxHelper.GetAttributeSyntaxes(methodSyntax, methodName);
            if (attributes.Any())
                return attributes[0];
        }

        return null;
    }
    /// <summary>
    /// 获取Header特性的Replace设置
    /// </summary>
    /// <param name="headerAttr">Header特性</param>
    /// <returns>是否替换已存在的Header</returns>
    private static bool GetHeaderReplace(AttributeData headerAttr)
    {
        return AttributeDataHelper.GetBoolValueFromAttribute(headerAttr, "Replace", false);
    }


    /// <summary>
    /// 检查方法是否具有指定的特性
    /// </summary>
    private static bool HasMethodAttribute(IMethodSymbol methodSymbol, params string[] attributeNames)
    {
        if (methodSymbol == null)
            return false;

        return methodSymbol.GetAttributes()
            .Any(attr => attributeNames.Contains(attr.AttributeClass?.Name));
    }

    /// <summary>
    /// 获取Header特性的名称
    /// </summary>
    /// <param name="headerAttr">Header特性</param>
    /// <returns>Header名称</returns>
    private static string GetHeaderName(AttributeData headerAttr)
    {
        return AttributeDataHelper.GetStringValueFromAttribute(headerAttr, ["AliasAs", "Name"], 0, "Unknown") ?? "Unknown";
    }

    /// <summary>
    /// 获取Header特性的值
    /// </summary>
    /// <param name="headerAttr">Header特性</param>
    /// <returns>Header值</returns>
    private static object? GetHeaderValue(AttributeData headerAttr)
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
    /// 从符号获取HttpContentType特性的ContentType值
    /// </summary>
    /// <param name="symbol">符号（方法或接口）</param>
    /// <returns>Content-Type值，如果未定义则返回null</returns>
    private static string? GetHttpContentTypeFromSymbol(ISymbol symbol)
    {
        if (symbol == null)
            return null;

        // 查找HttpContentType特性
        var httpContentTypeAttr = AttributeDataHelper.GetAttributeDataFromSymbol(
            symbol,
            HttpClientGeneratorConstants.HttpContentTypeAttributeNames);

        if (httpContentTypeAttr == null)
            return null;

        // 优先从构造函数参数获取
        if (httpContentTypeAttr.ConstructorArguments.Length > 0)
        {
            var constructorArg = httpContentTypeAttr.ConstructorArguments[0].Value?.ToString();
            if (!string.IsNullOrEmpty(constructorArg))
                return constructorArg;
        }

        // 从命名参数获取
        return AttributeDataHelper.GetStringValueFromAttribute(httpContentTypeAttr, ["ContentType"]);
    }

    /// <summary>
    /// 查询方法的语法对象。
    /// </summary>
    /// <param name="compilation"></param>
    /// <param name="methodSymbol"></param>
    /// <param name="interfaceDecl"></param>
    /// <returns></returns>
    public static MethodDeclarationSyntax? FindMethodSyntax(Compilation compilation, IMethodSymbol methodSymbol, InterfaceDeclarationSyntax interfaceDecl)
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
    public static IEnumerable<InterfaceDeclarationSyntax> GetAllBaseInterfaceSyntaxNodes(Compilation compilation, InterfaceDeclarationSyntax interfaceDecl)
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

    private static InterfaceDeclarationSyntax? GetInterfaceDeclarationSyntax(Compilation compilation, INamedTypeSymbol interfaceSymbol)
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

    private static object? GetAttributeArgumentValue(AttributeSyntax attribute, int index)
    {
        return attribute.GetConstructorArgument(null, index);
    }



    /// <summary>
    /// 获取参数的 Token 类型
    /// </summary>
    private static string GetTokenType(IParameterSymbol parameter)
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
    private static string GetDefaultValueLiteral(ITypeSymbol parameterType, object? defaultValue)
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
        return TypeSymbolHelper.GetEnumValueLiteral(enumType, defaultValue);
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
}
