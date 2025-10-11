namespace CodeBaseTest;

public abstract class CRUDEntity : BaseEntity<string>
{
    public long? CreatorId { get; set; }

    public DateTime? CreationTime { get; set; }

    public long? LastModifierId { get; set; }

    public DateTime? LastModificationTime { get; set; }
}
