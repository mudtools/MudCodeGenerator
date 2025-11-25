// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.CodeGenerator 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.ServiceCodeGenerator;

/// <summary>
/// 接口Header特性信息
/// </summary>
public class InterfaceHeaderAttribute
{
    /// <summary>
    /// Header名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Header值
    /// </summary>
    public object? Value { get; set; }

    /// <summary>
    /// 是否替换已存在的Header
    /// </summary>
    public bool Replace { get; set; }
}

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
    /// 是否是异步方法（返回类型为 Task 或 Task<T>）
    /// </summary>
    public bool IsAsyncMethod { get; set; }

    /// <summary>
    /// 异步方法的内部返回类型（如果是 Task<T>，这里是 T；如果是 Task，这里是 void）
    /// </summary>
    public string AsyncInnerReturnType { get; set; } = string.Empty;

    /// <summary>
    /// 方法参数列表
    /// </summary>
    public IReadOnlyList<ParameterInfo> Parameters { get; set; } = [];

    /// <summary>
    /// 是否忽略生成实现 [IgnoreImplement]
    /// </summary>
    public bool IgnoreImplement { get; set; }

    /// <summary>
    /// 是否忽略生成包装接口 [IgnoreWrapInterface]
    /// </summary>
    public bool IgnoreWrapInterface { get; set; }

    /// <summary>
    /// 接口特性列表（用于存储Header:Authorization、Query:Authorization等）
    /// </summary>
    public HashSet<string> InterfaceAttributes { get; set; } = [];

    /// <summary>
    /// 接口Header特性列表（用于存储所有Header特性的名称和值）
    /// </summary>
    public List<InterfaceHeaderAttribute> InterfaceHeaderAttributes { get; set; } = [];

    /// <summary>
    /// 无效的分析结果实例
    /// </summary>
    public static MethodAnalysisResult Invalid => new() { IsValid = false };
}