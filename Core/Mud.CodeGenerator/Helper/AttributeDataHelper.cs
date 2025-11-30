// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.CodeGenerator 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.CodeGenerator.Helper;

internal sealed class AttributeDataHelper
{
    /// <summary>
    /// 从特性获取<see cref="int"/>值。
    /// </summary>
    public static int GetIntValueFromAttribute(AttributeData attribute, string propertyName, int defaultVal = 0)
    {
        if (attribute == null)
            return defaultVal;

        var timeoutArg = attribute.NamedArguments
            .FirstOrDefault(a => a.Key.Equals(propertyName, StringComparison.OrdinalIgnoreCase));
        return timeoutArg.Value.Value is int value ? value : defaultVal;
    }

    /// <summary>
    /// 从特性获取注册组名称
    /// </summary>
    public static string? GetStringValueFromAttribute(AttributeData attribute, string propertyName)
    {
        if (attribute == null)
            return null;
        var registryGroupNameArg = attribute.NamedArguments
            .FirstOrDefault(a => a.Key.Equals(propertyName, StringComparison.OrdinalIgnoreCase));
        return registryGroupNameArg.Value.Value?.ToString();
    }


    /// <summary>
    /// 从特性获取IsAbstract属性值
    /// </summary>
    /// <param name="httpClientApiAttribute">HttpClientApi特性</param>
    /// <returns>是否为抽象类</returns>
    public static bool GetBoolValueFromAttribute(AttributeData? httpClientApiAttribute, string propertyName, bool defaultValue = false)
    {
        if (httpClientApiAttribute?.NamedArguments
            .FirstOrDefault(k => k.Key.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
            .Value.Value is bool isAbstract)
            return isAbstract;

        return defaultValue;
    }

    /// <summary>
    /// 从特性获取 BaseAddress，同时兼容构造函数参数方式
    /// </summary>
    /// <param name="httpClientApiAttribute">HttpClientApi特性</param>
    /// <returns>BaseAddress</returns>
    public static string? GetStringValueFromAttributeConstructor(AttributeData? httpClientApiAttribute, string propertyName)
    {
        if (httpClientApiAttribute == null)
            return null;

        // 优先检查命名参数 BaseAddress
        var baseAddressArg = GetStringValueFromAttribute(httpClientApiAttribute, propertyName);
        if (!string.IsNullOrEmpty(baseAddressArg))
            return baseAddressArg;

        // 检查构造函数参数（兼容旧版本）
        if (httpClientApiAttribute.ConstructorArguments.Length > 0 && httpClientApiAttribute.ConstructorArguments[0].Value is string baseAddress)
            return baseAddress;

        return null;
    }

    /// <summary>
    /// 获取HttpClientApi特性
    /// </summary>
    public static AttributeData? GetAttributeDataFromSymbol(INamedTypeSymbol interfaceSymbol, string[] attributeNames)
    {
        if (interfaceSymbol == null || attributeNames == null)
            return null;
        if (attributeNames.Length < 1)
            return null;
        return interfaceSymbol.GetAttributes()
            .FirstOrDefault(a => attributeNames.Contains(a.AttributeClass?.Name));
    }
}
