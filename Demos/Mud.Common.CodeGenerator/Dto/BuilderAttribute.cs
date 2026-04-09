namespace Mud.Common.CodeGenerator;


/// <summary>
/// 用于标记生成建造者模式代码。
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class BuilderAttribute : Attribute
{
}
