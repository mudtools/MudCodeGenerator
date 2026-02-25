using Mud.Common.CodeGenerator;
using Mud.HttpUtils;

namespace CommonClassLibrary;

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
    IMudAppContext UseApp(string appKey);
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