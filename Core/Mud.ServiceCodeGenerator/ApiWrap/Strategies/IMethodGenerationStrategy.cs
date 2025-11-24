// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.CodeGenerator 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.ServiceCodeGenerator.ApiWrap.Strategies;

/// <summary>
/// 方法生成策略接口
/// </summary>
/// <remarks>
/// 定义不同类型方法的生成策略
/// </remarks>
internal interface IMethodGenerationStrategy
{
    /// <summary>
    /// 判断是否支持处理指定的方法分析结果
    /// </summary>
    /// <param name="methodInfo">方法分析结果</param>
    /// <returns>是否支持处理</returns>
    bool CanProcess(MethodAnalysisResult methodInfo);

    /// <summary>
    /// 生成响应处理代码
    /// </summary>
    /// <param name="methodInfo">方法分析结果</param>
    /// <param name="context">生成上下文</param>
    /// <returns>生成的代码</returns>
    string GenerateResponseCode(MethodAnalysisResult methodInfo, MethodGenerationContext context);
}

/// <summary>
/// 方法生成上下文
/// </summary>
/// <remarks>
/// 包含方法生成过程中需要的上下文信息
/// </remarks>
internal class MethodGenerationContext
{
    /// <summary>
    /// 编译信息
    /// </summary>
    public Compilation Compilation { get; set; } = null!;

    /// <summary>
    /// 类名
    /// </summary>
    public string ClassName { get; set; } = string.Empty;

    /// <summary>
    /// 缩进级别
    /// </summary>
    public int IndentLevel { get; set; } = 3;

    /// <summary>
    /// 取消令牌参数名
    /// </summary>
    public string CancellationTokenParameter { get; set; } = string.Empty;

    /// <summary>
    /// 是否需要Token管理器
    /// </summary>
    public bool HasTokenManager { get; set; }

    /// <summary>
    /// 是否需要Authorization Header
    /// </summary>
    public bool HasAuthorizationHeader { get; set; }

    /// <summary>
    /// 是否需要Authorization Query
    /// </summary>
    public bool HasAuthorizationQuery { get; set; }
}