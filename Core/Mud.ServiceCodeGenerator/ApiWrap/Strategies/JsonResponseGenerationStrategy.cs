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
/// JSON响应处理生成策略
/// </summary>
/// <remarks>
/// 处理常规JSON响应的反序列化
/// </remarks>
internal class JsonResponseGenerationStrategy : IMethodGenerationStrategy
{
    /// <inheritdoc/>
    public bool CanProcess(MethodAnalysisResult methodInfo)
    {
        // 默认处理所有其他情况
        return true;
    }

    /// <inheritdoc/>
    public string GenerateResponseCode(MethodAnalysisResult methodInfo, MethodGenerationContext context)
    {
        var cancellationTokenParam = GetCancellationTokenParameter(methodInfo);
        var cancellationTokenArg = string.IsNullOrEmpty(cancellationTokenParam)
            ? ""
            : $", {cancellationTokenParam}";

        var deserializeType = methodInfo.IsAsyncMethod
            ? methodInfo.AsyncInnerReturnType
            : methodInfo.ReturnType;

        var code = new StringBuilder();

        code.AppendLine(CodeGenerationHelper.GenerateUsing(
            "var stream = await response.Content.ReadAsStreamAsync(" + cancellationTokenArg.TrimStart(',', ' ') + ")",
            "",
            context.IndentLevel
        ));
        code.AppendLine();

        code.AppendLine(CodeGenerationHelper.GenerateIfStatement(
            "stream.Length == 0",
            "return default;",
            indent: context.IndentLevel
        ));
        code.AppendLine();

        code.AppendLine(CodeGenerationHelper.GenerateVariableDeclaration(
            "var",
            "result",
            $"await JsonSerializer.DeserializeAsync<{deserializeType}>(stream, _jsonSerializerOptions{cancellationTokenArg});",
            context.IndentLevel
        ));
        code.AppendLine(CodeGenerationHelper.FormatParameterName("return result;", context.IndentLevel));

        return code.ToString();
    }

    private static string GetCancellationTokenParameter(MethodAnalysisResult methodInfo)
    {
        var cancellationTokenParam = methodInfo.Parameters.FirstOrDefault(p =>
            p.Type.Contains("CancellationToken"));
        return cancellationTokenParam?.Name ?? string.Empty;
    }
}