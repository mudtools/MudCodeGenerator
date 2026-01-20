namespace HttpClientApiTest.Api;

using Mud.Common.CodeGenerator;

/// <summary>
/// 飞书用户API测试接口
/// 测试飞书用户相关的API功能，包括相对路径和绝对路径的使用
/// </summary>
[HttpClientApi(BaseAddress = "https://api.example.com", Timeout = 120)]
public interface IFeishuUserApi
{
    /// <summary>
    /// 测试：获取用户信息（相对路径）
    /// 接口：GET /open-apis/contact/v3/users/{userId}
    /// 特点：使用相对路径，基于接口级BaseAddress
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户信息</returns>
    [Get("/open-apis/contact/v3/users/{userId}")]
    Task<UserInfo?> GetUserAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 测试：获取用户信息（绝对路径）
    /// 接口：GET https://api.example.com/users/{userId}
    /// 特点：使用绝对URL，覆盖接口级BaseAddress
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
