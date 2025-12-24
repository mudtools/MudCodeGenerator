namespace Mud.Common.CodeGenerator;


/// <summary>
/// COM封装接口的属性信息。
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public class ComPropertyWrapAttribute : Attribute
{
    /// <summary>
    /// 属性对象所在的COM对象命名空间。
    /// </summary>
    public string ComNamespace { get; set; }

    /// <summary>
    /// 属性默认值。
    /// </summary>
    public string DefaultValue { get; set; }

    /// <summary>
    /// 是否需要转换属性值。
    /// </summary>
    public bool NeedConvert { get; set; } = false;

    /// <summary>
    /// 是否需要释放属性值资源。
    /// </summary>
    public bool NeedDispose { get; set; } = true;

    /// <summary>
    /// 标记该属性采用get、set方法进行访问。
    /// </summary>
    public bool IsMethod { get; set; }

    /// <summary>
    /// 获取或设置属性名，默认为空（即原始属性名。）
    /// </summary>
    public string PropertyName { get; set; }
}
