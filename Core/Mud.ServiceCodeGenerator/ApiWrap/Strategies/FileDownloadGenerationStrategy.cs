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
/// 文件下载方法生成策略
/// </summary>
/// <remarks>
/// 处理文件下载场景，包括有FilePath参数和返回byte[]的情况
/// </remarks>
internal class FileDownloadGenerationStrategy : IMethodGenerationStrategy
{
    /// <inheritdoc/>
    public bool CanProcess(MethodAnalysisResult methodInfo)
    {
        var hasFilePathParam = methodInfo.Parameters.Any(p =>
            p.Attributes.Any(attr => attr.Name == GeneratorConstants.FilePathAttribute));

        var isFileDownload = methodInfo.IsAsyncMethod &&
                             methodInfo.AsyncInnerReturnType.Equals("byte[]", StringComparison.OrdinalIgnoreCase);

        return hasFilePathParam || isFileDownload;
    }

    /// <inheritdoc/>
    public string GenerateResponseCode(MethodAnalysisResult methodInfo, MethodGenerationContext context)
    {
        var filePathParam = methodInfo.Parameters.FirstOrDefault(p =>
            p.Attributes.Any(attr => attr.Name == GeneratorConstants.FilePathAttribute));
        var hasFilePathParam = filePathParam != null;
        var isFileDownload = methodInfo.IsAsyncMethod &&
                             methodInfo.AsyncInnerReturnType.Equals("byte[]", StringComparison.OrdinalIgnoreCase);

        var cancellationTokenParam = GetCancellationTokenParameter(methodInfo);
        var cancellationTokenArg = string.IsNullOrEmpty(cancellationTokenParam)
            ? ""
            : $", {cancellationTokenParam}";

        if (hasFilePathParam)
        {
            return GenerateFilePathResponse(methodInfo, filePathParam!, context, cancellationTokenArg);
        }
        else if (isFileDownload)
        {
            return GenerateByteArrayResponse(context, cancellationTokenArg);
        }

        return string.Empty;
    }

    private static string GenerateFilePathResponse(
        MethodAnalysisResult methodInfo,
        ParameterInfo filePathParam,
        MethodGenerationContext context,
        string cancellationTokenArg)
    {
        var code = new StringBuilder();

        // 生成文件保存代码
        var streamCode = CodeGenerationHelper.GenerateUsing(
            "var stream = await response.Content.ReadAsStreamAsync(" + cancellationTokenArg.TrimStart(',', ' ') + ")",
            CodeGenerationHelper.GenerateUsing(
                "var fileStream = File.Create(" + filePathParam.Name + ")",
                $"await stream.CopyToAsync(fileStream, {GetBufferSizeFromAttribute(filePathParam)}{cancellationTokenArg});",
                context.IndentLevel + 1
            ),
            context.IndentLevel
        );

        code.AppendLine(streamCode);

        // 生成返回语句
        if (!methodInfo.IsAsyncMethod ||
            (methodInfo.IsAsyncMethod && methodInfo.AsyncInnerReturnType.Equals("void", StringComparison.OrdinalIgnoreCase)))
        {
            code.AppendLine(CodeGenerationHelper.FormatParameterName("return;", context.IndentLevel));
        }
        else if (methodInfo.IsAsyncMethod &&
                !string.IsNullOrEmpty(methodInfo.AsyncInnerReturnType) &&
                !methodInfo.AsyncInnerReturnType.Equals("void", StringComparison.OrdinalIgnoreCase))
        {
            code.AppendLine(CodeGenerationHelper.FormatParameterName("return default;", context.IndentLevel));
        }

        return code.ToString();
    }

    private static string GenerateByteArrayResponse(MethodGenerationContext context, string cancellationTokenArg)
    {
        var code = new StringBuilder();

        code.AppendLine(CodeGenerationHelper.GenerateVariableDeclaration(
            "byte[]",
            "fileBytes",
            $"await response.Content.ReadAsByteArrayAsync({cancellationTokenArg.TrimStart(',', ' ')})",
            context.IndentLevel
        ));
        code.AppendLine(CodeGenerationHelper.FormatParameterName("return fileBytes;", context.IndentLevel));

        return code.ToString();
    }

    private static string GetCancellationTokenParameter(MethodAnalysisResult methodInfo)
    {
        var cancellationTokenParam = methodInfo.Parameters.FirstOrDefault(p =>
            p.Type.Contains("CancellationToken"));
        return cancellationTokenParam?.Name ?? string.Empty;
    }

    private static int GetBufferSizeFromAttribute(ParameterInfo filePathParam)
    {
        var filePathAttr = filePathParam.Attributes.First(a => a.Name == GeneratorConstants.FilePathAttribute);

        // 首先检查命名参数
        if (filePathAttr.NamedArguments.TryGetValue("BufferSize", out var bufferSizeValue))
        {
            if (int.TryParse(bufferSizeValue?.ToString(), out var bufferSize))
            {
                return bufferSize;
            }
        }

        // 然后检查构造函数参数
        if (filePathAttr.Arguments.Length > 0)
        {
            var firstArg = filePathAttr.Arguments[0];
            if (int.TryParse(firstArg?.ToString(), out var bufferSize))
            {
                return bufferSize;
            }
        }

        // 如果都没有设置，使用默认值 81920 (80KB)
        return 81920;
    }
}