namespace Mud.Common.CodeGenerator
{
    /// <summary>
    /// 标记方法的COM方法名。
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class MethodNameAttribute : Attribute
    {
        /// <summary>
        /// COM方法名。
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// 默认构造函数。
        /// </summary>
        public MethodNameAttribute()
        {
        }

        /// <summary>
        /// 默认构造函数。
        /// </summary>
        public MethodNameAttribute(string? name)
        {
            Name = name;
        }
    }
}
