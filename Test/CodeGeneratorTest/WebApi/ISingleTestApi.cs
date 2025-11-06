namespace CodeGeneratorTest.WebApi;

[HttpClientApi("https://api.dingtalk.com", Timeout = 60)]
public interface ISingleTestApi
{
    [Get("/api/v2/dept")]
    Task<SysDeptInfoOutput> GetDeptPageAsync([Query] SysDeptQueryInput input, [Query] int? age, CancellationToken cancellationToken = default);

    [Post("/api/v2/dept/{id}/{age}")]
    Task<SysDeptInfoOutput> GetDeptPageAsync([Path] string id, [Query] DateTime? birthday, [Path] int? age, [Query] DataQueryInput input, CancellationToken cancellationToken = default);
}



