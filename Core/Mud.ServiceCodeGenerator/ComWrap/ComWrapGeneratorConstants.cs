namespace Mud.ServiceCodeGenerator.ComWrapSourceGenerator;

/// <summary>
/// COM对象包装生成器常量配置
/// </summary>
internal static class ComWrapGeneratorConstants
{
    /// <summary>
    /// ComObjectWrap特性名称数组
    /// </summary>
    public static readonly string[] ComObjectWrapAttributeNames = ["ComObjectWrapAttribute", "ComObjectWrap"];

    /// <summary>
    /// ComPropertyWrap特性名称数组
    /// </summary>
    public static readonly string[] ComPropertyWrapAttributeNames = ["ComPropertyWrapAttribute", "ComPropertyWrap"];

    /// <summary>
    /// 忽略生成器特性名称
    /// </summary>
    public const string IgnoreGeneratorAttribute = "IgnoreGeneratorAttribute";

    /// <summary>
    /// 默认COM命名空间
    /// </summary>
    public const string DefaultComNamespace = "UNKNOWN_NAMESPACE";

    /// <summary>
    /// 默认COM类名
    /// </summary>
    public const string DefaultComClassName = "UNKNOWN_CLASS";
}