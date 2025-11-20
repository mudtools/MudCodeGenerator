namespace Mud.ServiceCodeGenerator;

/// <summary>
/// 参数特性信息
/// </summary>
/// <remarks>
/// 存储参数特性的详细信息，包括特性名称、构造函数参数和命名参数。
/// </remarks>
public class ParameterAttributeInfo
{
    /// <summary>
    /// 特性名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 构造函数参数数组
    /// </summary>
    public object?[] Arguments { get; set; } = [];

    /// <summary>
    /// 命名参数字典
    /// </summary>
    public IReadOnlyDictionary<string, object?> NamedArguments { get; set; } = new Dictionary<string, object?>();
}
