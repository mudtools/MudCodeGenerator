namespace Mud.Common.CodeGenerator;

/// <summary>
/// 用于标注方法的返回参数是否需要转换。
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public class ReturnValueConvertAttribute : Attribute
{
}
