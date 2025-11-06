

namespace PublicPackageTest.WebApi;

/// <summary>
/// 钉钉用户接口
/// </summary>
[HttpClientApi("https://api.dingtalk.com", Timeout = 60)]
public interface IDingTalkUserApi
{
    [Get("/api/v1/user/{id}")]
    Task<ResponseResult<SysUserInfoOutput>> GetUserAsync([Query] string id, CancellationToken cancellationToken = default);

    [Post("/api/v1/user")]
    Task<ResponseResult<bool>> CreateUserAsync([Body] SysUserCrInput user, CancellationToken cancellationToken = default);

    [Put("/api/v1/user/{id}")]
    Task<ResponseResult<bool>> UpdateUserAsync([Path] string id, [Body] SysClientUpInput user, CancellationToken cancellationToken = default);

    [Delete("/api/v1/user/{id}")]
    Task<ResponseResult<bool>> DeleteUserAsync([Path] string id, CancellationToken cancellationToken = default);

    [Get("/api/protected")]
    Task<ProtectedData> GetProtectedDataAsync([Header("X-API-Key")] string apiKey, [Header("X-API-Value")] string apiValue, CancellationToken cancellationToken = default);

    [Post("/api/protected")]
    Task<ProtectedData> GetProtectedDataAsync([Header("X-API-Key")] string apiKey, [Header("X-API-Value")] string apiValue, [Body] SysUserCrInput user, CancellationToken cancellationToken = default);

    [Post("/api/protected")]
    Task<ProtectedData> GetProtectedXmlDataAsync([Header("X-API-Key")] string apiKey, [Header("X-API-Value")] string apiValue, [Body(ContentType = "application/xml", UseStringContent = true)] string userJson, CancellationToken cancellationToken = default);

    [Get("/api/search")]
    Task<ResponseResult<List<SysUserListOutput>>> SearchUsersAsync([Query] SysUserQueryInput criteria, CancellationToken cancellationToken = default);

    [Post("/api/v1/user")]
    Task<ResponseResult<bool>> CreateUserTestAsync([Body] SysUserCrInput user, CancellationToken cancellationToken = default);
}


public class ProtectedData
{
    public string Data { get; set; }
}
