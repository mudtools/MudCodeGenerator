namespace Mud.ServiceCodeGenerator;

/// <summary>
/// 表示 HttpClient Wrap API 的元数据信息
/// </summary>
public sealed class HttpClientWrapApiInfo
{
    public HttpClientWrapApiInfo(string originalInterfaceName, string wrapInterfaceName, string wrapClassName, string namespaceName, string baseUrl, int timeout, string? registryGroupName = null)
    {
        OriginalInterfaceName = originalInterfaceName ?? throw new ArgumentNullException(nameof(originalInterfaceName));
        WrapInterfaceName = wrapInterfaceName ?? throw new ArgumentNullException(nameof(wrapInterfaceName));
        WrapClassName = wrapClassName ?? throw new ArgumentNullException(nameof(wrapClassName));
        Namespace = namespaceName ?? throw new ArgumentNullException(nameof(namespaceName));
        BaseUrl = baseUrl ?? throw new ArgumentNullException(nameof(baseUrl));
        Timeout = timeout;
        RegistryGroupName = registryGroupName;
    }

    public string OriginalInterfaceName { get; }
    public string WrapInterfaceName { get; }
    public string WrapClassName { get; }
    public string Namespace { get; }
    public string BaseUrl { get; }
    public int Timeout { get; }
    public string? RegistryGroupName { get; }
}