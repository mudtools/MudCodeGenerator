using Mud.Common.CodeGenerator;

namespace CodeBaseTest;

public abstract class CRUDEntity : BaseEntity<string>
{
    /// <summary>
    /// 对象创建人
    /// </summary>
    [IgnoreGenerator]
    public long? CreatorId { get; set; }

    /// <summary>
    /// 对象创建时间
    /// </summary>
    public DateTime? CreationTime { get; set; }

    /// <summary>
    /// 最后修改人
    /// </summary>
    [IgnoreGenerator]
    public long? LastModifierId { get; set; }

    /// <summary>
    /// 最后修改时间
    /// </summary>
    [IgnoreGenerator]
    public DateTime? LastModificationTime { get; set; }
}
