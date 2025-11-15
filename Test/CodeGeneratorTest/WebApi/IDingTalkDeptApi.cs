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

}


