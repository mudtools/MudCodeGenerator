

namespace CodeGeneratorTest.WebApi;

/// <summary>
/// 钉钉用户接口
/// </summary>
[HttpClientApi("https://api.dingtalk.com", Timeout = 60)]
[HttpClientApiWrap(TokenManage = "ITokenManage", WrapInterface = "IDingTalkUser")]
public interface IDingTalkUserApi
{
    [Get("/api/v1/user/{id}")]
    Task<UserDto> GetUserAsync([Token][Header("x-token")] string token, [Path] string id);

    /// <summary>
    /// 测试钉钉用户接口
    /// </summary>
    /// <param name="token"></param>
    /// <param name="ids"></param>
    /// <returns></returns>
    [Get("/api/v1/user")]
    Task<UserDto> GetUsers1Async([Token][Header("x-token")] string token, [Query] string[] ids);

    [Get("/api/v1/user")]
    Task<UserDto> GetUsersAsync([Token][Header("x-token")] string token, [ArrayQuery] string[] ids);

    [Get("/api/v1/user")]
    Task<UserDto> GetUser1Async([Token][Header("x-token")] string token, [ArrayQuery("Ids", ";")] string[] ids);

    [Post("/api/v1/user")]
    Task<UserDto> CreateUserAsync([Token][Header("x-token")] string token, [Body] UserDto user);

    [Put("/api/v1/user/{id}")]
    Task<UserDto> UpdateUserAsync([Path] string id, [Token][Header("x-token")] string token, [Body] UserDto user);

    [Delete("/api/v1/user/{id}")]
    Task<bool> DeleteUserAsync([Token][Header("x-token")] string token, [Path] string id);

    [Get("/api/protected")]
    Task<ProtectedData> GetProtectedDataAsync([Token][Header("X-API-Key")] string apiKey, [Header("X-API-Value")] string apiValue);

    [Post("/api/protected")]
    Task<ProtectedData> GetProtectedDataAsync([Token][Header("X-API-Key")] string apiKey, [Header("X-API-Value")] string apiValue, [Body] UserDto user);

    [Post("/api/protected")]
    Task<ProtectedData> GetProtectedXmlDataAsync([Token][Header("X-API-Key")] string apiKey, [Header("X-API-Value")] string apiValue, [Body(ContentType = "application/xml", UseStringContent = true)] UserDto user);

    [Get("/api/search")]
    Task<List<UserDto>> SearchUsersAsync([Token][Header("x-token")] string token, [Query] UserSearchCriteria criteria);

    [Post("/api/v1/user")]
    Task<UserDto> CreateUserTestAsync([Token][Header("x-token")] string token, [Body] UserDto user, CancellationToken cancellationToken = default);
}

public class UserDto
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
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