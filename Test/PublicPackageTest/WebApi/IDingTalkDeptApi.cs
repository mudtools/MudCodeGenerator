namespace PublicPackageTest.WebApi;

[HttpClientApi("https://api.dingtalk.com", Timeout = 60)]
public interface IDingTalkDeptApi
{
    [Get("/api/v2/dept/{id}")]
    Task<ResponseResult<SysDeptInfoOutput>> GetDeptAsync([Path] string id, CancellationToken cancellationToken = default);

    [Get("/api/v2/dept/{id}")]
    Task<ResponseResult<SysDeptInfoOutput>> GetDeptAsync([Header("X-API-Key")] string apiKey, [Query] string tid, [Path] int id, CancellationToken cancellationToken = default);

    [Get("/api/v2/dept/{id}")]
    Task<ResponseResult<SysDeptInfoOutput>> GetDeptAsync([Path] long id, [Query] string tid, CancellationToken cancellationToken = default);

    [Get("/api/v2/dept")]
    Task<ResponseResult<List<SysDeptListOutput>>> GetDeptPageAsync([Query] ProjectQueryInput input, CancellationToken cancellationToken = default);

    [Get("/api/v2/dept/{age}")]
    Task<ResponseResult<List<SysDeptListOutput>>> GetDeptPageAsync([Query] string id, [Path] int? age, [Query] ProjectQueryInput input, CancellationToken cancellationToken = default);

    [Post("/api/v2/dept")]
    Task<ResponseResult<SysDeptInfoOutput>> CreateDeptAsync([Body] SysDeptCrInput Dept, CancellationToken cancellationToken = default);

    [Put("/api/v2/dept/{id}")]
    Task<ResponseResult<bool>> UpdateDeptAsync([Path] string id, [Body] SysDeptUpInput Dept, CancellationToken cancellationToken = default);

    [Delete("/api/v2/dept/{id}")]
    Task<ResponseResult<bool>> DeleteDeptAsync([Path] string id, CancellationToken cancellationToken = default);
}


