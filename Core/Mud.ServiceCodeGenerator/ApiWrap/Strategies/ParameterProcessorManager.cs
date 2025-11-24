// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.CodeGenerator 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.ServiceCodeGenerator.ApiWrap.Helpers;
using System.Text;

namespace Mud.ServiceCodeGenerator.ApiWrap.Strategies;

/// <summary>
/// 参数处理器管理器
/// </summary>
/// <remarks>
/// 管理所有参数处理器，提供统一的参数处理接口
/// </remarks>
internal class ParameterProcessorManager
{
    private readonly List<IParameterProcessor> _processors;

    /// <summary>
    /// 初始化参数处理器管理器
    /// </summary>
    public ParameterProcessorManager()
    {
        _processors =
        [
            new QueryParameterProcessor(),
            new ArrayQueryParameterProcessor(),
            new HeaderParameterProcessor(),
            new BodyParameterProcessor()
        ];
    }

    /// <summary>
    /// 处理所有参数并生成代码
    /// </summary>
    /// <param name="parameters">参数列表</param>
    /// <param name="context">生成上下文</param>
    /// <returns>生成的代码</returns>
    public string ProcessParameters(IReadOnlyList<ParameterInfo> parameters, ParameterGenerationContext context)
    {
        var codeBuilder = new StringBuilder();

        // 首先生成查询参数初始化代码
        var queryParameters = parameters.Where(p =>
            p.Attributes.Any(attr =>
                attr.Name == GeneratorConstants.QueryAttribute ||
                attr.Name == GeneratorConstants.ArrayQueryAttribute)).ToList();

        if (queryParameters.Any() || HasAuthorizationQuery(parameters))
        {
            codeBuilder.AppendLine(CodeGenerationHelper.GenerateVariableDeclaration(
                "var",
                "queryParams",
                "HttpUtility.ParseQueryString(string.Empty);",
                context.IndentLevel
            ));
            codeBuilder.AppendLine();
        }

        // 处理每个参数
        foreach (var parameter in parameters)
        {
            var processor = _processors.FirstOrDefault(p => p.CanProcess(parameter));
            if (processor != null)
            {
                var generatedCode = processor.GenerateCode(parameter, context);
                if (!string.IsNullOrEmpty(generatedCode))
                {
                    codeBuilder.AppendLine(generatedCode);
                    codeBuilder.AppendLine();
                }
            }
        }

        // 生成查询参数的URL追加代码
        if (queryParameters.Any() || HasAuthorizationQuery(parameters))
        {
            codeBuilder.AppendLine(CodeGenerationHelper.GenerateIfStatement(
                "queryParams.Count > 0",
                $"{context.UrlVariable} += \"?\" + queryParams.ToString();",
                indent: context.IndentLevel
            ));
        }

        return codeBuilder.ToString();
    }

    /// <summary>
    /// 检查是否包含授权查询参数
    /// </summary>
    /// <param name="parameters">参数列表</param>
    /// <returns>是否包含授权查询参数</returns>
    private static bool HasAuthorizationQuery(IReadOnlyList<ParameterInfo> parameters)
    {
        return parameters.Any(p =>
            p.Attributes.Any(attr =>
                attr.Name == "QueryAttribute" &&
                attr.Arguments.Length > 0 &&
                attr.Arguments[0]?.ToString() == "Authorization"));
    }

    /// <summary>
    /// 添加自定义参数处理器
    /// </summary>
    /// <param name="processor">参数处理器</param>
    public void AddProcessor(IParameterProcessor processor)
    {
        if (processor != null && !_processors.Any(p => p.GetType() == processor.GetType()))
        {
            _processors.Add(processor);
        }
    }

    /// <summary>
    /// 获取指定参数的处理器
    /// </summary>
    /// <param name="parameter">参数信息</param>
    /// <returns>参数处理器，如果没有找到则返回null</returns>
    public IParameterProcessor? GetProcessor(ParameterInfo parameter)
    {
        return _processors.FirstOrDefault(p => p.CanProcess(parameter));
    }
}