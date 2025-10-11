using FreeSql.DataAnnotations;

namespace CodeBaseTest;

public abstract class BaseEntity<TKey>
{
    [Column(Name = "id", IsPrimary = true, Position = 1)]
    public TKey Id { get; set; }
}
