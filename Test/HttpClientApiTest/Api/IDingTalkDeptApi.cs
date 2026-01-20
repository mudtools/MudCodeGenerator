namespace HttpClientApiTest.WebApi;

using Mud.Common.CodeGenerator;
using CodeBaseTest.Interface;

[HttpClientApi("https://api.dingtalk.com", Timeout = 60, RegistryGroupName = "Dingtalk")]
[HttpClientApiWrap(TokenManage = "ITokenManage", WrapInterface = nameof(DingTalkDept))]
[Header("Authorization")]
public interface IDingTalkDeptApi
{
    [Get("/api/v2/dept/{id}")]
    [IgnoreImplement]
    Task<SysDeptInfoOutput> GetDeptXXXAsync([Token(TokenType.TenantAccessToken)][Header("X-API-Key")] string apiKey, [Path] string? id);

    [Get("/api/v2/dept/{id}")]
    Task<SysDeptInfoOutput?> GetDeptAsync([Token(TokenType = TokenType.UserAccessToken)][Header("X-API-Key")] string apiKey, [Query] string tid, [Path] int id);

    [Get("/api/v2/dept/{id}")]
    Task<SysDeptInfoOutput> GetDeptAsync([Path] long id, [Token][Header("X-API-Key")] string apiKey, [Query] string? tid = null);

    [Get("/api/v2/dept")]
    Task<List<SysDeptListOutput>> GetDeptPageAsync([Token][Header("X-API-Key")] string apiKey, [Query] ProjectQueryInput input);

    [Get("/api/v2/dept/{age}")]
    Task<List<SysDeptListOutput>> GetDeptPageAsync([Token][Header("X-API-Key")] string apiKey, [Query] string id, [Path] int? age, [Query] ProjectQueryInput input);

    [Post("/api/v2/dept")]
    Task<SysDeptInfoOutput> CreateDeptAsync([Token][Header("X-API-Key")] string apiKey, [Body] SysDeptCrInput Dept);

    [Put("/api/v2/dept/{id}")]
    Task<bool> UpdateDeptAsync([Token][Query("X-API-Key")] string apiKey, [Path] string id, [Body] SysDeptUpInput Dept);

    [Delete("/api/v2/dept/{id}")]
    Task<bool> DeleteDeptAsync([Token][Query("X-API-Key")] string apiKey, [Path] string id);
}