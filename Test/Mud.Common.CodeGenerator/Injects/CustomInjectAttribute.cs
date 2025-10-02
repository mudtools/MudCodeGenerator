namespace Mud.Common.CodeGenerator;

/// <summary>
/// 自定义注入特性，用于标记需要进行依赖注入的类
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class CustomInjectAttribute : Attribute
{
    /// <summary>
    /// 获取或设置变量类型
    /// </summary>
    public string VarType { get; set; }

    /// <summary>
    /// 获取或设置变量名称
    /// </summary>
    public string VarName { get; set; }

    public CustomInjectAttribute(string varType) : this(varType, null)
    {
    }

    public CustomInjectAttribute(string varType, string varName = null)
    {
        VarType = varType;
        VarName = varName;
    }
}