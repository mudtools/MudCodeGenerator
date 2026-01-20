namespace HttpClientApiTest.Api;

using Mud.Common.CodeGenerator;

/// <summary>
/// Token测试基类接口
/// 抽象基类，定义了通用的Token测试方法
/// </summary>
[HttpClientApi(TokenManage = nameof(ITokenManager), IsAbstract = true)]
public interface ITestBaseTokenApi
{
    /// <summary>
    /// 测试：基类接口中获取用户信息
    /// 接口：GET api/users/{id}
    /// 特点：基类方法，使用默认Token
    /// </summary>
    [Get("api/users/{id}")]
    Task<UserInfo> GetBaeUserAsync([Path] string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 测试：基类接口中创建用户
    /// 接口：POST /api/v1/user
    /// 特点：基类方法，使用默认Token，包含用户创建请求体
    /// </summary>
    [Post("/api/v1/user")]
    Task<SysUserInfoOutput> CreateUserAsync([Token][Header("x-token")] string token, [Body] SysUserInfoOutput user, CancellationToken cancellationToken = default);

}

/// <summary>
/// Null Token测试接口
/// 测试使用IUserTokenManager的场景
/// </summary>
[HttpClientApi(TokenManage = nameof(IUserTokenManager), InheritedFrom = "TestBaseTokenApi")]
[Header("Authorization", AliasAs = "X-Token")]
[Header("xx1", "xxValue1")]
[Header("xx2", "xxValue3")]
public interface ITestNullTokenApi : ITestBaseTokenApi
{
}

/// <summary>
/// Tenant Token测试接口
/// 测试使用ITenantTokenManager的场景
/// </summary>
[HttpClientApi(TokenManage = nameof(ITenantTokenManager), InheritedFrom = "TestBaseTokenApi")]
[Header("Authorization", AliasAs = "X-Token")]
[Header("xx1", "xxValue1")]
[Header("xx2", "xxValue3")]
public interface ITestTokenApi : ITestBaseTokenApi
{
    /// <summary>
    /// 测试：获取用户信息（TenantToken）
    /// 接口：GET api/users/{id}
    /// 特点：使用TenantTokenManager，重写基类方法
    /// </summary>
    [Get("api/users/{id}")]
    Task<UserInfo> GetUserAsync([Path] string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 测试：获取用户列表（TenantToken）
    /// 接口：GET api/users
    /// 特点：使用TenantTokenManager
    /// </summary>
    [Get("api/users")]
    Task<List<UserInfo>> GetUsersAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Query Authorization测试接口
/// 测试通过Query参数传递Token的场景
/// </summary>
[HttpClientApi(TokenManage = nameof(ITokenManager))]
[Query("Authorization", AliasAs = "X-Token")]
public interface ITestTokenQueryApi
{
    /// <summary>
    /// 测试：获取用户信息（Query参数传递Token）
    /// 接口：GET api/users/{id}
    /// 特点：通过Query参数传递Authorization
    /// </summary>
    [Get("api/users/{id}")]
    Task<UserInfo> GetUserAsync([Path] string id, CancellationToken cancellationToken = default);
}