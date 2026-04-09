namespace Mud.Common.CodeGenerator;

/// <summary>
/// 生成IUserManager对象，并完成构造函数注入。
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class UserInjectAttribute : Attribute
{
}
