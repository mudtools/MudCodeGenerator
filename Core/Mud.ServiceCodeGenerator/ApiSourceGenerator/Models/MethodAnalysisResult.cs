namespace Mud.ServiceCodeGenerator;


/// <summary>
/// 方法分析结果
/// </summary>
/// <remarks>
/// 用于存储接口方法的分析信息，包括 HTTP 方法、URL 模板、参数等。
/// </remarks>
public class MethodAnalysisResult
{
    /// <summary>
    /// 接口名称
    /// </summary>
    public string InterfaceName { get; set; } = string.Empty;

    /// <summary>
    /// 方法是否有效
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// 方法名称
    /// </summary>
    public string MethodName { get; set; } = string.Empty;

    /// <summary>
    /// HTTP 方法（GET, POST, PUT, DELETE, PATCH, HEAD, OPTIONS）
    /// </summary>
    public string HttpMethod { get; set; } = string.Empty;

    /// <summary>
    /// URL 模板，支持参数占位符
    /// </summary>
    public string UrlTemplate { get; set; } = string.Empty;

    /// <summary>
    /// 返回类型显示字符串
    /// </summary>
    public string ReturnType { get; set; } = string.Empty;

    /// <summary>
    /// 方法参数列表
    /// </summary>
    public IReadOnlyList<ParameterInfo> Parameters { get; set; } = [];

    /// <summary>
    /// 无效的分析结果实例
    /// </summary>
    public static MethodAnalysisResult Invalid => new() { IsValid = false };
}