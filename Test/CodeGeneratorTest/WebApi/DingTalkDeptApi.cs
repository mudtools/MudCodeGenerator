namespace CodeGeneratorTest.WebApi;

partial class DingTalkDeptApi : IDingTalkDeptApi
{
    partial void OnCreateDeptBefore(HttpRequestMessage request, string url)
    {
        request.Headers.Add("X-Test-Header", "TestValue");
    }

    partial void OnDingTalkDeptRequestBefore(HttpRequestMessage request, string url)
    {
        request.Headers.Add("X-APX-Header", "TestValue");
    }

    partial void OnDingTalkDeptRequestAfter(HttpResponseMessage response, string url)
    {
        if (!response.IsSuccessStatusCode)
        {
            // Log error or throw custom exception
            throw new HttpRequestException($"Request to {url} failed with status code {response.StatusCode}");
        }
    }

    public async Task<SysDeptInfoOutput> GetDeptXXXAsync([Token(TokenType.TenantAccessToken)][Header("X-API-Key")] string apiKey, [Path] string id)
    {
        return null;
    }
}
