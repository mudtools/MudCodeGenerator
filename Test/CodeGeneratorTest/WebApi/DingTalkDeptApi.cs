namespace CodeGeneratorTest.WebApi.Internal;

partial class DingTalkDeptApi : IDingTalkDeptApi
{
    public async Task<SysDeptInfoOutput> GetDeptXXXAsync([Token(TokenType.TenantAccessToken)][Header("X-API-Key")] string apiKey, [Path] string id)
    {
        return null;
    }
}
