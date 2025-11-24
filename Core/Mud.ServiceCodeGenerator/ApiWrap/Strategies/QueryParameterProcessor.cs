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
/// 查询参数处理器
/// </summary>
/// <remarks>
/// 处理标记了 [Query] 特性的参数，生成查询字符串代码
/// </remarks>
internal class QueryParameterProcessor : IParameterProcessor
{
    /// <inheritdoc/>
    public bool CanProcess(ParameterInfo parameter)
    {
        return parameter.Attributes.Any(attr => attr.Name == GeneratorConstants.QueryAttribute);
    }

    /// <inheritdoc/>
    public string GenerateCode(ParameterInfo parameter, ParameterGenerationContext context)
    {
        var queryAttr = parameter.Attributes.First(a => a.Name == GeneratorConstants.QueryAttribute);
        var paramName = GetQueryParameterName(queryAttr, parameter.Name);
        var formatString = GetFormatString(queryAttr);

        if (IsSimpleType(parameter.Type))
        {
            return GenerateSimpleQueryParameter(parameter, paramName, formatString, context);
        }
        else
        {
            return GenerateComplexQueryParameter(parameter, context);
        }
    }

    private static string GenerateSimpleQueryParameter(
        ParameterInfo parameter,
        string paramName,
        string? formatString,
        ParameterGenerationContext context)
    {
        if (IsArrayType(parameter.Type))
        {
            // 处理数组类型：使用默认分号分隔符格式
            return CodeGenerationHelper.GenerateIfStatement(
                $"{parameter.Name} != null && {parameter.Name}.Length > 0",
                CodeGenerationHelper.GenerateVariableDeclaration(
                    "var",
                    "joinedValues",
                    $"string.Join(\";\", {parameter.Name}.Where(item => item != null).Select(item => HttpUtility.UrlEncode(item.ToString())));",
                    context.IndentLevel + 1
                ) + Environment.NewLine + CodeGenerationHelper.FormatParameterName(
                    $"queryParams.Add(\"{paramName}\", joinedValues);",
                    context.IndentLevel + 1
                ),
                indent: context.IndentLevel
            );
        }
        else if (IsStringType(parameter.Type))
        {
            return CodeGenerationHelper.GenerateIfStatement(
                $"!string.IsNullOrEmpty({parameter.Name})",
                CodeGenerationHelper.GenerateVariableDeclaration(
                    "var",
                    "encodedValue",
                    $"HttpUtility.UrlEncode({parameter.Name});",
                    context.IndentLevel + 1
                ) + Environment.NewLine + CodeGenerationHelper.FormatParameterName(
                    $"queryParams.Add(\"{paramName}\", encodedValue);",
                    context.IndentLevel + 1
                ),
                indent: context.IndentLevel
            );
        }
        else
        {
            var formatExpression = !string.IsNullOrEmpty(formatString)
                ? $".ToString(\"{formatString}\")"
                : ".ToString()";
            return CodeGenerationHelper.GenerateIfStatement(
                $"{parameter.Name} != null",
                $"queryParams.Add(\"{paramName}\", {parameter.Name}{formatExpression});",
                indent: context.IndentLevel
            );
        }
    }

    private static string GenerateComplexQueryParameter(
        ParameterInfo parameter,
        ParameterGenerationContext context)
    {
        var code = new StringBuilder();

        code.AppendLine(CodeGenerationHelper.GenerateIfStatement(
            $"{parameter.Name} != null",
            "",
            indent: context.IndentLevel
        ));

        code.AppendLine(CodeGenerationHelper.FormatParameterName("{", context.IndentLevel + 1));

        code.AppendLine(CodeGenerationHelper.FormatParameterName(
            $"var properties = {parameter.Name}.GetType().GetProperties();",
            context.IndentLevel + 2
        ));

        code.AppendLine(CodeGenerationHelper.GenerateForEach(
            "prop",
            "properties",
            CodeGenerationHelper.GenerateIfStatement(
                "prop.GetValue({0}) != null".Replace("{0}", parameter.Name),
                "queryParams.Add(prop.Name, HttpUtility.UrlEncode(value.ToString()));",
                indent: context.IndentLevel + 3
            ),
            context.IndentLevel + 2
        ));

        code.AppendLine(CodeGenerationHelper.FormatParameterName("}", context.IndentLevel + 1));

        return code.ToString().TrimEnd();
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

    private static string? GetFormatString(ParameterAttributeInfo attribute)
    {
        // 检查构造函数参数
        if (attribute.Arguments.Length > 1)
        {
            return attribute.Arguments[1] as string;
        }
        else if (attribute.Arguments.Length == 1 && GeneratorConstants.PathAttributes.Contains(attribute.Name))
        {
            return attribute.Arguments[0] as string;
        }

        // 检查命名参数
        return attribute.NamedArguments.TryGetValue("FormatString", out var formatString)
            ? formatString as string
            : null;
    }

    private static bool IsSimpleType(string typeName)
    {
        var simpleTypes = new[] {
            "string", "int", "long", "float", "double", "decimal", "bool",
            "DateTime", "System.DateTime", "Guid", "System.Guid",
            "string[]", "int[]", "long[]", "float[]", "double[]", "decimal[]",
            "DateTime[]", "System.DateTime[]", "Guid[]", "System.Guid[]"
        };
        return simpleTypes.Contains(typeName) ||
               (typeName.EndsWith("?", StringComparison.OrdinalIgnoreCase) &&
                simpleTypes.Contains(typeName.TrimEnd('?')));
    }

    private static bool IsStringType(string typeName)
    {
        return typeName.Equals("string", StringComparison.OrdinalIgnoreCase) ||
               typeName.Equals("string?", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsArrayType(string typeName)
    {
        return typeName.EndsWith("[]", StringComparison.OrdinalIgnoreCase) ||
               typeName.EndsWith("[]?", StringComparison.OrdinalIgnoreCase);
    }
}