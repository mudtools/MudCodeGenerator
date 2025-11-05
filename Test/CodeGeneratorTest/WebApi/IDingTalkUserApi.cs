

namespace CodeGeneratorTest.WebApi;

/// <summary>
/// 钉钉用户接口
/// </summary>
[HttpClientApi("https://api.dingtalk.com", Timeout = 60)]
public interface IDingTalkUserApi
{
    [Get("/api/v1/user/{id}")]
    Task<UserDto> GetUserAsync([Query] string id);

    [Post("/api/v1/user")]
    Task<UserDto> CreateUserAsync([Body] UserDto user);

    [Put("/api/v1/user/{id}")]
    Task<UserDto> UpdateUserAsync([Path] string id, [Body] UserDto user);

    [Delete("/api/v1/user/{id}")]
    Task<bool> DeleteUserAsync([Path] string id);

    [Get("/api/protected")]
    Task<ProtectedData> GetProtectedDataAsync([Header("X-API-Key")] string apiKey, [Header("X-API-Value")] string apiValue);

    [Post("/api/protected")]
    Task<ProtectedData> GetProtectedDataAsync([Header("X-API-Key")] string apiKey, [Header("X-API-Value")] string apiValue, [Body] UserDto user);

    [Post("/api/protected")]
    Task<ProtectedData> GetProtectedXmlDataAsync([Header("X-API-Key")] string apiKey, [Header("X-API-Value")] string apiValue, [Body(ContentType = "application/xml")] UserDto user);

    [Get("/api/search")]
    Task<List<UserDto>> SearchUsersAsync([Query] UserSearchCriteria criteria);
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