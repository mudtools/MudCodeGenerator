namespace CodeGeneratorTest.Entites;

/// <summary>
/// 客户端信息实体类
/// </summary>
[Table(Name = "sys_client")]
[DtoGenerator, Builder]
public partial class SysClientEntity
{
    /// <summary>
    /// id
    /// </summary>
    [property: Column(Name = "id", IsPrimary = true, Position = 1)]
    [property: Required(ErrorMessage = "id不能为空")]
    private long? _id;

    /// <summary>
    /// 客户端key
    /// </summary>
    [property: Column(Name = "client_key", Position = 3)]
    [property: Required(ErrorMessage = "客户端key不能为空")]
    [property: CustomVo1, CustomVo2]
    [property: CustomBo1, CustomBo2]
    private string _clientKey;

    /// <summary>
    /// 删除标志（0代表存在 2代表删除）
    /// </summary>
    [property: Column(Name = "del_flag", Position = 10)]
    [IgnoreQuery]
    private string _delFlag;

}
