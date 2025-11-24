namespace CodeGeneratorTest.WebApi;

/// <summary>
/// 测试Token功能的API接口
/// </summary>
[HttpClientApi(TokenManage = nameof(ITokenManager))]
[Header("Authorization")]
public interface ITestTokenApi
{
    /// <summary>
    /// 获取用户信息
    /// </summary>
    [Get("api/users/{id}")]
    Task<UserInfo> GetUserAsync([Path] string id);

    /// <summary>
    /// 获取用户列表
    /// </summary>
    [Get("api/users")]
    Task<List<UserInfo>> GetUsersAsync();
}

/// <summary>
/// 测试Query Authorization的API接口
/// </summary>
[HttpClientApi(TokenManage = nameof(ITokenManager))]
[Query("Authorization", AliasAs = "X-Token")]
public interface ITestTokenQueryApi
{
    /// <summary>
    /// 获取用户信息（使用Query参数传递Token）
    /// </summary>
    [Get("api/users/{id}")]
    Task<UserInfo> GetUserAsync([Path] string id);
}

/// <summary>
/// 用户信息模型
/// </summary>
public class UserInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}