namespace Mud.Common.CodeGenerator;

/// <summary>
/// 忽略代码生成。
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public class IgnoreGeneratorAttribute : Attribute
{
}
