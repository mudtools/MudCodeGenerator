using Mud.Common.CodeGenerator;

namespace CodeBaseTest;

public abstract class CRUDEntity : BaseEntity<string>
{
    [IgnoreGenerator]
    public long? CreatorId { get; set; }

    [IgnoreGenerator]
    public DateTime? CreationTime { get; set; }

    [IgnoreGenerator]
    public long? LastModifierId { get; set; }

    [IgnoreGenerator]
    public DateTime? LastModificationTime { get; set; }
}
