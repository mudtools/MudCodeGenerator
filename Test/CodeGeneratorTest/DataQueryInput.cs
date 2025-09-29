namespace CodeGeneratorTest;

/// <summary>
/// 公共的业务数据查询类的基类
/// </summary>
public class DataQueryInput
{
    /// <summary>
    /// 获取或设置 创建时间
    /// </summary>
    public virtual DateTime? createTime { get; set; }

    /// <summary>
    /// 获取或设置 创建用户
    /// </summary>
    public virtual long? createBy { get; set; }

    /// <summary>
    /// 获取或设置 创建部门
    /// </summary>
    public virtual long? creatorDeptId { get; set; }

    /// <summary>
    /// 获取或设置 修改时间
    /// </summary>
    public virtual DateTime? updateTime { get; set; }

    /// <summary>
    /// 获取或设置 修改用户
    /// </summary>
    public virtual long? updateBy { get; set; }
    /// <summary>
    /// 开始时间
    /// </summary>
    public DateTime? beginTime { get; set; }
    /// <summary>
    /// 结束时间
    /// </summary>
    public DateTime? endTime { get; set; }

}