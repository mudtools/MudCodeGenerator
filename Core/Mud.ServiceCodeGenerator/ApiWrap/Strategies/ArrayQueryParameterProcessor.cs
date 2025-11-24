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
/// 数组查询参数处理器
/// </summary>
/// <remarks>
/// 处理标记了 [ArrayQuery] 特性的参数，支持重复键名和分隔符两种格式
/// </remarks>
internal class ArrayQueryParameterProcessor : IParameterProcessor
{
    /// <inheritdoc/>
    public bool CanProcess(ParameterInfo parameter)
    {
        return parameter.Attributes.Any(attr => attr.Name == GeneratorConstants.ArrayQueryAttribute);
    }

    /// <inheritdoc/>
    public string GenerateCode(ParameterInfo parameter, ParameterGenerationContext context)
    {
        var arrayQueryAttr = parameter.Attributes.First(a => a.Name == GeneratorConstants.ArrayQueryAttribute);
        var paramName = GetQueryParameterName(arrayQueryAttr, parameter.Name);
        var separator = GetArrayQuerySeparator(arrayQueryAttr);

        var code = new StringBuilder();

        code.AppendLine(CodeGenerationHelper.GenerateIfStatement(
            $"{parameter.Name} != null && {parameter.Name}.Length > 0",
            "",
            indent: context.IndentLevel
        ));

        code.AppendLine(CodeGenerationHelper.FormatParameterName("{", context.IndentLevel + 1));

        if (string.IsNullOrEmpty(separator))
        {
            // 使用重复键名格式：user_ids=id0&user_ids=id1&user_ids=id2
            code.AppendLine(GenerateRepeatedKeyFormat(parameter, paramName, context));
        }
        else
        {
            // 使用分隔符连接格式：user_ids=id0;id1;id2
            code.AppendLine(GenerateSeparatorFormat(parameter, paramName, separator, context));
        }

        code.AppendLine(CodeGenerationHelper.FormatParameterName("}", context.IndentLevel + 1));

        return code.ToString().TrimEnd();
    }

    private static string GenerateRepeatedKeyFormat(
        ParameterInfo parameter,
        string paramName,
        ParameterGenerationContext context)
    {
        return CodeGenerationHelper.GenerateForEach(
            "item",
            parameter.Name,
            CodeGenerationHelper.GenerateIfStatement(
                "item != null",
                CodeGenerationHelper.GenerateVariableDeclaration(
                    "var",
                    "encodedValue",
                    "HttpUtility.UrlEncode(item.ToString());",
                    context.IndentLevel + 3
                ) + Environment.NewLine + CodeGenerationHelper.FormatParameterName(
                    $"queryParams.Add(\"{paramName}\", encodedValue);",
                    context.IndentLevel + 3
                ),
                indent: context.IndentLevel + 2
            ),
            context.IndentLevel + 2
        );
    }

    private static string GenerateSeparatorFormat(
        ParameterInfo parameter,
        string paramName,
        string separator,
        ParameterGenerationContext context)
    {
        return CodeGenerationHelper.GenerateVariableDeclaration(
            "var",
            "joinedValues",
            $"string.Join(\"{separator}\", {parameter.Name}.Where(item => item != null).Select(item => HttpUtility.UrlEncode(item.ToString())));",
            context.IndentLevel + 2
        ) + Environment.NewLine + CodeGenerationHelper.FormatParameterName(
            $"queryParams.Add(\"{paramName}\", joinedValues);",
            context.IndentLevel + 2
        );
    }

    private static string GetQueryParameterName(ParameterAttributeInfo attribute, string defaultName)
    {
        if (attribute.Arguments.Length > 0)
        {
            var nameArg = attribute.Arguments[0] as string;
            if (!string.IsNullOrEmpty(nameArg))
                return nameArg;
        }

        return attribute.NamedArguments.TryGetValue("Name", out var nameNamedArg)
            ? nameNamedArg as string ?? defaultName
            : defaultName;
    }

    private static string? GetArrayQuerySeparator(ParameterAttributeInfo attribute)
    {
        // 检查构造函数参数
        if (attribute.Arguments.Length > 1)
        {
            return attribute.Arguments[1] as string;
        }

        // 检查命名参数
        return attribute.NamedArguments.TryGetValue("Separator", out var separator)
            ? separator as string
            : null;
    }
}