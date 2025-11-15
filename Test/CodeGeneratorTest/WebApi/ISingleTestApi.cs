namespace CodeGeneratorTest.WebApi;

/// <summary>
/// 测试注释。
/// </summary>
[HttpClientApi("https://api.dingtalk.com", Timeout = 60)]
[HttpClientApiWrap(TokenManage = "ITokenManage")]
public interface ISingleTestApi
{
    /// <summary>
    /// 根据用户ID获取用户对象。
    /// </summary>
    /// <param name="token">令牌</param>
    /// <param name="birthday">生日</param>
    /// <returns></returns>
    [Get("/api/v1/user/{birthday}")]
    Task<UserDto> GetUserAsync([Token][Header("x-token")] string token, [Path("yyyy-MM-dd")] DateTime birthday);

    // 以下接口生成的url需要将birthday格式化处理：birthday.ToString("yyyy-MM-dd")
    [Get("/api/v1/user/{birthday}")]
    Task<UserDto> GetUser1Async([Token][Header("x-token")] string token, [Path(FormatString = "yyyy-MM-dd")] DateTime birthday);

    // 以下接口生成的url为：/api/v1/user?idKey=id值
    [Get("/api/v1/user")]
    Task<UserDto> GetUser2Async([Token][Header("x-token")] string token, [Query("idKey")] string id, CancellationToken cancellationToken = default);

    // 以下接口生成的url为：/api/v1/user?idKey=id值
    [Get("/api/v1/user")]
    Task<UserDto> GetUser3Async([Token][Header("x-token")] string token, [Query(Name = "idKey")] string id);

    // 以下接口生成的url为：/api/v1/user?bth=birthday.ToString("yyyy-MM-dd")值
    [Get("/api/v1/user")]
    Task<UserDto> GetUser2Async([Token][Header("x-token")] string token, [Query("bth", "yyyy-MM-dd")] DateTime birthday);

    // 以下接口生成的url为：/api/v1/user?bth=birthday.ToString("yyyy-MM-dd")值
    [Get("/api/v1/user")]
    Task<UserDto> GetUser3Async([Query(Name = "bth", FormatString = "yyyy-MM-dd")] DateTime birthday, [Token][Header("x-token")] string token);


    [Get("/api/v2/dept")]
    Task<SysDeptInfoOutput> GetDeptPageAsync([Query] SysDeptQueryInput input, [Query] int? age, CancellationToken cancellationToken = default);

    [Post("/api/v2/dept/{id}/{age}")]
    Task<SysDeptInfoOutput> GetDeptPageAsync([Path] string id, [Query("birthday")] DateTime birthday, [Path] int? age, [Query] DataQueryInput input, CancellationToken cancellationToken = default);
}



