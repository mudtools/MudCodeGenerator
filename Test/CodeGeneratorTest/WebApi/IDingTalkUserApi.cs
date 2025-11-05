

namespace CodeGeneratorTest.WebApi;

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
}

public class UserDto
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
}