namespace Mud.Common.CodeGenerator
{
    /// <summary>
    /// 指示当前COM组件不能进行枚举取值。
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
    public class NoneEnumerableAttribute : Attribute
    {
    }

}
