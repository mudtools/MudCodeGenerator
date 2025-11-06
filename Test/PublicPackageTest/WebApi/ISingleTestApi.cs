namespace PublicPackageTest.WebApi;

[HttpClientApi("https://api.dingtalk.com", Timeout = 60)]
public interface ISingleTestApi
{
    // 以下接口生成的url需要将birthday格式化处理：birthday.ToString("yyyy-MM-dd")
    [Get("/api/v1/user/{birthday}")]
    Task<SysUserInfoOutput> GetUserAsync([Path("yyyy-MM-dd")] DateTime birthday);

    // 以下接口生成的url需要将birthday格式化处理：birthday.ToString("yyyy-MM-dd")
    [Get("/api/v1/user/{birthday}")]
    Task<SysUserInfoOutput> GetUser1Async([Path(FormatString = "yyyy-MM-dd")] DateTime birthday);

    // 以下接口生成的url为：/api/v1/user?idKey=id值
    [Get("/api/v1/user")]
    Task<SysUserInfoOutput> GetUser2Async([Query("idKey")] string id);

    // 以下接口生成的url为：/api/v1/user?idKey=id值
    [Get("/api/v1/user")]
    Task<SysUserInfoOutput> GetUser3Async([Query(Name = "idKey")] string id);

    // 以下接口生成的url为：/api/v1/user?bth=birthday.ToString("yyyy-MM-dd")值
    [Get("/api/v1/user")]
    Task<SysUserInfoOutput> GetUser2Async([Query("bth", "yyyy-MM-dd")] DateTime birthday);

    // 以下接口生成的url为：/api/v1/user?bth=birthday.ToString("yyyy-MM-dd")值
    [Get("/api/v1/user")]
    Task<SysUserInfoOutput> GetUser3Async([Query(Name = "bth", FormatString = "yyyy-MM-dd")] DateTime birthday, CancellationToken cancellationToken = default);


    [Get("/api/v2/dept")]
    Task<SysDeptInfoOutput> GetDeptPageAsync([Query] SysDeptQueryInput input, [Query] int? age, CancellationToken cancellationToken = default);

    [Post("/api/v2/dept/{id}/{age}")]
    Task<SysDeptInfoOutput> GetDeptPageAsync([Path] string id, [Query("birthday")] DateTime birthday, [Path] int? age, [Query] DataQueryInput input, CancellationToken cancellationToken = default);
}



