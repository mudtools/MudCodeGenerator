namespace CodeGeneratorTest.WebApi;

/// <summary>
/// 飞书用户API接口
/// </summary>
[HttpClientApi]
public interface IFeishuUserApi
{
    /// <summary>
    /// 获取用户信息
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户信息</returns>
    [Get("/open-apis/contact/v3/users/{userId}")]
    Task<UserInfo?> GetUserAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 使用绝对URL获取用户信息
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户信息</returns>
    [Get("https://api.example.com/users/{userId}")]
    Task<UserInfo?> GetUserAbsoluteAsync(string userId, CancellationToken cancellationToken = default);
}

/// <summary>
/// 用户信息模型
/// </summary>
public class UserInfo
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// 邮箱
    /// </summary>
    public string? Email { get; set; }
}