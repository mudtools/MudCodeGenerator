using Mud.Common.CodeGenerator;

/// <summary>
/// Token特性测试接口 - 使用 AppAccessToken
/// </summary>
[HttpClientApi("https://api.example.com")]
[Token(TokenType.AppAccessToken)]
public interface IAppTokenService
{
    [Get("/api/data")]
    Task<string> GetDataAsync();
}

/// <summary>
/// Token特性测试接口 - 使用 TenantAccessToken（默认）
/// </summary>
[HttpClientApi("https://api.example.com")]
[Token]
public interface ITenantTokenService
{
    [Get("/api/data")]
    Task<string> GetDataAsync();
}

/// <summary>
/// Token特性测试接口 - 使用 Both
/// </summary>
[HttpClientApi("https://api.example.com")]
[Token(TokenType.Both)]
public interface IBothTokenService
{
    [Get("/api/data")]
    Task<string> GetDataAsync();
}

/// <summary>
/// 无 Token 特性的接口
/// </summary>
[HttpClientApi("https://api.example.com")]
public interface INoTokenService
{
    [Get("/api/data")]
    Task<string> GetDataAsync();
}
