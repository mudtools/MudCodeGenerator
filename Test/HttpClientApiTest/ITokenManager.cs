namespace Mud.HttpUtils.Attributes;

/// <summary>
/// Token管理器接口
/// </summary>
public interface ITokenManager
{
    /// <summary>
    /// 获取访问令牌
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>访问令牌字符串</returns>
    Task<string> GetTokenAsync(CancellationToken cancellationToken = default);
}

public interface ITenantTokenManager : ITokenManager
{

}

public interface IUserTokenManager : ITokenManager
{

}

public interface IAppTokenManager : ITokenManager
{
}

/// <summary>
/// Token管理器实现（示例）
/// </summary>
public class TestTokenManager : ITokenManager
{
    public Task<string> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult("Bearer test-access-token");
    }
}