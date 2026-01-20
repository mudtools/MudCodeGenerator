namespace HttpClientApiTest.Api;

using Mud.Common.CodeGenerator;

/// <summary>
/// 钉钉用户API测试接口
/// 测试各种用户相关的API功能，包括不同Token类型、参数位置、数组查询等场景
/// </summary>
[HttpClientApi("https://api.dingtalk.com", Timeout = 60, RegistryGroupName = "Dingtalk")]
[HttpClientApiWrap(TokenManage = "ITokenManage", WrapInterface = "IDingTalkUser")]
public interface IDingTalkUserApi
{
    /// <summary>
    /// 测试：根据用户ID获取用户信息（默认值路径参数）
    /// 接口：GET /api/v1/user/{id}
    /// 特点：使用默认Token，id路径参数有默认值
    /// </summary>
    [Get("/api/v1/user/{id}")]
    Task<SysUserInfoOutput> GetUserAsync([Token][Header("x-token")] string token, [Path] string id = "xxx");

    /// <summary>
    /// 测试：获取多个用户信息（普通数组查询参数）
    /// 接口：GET /api/v1/user
    /// 特点：使用默认Token，普通数组查询参数
    /// </summary>
    [Get("/api/v1/user")]
    Task<SysUserInfoOutput> GetUsers1Async([Token][Header("x-token")] string token, [Query] string[] ids);

    /// <summary>
    /// 测试：获取多个用户信息（ArrayQuery属性）
    /// 接口：GET /api/v1/user
    /// 特点：使用默认Token，ArrayQuery属性
    /// </summary>
    [Get("/api/v1/user")]
    Task<SysUserInfoOutput> GetUsersAsync([Token][Header("x-token")] string token, [ArrayQuery] string[] ids);

    /// <summary>
    /// 测试：获取多个用户信息（自定义ArrayQuery分隔符）
    /// 接口：GET /api/v1/user
    /// 特点：使用默认Token，自定义ArrayQuery名称和分隔符
    /// </summary>
    [Get("/api/v1/user")]
    Task<SysUserInfoOutput> GetUser1Async([Token][Header("x-token")] string token, [ArrayQuery("Ids", ";")] string[] ids);

    /// <summary>
    /// 测试：创建用户
    /// 接口：POST /api/v1/user
    /// 特点：使用默认Token，包含用户创建请求体
    /// </summary>
    [Post("/api/v1/user")]
    Task<SysUserInfoOutput> CreateUserAsync([Token][Header("x-token")] string token, [Body] SysUserInfoOutput user);

    /// <summary>
    /// 测试：更新用户信息
    /// 接口：PUT /api/v1/user/{id}
    /// 特点：使用默认Token，包含用户更新请求体
    /// </summary>
    [Put("/api/v1/user/{id}")]
    Task<SysUserInfoOutput> UpdateUserAsync([Path] string id, [Token][Header("x-token")] string token, [Body] SysUserInfoOutput user);

    /// <summary>
    /// 测试：删除用户
    /// 接口：DELETE /api/v1/user/{id}
    /// 特点：使用默认Token
    /// </summary>
    [Delete("/api/v1/user/{id}")]
    Task<bool> DeleteUserAsync([Token][Header("x-token")] string token, [Path] string id);

    /// <summary>
    /// 测试：获取受保护数据（GET方式）
    /// 接口：GET /api/protected
    /// 特点：使用默认Token，API Key和Value通过Header传递
    /// </summary>
    [Get("/api/protected")]
    Task<ProtectedData> GetProtectedDataAsync([Token][Header("X-API-Key")] string apiKey, [Header("X-API-Value")] string apiValue);

    /// <summary>
    /// 测试：获取受保护数据（POST方式，默认Content-Type）
    /// 接口：POST /api/protected
    /// 特点：使用默认Token，API Key和Value通过Header传递，包含用户请求体
    /// </summary>
    [Post("/api/protected")]
    Task<ProtectedData> GetProtectedDataAsync([Token][Header("X-API-Key")] string apiKey, [Header("X-API-Value")] string apiValue, [Body] SysUserInfoOutput user);

    /// <summary>
    /// 测试：获取受保护数据（POST方式，XML Content-Type）
    /// 接口：POST /api/protected
    /// 特点：使用默认Token，API Key和Value通过Header传递，XML格式请求体
    /// </summary>
    [Post("/api/protected")]
    Task<ProtectedData> GetProtectedXmlDataAsync([Token][Header("X-API-Key")] string apiKey, [Header("X-API-Value")] string apiValue, [Body(ContentType = "application/xml", UseStringContent = true)] SysUserInfoOutput user);

    /// <summary>
    /// 测试：搜索用户
    /// 接口：GET /api/search
    /// 特点：使用默认Token，复杂查询参数
    /// </summary>
    [Get("/api/search")]
    Task<List<SysUserInfoOutput>> SearchUsersAsync([Token][Header("x-token")] string token, [Query] UserSearchCriteria criteria);

    /// <summary>
    /// 测试：创建用户（带CancellationToken）
    /// 接口：POST /api/v1/user
    /// 特点：使用默认Token，包含CancellationToken参数
    /// </summary>
    [Post("/api/v1/user")]
    Task<SysUserInfoOutput> CreateUserTestAsync([Token][Header("x-token")] string token, [Body] SysUserInfoOutput user, CancellationToken cancellationToken = default);
}