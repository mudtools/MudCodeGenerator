namespace Mud.Common.CodeGenerator;

/// <summary>
/// 生成日志输出对象，并完成注入。
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class LoggerInjectAttribute : Attribute
{
}
