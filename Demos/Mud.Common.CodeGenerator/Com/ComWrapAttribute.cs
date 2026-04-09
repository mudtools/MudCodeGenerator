namespace Mud.Common.CodeGenerator;

/// <summary>
/// 指示封装的是一个普通的COM对象。
/// </summary>
public abstract class ComWrapAttribute : Attribute
{
    /// <summary>
    /// COM对象所在的命名空间。
    /// </summary>
    public string ComNamespace { get; set; }

    /// <summary>
    /// COM对象类名
    /// </summary>
    public string ComClassName { get; set; }

    /// <summary>
    /// 不生成构造函数。
    /// </summary>
    public bool NoneConstructor { get; set; } = false;

    /// <summary>
    /// 不生成资源释放函数。
    /// </summary>
    public bool NoneDisposed { get; set; }
}
