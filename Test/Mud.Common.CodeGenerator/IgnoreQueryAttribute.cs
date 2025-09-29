namespace Mud.Common.CodeGenerator;

/// <summary>
/// 忽略生成查询代码。
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public class IgnoreQueryAttribute : Attribute
{
}
