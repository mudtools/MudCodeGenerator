namespace Mud.Common.CodeGenerator;

/// <summary>
/// 声明用于保存下载远程服务二进制文件的文件路径参数特性
/// </summary>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class FilePathAttribute : Attribute
{

}