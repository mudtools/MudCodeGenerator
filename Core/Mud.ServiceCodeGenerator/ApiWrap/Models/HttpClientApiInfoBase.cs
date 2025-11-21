namespace Mud.ServiceCodeGenerator;

/// <summary>
/// HttpClient API 信息的基类
/// </summary>
/// <remarks>
/// 包含所有 HttpClient API 相关的通用属性，提供统一的基础结构
/// </summary>
public abstract class HttpClientApiInfoBase
{
    /// <summary>
    /// 初始化 HttpClient API 信息基类
    /// </summary>
    /// <param name="namespaceName">命名空间名称</param>
    /// <param name="baseUrl">API 基础地址</param>
    /// <param name="timeout">超时时间（秒）</param>
    /// <param name="registryGroupName">注册组名称</param>
    protected HttpClientApiInfoBase(string namespaceName, string baseUrl, int timeout, string? registryGroupName = null)
    {
        Namespace = namespaceName ?? throw new ArgumentNullException(nameof(namespaceName));
        BaseUrl = baseUrl ?? throw new ArgumentNullException(nameof(baseUrl));
        Timeout = timeout;
        RegistryGroupName = registryGroupName;
    }

    /// <summary>
    /// 命名空间名称
    /// </summary>
    public string Namespace { get; }

    /// <summary>
    /// API 基础地址
    /// </summary>
    public string BaseUrl { get; }

    /// <summary>
    /// 超时时间（秒）
    /// </summary>
    public int Timeout { get; }

    /// <summary>
    /// 注册组名称
    /// </summary>
    /// <remarks>
    /// 用于按组生成服务注册函数，如果为空则使用默认注册函数
    /// </remarks>
    public string? RegistryGroupName { get; }
}