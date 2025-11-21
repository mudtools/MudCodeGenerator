namespace CodeGeneratorTest.WebApi;

/// <summary>
/// 测试注释。
/// </summary>  
/// <remarks>
/// <para>1222</para>
/// <para>54354</para>
/// </remarks>
[HttpClientApi("https://api.dingtalk.com", Timeout = 60, RegistryGroupName = "Test", ContentType = "application/xml")]
[HttpClientApiWrap(TokenManage = "ITokenManage", WrapInterface = nameof(ISingleUserTest))]
public interface ISingleTestApi
{
    /// <summary>
    /// 根据用户ID获取用户对象。
    /// </summary>
    /// <param name="token">令牌</param>
    /// <param name="birthday">生日</param>
    /// <returns></returns>
    [Get("/api/v1/user/{birthday}")]
    Task<SysUserInfoOutput> GetUserAsync([Token(TokenType = TokenType.Both)][Header("x-token")] string token, [Path("yyyy-MM-dd")] DateTime birthday);

    /// <summary>
    /// 以下接口生成的url需要将birthday格式化处理：birthday.ToString("yyyy-MM-dd")
    /// </summary>
    /// <param name="token"></param>
    /// <param name="birthday"></param>
    /// <returns></returns>
    [Get("/api/v1/user/{birthday}")]
    Task<SysUserInfoOutput> GetUser1Async([Token][Header("x-token")] string token, [Path(FormatString = "yyyy-MM-dd")] DateTime birthday);

    /// <summary>
    /// 以下接口生成的url为：/api/v1/user?idKey=id值
    /// </summary>
    /// <param name="token"></param>
    /// <param name="id"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [Get("/api/v1/user")]
    Task<SysUserInfoOutput> GetUser2Async([Token][Header("x-token")] string token, [Query("idKey")] string id, CancellationToken cancellationToken = default);


    /// <summary>
    /// 以下接口生成的url为：/api/v1/user?bth=birthday.ToString("yyyy-MM-dd")值
    /// </summary>
    /// <param name="token"></param>
    /// <param name="birthday"></param>
    /// <returns></returns>
    [Get("/api/v1/user")]
    Task<SysUserInfoOutput> GetUser2Async([Token][Header("x-token")] string token, [Query("bth", "yyyy-MM-dd")] DateTime birthday);

    /// <summary>
    /// 测试注释1。
    /// </summary>
    /// <param name="token"></param>
    /// <param name="id"></param>
    /// <returns></returns>
    [Get("/api/v1/user")]
    Task<SysUserInfoOutput> GetUser3Async([Token][Header("x-token")] string token, [Query(Name = "idKey")] string id);


    /// <summary>
    /// 测试注释2。
    /// </summary>
    /// <param name="birthday">生日</param>
    /// <param name="token">令牌</param>
    /// <returns></returns>
    [Get("/api/v1/user")]
    Task<SysUserInfoOutput> GetUser3Async([Query(Name = "bth", FormatString = "yyyy-MM-dd")] DateTime birthday, [Token][Header("x-token")] string token);

    [Get("/api/v2/dept")]
    Task<SysDeptInfoOutput> GetDeptPageAsync([Token][Header("x-token")] string token, [Query] int? age, CancellationToken cancellationToken = default);

    [Post("/api/v2/dept/{id}/{age}")]
    Task<SysDeptInfoOutput> GetDeptPageAsync([Path] string id, [Query("birthday")] DateTime birthday, [Path] int? age, [Query] DataQueryInput input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 测试TenantAccessToken类型
    /// </summary>
    [Get("/api/v1/tenant/test")]
    Task<SysUserInfoOutput> GetTenantTestAsync([Token(TokenType.TenantAccessToken)][Header("x-token")] string token);

    /// <summary>
    /// 测试UserAccessToken类型
    /// </summary>
    [Get("/api/v1/user/test")]
    Task<SysUserInfoOutput> GetUserTestAsync([Token(TokenType.UserAccessToken)][Header("x-token")] string token);

    /// <summary>
    /// 测试TenantAccessToken类型（带CancellationToken）
    /// </summary>
    [Get("/api/v1/tenant/test/cancel")]
    Task<SysUserInfoOutput> GetTenantTestWithCancellationAsync([Token(TokenType.TenantAccessToken)][Header("x-token")] string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// 测试UserAccessToken类型（带CancellationToken）
    /// </summary>
    [Get("/api/v1/user/test/cancel")]
    Task<SysUserInfoOutput> GetUserTestWithCancellationAsync([Token(TokenType.Both)][Header("x-token")] string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// 下载文件测试
    /// </summary>
    [Get("/api/v1/file/download")]
    Task<byte[]> DownloadFileAsync([Token][Header("x-token")] string token, [Query("fileId")] string fileId);

    /// <summary>
    /// 下载文件测试（带CancellationToken）
    /// </summary>
    [Get("/api/v1/file/download/cancel")]
    Task<byte[]> DownloadFileWithCancellationAsync([Token][Header("x-token")] string token, [Query("fileId")] string fileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 下载文件测试
    /// </summary>
    [Get("/api/v1/file/download")]
    Task DownloadLargeFileAsync([Token][Header("x-token")] string token, [Query("fileId")] string fileId, [FilePath] string filePath);

    /// <summary>
    /// 下载文件测试（带CancellationToken）
    /// </summary>
    [Get("/api/v1/file/download/cancel")]
    Task DownloadLargeFileWithCancellationAsync([Token][Header("x-token")] string token, [Query("fileId")] string fileId, [FilePath(BufferSize = 40960)] string filePath, CancellationToken cancellationToken = default);

}



