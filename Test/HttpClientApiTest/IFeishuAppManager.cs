namespace HttpClientApiTest;

/// <summary>
/// 所有HTTP客户端服务的标记接口
/// </summary>
public interface IMudHttpClientService
{
    /// <summary>
    /// 更改当前HTTP客户端服务的应用上下文
    /// </summary>
    /// <param name="appKey">应用键</param>
    /// <returns>当前HTTP客户端服务的应用上下文</returns>
    /// <remarks>
    /// 根据应用键更改当前对应的飞书应用上下文。
    /// 应用键是在配置中定义的唯一标识，如 "default", "hr-app" 等。
    /// </remarks>
    [IgnoreGenerator]
    IMudAppContext ChangeCurrentContext(string appKey);
}

public interface IFeishuAppManager
{
    FeishuAppContext GetDefaultApp();

    ITokenManage GetTokenManager(TokenType tokenType);

    IEnhancedHttpClient GetHttpClient();

    FeishuAppContext GetApp(string appKey);
}

/// <summary>
/// 应用上下文
/// </summary>
/// <remarks>
/// 封装单个应用的所有资源和配置，包括：
/// - 应用配置信息
/// - 各种类型的令牌管理器（租户令牌、应用令牌、用户令牌）
/// - 认证API客户端
/// - 令牌缓存
/// - HTTP客户端
/// 
/// 每个应用上下文是完全独立的，不同应用之间的配置、缓存和资源互不干扰。
/// </remarks>
public interface IMudAppContext : IDisposable
{
    /// <summary>
    /// HTTP客户端
    /// </summary>
    /// <remarks>
    /// 用于发送HTTP请求到远程API的客户端实例。
    /// 每个应用拥有独立的HTTP客户端实例。
    /// </remarks>
    IEnhancedHttpClient HttpClient { get; }

    /// <summary>
    /// 根据令牌类型获取对应的令牌管理器
    /// </summary>
    /// <param name="tokenType">令牌类型</param>
    /// <returns></returns>
    ITokenManager GetTokenManager(TokenType tokenType);
}


public class FeishuAppContext : IMudAppContext
{
    /// <summary>
    /// HTTP客户端
    /// </summary>
    /// <remarks>
    /// 用于发送HTTP请求到远程API的客户端实例。
    /// 每个应用拥有独立的HTTP客户端实例。
    /// </remarks>
    public IEnhancedHttpClient HttpClient { get; }

    /// <summary>
    /// 根据令牌类型获取对应的令牌管理器
    /// </summary>
    /// <param name="tokenType">令牌类型</param>
    /// <returns></returns>
    public ITokenManager GetTokenManager(TokenType tokenType) => throw new NotImplementedException();

    public void Dispose()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// 租户令牌管理器
    /// </summary>
    /// <remarks>
    /// 用于获取和管理租户访问令牌（Tenant Access Token）。
    /// 租户令牌用于租户级别的权限验证。
    /// </remarks>
    public ITenantTokenManager TenantTokenManager { get; }

    /// <summary>
    /// 应用令牌管理器
    /// </summary>
    /// <remarks>
    /// 用于获取和管理应用身份访问令牌（App Access Token）。
    /// 应用令牌用于应用级别的权限验证。
    /// </remarks>
    public IAppTokenManager AppTokenManager { get; }

    /// <summary>
    /// 用户令牌管理器
    /// </summary>
    /// <remarks>
    /// 用于获取和管理用户访问令牌（User Access Token）。
    /// 用户令牌通过OAuth授权流程获取，需要用户授权。
    /// </remarks>
    public IUserTokenManager UserTokenManager { get; }

}