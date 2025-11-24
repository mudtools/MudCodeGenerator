// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.CodeGenerator 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.ServiceCodeGenerator.ApiWrap.Strategies;

/// <summary>
/// 参数处理器接口
/// </summary>
/// <remarks>
/// 定义参数处理的统一接口，支持不同类型参数的生成策略
/// </remarks>
internal interface IParameterProcessor
{
    /// <summary>
    /// 判断是否支持处理指定的参数
    /// </summary>
    /// <param name="parameter">参数信息</param>
    /// <returns>是否支持处理</returns>
    bool CanProcess(ParameterInfo parameter);

    /// <summary>
    /// 生成参数处理代码
    /// </summary>
    /// <param name="parameter">参数信息</param>
    /// <param name="context">生成上下文</param>
    /// <returns>生成的代码</returns>
    string GenerateCode(ParameterInfo parameter, ParameterGenerationContext context);
}

/// <summary>
/// 参数生成上下文
/// </summary>
/// <remarks>
/// 包含参数生成过程中需要的上下文信息
/// </remarks>
internal class ParameterGenerationContext
{
    /// <summary>
    /// HTTP 请求变量名
    /// </summary>
    public string HttpRequestVariable { get; set; } = "request";

    /// <summary>
    /// URL 变量名
    /// </summary>
    public string UrlVariable { get; set; } = "url";

    /// <summary>
    /// 编译信息
    /// </summary>
    public Compilation Compilation { get; set; } = null!;

    /// <summary>
    /// 缩进级别
    /// </summary>
    public int IndentLevel { get; set; } = 3;

    /// <summary>
    /// 是否包含取消令牌参数
    /// </summary>
    public bool HasCancellationToken { get; set; }

    /// <summary>
    /// 取消令牌参数名
    /// </summary>
    public string CancellationTokenParameter { get; set; } = "cancellationToken";
}