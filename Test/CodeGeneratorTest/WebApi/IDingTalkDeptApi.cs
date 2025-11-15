namespace CodeGeneratorTest.WebApi;

[HttpClientApi("https://api.dingtalk.com", Timeout = 60)]
[HttpClientApiWrap(TokenManage = "ITokenManage")]
public interface IDingTalkDeptApi
{
    [Get("/api/v2/dept/{id}")]
    Task<SysDeptInfoOutput> GetDeptAsync([Path] string id);

    [Get("/api/v2/dept/{id}")]
    Task<SysDeptInfoOutput> GetDeptAsync([Header("X-API-Key")] string apiKey, [Query] string tid, [Path] int id);

    [Get("/api/v2/dept/{id}")]
    Task<SysDeptInfoOutput> GetDeptAsync([Path] long id, [Query] string tid);

    [Get("/api/v2/dept")]
    Task<List<SysDeptListOutput>> GetDeptPageAsync([Query] ProjectQueryInput input);

    [Get("/api/v2/dept/{age}")]
    Task<List<SysDeptListOutput>> GetDeptPageAsync([Query] string id, [Path] int? age, [Query] ProjectQueryInput input);

    [Post("/api/v2/dept")]
    Task<SysDeptInfoOutput> CreateDeptAsync([Body] SysDeptCrInput Dept);

    [Put("/api/v2/dept/{id}")]
    Task<bool> UpdateDeptAsync([Path] string id, [Body] SysDeptUpInput Dept);

    [Delete("/api/v2/dept/{id}")]
    Task<bool> DeleteDeptAsync([Path] string id);
}


