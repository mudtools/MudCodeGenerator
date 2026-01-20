namespace HttpClientApiTest.WebApi;

using Mud.Common.CodeGenerator;
using System.Text.Json.Serialization;
using CodeBaseTest.Interface;

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