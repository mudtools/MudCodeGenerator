namespace Mud.Common.CodeGenerator
{
    /// <summary>
    /// 标记this属性为Item索引取值。
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    internal class ItemIndexAttribute : Attribute
    {
    }
}
