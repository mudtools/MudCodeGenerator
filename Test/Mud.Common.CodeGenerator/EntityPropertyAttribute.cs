namespace Mud.Common.CodeGenerator;

/// <summary>
/// 标识用于生成实体属性
/// </summary>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public sealed class EntityPropertyAttribute : Attribute
{
}
