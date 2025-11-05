namespace CodeGeneratorTest.WebApi;

[HttpClientApi("https://api.dingtalk.com", Timeout = 60)]
public interface ISingleTestApi
{
    [Get("/api/v2/dept")]
    Task<DeptDto> GetDeptPageAsync([Query] DataQueryInput input, [Query] int? age, CancellationToken cancellationToken = default);

    [Post("/api/v2/dept/{age}")]
    Task<DeptDto> GetDeptPageAsync([Query] string id, [Query] DateTime? birthday, [Path] int? age, [Query] DataQueryInput input, CancellationToken cancellationToken = default);
}



