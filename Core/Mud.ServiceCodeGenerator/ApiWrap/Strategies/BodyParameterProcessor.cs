// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.CodeGenerator 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.ServiceCodeGenerator.ApiWrap.Helpers;

namespace Mud.ServiceCodeGenerator.ApiWrap.Strategies;

/// <summary>
/// 请求体参数处理器
/// </summary>
/// <remarks>
/// 处理标记了 [Body] 特性的参数，生成请求体内容代码
/// </remarks>
internal class BodyParameterProcessor : IParameterProcessor
{
    /// <inheritdoc/>
    public bool CanProcess(ParameterInfo parameter)
    {
        return parameter.Attributes.Any(attr => attr.Name == GeneratorConstants.BodyAttribute);
    }

    /// <inheritdoc/>
    public string GenerateCode(ParameterInfo parameter, ParameterGenerationContext context)
    {
        var bodyAttr = parameter.Attributes.First(a => a.Name == GeneratorConstants.BodyAttribute);
        var useStringContent = GetUseStringContentFlag(bodyAttr);
        var contentType = GetBodyContentType(bodyAttr);

        // 检查参数是否明确指定了ContentType
        var hasExplicitContentType = bodyAttr.NamedArguments.ContainsKey("ContentType");
        var contentTypeExpression = hasExplicitContentType
            ? $"\"{contentType}\""
            : "GetMediaType(_defaultContentType)";

        return CodeGenerationHelper.GenerateIfStatement(
            $"{parameter.Name} != null",
            GenerateBodyContent(parameter, useStringContent, contentTypeExpression, context),
            indent: context.IndentLevel
        );
    }

    private static string GenerateBodyContent(
        ParameterInfo parameter,
        bool useStringContent,
        string contentTypeExpression,
        ParameterGenerationContext context)
    {
        var innerCode = useStringContent
            ? CodeGenerationHelper.FormatParameterName(
                $"request.Content = new StringContent({parameter.Name}.ToString() ?? \"\", Encoding.UTF8, {contentTypeExpression});",
                context.IndentLevel + 1
            )
            : string.Join(Environment.NewLine,
                CodeGenerationHelper.GenerateVariableDeclaration(
                    "var",
                    "jsonContent",
                    $"JsonSerializer.Serialize({parameter.Name}, _jsonSerializerOptions);",
                    context.IndentLevel + 1
                ),
                CodeGenerationHelper.FormatParameterName(
                    $"request.Content = new StringContent(jsonContent, Encoding.UTF8, {contentTypeExpression});",
                    context.IndentLevel + 1
                )
            );

        return innerCode;
    }

    private static string GetBodyContentType(ParameterAttributeInfo bodyAttr)
    {
        return bodyAttr.NamedArguments.TryGetValue("ContentType", out var contentTypeArg)
            ? (contentTypeArg?.ToString() ?? "application/json")
            : "application/json";
    }

    private static bool GetUseStringContentFlag(ParameterAttributeInfo bodyAttr)
    {
        return bodyAttr.NamedArguments.TryGetValue("UseStringContent", out var useStringContentArg)
            && bool.Parse(useStringContentArg?.ToString() ?? "false");
    }
}