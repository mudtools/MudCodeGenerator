namespace Mud.Common.CodeGenerator;

/// <summary>
/// 自动生成构造函数，将字段从构造函数中注入。
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class ConstructorInjectAttribute : Attribute
{
}
