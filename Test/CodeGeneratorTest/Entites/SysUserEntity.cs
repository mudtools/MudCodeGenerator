namespace CodeGeneratorTest.Entites;

/// <summary>
/// 用户对象 sys_user
/// </summary>
[Table(Name = "sys_user")]
[DtoGenerator]
public partial class SysUserEntity : BaseEntity<string>
{
    /// <summary>
    /// 用户ID
    /// </summary>
    [property: Column(Name = "user_id", IsPrimary = true, Position = 1), OrderBy]
    [property: Required(ErrorMessage = "用户ID主键不能为空")]
    private long? userId;

    /// <summary>
    /// 部门ID
    /// </summary>
    [property: Column(Name = "dept_id", Position = 2)]
    [property: Required(ErrorMessage = "用户所属部门不能为空")]
    [property: PropertyTranslation(ConverterType = typeof(TranslationConvert), PropertyName = "deptName")]
    private long? deptId;

    /// <summary>
    /// 用户账号
    /// </summary>
    [property: Column(Name = "user_name", StringLength = 30, Position = 3), LikeQuery]
    [property: Required(ErrorMessage = "用户账号不能为空")]
    [property: MaxLength(30, ErrorMessage = "用户账号长度不能超过{1}个字符")]
    private string userName;

    /// <summary>
    /// 用户昵称
    /// </summary>
    [property: Column(Name = "nick_name", StringLength = 30, Position = 4), LikeQuery]
    [property: Required(ErrorMessage = "用户昵称不能为空")]
    [property: MaxLength(30, ErrorMessage = "用户昵称长度不能超过{1}个字符")]
    private string nickName;

    /// <summary>
    /// 用户类型（sys_user系统用户）
    /// </summary>
    [property: Column(Name = "user_type", StringLength = 10, Position = 5)]
    private string userType = "sys_user";

    /// <summary>
    /// 用户邮箱
    /// </summary>
    [property: Column(Name = "email", StringLength = 50, Position = 6), LikeQuery]
    //[property: DataValidation(ValidationTypes.EmailAddress, AllowNullValue = true, AllowEmptyStrings = true)]
    [property: MaxLength(50, ErrorMessage = "邮箱长度不能超过{1}个字符")]
    private string email;

    /// <summary>
    /// 手机号码
    /// </summary>
    [property: Column(Name = "phonenumber", StringLength = 20, Position = 7), LikeQuery]
    // [property: DataValidation(ValidationTypes.PhoneOrTelNumber, AllowNullValue = true, AllowEmptyStrings = true)]
    [property: MaxLength(16)]
    private string phonenumber;

    /// <summary>
    /// 用户性别
    /// </summary>
    [property: Column(Name = "sex", StringLength = 2, Position = 8)]
    private string sex;

    /// <summary>
    /// 用户头像
    /// </summary>
    [property: Column(Name = "avatar", Position = 9)]
    private long? avatar;

    /// <summary>
    /// 密码
    /// </summary>
    [property: Column(Name = "password", StringLength = 100, Position = 10)]
    private string password;

    /// <summary>
    /// 最后登录IP
    /// </summary>
    [property: Column(Name = "login_ip", StringLength = 128, Position = 13)]
    private string loginIp;

    /// <summary>
    /// 最后登录时间
    /// </summary>
    [property: Column(Name = "login_date", Position = 14)]
    private DateTime? loginDate;

    /// <summary>
    /// 备注
    /// </summary>
    [property: Column(Name = "remark", StringLength = 500, Position = 15), LikeQuery]
    private string remark;

}
