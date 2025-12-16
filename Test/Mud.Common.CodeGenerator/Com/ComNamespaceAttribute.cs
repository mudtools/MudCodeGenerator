namespace Mud.Common.CodeGenerator.Com;


/// <summary>
/// 用于标识COM组件所在的命名空间。
/// </summary>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public class ComNamespaceAttribute : Attribute
{
    /// <summary>
    /// 默认构造函数。
    /// </summary>
    public ComNamespaceAttribute() { }

    /// <summary>
    /// 默认构造函数。
    /// </summary>
    public ComNamespaceAttribute(string name)
    {
        Name = name;
    }

    /// <summary>
    /// COM组件所在的命名空间。
    /// </summary>
    public string? Name { get; set; }
}
