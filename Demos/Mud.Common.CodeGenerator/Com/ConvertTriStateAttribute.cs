namespace Mud.Common.CodeGenerator.Com
{
    /// <summary>
    /// 标记属性或参数需要进行三态转换。
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    public class ConvertTriStateAttribute : Attribute
    {
    }
}
