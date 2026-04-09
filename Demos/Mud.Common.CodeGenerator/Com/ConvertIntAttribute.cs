namespace Mud.Common.CodeGenerator.Com
{
    /// <summary>
    /// 标记属性或参数需要转换为int。
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    public class ConvertIntAttribute : Attribute
    {
    }
}
