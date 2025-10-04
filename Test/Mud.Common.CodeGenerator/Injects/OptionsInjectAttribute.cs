namespace Mud.Common.CodeGenerator;

/// <summary>
/// 配置项对象注入属性。
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class OptionsInjectAttribute : Attribute
{
#if NET7_0_OR_GREATER
    /// <summary>
    /// 获取或设置配置项类型
    /// </summary>
    public required string OptionType { get; set; }
#else
    /// <summary>
    /// 获取或设置配置项类型
    /// </summary>
    public string OptionType { get; set; }
#endif

    /// <summary>
    /// 获取或设置变量名称
    /// </summary>
    public string VarName { get; set; }
    public OptionsInjectAttribute()
    {

    }
#if NET7_0_OR_GREATER  
    public OptionsInjectAttribute(string optionType)
    {
        OptionType = optionType;
    }

#else
    public OptionsInjectAttribute(string optionType) : this(optionType, null)
    {
    }

    public OptionsInjectAttribute(string optionType, string varName = null)
    {
        OptionType = optionType;
        VarName = varName;
    }
#endif
}
