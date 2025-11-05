namespace CodeGeneratorTest.WebApi;

[HttpClientApi("https://api.dingtalk.com", Timeout = 60)]
public interface IDingTalkDeptApi
{
    [Get("/api/v2/dept/{id}")]
    Task<DeptDto> GetDeptAsync([Query] string id);

    [Get("/api/v2/dept")]
    Task<DeptDto> GetDeptPageAsync([Query] DataQueryInput input);

    [Get("/api/v2/dept/{id}")]
    Task<DeptDto> GetDeptPageAsync([Query] string id, [Query] int? age, [Query] DataQueryInput input);

    [Post("/api/v2/dept")]
    Task<DeptDto> CreateDeptAsync([Body] DeptDto Dept);

    [Put("/api/v2/dept/{id}")]
    Task<DeptDto> UpdateDeptAsync([Path] string id, [Body] DeptDto Dept);

    [Delete("/api/v2/dept/{id}")]
    Task<bool> DeleteDeptAsync([Path] string id);
}

public class DeptDto
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string ParentId { get; set; }
}


