using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Mud.ServiceCodeGenerator.Test
{
    /// <summary>
    /// Token管理接口测试
    /// </summary>
    public interface ITokenManage
    {
        Task<string> GetTokenAsync();
    }

    /// <summary>
    /// 钉钉Token管理接口测试
    /// </summary>
    public interface IDingTokenManage
    {
        Task<string> GetTokenAsync();
    }

    /// <summary>
    /// 系统部门查询输入
    /// </summary>
    public class SysDeptQueryInput
    {
        public string? Name { get; set; }
        public int? PageIndex { get; set; }
        public int? PageSize { get; set; }
    }

    /// <summary>
    /// 用户数据传输对象
    /// </summary>
    public class UserDto
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
    }

    /// <summary>
    /// 测试场景1：使用默认Token管理接口（ITokenManage）
    /// </summary>
    [Mud.Common.CodeGenerator.HttpClientApi("https://api.dingtalk.com", Timeout = 60)]
    [Mud.Common.CodeGenerator.HttpClientApiWrap("ITokenManage")]
    public interface ISingleTestApi
    {
        [Mud.Common.CodeGenerator.Get("/api/v1/user/{birthday}")]
        Task<UserDto> GetUserAsync([Mud.Common.CodeGenerator.Token][Mud.Common.CodeGenerator.Header("x-token")] string token, [Mud.Common.CodeGenerator.Path("yyyy-MM-dd")] DateTime birthday);

        [Mud.Common.CodeGenerator.Get("/api/v1/user/{birthday}")]
        Task<UserDto> GetUser1Async([Mud.Common.CodeGenerator.Token][Mud.Common.CodeGenerator.Header("x-token")] string token, [Mud.Common.CodeGenerator.Query] SysDeptQueryInput input, [Mud.Common.CodeGenerator.Query] int? age, System.Threading.CancellationToken cancellationToken = default);

        [Mud.Common.CodeGenerator.Get("/api/v1/tenant/test")]
        Task<UserDto> GetTenantTestAsync([Mud.Common.CodeGenerator.Token(Mud.Common.CodeGenerator.FeishuTokenType.TenantAccessToken)][Mud.Common.CodeGenerator.Header("x-token")] string token);

        [Mud.Common.CodeGenerator.Get("/api/v1/user/test")]
        Task<UserDto> GetUserTestAsync([Mud.Common.CodeGenerator.Token(Mud.Common.CodeGenerator.FeishuTokenType.UserAccessToken)][Mud.Common.CodeGenerator.Header("x-token")] string token);
    }

    /// <summary>
    /// 测试场景2：使用指定的Token管理接口（IDingTokenManage）
    /// </summary>
    [Mud.Common.CodeGenerator.HttpClientApi("https://api.dingtalk.com", Timeout = 60)]
    [Mud.Common.CodeGenerator.HttpClientApiWrap(TokenManage = "IDingTokenManage")]
    public interface ISingleTestApi2
    {
        [Mud.Common.CodeGenerator.Get("/api/v1/user/{birthday}")]
        Task<UserDto> GetUserAsync([Mud.Common.CodeGenerator.Token][Mud.Common.CodeGenerator.Header("x-token")] string token, [Mud.Common.CodeGenerator.Path("yyyy-MM-dd")] DateTime birthday);

        [Mud.Common.CodeGenerator.Get("/api/v1/user/{birthday}")]
        Task<UserDto> GetUser1Async([Mud.Common.CodeGenerator.Query] SysDeptQueryInput input, [Mud.Common.CodeGenerator.Token][Mud.Common.CodeGenerator.Header("x-token")] string token, [Mud.Common.CodeGenerator.Query] int? age, System.Threading.CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// 测试场景3：使用指定的Token管理接口和包装接口名称
    /// </summary>
    [Mud.Common.CodeGenerator.HttpClientApi("https://api.dingtalk.com", Timeout = 60)]
    [Mud.Common.CodeGenerator.HttpClientApiWrap(TokenManage = "ITokenManage", WrapInterface = "IDingTalkUserWrap")]
    public interface ISingleTestApi3
    {
        [Mud.Common.CodeGenerator.Get("/api/v1/user/{birthday}")]
        Task<UserDto> GetUserAsync([Mud.Common.CodeGenerator.Token][Mud.Common.CodeGenerator.Header("x-token")] string token, [Mud.Common.CodeGenerator.Path("yyyy-MM-dd")] DateTime birthday);

        [Mud.Common.CodeGenerator.Get("/api/v1/user/{birthday}")]
        Task<UserDto> GetUser1Async([Mud.Common.CodeGenerator.Token][Mud.Common.CodeGenerator.Header("x-token")] string token, [Mud.Common.CodeGenerator.Query] SysDeptQueryInput input, [Mud.Common.CodeGenerator.Query] int? age, System.Threading.CancellationToken cancellationToken = default);

        [Mud.Common.CodeGenerator.Get("/api/v1/tenant/test")]
        Task<UserDto> GetTenantTestAsync([Mud.Common.CodeGenerator.Token(Mud.Common.CodeGenerator.FeishuTokenType.TenantAccessToken)][Mud.Common.CodeGenerator.Header("x-token")] string token);

        [Mud.Common.CodeGenerator.Get("/api/v1/user/test")]
        Task<UserDto> GetUserTestAsync([Mud.Common.CodeGenerator.Token(Mud.Common.CodeGenerator.FeishuTokenType.UserAccessToken)][Mud.Common.CodeGenerator.Header("x-token")] string token);
    }

    /// <summary>
    /// 测试场景4：测试不同的Token类型
    /// </summary>
    [Mud.Common.CodeGenerator.HttpClientApi("https://api.feishu.com", Timeout = 60)]
    [Mud.Common.CodeGenerator.HttpClientApiWrap(TokenManage = "ITokenManage")]
    public interface IFeishuTestApi
    {
        [Mud.Common.CodeGenerator.Get("/api/v1/tenant/info")]
        Task<UserDto> GetTenantInfoAsync([Mud.Common.CodeGenerator.Token(Mud.Common.CodeGenerator.FeishuTokenType.TenantAccessToken)][Mud.Common.CodeGenerator.Header("X-API-Key")] string token);

        [Mud.Common.CodeGenerator.Get("/api/v1/user/info")]
        Task<UserDto> GetUserInfoAsync([Mud.Common.CodeGenerator.Token(Mud.Common.CodeGenerator.FeishuTokenType.UserAccessToken)][Mud.Common.CodeGenerator.Header("X-API-Key")] string token);

        [Mud.Common.CodeGenerator.Get("/api/v1/mixed/test")]
        Task<UserDto> GetMixedTestAsync([Mud.Common.CodeGenerator.Token][Mud.Common.CodeGenerator.Header("x-token")] string token);
    }
}