// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.CodeGenerator 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.ServiceCodeGenerator.HttpInvoke;

/// <summary>
/// 表示 HttpClient Wrap API 的元数据信息
/// </summary>
/// <remarks>
/// 继承自 HttpClientApiInfoBase，包含包装API特有的信息
/// </remarks>
public sealed class HttpClientWrapApiInfo : HttpClientApiInfoBase
{
    /// <summary>
    /// 初始化 HttpClient Wrap API 信息
    /// </summary>
    /// <param name="originalInterfaceName">原始接口名称</param>
    /// <param name="wrapInterfaceName">包装接口名称</param>
    /// <param name="wrapClassName">包装类名称</param>
    /// <param name="namespaceName">命名空间名称</param>
    /// <param name="baseUrl">API 基础地址</param>
    /// <param name="timeout">超时时间（秒）</param>
    /// <param name="registryGroupName">注册组名称</param>
    public HttpClientWrapApiInfo(string originalInterfaceName, string wrapInterfaceName, string wrapClassName, string namespaceName, string baseUrl, int timeout, string? registryGroupName = null)
        : base(namespaceName, baseUrl, timeout, registryGroupName)
    {
        OriginalInterfaceName = originalInterfaceName ?? throw new ArgumentNullException(nameof(originalInterfaceName));
        WrapInterfaceName = wrapInterfaceName ?? throw new ArgumentNullException(nameof(wrapInterfaceName));
        WrapClassName = wrapClassName ?? throw new ArgumentNullException(nameof(wrapClassName));
    }

    /// <summary>
    /// 原始接口名称
    /// </summary>
    public string OriginalInterfaceName { get; }

    /// <summary>
    /// 包装接口名称
    /// </summary>
    public string WrapInterfaceName { get; }

    /// <summary>
    /// 包装类名称
    /// </summary>
    public string WrapClassName { get; }
}