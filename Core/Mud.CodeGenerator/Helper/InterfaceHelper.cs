// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.CodeGenerator 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.CodeGenerator.Helper;

internal sealed class InterfaceHelper
{
    /// <summary>
    /// 获取方法参数列表字符串
    /// </summary>
    public static string GetParameterList(IMethodSymbol methodSymbol)
    {
        if (methodSymbol == null)
            return string.Empty;

        return string.Join(", ", methodSymbol.Parameters.Select(p => $"{p.Type} {p.Name}"));
    }

    public static bool HasInterfaceAttribute(INamedTypeSymbol interfaceSymbol, string attributeType, string attributeValue)
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
    /// 递归获取接口及其所有父接口的所有方法（去重）
    /// </summary>
    /// <param name="interfaceSymbol">接口符号</param>
    /// <param name="includeParentInterfaces">是否包含父接口的方法</param>
    public static IEnumerable<IMethodSymbol> GetAllInterfaceMethods(INamedTypeSymbol interfaceSymbol, bool includeParentInterfaces = true)
    {
        if (interfaceSymbol == null)
            return [];
        var visitedInterfaces = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
        return GetAllInterfaceMethodsRecursive(interfaceSymbol, visitedInterfaces, includeParentInterfaces);
    }

    private static IEnumerable<IMethodSymbol> GetAllInterfaceMethodsRecursive(INamedTypeSymbol interfaceSymbol, HashSet<INamedTypeSymbol> visitedInterfaces, bool includeParentInterfaces)
    {
        // 避免循环引用
        if (visitedInterfaces.Contains(interfaceSymbol))
            yield break;

        visitedInterfaces.Add(interfaceSymbol);

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
            foreach (var baseMethod in GetAllInterfaceMethodsRecursive(baseInterface, visitedInterfaces, includeParentInterfaces))
            {
                yield return baseMethod;
            }
        }
    }

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
}
