namespace Mud.Common.CodeGenerator
{
    /// <summary>
    /// 标记方法的访问采用索引方式。
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class MethodIndexAttribute : Attribute
    {
    }
}
