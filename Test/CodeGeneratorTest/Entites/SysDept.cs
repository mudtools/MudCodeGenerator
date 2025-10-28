
namespace CodeGeneratorTest.Entites;

/// <summary>
/// 部门表 sys_dept
/// </summary>
[Table(Name = "sys_dept")]
[DtoGenerator(GenMapMethod = true), Builder]
public partial class SysDept : BaseEntity<string>
{
    /// <summary>
    /// 部门ID
    /// </summary>
    [Key]
    [Column(Name = "dept_id")]
    public long DeptId { get; set; }

    /// <summary>
    /// 父部门ID
    /// </summary>
    [Column(Name = "parent_id")]
    public long ParentId { get; set; }

    /// <summary>
    /// 部门名称
    /// </summary>
    [Column(Name = "dept_name")]
    public string DeptName { get; set; }

    /// <summary>
    /// 部门类别编码
    /// </summary>
    [Column(Name = "dept_category")]
    public string DeptCategory { get; set; }

    /// <summary>
    /// 显示顺序
    /// </summary>
    [Column(Name = "order_num")]
    public int OrderNum { get; set; }

    /// <summary>
    /// 负责人
    /// </summary>
    [Column(Name = "leader")]
    public long Leader { get; set; }

    /// <summary>
    /// 联系电话
    /// </summary>
    [Column(Name = "phone")]
    public string Phone { get; set; }

    /// <summary>
    /// 邮箱
    /// </summary>
    [Column(Name = "email")]
    public string Email { get; set; }

    /// <summary>
    /// 部门状态:0正常,1停用
    /// </summary>
    [Column(Name = "status")]
    public string Status { get; set; }

    /// <summary>
    /// 删除标志（0代表存在 1代表删除）
    /// </summary>
    [Column(Name = "del_flag")]
    public string DelFlag { get; set; }

    /// <summary>
    /// 祖级列表
    /// </summary>
    [Column(Name = "ancestors")]
    public string Ancestors { get; set; }

    /// <summary>
    /// 父部门名称
    /// </summary>
    public string ParentName { get; set; }

    /// <summary>
    /// 子部门
    /// </summary>
    public SysDept[] Children { get; set; }
}