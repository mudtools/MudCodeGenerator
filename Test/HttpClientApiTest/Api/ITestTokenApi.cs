namespace HttpClientApiTest.WebApi;

using Mud.Common.CodeGenerator;

[HttpClientApi(TokenManage = nameof(ITokenManager), IsAbstract = true)]
public interface ITestBaseTokenApi
{
    /// <summary>
    /// 基类接口中获取用户信息
    /// </summary>
    [Get("api/users/{id}")]
    Task<UserInfo> GetBaeUserAsync([Path] string id, CancellationToken cancellationToken = default);

    [Post("/api/v1/user")]
    Task<SysUserInfoOutput> CreateUserAsync([Token][Header("x-token")] string token, [Body] SysUserInfoOutput user, CancellationToken cancellationToken = default);

}

/// <summary>
/// 测试Token功能的API接口
/// </summary>
[HttpClientApi(TokenManage = nameof(IUserTokenManager), InheritedFrom = "TestBaseTokenApi")]
[Header("Authorization", AliasAs = "X-Token")]
[Header("xx1", "xxValue1")]
[Header("xx2", "xxValue3")]
public interface ITestNullTokenApi : ITestBaseTokenApi
{
}

/// <summary>
/// 测试Token功能的API接口
/// </summary>
[HttpClientApi(TokenManage = nameof(ITenantTokenManager), InheritedFrom = "TestBaseTokenApi")]
[Header("Authorization", AliasAs = "X-Token")]
[Header("xx1", "xxValue1")]
[Header("xx2", "xxValue3")]
public interface ITestTokenApi : ITestBaseTokenApi
{
    /// <summary>
    /// 获取用户信息
    /// </summary>
    [Get("api/users/{id}")]
    Task<UserInfo> GetUserAsync([Path] string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户列表
    /// </summary>
    [Get("api/users")]
    Task<List<UserInfo>> GetUsersAsync(CancellationToken cancellationToken = default);
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
    Task<UserInfo> GetUserAsync([Path] string id, CancellationToken cancellationToken = default);
}