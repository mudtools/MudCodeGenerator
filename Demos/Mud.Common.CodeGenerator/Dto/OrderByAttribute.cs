namespace Mud.Common.CodeGenerator;

/// <summary>
/// 自动生成服务查询代码排序属性。
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class OrderByAttribute : Attribute
{
    /// <summary>
    /// 是否为升序查询。
    /// </summary>
    public bool IsAsc { get; set; } = true;

    /// <summary>
    /// 多个排序字段顺序。
    /// </summary>
    public int OrderNum { get; set; } = 0;
}
