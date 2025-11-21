

namespace CodeGeneratorTest.WebApi;

/// <summary>
/// 钉钉用户接口
/// </summary>
[HttpClientApi("https://api.dingtalk.com", Timeout = 60, Group = "Dingtalk")]
[HttpClientApiWrap(TokenManage = "ITokenManage", WrapInterface = "IDingTalkUser")]
public interface IDingTalkUserApi
{
    [Get("/api/v1/user/{id}")]
    Task<SysUserInfoOutput> GetUserAsync([Token][Header("x-token")] string token, [Path] string id = "xxx");

    /// <summary>
    /// 测试钉钉用户接口
    /// </summary>
    /// <param name="token"></param>
    /// <param name="ids"></param>
    /// <returns></returns>
    [Get("/api/v1/user")]
    Task<SysUserInfoOutput> GetUsers1Async([Token][Header("x-token")] string token, [Query] string[] ids);

    [Get("/api/v1/user")]
    Task<SysUserInfoOutput> GetUsersAsync([Token][Header("x-token")] string token, [ArrayQuery] string[] ids);

    [Get("/api/v1/user")]
    Task<SysUserInfoOutput> GetUser1Async([Token][Header("x-token")] string token, [ArrayQuery("Ids", ";")] string[] ids);

    [Post("/api/v1/user")]
    Task<SysUserInfoOutput> CreateUserAsync([Token][Header("x-token")] string token, [Body] SysUserInfoOutput user);

    [Put("/api/v1/user/{id}")]
    Task<SysUserInfoOutput> UpdateUserAsync([Path] string id, [Token][Header("x-token")] string token, [Body] SysUserInfoOutput user);

    [Delete("/api/v1/user/{id}")]
    Task<bool> DeleteUserAsync([Token][Header("x-token")] string token, [Path] string id);

    [Get("/api/protected")]
    Task<ProtectedData> GetProtectedDataAsync([Token][Header("X-API-Key")] string apiKey, [Header("X-API-Value")] string apiValue);

    [Post("/api/protected")]
    Task<ProtectedData> GetProtectedDataAsync([Token][Header("X-API-Key")] string apiKey, [Header("X-API-Value")] string apiValue, [Body] SysUserInfoOutput user);

    [Post("/api/protected")]
    Task<ProtectedData> GetProtectedXmlDataAsync([Token][Header("X-API-Key")] string apiKey, [Header("X-API-Value")] string apiValue, [Body(ContentType = "application/xml", UseStringContent = true)] SysUserInfoOutput user);

    [Get("/api/search")]
    Task<List<SysUserInfoOutput>> SearchUsersAsync([Token][Header("x-token")] string token, [Query] UserSearchCriteria criteria);

    [Post("/api/v1/user")]
    Task<SysUserInfoOutput> CreateUserTestAsync([Token][Header("x-token")] string token, [Body] SysUserInfoOutput user, CancellationToken cancellationToken = default);
}


public class ProtectedData
{
    public string Data { get; set; }
}

public class UserSearchCriteria
{
    public string Name { get; set; }
    public int? Age { get; set; }
    public string Department { get; set; }
}