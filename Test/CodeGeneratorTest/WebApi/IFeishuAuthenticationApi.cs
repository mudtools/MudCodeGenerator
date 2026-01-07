namespace CodeGeneratorTest.WebApi;

/// <summary>
/// 飞书认证授权相关的API
/// </summary>
[HttpClientApi("https://api.dingtalk.com", Timeout = 60, RegistryGroupName = "Feishu")]
[HttpClientApiWrap(TokenManage = "ITokenManage", WrapInterface = nameof(IFeishuAuthentication))]
public interface IFeishuAuthenticationApi
{
    /// <summary>
    /// 获取自建应用获取 tenant_access_token。
    /// </summary>
    /// <param name="credentials">应用唯一标识及应用秘钥信息</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <remarks>
    /// <para>tenant_access_token 的最大有效期是 2 小时。</para>
    /// <para>剩余有效期小于 30 分钟时，调用本接口会返回一个新的 tenant_access_token，这会同时存在两个有效的 tenant_access_token。</para>
    /// <para>剩余有效期大于等于 30 分钟时，调用本接口会返回原有的 tenant_access_token。</para>
    /// </remarks>
    /// <returns></returns>
    [Post("https://open.feishu.cn/open-apis/auth/v3/tenant_access_token/internal")]
    Task<TenantAppCredentialResult> GetTenantAccessTokenAsync(
        [Body] AppCredentials credentials,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 自建应用获取 app_access_token
    /// </summary>
    /// <param name="credentials">应用唯一标识及应用秘钥信息</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <remarks>
    /// <para>app_access_token 的最大有效期是 2 小时。</para>
    /// <para>剩余有效期小于 30 分钟时，调用本接口会返回一个新的 app_access_token，这会同时存在两个有效的 app_access_token。</para>
    /// <para>剩余有效期大于等于 30 分钟时，调用本接口会返回原有的 app_access_token。</para>
    /// </remarks>
    /// <returns></returns>
    [Post("https://open.feishu.cn/open-apis/auth/v3/app_access_token/internal")]
    Task<AppCredentialResult> GetAppAccessTokenAsync(
        [Body] AppCredentials credentials,
        CancellationToken cancellationToken = default);
}


/// <summary>
/// 应用凭证
/// </summary>
public class AppCredentials
{
    /// <summary>
    /// <para> 应用唯一标识，创建应用后获得。有关app_id 的详细介绍。请参考通用参数介绍</para>
    /// <para>示例值： "cli_slkdjalasdkjasd"</para>
    /// </summary>
    [JsonPropertyName("app_id")]
    public string AppId { get; set; }

    /// <summary>
    /// <para>应用秘钥，创建应用后获得。有关 app_secret 的详细介绍，请参考通用参数介绍</para>
    /// <para>示例值： "dskLLdkasdjlasdKK"</para>
    /// </summary>
    [JsonPropertyName("app_secret")]
    public string AppSecret { get; set; }

}

/// <summary>
/// API响应结果模型
/// </summary>
public class FeishuApiResult
{
    /// <summary>
    /// 错误码，0表示成功，非 0 取值表示失败
    /// </summary>
    [JsonPropertyName("code")]
    public int Code { get; set; }

    /// <summary>
    /// 错误描述
    /// </summary>
    [JsonPropertyName("msg")]
    public string? Msg { get; set; }
}

/// <summary>
/// 自建应用认证响应结果
/// </summary>
public class AppCredentialResult : TenantAppCredentialResult
{
    /// <summary>
    /// 应用访问凭证
    /// </summary>
    [JsonPropertyName("app_access_token")]
    public string? AppAccessToken { get; set; }
}


/// <summary>
/// 自建应用租户认证响应结果
/// </summary>
public class TenantAppCredentialResult : FeishuApiResult
{
    /// <summary>
    /// token 的过期时间，单位为秒
    /// </summary>
    [JsonPropertyName("expire")]
    public int Expire { get; set; } = 0;
    /// <summary>
    /// 租户访问凭证
    /// </summary>
    [JsonPropertyName("tenant_access_token")]
    public string? TenantAccessToken { get; set; }
}
