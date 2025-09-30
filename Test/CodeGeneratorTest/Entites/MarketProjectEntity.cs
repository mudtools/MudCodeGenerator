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
    [Required(ErrorMessage = "父项目ID不能为空。")]
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
    [MinLength(10)]
    public string ErpNum { get; set; }

    /// <summary>
    /// 获取或设置子项数量。
    /// </summary>
    [Column(Name = "F_Items")]
    public int? Items { get; set; }
}
