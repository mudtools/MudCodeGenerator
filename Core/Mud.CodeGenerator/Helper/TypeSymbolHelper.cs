// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.CodeGenerator 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.CodeGenerator.Helper;

internal sealed class TypeSymbolHelper
{
    #region 类型信息获取

    /// <summary>
    /// 获取类型或接口的完整命名空间
    /// </summary>
    /// <param name="compilation">编译对象</param>
    /// <param name="typeName">类型或接口名称</param>
    /// <returns>类型或接口的完整命名空间</returns>
    public static string GetTypeAllDisplayString(Compilation compilation, string typeName)
    {
        if (compilation != null)
        {
            var tokenManagerSymbol = compilation.GetTypeByMetadataName(typeName);
            if (tokenManagerSymbol != null)
            {
                return tokenManagerSymbol.ToDisplayString();
            }
        }

        // 如果没有找到，返回原始名称
        return typeName;
    }

    #endregion

    #region 方法信息处理

    /// <summary>
    /// 获取方法参数列表字符串（包含默认值、命名空间和可为空修饰符）
    /// </summary>
    public static string GetParameterList(IMethodSymbol methodSymbol)
    {
        if (methodSymbol == null)
            return string.Empty;

        // 基于FullyQualifiedFormat进行自定义
        var format = SymbolDisplayFormat.FullyQualifiedFormat
            .WithParameterOptions(
                SymbolDisplayParameterOptions.IncludeName |
                SymbolDisplayParameterOptions.IncludeType |
                SymbolDisplayParameterOptions.IncludeDefaultValue |
                SymbolDisplayParameterOptions.IncludeParamsRefOut)
            .WithMiscellaneousOptions(
                SymbolDisplayMiscellaneousOptions.UseSpecialTypes |
                SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier |
                SymbolDisplayMiscellaneousOptions.AllowDefaultLiteral);

        return string.Join(", ", methodSymbol.Parameters.Select(p =>
            p.ToDisplayString(format)));
    }


    /// <summary>
    /// 递归获取接口及其所有父接口的所有方法（去重）
    /// </summary>
    /// <param name="interfaceSymbol">接口符号</param>
    /// <param name="includeParentInterfaces">是否包含父接口的方法</param>
    /// <param name="excludedInterfaces">要排除的接口名称列表（可选）</param>
    public static IEnumerable<IMethodSymbol> GetAllMethods(
        INamedTypeSymbol interfaceSymbol,
        bool includeParentInterfaces = true,
        IEnumerable<string> excludedInterfaces = null)
    {
        if (interfaceSymbol == null)
            return [];

        var visitedInterfaces = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
        var excludedSet = excludedInterfaces.ToHashSet();
        return GetAllRecursive(interfaceSymbol, visitedInterfaces, includeParentInterfaces, excludedSet);
    }

    private static IEnumerable<IMethodSymbol> GetAllRecursive(
        INamedTypeSymbol interfaceSymbol,
        HashSet<INamedTypeSymbol> visitedInterfaces,
        bool includeParentInterfaces,
        HashSet<string> excludedInterfaces)
    {
        // 避免循环引用
        if (visitedInterfaces.Contains(interfaceSymbol))
            yield break;

        visitedInterfaces.Add(interfaceSymbol);

        // 检查当前接口是否在排除列表中
        if (excludedInterfaces != null && excludedInterfaces.Count > 0)
        {
            // 检查简单名称或完整名称是否在排除列表中
            if (ShouldExcludeInterface(interfaceSymbol, excludedInterfaces))
            {
                yield break; // 跳过整个接口及其方法
            }
        }

        // 首先处理当前接口的方法
        foreach (var method in interfaceSymbol.GetMembers().OfType<IMethodSymbol>())
        {
            yield return method;
        }

        // 如果不需要父接口的方法，则直接返回
        if (!includeParentInterfaces)
            yield break;

        // 然后递归处理所有父接口
        foreach (var baseInterface in interfaceSymbol.Interfaces)
        {
            foreach (var baseMethod in GetAllRecursive(
                baseInterface,
                visitedInterfaces,
                includeParentInterfaces,
                excludedInterfaces))
            {
                yield return baseMethod;
            }
        }
    }

    #endregion

    #region 属性特性处理

    /// <summary>
    /// 检查接口是否具有指定的特性
    /// </summary>
    /// <param name="interfaceSymbol">接口符号</param>
    /// <param name="attributeType">特性类型</param>
    /// <param name="attributeValue">特性值</param>
    /// <returns>如果接口具有指定特性返回true，否则返回false</returns>
    public static bool HasPropertyAttribute(INamedTypeSymbol interfaceSymbol, string attributeType, string attributeValue)
    {
        if (interfaceSymbol == null)
            return false;

        var attributeName = attributeType + "Attribute";

        return interfaceSymbol.GetAttributes()
                              .Any(attr =>
                                  (attr.AttributeClass?.Name == attributeName || attr.AttributeClass?.Name == attributeType) &&
                                  attr.ConstructorArguments.Length > 0 &&
                                  attr.ConstructorArguments[0].Value?.ToString() == attributeValue);
    }


    /// <summary>
    /// 判断属性是否具有特定特性
    /// </summary>
    public static bool HasPropertyAttribute(IPropertySymbol propertySymbol, string attributeName)
    {
        if (propertySymbol == null || string.IsNullOrEmpty(attributeName))
            return false;

        var fullAttributeName = attributeName.EndsWith("Attribute", StringComparison.Ordinal)
            ? attributeName
            : attributeName + "Attribute";

        return propertySymbol.GetAttributes()
            .Any(attr => attr.AttributeClass?.Name == fullAttributeName ||
                         attr.AttributeClass?.Name == attributeName);
    }
    #endregion

    #region 类名称生成

    /// <summary>
    /// 根据接口名称获取实现类名称
    /// </summary>
    /// <param name="interfaceName">接口名称</param>
    /// <returns>实现类名称</returns>
    /// <remarks>
    /// 如果接口名称以"I"开头且第二个字符为大写，则移除"I"前缀；否则添加"Impl"后缀
    /// </remarks>
    public static string GetImplementationClassName(string interfaceName)
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
    /// <param name="wrapInterfaceName">包装接口名称</param>
    /// <returns>包装类名称</returns>
    public static string GetWrapClassName(string wrapInterfaceName)
    {
        if (string.IsNullOrEmpty(wrapInterfaceName))
            return "NullOrEmptyWrapInterfaceName";

        if (wrapInterfaceName.StartsWith("I", StringComparison.Ordinal) && wrapInterfaceName.Length > 1)
        {
            return wrapInterfaceName.Substring(1);
        }
        return wrapInterfaceName + HttpClientGeneratorConstants.DefaultWrapSuffix;
    }

    #endregion

    #region 获取所有属性列表
    /// <summary>
    /// 递归获取接口及其所有父接口的所有属性（去重）
    /// </summary>
    /// <param name="interfaceSymbol">接口符号</param>
    /// <param name="includeParentInterfaces">是否包含父接口的属性</param>
    /// <param name="excludedInterfaces">要排除的接口名称列表（可选）</param>
    public static IEnumerable<IPropertySymbol> GetAllProperties(
        INamedTypeSymbol interfaceSymbol,
        bool includeParentInterfaces = true,
        IEnumerable<string> excludedInterfaces = null)
    {
        if (interfaceSymbol == null)
            return [];

        var visitedInterfaces = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
        var excludedSet = excludedInterfaces.ToHashSet();
        return GetAllPropertiesRecursive(interfaceSymbol, visitedInterfaces, includeParentInterfaces, excludedSet);
    }

    private static IEnumerable<IPropertySymbol> GetAllPropertiesRecursive(
        INamedTypeSymbol interfaceSymbol,
        HashSet<INamedTypeSymbol> visitedInterfaces,
        bool includeParentInterfaces,
        HashSet<string> excludedInterfaces)
    {
        // 避免循环引用
        if (visitedInterfaces.Contains(interfaceSymbol))
            yield break;

        visitedInterfaces.Add(interfaceSymbol);

        // 检查当前接口是否在排除列表中
        if (excludedInterfaces != null && excludedInterfaces.Count > 0)
        {
            if (ShouldExcludeInterface(interfaceSymbol, excludedInterfaces))
            {
                yield break; // 跳过整个接口及其属性
            }
        }

        // 首先处理当前接口的属性
        foreach (var property in interfaceSymbol.GetMembers().OfType<IPropertySymbol>())
        {
            yield return property;
        }

        // 如果不需要父接口的属性，则直接返回
        if (!includeParentInterfaces)
            yield break;

        // 然后递归处理所有父接口
        foreach (var baseInterface in interfaceSymbol.Interfaces)
        {
            foreach (var baseProperty in GetAllPropertiesRecursive(
                baseInterface,
                visitedInterfaces,
                includeParentInterfaces,
                excludedInterfaces))
            {
                yield return baseProperty;
            }
        }
    }

    /// <summary>
    /// 检查接口是否应该被排除（支持泛型）
    /// </summary>
    private static bool ShouldExcludeInterface(
        INamedTypeSymbol interfaceSymbol,
        HashSet<string> excludedInterfaces)
    {
        if (excludedInterfaces == null || excludedInterfaces.Count == 0)
            return false;

        // 检查各种可能的名称格式
        var namesToCheck = new List<string>
    {
        interfaceSymbol.Name,                              // 简单名称：IDisposable
        interfaceSymbol.ToDisplayString(),                 // 完整名称：System.IDisposable
        interfaceSymbol.MetadataName,                      // 元数据名称：IDisposable
        interfaceSymbol.ToString()                         // 字符串表示
    };

        // 添加命名空间和名称的组合
        if (!string.IsNullOrEmpty(interfaceSymbol.ContainingNamespace?.Name))
        {
            namesToCheck.Add($"{interfaceSymbol.ContainingNamespace}.{interfaceSymbol.Name}");
        }

        // 检查泛型接口
        if (interfaceSymbol.IsGenericType)
        {
            // 添加泛型定义
            var originalDefinition = interfaceSymbol.OriginalDefinition;
            namesToCheck.Add(originalDefinition.ToDisplayString());
            namesToCheck.Add(originalDefinition.MetadataName);

            // 添加无参数版本的泛型名称
            var genericNameWithoutArity = interfaceSymbol.Name;
            if (genericNameWithoutArity.Contains('`'))
            {
                namesToCheck.Add(genericNameWithoutArity.Substring(0, genericNameWithoutArity.IndexOf('`')));
            }
        }

        // 检查是否有匹配的排除项
        foreach (var name in namesToCheck)
        {
            if (excludedInterfaces.Contains(name))
                return true;
        }

        return false;
    }
    #endregion

    #region Type Display Helpers

    /// <summary>
    /// 获取参数类型的显示字符串，正确处理多维数组、交错数组和泛型类型
    /// </summary>
    /// <param name="typeSymbol">类型符号</param>
    /// <returns>正确的类型显示字符串</returns>
    public static string GetParameterTypeDisplayString(ITypeSymbol typeSymbol)
    {
        if (typeSymbol == null)
            return string.Empty;

        // 处理数组类型（包括多维数组和交错数组）
        if (typeSymbol is IArrayTypeSymbol arrayTypeSymbol)
        {
            return GetArrayTypeDisplayString(arrayTypeSymbol);
        }

        // 处理指针类型
        if (typeSymbol is IPointerTypeSymbol pointerTypeSymbol)
        {
            return GetParameterTypeDisplayString(pointerTypeSymbol.PointedAtType) + "*";
        }

        // 处理可为null的值类型（Nullable<T>）
        if (typeSymbol.IsValueType && typeSymbol.NullableAnnotation == NullableAnnotation.Annotated)
        {
            var underlyingType = ((INamedTypeSymbol)typeSymbol).TypeArguments[0];
            return GetParameterTypeDisplayString(underlyingType) + "?";
        }

        // 对于非数组类型，使用适当的显示格式
        var displayFormat = new SymbolDisplayFormat(
            globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes |
                               SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier,
            parameterOptions: SymbolDisplayParameterOptions.IncludeType,
            propertyStyle: SymbolDisplayPropertyStyle.ShowReadWriteDescriptor,
            localOptions: SymbolDisplayLocalOptions.IncludeType,
            memberOptions: SymbolDisplayMemberOptions.IncludeType |
                         SymbolDisplayMemberOptions.IncludeParameters |
                         SymbolDisplayMemberOptions.IncludeContainingType,
            delegateStyle: SymbolDisplayDelegateStyle.NameAndSignature,
            extensionMethodStyle: SymbolDisplayExtensionMethodStyle.StaticMethod);

        return typeSymbol.ToDisplayString(displayFormat);
    }

    /// <summary>
    /// 专门处理数组类型的显示字符串
    /// </summary>
    private static string GetArrayTypeDisplayString(IArrayTypeSymbol arrayTypeSymbol)
    {
        // 递归获取元素类型的显示字符串
        var elementTypeDisplay = GetParameterTypeDisplayString(arrayTypeSymbol.ElementType);

        // 处理多维数组
        if (arrayTypeSymbol.Rank > 1)
        {
            // 对于多维数组，使用逗号表示维度，例如 int[,] 或 string[,,]
            var commas = new string(',', arrayTypeSymbol.Rank - 1);
            return $"{elementTypeDisplay}[{commas}]";
        }
        // 处理一维数组（包括交错数组）
        else
        {
            // 对于一维数组，检查元素类型是否也是数组（交错数组）
            if (arrayTypeSymbol.ElementType is IArrayTypeSymbol)
            {
                // 交错数组：int[][], string[][][] 等
                // 元素类型已经包含了自己的[]，所以这里不需要额外处理
                // 但需要确保格式正确，例如 int[][] 而不是 int[] []
                return elementTypeDisplay + "[]";
            }
            else
            {
                // 普通一维数组
                return $"{elementTypeDisplay}[]";
            }
        }
    }

    #endregion

    #region 对象类型判断

    /// <summary>
    /// 检查是否为.net基本数据类型
    /// </summary>
    /// <param name="typeName">类型名称</param>
    /// <returns>如果是基本类型返回true，否则返回false</returns>
    public static bool IsBasicType(string typeName)
    {
        return typeName switch
        {
            "string" or "string?" => true,
            "int" or "int?" => true,
            "short" or "short?" => true,
            "long" or "long?" => true,
            "float" or "float?" => true,
            "double" or "double?" => true,
            "decimal" or "decimal?" => true,
            "bool" or "bool?" => true,
            "byte" or "byte?" => true,
            "char" or "char?" => true,
            "uint" or "uint?" => true,
            "ushort" or "ushort?" => true,
            "ulong" or "ulong?" => true,
            "sbyte" or "sbyte?" => true,
            "object" or "object?" => true,
            _ => false
        };
    }

    /// <summary>
    /// 通过语义分析判断类型是否为.net枚举类型
    /// </summary>
    /// <param name="typeSymbol">类型符号</param>
    /// <returns>如果是枚举类型返回true，否则返回false</returns>
    public static bool IsEnumType(ITypeSymbol typeSymbol)
    {
        if (typeSymbol == null)
            return false;
        // 首先检查是否是直接的枚举类型
        if (typeSymbol.TypeKind == TypeKind.Enum)
            return true;

        // 如果是可空类型，获取其底层类型再检查
        if (typeSymbol is INamedTypeSymbol namedType && namedType.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T)
        {
            var underlyingType = namedType.TypeArguments.FirstOrDefault();
            return underlyingType?.TypeKind == TypeKind.Enum;
        }

        return false;
    }

    /// <summary>
    /// 通过语义分析判断类型是否为复杂对象类型
    /// </summary>
    /// <param name="typeSymbol">类型符号</param>
    /// <returns>如果是复杂对象类型返回true，否则返回false</returns>
    public static bool IsComplexObjectType(ITypeSymbol typeSymbol)
    {
        if (typeSymbol == null)
            return false;
        // 首先检查是否是直接的枚举类型
        if (typeSymbol.TypeKind == TypeKind.Interface)
            return true;
        // 如果是可空类型，获取其底层类型再检查
        if (typeSymbol is INamedTypeSymbol namedType &&
            namedType.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T)
        {
            var underlyingType = namedType.TypeArguments.FirstOrDefault();
            return underlyingType?.TypeKind == TypeKind.Interface;
        }

        return false;
    }
    #endregion
}
