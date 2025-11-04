// ------------------------------------------------------------------------
// 版权信息
// 版权归百小僧及百签科技（广东）有限公司所有。
// 所有权利保留。
// 官方网站：https://baiqian.com
//
// 许可证信息
// Furion 项目主要遵循 MIT 许可证和 Apache 许可证（版本 2.0）进行分发和使用。
namespace Mud.Common.CodeGenerator;

/// <summary>
///     HTTP 声明式 HEAD 请求方式特性
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class HeadAttribute : HttpMethodAttribute
{
    /// <summary>
    ///     <inheritdoc cref="HeadAttribute" />
    /// </summary>
    /// <param name="requestUri">请求地址</param>
    public HeadAttribute(string? requestUri = null)
        : base(HttpMethod.Head, requestUri)
    {
    }
}