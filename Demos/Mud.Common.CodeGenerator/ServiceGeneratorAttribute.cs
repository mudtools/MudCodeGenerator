namespace Mud.Common.CodeGenerator;

/// <summary>
/// 自动生成服务类辅助内容注解。
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class ServiceGeneratorAttribute : Attribute
{
}
