namespace Mud.ServiceCodeGenerator;


/// <summary>
/// 表示 HttpClient API 的元数据信息
/// </summary>
public sealed class HttpClientApiInfo
{
    public HttpClientApiInfo(string interfaceName, string implementationName, string namespaceName, string baseUrl, int timeout, string? registryGroupName = null)
    {
        InterfaceName = interfaceName ?? throw new ArgumentNullException(nameof(interfaceName));
        ImplementationName = implementationName ?? throw new ArgumentNullException(nameof(implementationName));
        Namespace = namespaceName ?? throw new ArgumentNullException(nameof(namespaceName));
        BaseUrl = baseUrl ?? throw new ArgumentNullException(nameof(baseUrl));
        Timeout = timeout;
        RegistryGroupName = registryGroupName;
    }

    public string InterfaceName { get; }
    public string ImplementationName { get; }
    public string Namespace { get; }
    public string BaseUrl { get; }
    public int Timeout { get; }
    public string? RegistryGroupName { get; }
}