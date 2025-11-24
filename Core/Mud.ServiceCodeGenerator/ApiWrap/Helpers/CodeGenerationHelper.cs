// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.CodeGenerator 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text;

namespace Mud.ServiceCodeGenerator.ApiWrap.Helpers;

/// <summary>
/// HTTP 代码生成辅助类
/// </summary>
/// <remarks>
/// 提供统一的代码生成字符串格式化和拼接功能
/// </remarks>
internal static class CodeGenerationHelper
{
    /// <summary>
    /// 格式化参数名称
    /// </summary>
    /// <param name="name">参数名</param>
    /// <param name="indent">缩进级别</param>
    /// <returns>格式化的参数名</returns>
    public static string FormatParameterName(string name, int indent = 0)
    {
        return $"{new string(' ', indent * 4)}{name}";
    }

    /// <summary>
    /// 生成缩进字符串
    /// </summary>
    /// <param name="level">缩进级别</param>
    /// <returns>缩进字符串</returns>
    public static string Indent(int level = 1)
    {
        return new string(' ', level * 4);
    }

    /// <summary>
    /// 生成属性访问代码
    /// </summary>
    /// <param name="instance">实例名</param>
    /// <param name="property">属性名</param>
    /// <returns>属性访问代码</returns>
    public static string GeneratePropertyAccess(string instance, string property)
    {
        return $"{instance}.{property}";
    }

    /// <summary>
    /// 生成方法调用代码
    /// </summary>
    /// <param name="instance">实例名</param>
    /// <param name="method">方法名</param>
    /// <param name="parameters">参数列表</param>
    /// <returns>方法调用代码</returns>
    public static string GenerateMethodCall(string instance, string method, params string[] parameters)
    {
        var paramList = parameters.Length > 0 ? $", {string.Join(", ", parameters)}" : "";
        return $"{instance}.{method}({paramList.TrimStart(',')})";
    }

    /// <summary>
    /// 生成条件语句
    /// </summary>
    /// <param name="condition">条件</param>
    /// <param name="trueBody">条件为真的代码块</param>
    /// <param name="falseBody">条件为假的代码块（可选）</param>
    /// <param name="indent">缩进级别</param>
    /// <returns>条件语句代码</returns>
    public static string GenerateIfStatement(string condition, string trueBody, string? falseBody = null, int indent = 3)
    {
        var sb = new StringBuilder();
        var indentStr = Indent(indent);
        var bodyIndent = Indent(indent + 1);

        sb.AppendLine($"{indentStr}if ({condition})");
        sb.AppendLine($"{indentStr}{{");
        sb.AppendLine($"{bodyIndent}{trueBody}");

        if (!string.IsNullOrEmpty(falseBody))
        {
            sb.AppendLine($"{indentStr}}}");
            sb.AppendLine($"{indentStr}else");
            sb.AppendLine($"{indentStr}{{");
            sb.AppendLine($"{bodyIndent}{falseBody}");
        }

        sb.AppendLine($"{indentStr}}}");
        return sb.ToString();
    }

    /// <summary>
    /// 生成 foreach 循环
    /// </summary>
    /// <param name="variable">循环变量</param>
    /// <param name="collection">集合表达式</param>
    /// <param name="body">循环体代码</param>
    /// <param name="indent">缩进级别</param>
    /// <returns>foreach 循环代码</returns>
    public static string GenerateForEach(string variable, string collection, string body, int indent = 3)
    {
        var sb = new StringBuilder();
        var indentStr = Indent(indent);
        var bodyIndent = Indent(indent + 1);

        sb.AppendLine($"{indentStr}foreach (var {variable} in {collection})");
        sb.AppendLine($"{indentStr}{{");
        sb.AppendLine($"{bodyIndent}{body}");
        sb.AppendLine($"{indentStr}}}");

        return sb.ToString();
    }

    /// <summary>
    /// 生成 try-catch 语句
    /// </summary>
    /// <param name="tryBody">try 代码块</param>
    /// <param name="catchBody">catch 代码块</param>
    /// <param name="exceptionType">异常类型</param>
    /// <param name="exceptionVariable">异常变量名</param>
    /// <param name="indent">缩进级别</param>
    /// <returns>try-catch 语句代码</returns>
    public static string GenerateTryCatch(
        string tryBody,
        string catchBody,
        string exceptionType = "System.Exception",
        string exceptionVariable = "ex",
        int indent = 2)
    {
        var sb = new StringBuilder();
        var indentStr = Indent(indent);
        var bodyIndent = Indent(indent + 1);

        sb.AppendLine($"{indentStr}try");
        sb.AppendLine($"{indentStr}{{");
        sb.AppendLine($"{bodyIndent}{tryBody}");
        sb.AppendLine($"{indentStr}}}");
        sb.AppendLine($"{indentStr}catch ({exceptionType} {exceptionVariable})");
        sb.AppendLine($"{indentStr}{{");
        sb.AppendLine($"{bodyIndent}{catchBody}");
        sb.AppendLine($"{indentStr}}}");

        return sb.ToString();
    }

    /// <summary>
    /// 生成 using 语句
    /// </summary>
    /// <param name="resource">资源声明</param>
    /// <param name="body">代码体</param>
    /// <param name="indent">缩进级别</param>
    /// <returns>using 语句代码</returns>
    public static string GenerateUsing(string resource, string body, int indent = 3)
    {
        var sb = new StringBuilder();
        var indentStr = Indent(indent);
        var bodyIndent = Indent(indent + 1);

        sb.AppendLine($"{indentStr}using ({resource})");
        sb.AppendLine($"{indentStr}{{");
        sb.AppendLine($"{bodyIndent}{body}");
        sb.AppendLine($"{indentStr}}}");

        return sb.ToString();
    }

    /// <summary>
    /// 生成变量声明
    /// </summary>
    /// <param name="type">变量类型</param>
    /// <param name="name">变量名</param>
    /// <param name="value">初始值（可选）</param>
    /// <param name="indent">缩进级别</param>
    /// <returns>变量声明代码</returns>
    public static string GenerateVariableDeclaration(
        string type,
        string name,
        string? value = null,
        int indent = 3)
    {
        var indentStr = Indent(indent);
        var valueStr = string.IsNullOrEmpty(value) ? "" : $" = {value}";
        return $"{indentStr}{type} {name}{valueStr};";
    }

    /// <summary>
    /// 生成方法签名
    /// </summary>
    /// <param name="returnType">返回类型</param>
    /// <param name="methodName">方法名</param>
    /// <param name="parameters">参数列表</param>
    /// <param name="isAsync">是否异步方法</param>
    /// <param name="indent">缩进级别</param>
    /// <returns>方法签名代码</returns>
    public static string GenerateMethodSignature(
        string returnType,
        string methodName,
        string parameters,
        bool isAsync = false,
        int indent = 2)
    {
        var indentStr = Indent(indent);
        var asyncKeyword = isAsync ? "async " : "";
        return $"{indentStr}{asyncKeyword}{returnType} {methodName}({parameters})";
    }

    /// <summary>
    /// 生成方法文档注释
    /// </summary>
    /// <param name="summary">摘要</param>
    /// <param name="parameters">参数说明字典（参数名 -> 说明）</param>
    /// <param name="returns">返回值说明</param>
    /// <param name="indent">缩进级别</param>
    /// <returns>文档注释代码</returns>
    public static string GenerateDocumentation(
        string summary,
        Dictionary<string, string>? parameters = null,
        string? returns = null,
        int indent = 2)
    {
        var sb = new StringBuilder();
        var indentStr = Indent(indent);
        var docIndent = Indent(indent + 1);

        sb.AppendLine($"{indentStr}/// <summary>");
        sb.AppendLine($"{docIndent}/// {summary}");
        sb.AppendLine($"{indentStr}/// </summary>");

        if (parameters != null)
        {
            foreach (var parameter in parameters)
            {
                sb.AppendLine($"{indentStr}/// <param name=\"{parameter.Key}\">{parameter.Value}</param>");
            }
        }

        if (!string.IsNullOrEmpty(returns))
        {
            sb.AppendLine($"{indentStr}/// <returns>{returns}</returns>");
        }

        return sb.ToString();
    }

    /// <summary>
    /// 生成特性应用代码
    /// </summary>
    /// <param name="attributeName">特性名</param>
    /// <param name="arguments">构造函数参数</param>
    /// <param name="namedArguments">命名参数字典</param>
    /// <param name="indent">缩进级别</param>
    /// <returns>特性应用代码</returns>
    public static string GenerateAttribute(
        string attributeName,
        string[]? arguments = null,
        Dictionary<string, string>? namedArguments = null,
        int indent = 1)
    {
        var sb = new StringBuilder();
        var indentStr = Indent(indent);
        sb.Append($"{indentStr}[{attributeName}");

        var allArguments = new List<string>();

        if (arguments?.Length > 0)
        {
            allArguments.AddRange(arguments);
        }

        if (namedArguments != null)
        {
            allArguments.AddRange(namedArguments.Select(kv => $"{kv.Key} = {kv.Value}"));
        }

        if (allArguments.Count > 0)
        {
            sb.Append($"({string.Join(", ", allArguments)})");
        }

        sb.Append(']');
        return sb.ToString();
    }
}