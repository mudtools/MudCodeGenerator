namespace Mud.ServiceCodeGenerator;

/// <summary>
/// 表示 HttpClient Wrap API 的元数据信息
/// </summary>
/// <remarks>
/// 继承自 HttpClientApiInfoBase，包含包装API特有的信息
/// </remarks>
public sealed class HttpClientWrapApiInfo : HttpClientApiInfoBase
{
    /// <summary>
    /// 初始化 HttpClient Wrap API 信息
    /// </summary>
    /// <param name="originalInterfaceName">原始接口名称</param>
    /// <param name="wrapInterfaceName">包装接口名称</param>
    /// <param name="wrapClassName">包装类名称</param>
    /// <param name="namespaceName">命名空间名称</param>
    /// <param name="baseUrl">API 基础地址</param>
    /// <param name="timeout">超时时间（秒）</param>
    /// <param name="registryGroupName">注册组名称</param>
    public HttpClientWrapApiInfo(string originalInterfaceName, string wrapInterfaceName, string wrapClassName, string namespaceName, string baseUrl, int timeout, string? registryGroupName = null)
        : base(namespaceName, baseUrl, timeout, registryGroupName)
    {
        OriginalInterfaceName = originalInterfaceName ?? throw new ArgumentNullException(nameof(originalInterfaceName));
        WrapInterfaceName = wrapInterfaceName ?? throw new ArgumentNullException(nameof(wrapInterfaceName));
        WrapClassName = wrapClassName ?? throw new ArgumentNullException(nameof(wrapClassName));
    }

    /// <summary>
    /// 原始接口名称
    /// </summary>
    public string OriginalInterfaceName { get; }

    /// <summary>
    /// 包装接口名称
    /// </summary>
    public string WrapInterfaceName { get; }

    /// <summary>
    /// 包装类名称
    /// </summary>
    public string WrapClassName { get; }
}