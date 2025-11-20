namespace Mud.Common.CodeGenerator;

/// <summary>
/// 指示封装的是一个集合类型的COM对象。
/// </summary>
[AttributeUsage(AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
public class ComCollectionWrapAttribute : Attribute
{
    /// <summary>
    /// COM对象所在的命名空间。
    /// </summary>
    public string ComNamespace { get; set; }
}
