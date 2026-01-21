// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.CodeGenerator 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.CodeGenerator;

namespace Mud.ServiceCodeGenerator.ComWrap;

/// <summary>
/// 参数处理上下文类，封装参数处理所需的全部信息
/// </summary>
internal sealed class ParameterProcessingContext
{
    /// <summary>
    /// 参数符号
    /// </summary>
    public IParameterSymbol Parameter { get; }

    /// <summary>
    /// 参数类型完整名称
    /// </summary>
    public string ParameterType { get; }

    /// <summary>
    /// 是否为枚举类型
    /// </summary>
    public bool IsEnumType { get; }

    /// <summary>
    /// 是否为复杂对象类型（需要包装的类型）
    /// </summary>
    public bool IsObjectType { get; }

    /// <summary>
    /// 是否有 ConvertTriState 特性
    /// </summary>
    public bool HasConvertTriState { get; }

    /// <summary>
    /// 是否需要转换为整数
    /// </summary>
    public bool ConvertToInteger { get; }

    /// <summary>
    /// COM 命名空间
    /// </summary>
    public string ComNamespace { get; }

    /// <summary>
    /// 枚举默认值名称
    /// </summary>
    public string EnumValueName { get; }

    /// <summary>
    /// 实现类类型名称
    /// </summary>
    public string ConstructType { get; }

    /// <summary>
    /// 是否为 out 参数
    /// </summary>
    public bool IsOut { get; }

    /// <summary>
    /// 是否为 ref 参数
    /// </summary>
    public bool IsRef { get; }

    /// <summary>
    /// 是否为可空类型
    /// </summary>
    public bool IsNullable { get; }

    /// <summary>
    /// 默认值字面量
    /// </summary>
    public string? DefaultValueLiteral { get; }

    private ParameterProcessingContext(
        IParameterSymbol parameter,
        string parameterType,
        bool isEnumType,
        bool isObjectType,
        bool hasConvertTriState,
        bool convertToInteger,
        string comNamespace,
        string enumValueName,
        string constructType,
        bool isOut,
        bool isRef,
        bool isNullable,
        string? defaultValueLiteral)
    {
        Parameter = parameter;
        ParameterType = parameterType;
        IsEnumType = isEnumType;
        IsObjectType = isObjectType;
        HasConvertTriState = hasConvertTriState;
        ConvertToInteger = convertToInteger;
        ComNamespace = comNamespace;
        EnumValueName = enumValueName;
        ConstructType = constructType;
        IsOut = isOut;
        IsRef = isRef;
        IsNullable = isNullable;
        DefaultValueLiteral = defaultValueLiteral;
    }

    /// <summary>
    /// 为方法参数创建处理上下文
    /// </summary>
    /// <param name="parameter">参数符号</param>
    /// <param name="interfaceSymbol">接口符号</param>
    /// <param name="interfaceDeclaration">接口声明</param>
    /// <param name="getDefaultValueFunc">获取默认值的函数</param>
    /// <returns>参数处理上下文</returns>
    public static ParameterProcessingContext CreateForMethodParameter(
        IParameterSymbol parameter,
        INamedTypeSymbol interfaceSymbol,
        InterfaceDeclarationSyntax interfaceDeclaration,
        Func<InterfaceDeclarationSyntax, ISymbol, ITypeSymbol, string> getDefaultValueFunc)
    {
        var parameterType = TypeSymbolHelper.GetTypeFullName(parameter.Type);
        var isEnumType = TypeSymbolHelper.IsEnumType(parameter.Type);
        var isObjectType = TypeSymbolHelper.IsComplexObjectType(parameter.Type);
        var hasConvertTriState = HasConvertTriStateAttribute(parameter);
        var convertToInteger = AttributeDataHelper.HasAttribute(parameter, ComWrapConstants.ConvertIntAttributeNames);
        var isOut = parameter.RefKind == RefKind.Out;
        var isRef = parameter.RefKind == RefKind.Ref;
        var isNullable = parameter.Type.NullableAnnotation == NullableAnnotation.Annotated ||
                        parameterType.EndsWith("?", StringComparison.Ordinal);

        var defaultValue = getDefaultValueFunc(interfaceDeclaration, parameter, parameter.Type);
        
        // 对于枚举类型，保留完整的枚举类型名称（包括类型名），不提取成员名
        // 这样在生成代码时可以使用完整路径：MsWord.WdSaveOptions.wdPromptToSaveChanges
        var enumValueName = string.Empty;
        var comNamespace = string.Empty;
        
        if (isEnumType)
        {
            enumValueName = defaultValue;  // 保留完整的枚举值名称，如 "ComObjectWrapTest.WdSaveOptions.wdPromptToSaveChanges"
            comNamespace = GetComNamespace(parameter, interfaceSymbol, interfaceDeclaration);  // 仍然需要获取 COM 命名空间
        }
        else
        {
            enumValueName = GetEnumValueWithoutNamespace(defaultValue);
            comNamespace = GetComNamespace(parameter, interfaceSymbol, interfaceDeclaration);
        }

        // 对于枚举类型，直接使用类型名，不需要转换为实现类型
        var constructType = isEnumType
            ? TypeSymbolHelper.GetTypeFullName(parameter.Type)
            : GetImplementationType(TypeSymbolHelper.GetTypeFullName(parameter.Type));

        string? defaultValueLiteral = null;
        if (parameter.HasExplicitDefaultValue)
        {
            defaultValueLiteral = GetParameterDefaultValueLiteral(parameter);
        }

        return new ParameterProcessingContext(
            parameter,
            parameterType,
            isEnumType,
            isObjectType,
            hasConvertTriState,
            convertToInteger,
            comNamespace,
            enumValueName,
            constructType,
            isOut,
            isRef,
            isNullable,
            defaultValueLiteral);
    }

    /// <summary>
    /// 为方法参数创建处理上下文（简化版，使用内置GetDefaultValue逻辑）
    /// </summary>
    /// <param name="parameter">参数符号</param>
    /// <param name="interfaceSymbol">接口符号</param>
    /// <param name="interfaceDeclaration">接口声明</param>
    /// <returns>参数处理上下文</returns>
    public static ParameterProcessingContext CreateForMethodParameter(
        IParameterSymbol parameter,
        INamedTypeSymbol interfaceSymbol,
        InterfaceDeclarationSyntax interfaceDeclaration)
    {
        return CreateForMethodParameter(parameter, interfaceSymbol, interfaceDeclaration, GetDefaultValue);
    }

    /// <summary>
    /// 获取参数的默认值（兼容基类实现）
    /// </summary>
    private static string GetDefaultValue(InterfaceDeclarationSyntax interfaceDeclaration, ISymbol symbol, ITypeSymbol typeSymbol)
    {
        if (interfaceDeclaration == null || symbol == null || typeSymbol == null)
            return "default";

        if (typeSymbol.Kind == SymbolKind.ErrorType)
            return "default";

        // 尝试从特性中获取显式指定的默认值
        var defaultValue = GetDefaultValueFromAttribute(interfaceDeclaration, symbol);
        if (!string.IsNullOrEmpty(defaultValue) && defaultValue != "default")
        {
            return defaultValue;
        }

        // 如果没有显式指定，基于类型推断合理的默认值
        return InferDefaultValue(typeSymbol);
    }

    /// <summary>
    /// 从特性中获取默认值
    /// </summary>
    private static string GetDefaultValueFromAttribute(InterfaceDeclarationSyntax interfaceDeclaration, ISymbol symbol)
    {
        // 对于参数，如果有显式默认值，则返回该值的字面量表示
        if (symbol.Kind == SymbolKind.Parameter && symbol is IParameterSymbol parameter)
        {
            if (parameter.HasExplicitDefaultValue)
            {
                return GetParameterDefaultValueLiteral(parameter);
            }
            return "default";
        }

        // 查找对应的属性声明
        SyntaxNode declaration = null;
        if (symbol.Kind == SymbolKind.Property)
        {
            declaration = interfaceDeclaration.DescendantNodes()
                .OfType<PropertyDeclarationSyntax>()
                .FirstOrDefault(prop => prop.Identifier.Text == symbol.Name);
        }

        if (declaration == null)
            return "default";

        // 查找相关的特性
        string? attributeName = null;
        if (symbol.Kind == SymbolKind.Property)
        {
            attributeName = ComWrapConstants.ComPropertyWrapAttributeNames.FirstOrDefault(attr =>
                AttributeSyntaxHelper.GetAttributeSyntaxes(
                    declaration as PropertyDeclarationSyntax, attr).Any());
        }

        if (string.IsNullOrEmpty(attributeName))
            return "default";

        var attributes = AttributeSyntaxHelper.GetAttributeSyntaxes(
            declaration as PropertyDeclarationSyntax, attributeName);
        if (attributes == null || !attributes.Any())
            return "default";

        var defaultValue = attributes[0].GetPropertyValue("DefaultValue", null)?.ToString();
        if (defaultValue == null)
            return "default";

        // 处理 nameof() 表达式
        if (defaultValue.StartsWith("nameof(", StringComparison.OrdinalIgnoreCase) &&
            defaultValue.EndsWith(")", StringComparison.OrdinalIgnoreCase))
        {
            var nameofContent = defaultValue.Substring(7, defaultValue.Length - 8);
            return nameofContent.Trim();
        }

        // 处理字符串字面量，移除引号
        return defaultValue.Trim('"');
    }

    /// <summary>
    /// 基于类型推断合理的默认值
    /// </summary>
    private static string InferDefaultValue(ITypeSymbol typeSymbol)
    {
        if (typeSymbol == null)
            return "default";

        var typeName = TypeSymbolHelper.GetTypeFullName(typeSymbol);

        // 处理可空类型
        if (typeName.EndsWith("?", StringComparison.Ordinal))
        {
            return "null";
        }

        // 处理枚举类型
        if (typeSymbol.TypeKind == TypeKind.Enum)
        {
            return InferEnumDefaultValue(typeSymbol);
        }

        // 处理复杂对象类型
        if (TypeSymbolHelper.IsComplexObjectType(typeSymbol))
        {
            return "null";
        }

        // 处理基本类型
        var specialType = typeSymbol.SpecialType;
        return specialType switch
        {
            SpecialType.System_String => "string.Empty",
            SpecialType.System_Boolean => "false",
            SpecialType.System_Char => "'\\0'",
            SpecialType.System_SByte => "0",
            SpecialType.System_Byte => "0",
            SpecialType.System_Int16 => "0",
            SpecialType.System_UInt16 => "0",
            SpecialType.System_Int32 => "0",
            SpecialType.System_UInt32 => "0",
            SpecialType.System_Int64 => "0L",
            SpecialType.System_UInt64 => "0UL",
            SpecialType.System_Single => "0.0f",
            SpecialType.System_Double => "0.0",
            SpecialType.System_Decimal => "0.0m",
            _ => "default"
        };
    }

    /// <summary>
    /// 推断枚举类型的默认值
    /// </summary>
    private static string InferEnumDefaultValue(ITypeSymbol enumType)
    {
        var typeName = TypeSymbolHelper.GetTypeFullName(enumType);

        // 尝试获取第一个枚举成员
        var firstMember = enumType.GetMembers()
            .OfType<IFieldSymbol>()
            .FirstOrDefault(f => f.HasConstantValue);

        if (firstMember != null)
        {
            return $"{typeName}.{firstMember.Name}";
        }

        // 降级：返回类型名本身
        return typeName;
    }

    /// <summary>
    /// 检查参数是否有 [ConvertTriState] 特性
    /// </summary>
    private static bool HasConvertTriStateAttribute(IParameterSymbol parameter)
    {
        return parameter.GetAttributes().Any(attr =>
            attr.AttributeClass?.Name == "ConvertTriStateAttribute");
    }

    /// <summary>
    /// 获取 COM 命名空间（支持参数级别的覆盖）
    /// </summary>
    private static string GetComNamespace(
        IParameterSymbol parameter,
        INamedTypeSymbol interfaceSymbol,
        InterfaceDeclarationSyntax interfaceDeclaration)
    {
        // 检查参数级别的 ComNamespace 特性
        var paramComNamespace = AttributeDataHelper.GetStringValueFromSymbol(
            parameter,
            ComWrapConstants.ComNamespaceAttributes,
            "Name",
            "");

        if (!string.IsNullOrEmpty(paramComNamespace))
            return paramComNamespace;

        // 从接口特性中获取 COM 命名空间（支持 ComObjectWrap 和 ComCollectionWrap）
        var interfaceNamespace = AttributeDataHelper.GetStringValueFromSymbol(
            interfaceSymbol,
            [.. ComWrapConstants.ComObjectWrapAttributeNames, .. ComWrapConstants.ComCollectionWrapAttributeNames],
            ComWrapConstants.ComNamespaceProperty,
            "");

        return !string.IsNullOrEmpty(interfaceNamespace)
            ? interfaceNamespace
            : SyntaxHelper.GetNamespaceName(interfaceDeclaration);
    }

    /// <summary>
    /// 从枚举值中移除命名空间
    /// </summary>
    private static string GetEnumValueWithoutNamespace(string enumValue)
    {
        if (string.IsNullOrEmpty(enumValue))
            return enumValue;

        var lastDotIndex = enumValue.LastIndexOf('.');
        return lastDotIndex >= 0
            ? enumValue.Substring(lastDotIndex + 1)
            : enumValue;
    }

    /// <summary>
    /// 获取参数默认值的字面量表示
    /// </summary>
    private static string? GetParameterDefaultValueLiteral(IParameterSymbol parameter)
    {
        if (!parameter.HasExplicitDefaultValue)
            return null;

        var value = parameter.ExplicitDefaultValue;
        if (value == null)
            return "null";

        var typeSymbol = parameter.Type;
        var typeName = TypeSymbolHelper.GetTypeFullName(parameter.Type);

        // 处理枚举类型
        if (typeSymbol.TypeKind == TypeKind.Enum)
        {
            return TypeSymbolHelper.GetEnumValueLiteral(typeSymbol, value);
        }

        // 处理可空枚举类型
        if (typeName.EndsWith("?", StringComparison.Ordinal) && TypeSymbolHelper.IsEnumType(parameter.Type))
        {
            if (parameter.Type is INamedTypeSymbol namedType &&
                namedType.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T &&
                namedType.TypeArguments.Length > 0)
            {
                var enumType = namedType.TypeArguments[0];
                return TypeSymbolHelper.GetEnumValueLiteral(enumType, value);
            }
        }

        // 处理基本类型
        return typeName switch
        {
            "bool" => value.ToString()!.ToLowerInvariant(),
            "int" or "float" or "double" or "decimal" => value.ToString()!,
            "string" => $"\"{EscapeString(value.ToString()!)}\"",
            _ when typeName.EndsWith("?", StringComparison.Ordinal) => HandleNullableDefaultValue(typeName, value),
            _ => value.ToString()!
        };
    }

    /// <summary>
    /// 处理可空类型的默认值
    /// </summary>
    private static string HandleNullableDefaultValue(string type, object value)
    {
        if (value == null)
            return "null";

        var nonNullType = type.TrimEnd('?');
        return nonNullType switch
        {
            "bool" => value.ToString()!.ToLowerInvariant(),
            "int" or "float" or "double" or "decimal" => value.ToString()!,
            "string" => $"\"{EscapeString(value.ToString()!)}\"",
            _ => value.ToString()!
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
    /// 获取实现类类型名称（移除接口前缀）
    /// </summary>
    private static string GetImplementationType(string interfaceTypeName)
    {
        return NamingHelper.RemoveInterfacePrefix(interfaceTypeName);
    }

    /// <summary>
    /// 公开的静态方法，用于获取默认值（供外部调用）
    /// </summary>
    public static string GetDefaultValueStatic(InterfaceDeclarationSyntax interfaceDeclaration, ISymbol symbol, ITypeSymbol typeSymbol)
    {
        return GetDefaultValue(interfaceDeclaration, symbol, typeSymbol);
    }
}
