namespace Mud.ServiceCodeGenerator;

/// <summary>
/// 表示 HttpClient API 的元数据信息
/// </summary>
/// <remarks>
/// 继承自 HttpClientApiInfoBase，包含 HttpClient API 接口特定的信息
/// </remarks>
public sealed class HttpClientApiInfo : HttpClientApiInfoBase
{
    /// <summary>
    /// 初始化 HttpClient API 信息
    /// </summary>
    /// <param name="interfaceName">接口名称</param>
    /// <param name="implementationName">实现类名称</param>
    /// <param name="namespaceName">命名空间名称</param>
    /// <param name="baseUrl">API 基础地址</param>
    /// <param name="timeout">超时时间（秒）</param>
    /// <param name="registryGroupName">注册组名称</param>
    public HttpClientApiInfo(string interfaceName, string implementationName, string namespaceName, string baseUrl, int timeout, string? registryGroupName = null)
        : base(namespaceName, baseUrl, timeout, registryGroupName)
    {
        InterfaceName = interfaceName ?? throw new ArgumentNullException(nameof(interfaceName));
        ImplementationName = implementationName ?? throw new ArgumentNullException(nameof(implementationName));
    }

    /// <summary>
    /// 接口名称
    /// </summary>
    public string InterfaceName { get; }

    /// <summary>
    /// 实现类名称
    /// </summary>
    public string ImplementationName { get; }
}