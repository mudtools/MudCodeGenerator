namespace CodeGeneratorTest.Entites;

/// <summary>
/// 经营项目管理
/// </summary>
[Table(Name = "PRJ_MARKETPROJECT")]
[DtoGenerator]
public class MarketProjectEntity
{
    /// <summary>
    /// 父项目ID
    /// </summary>
    [Column(Name = "F_PARENTID", StringLength = 150)]
    public string ParentId { get; set; }


    /// <summary>
    /// 父项目ProNum
    /// </summary>
    [Column(Name = "F_PARENTNUM", StringLength = 150)]
    public string ParentNum { get; set; }


    /// <summary>
    /// 项目编号
    /// </summary>
    [Column(Name = "F_PRONUM", StringLength = 150)]
    public string ProNum { get; set; }

    /// <summary>
    /// 获取或设置ERP编码
    /// </summary>
    [Column(Name = "F_ERPNUM", StringLength = 50)]
    public string ErpNum { get; set; }
}
