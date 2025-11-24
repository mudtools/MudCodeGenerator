namespace Mud.Common.CodeGenerator;

/// <summary>
/// Token管理器接口
/// </summary>
public interface ITokenManager
{
    /// <summary>
    /// 获取访问令牌
    /// </summary>
    /// <returns>访问令牌字符串</returns>
    Task<string> GetTokenAsync();
}

/// <summary>
/// Token管理器实现（示例）
/// </summary>
public class TestTokenManager : ITokenManager
{
    public Task<string> GetTokenAsync()
    {
        return Task.FromResult("Bearer test-access-token");
    }
}