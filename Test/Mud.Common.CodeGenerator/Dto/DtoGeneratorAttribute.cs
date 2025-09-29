namespace Mud.Common.CodeGenerator;

/// <summary>
/// 自动生成DTO类注解。
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class DtoGeneratorAttribute : Attribute
{
    /// <summary>
    /// 命名空间名。
    /// </summary>
    public string DtoNamespace { get; set; } = "Dto";

    /// <summary>
    /// 是否生成VO类。
    /// </summary>
    public bool GenVoClass { get; set; } = true;

    /// <summary>
    /// 是否生成BO类。
    /// </summary>
    public bool GenBoClass { get; set; } = true;

    /// <summary>
    /// 是否生成查询类。
    /// </summary>
    public bool GenQueryInputClass { get; set; } = true;

    /// <summary>
    /// 是否生成实体类映射方法
    /// </summary>
    public bool GenMapMethod { get; set; } = true;
}