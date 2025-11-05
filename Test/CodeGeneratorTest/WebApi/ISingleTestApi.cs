namespace CodeGeneratorTest.WebApi;

[HttpClientApi("https://api.dingtalk.com", Timeout = 60)]
public interface ISingleTestApi
{
    [Get("/api/v2/dept")]
    Task<DeptDto> GetDeptPageAsync([Query] DataQueryInput input);

    [Post("/api/v2/dept/{age}")]
    Task<DeptDto> GetDeptPageAsync([Query] string id, [Path] int? age, [Query] DataQueryInput input);
}



