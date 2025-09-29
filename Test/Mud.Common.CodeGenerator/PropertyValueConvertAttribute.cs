namespace Mud.Common.CodeGenerator;

/// <summary>
/// 视图输出字段属性转换。
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class PropertyTranslationAttribute : Attribute
{
    /// <summary>
    /// 需要生成的属性名，默认为当前属性加上Str后缀。
    /// </summary>
    public string PropertyName { get; set; }

    /// <summary>
    /// 转换器类型。
    /// </summary>
    public Type ConverterType { get; set; }
}
