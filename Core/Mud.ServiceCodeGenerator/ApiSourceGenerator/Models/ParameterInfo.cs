namespace Mud.ServiceCodeGenerator;


/// <summary>
/// 参数信息
/// </summary>
/// <remarks>
/// 存储方法参数的详细信息，包括参数名、类型、特性和默认值。
/// </remarks>
public class ParameterInfo
{
    /// <summary>
    /// 参数名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 参数类型显示字符串
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 参数特性列表
    /// </summary>
    public IReadOnlyList<ParameterAttributeInfo> Attributes { get; set; } = [];

    /// <summary>
    /// 是否具有默认值
    /// </summary>
    public bool HasDefaultValue { get; set; }

    /// <summary>
    /// 默认值
    /// </summary>
    public object? DefaultValue { get; set; }

    /// <summary>
    /// 默认值的字面量表示
    /// </summary>
    public string? DefaultValueLiteral { get; set; }
}