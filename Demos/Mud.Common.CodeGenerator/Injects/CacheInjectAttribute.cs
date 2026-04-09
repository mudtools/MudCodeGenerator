
namespace Mud.Common.CodeGenerator;

/// <summary>
/// 生成ICacheManager对象，并完成构造函数注入。
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class CacheInjectAttribute : Attribute
{
}
