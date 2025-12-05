// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.CodeGenerator 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.CodeGenerator.Helper;

/// <summary>
/// 特性数据辅助类，提供从特性数据中提取常用类型值的静态方法。
/// </summary>
internal static class AttributeDataHelper
{
    /// <summary>
    /// 从特性数据中获取整型属性值。
    /// </summary>
    /// <param name="attribute">特性数据对象</param>
    /// <param name="propertyName">属性名称</param>
    /// <param name="defaultVal">默认值，当属性不存在或无法解析时返回此值</param>
    /// <returns>解析得到的整型值或默认值</returns>
    public static int GetIntValueFromAttribute(AttributeData attribute, string propertyName, int defaultVal = 0)
    {
        if (attribute == null)
            return defaultVal;

        var timeoutArg = attribute.NamedArguments
            .FirstOrDefault(a => a.Key.Equals(propertyName, StringComparison.OrdinalIgnoreCase));
        return timeoutArg.Value.Value is int value ? value : defaultVal;
    }


    /// <summary>
    /// 从特性数据中获取字符串属性值。
    /// </summary>
    /// <param name="attribute">特性数据对象</param>
    /// <param name="propertyName">属性名称</param>
    /// <param name="defaultValue">默认值，当属性不存在或无法解析时返回此值</param>
    /// <returns>解析得到的字符串值或默认值</returns>
    public static string? GetStringValueFromAttribute(AttributeData attribute, string propertyName, string? defaultValue = null)
    {
        if (attribute == null)
            return defaultValue;
        var nameArg = attribute.NamedArguments
            .FirstOrDefault(a => a.Key.Equals(propertyName, StringComparison.OrdinalIgnoreCase));
        return nameArg.Value.Value?.ToString();
    }


    /// <summary>
    /// 从特性数据中获取布尔型属性值。
    /// </summary>
    /// <param name="attributeData">特性数据对象</param>
    /// <param name="propertyName">属性名称</param>
    /// <param name="defaultValue">默认值，当属性不存在或无法解析时返回此值</param>
    /// <returns>解析得到的布尔值或默认值</returns>
    public static bool GetBoolValueFromAttribute(AttributeData? attributeData, string propertyName, bool defaultValue = false)
    {
        if (attributeData?.NamedArguments
            .FirstOrDefault(k => k.Key.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
            .Value.Value is bool isAbstract)
            return isAbstract;

        return defaultValue;
    }


    /// <summary>
    /// 从特性数据中获取字符串属性值，同时兼容命名参数和构造函数参数两种方式。
    /// 优先返回命名参数的值，如果命名参数不存在，则返回构造函数参数的值。
    /// </summary>
    /// <param name="attributeData">特性数据对象</param>
    /// <param name="propertyName">属性名称</param>
    /// <returns>解析得到的字符串值，如果都未找到则返回 null</returns>
    /// <remarks>
    /// 此方法首先检查命名参数，如果找到非空的值则直接返回。
    /// 如果命名参数为空，则检查构造函数参数，返回第一个参数的值。
    /// </remarks>
    public static string? GetStringValueFromAttributeConstructor(AttributeData? attributeData, string propertyName)
    {
        if (attributeData == null)
            return null;

        var baseAddressArg = GetStringValueFromAttribute(attributeData, propertyName);
        if (!string.IsNullOrEmpty(baseAddressArg))
            return baseAddressArg;

        if (attributeData.ConstructorArguments.Length > 0 && attributeData.ConstructorArguments[0].Value is string baseAddress)
            return baseAddress;

        return null;
    }

    /// <summary>
    /// 从类型符号中获取指定名称的特性数据。
    /// </summary>
    /// <param name="interfaceSymbol">类型符号对象</param>
    /// <param name="attributeNames">要查找的特性名称数组，支持多个名称进行匹配</param>
    /// <returns>匹配到的特性数据对象，如果未找到则返回 null</returns>
    /// <remarks>
    /// 此方法会遍历类型的所有特性，返回第一个名称在给定名称数组中的特性。
    /// 常用于查找可能存在多个别名或不同命名空间的特性。
    /// </remarks>
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
