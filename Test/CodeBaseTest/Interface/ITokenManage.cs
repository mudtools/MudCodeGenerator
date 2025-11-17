namespace CodeBaseTest.Interface;

public interface ITokenManage
{
    Task<string> GetTokenAsync();

    Task<string> GetTenantAccessTokenAsync();

    Task<string> GetUserAccessTokenAsync();
}
