// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.CodeGenerator 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.ServiceCodeGenerator.ApiWrap.Strategies;

/// <summary>
/// 方法生成策略管理器
/// </summary>
/// <remarks>
/// 管理所有方法生成策略，提供统一的方法生成接口
/// </remarks>
internal class MethodGenerationStrategyManager
{
    private readonly List<IMethodGenerationStrategy> _strategies;

    /// <summary>
    /// 初始化方法生成策略管理器
    /// </summary>
    public MethodGenerationStrategyManager()
    {
        _strategies =
        [
            new FileDownloadGenerationStrategy(),
            new JsonResponseGenerationStrategy()
        ];
    }

    /// <summary>
    /// 生成响应处理代码
    /// </summary>
    /// <param name="methodInfo">方法分析结果</param>
    /// <param name="context">生成上下文</param>
    /// <returns>生成的代码</returns>
    public string GenerateResponseCode(MethodAnalysisResult methodInfo, MethodGenerationContext context)
    {
        var strategy = _strategies.FirstOrDefault(s => s.CanProcess(methodInfo))
            ?? throw new InvalidOperationException($"没有找到适合的方法生成策略来处理方法 {methodInfo.MethodName}");

        return strategy.GenerateResponseCode(methodInfo, context);
    }

    /// <summary>
    /// 添加自定义方法生成策略
    /// </summary>
    /// <param name="strategy">方法生成策略</param>
    public void AddStrategy(IMethodGenerationStrategy strategy)
    {
        if (strategy != null && !_strategies.Any(s => s.GetType() == strategy.GetType()))
        {
            _strategies.Add(strategy);
        }
    }

    /// <summary>
    /// 获取指定方法的生成策略
    /// </summary>
    /// <param name="methodInfo">方法分析结果</param>
    /// <returns>方法生成策略，如果没有找到则返回null</returns>
    public IMethodGenerationStrategy? GetStrategy(MethodAnalysisResult methodInfo)
    {
        return _strategies.FirstOrDefault(s => s.CanProcess(methodInfo));
    }
}