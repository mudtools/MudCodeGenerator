namespace CodeBaseTest.Interface;

public interface ITokenManage
{
    Task<string> GetTokenAsync(CancellationToken cancellationToken = default);

    Task<string> GetTenantAccessTokenAsync(CancellationToken cancellationToken = default);

    Task<string> GetUserAccessTokenAsync(CancellationToken cancellationToken = default);
}
