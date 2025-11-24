// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.CodeGenerator 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.ServiceCodeGenerator.ApiWrap.Helpers;

/// <summary>
/// HTTP API 配置验证器
/// </summary>
/// <remarks>
/// 提供接口、方法、参数的配置验证功能
/// </remarks>
internal static class HttpApiConfigurationValidator
{
    /// <summary>
    /// 验证接口配置
    /// </summary>
    /// <param name="interfaceSymbol">接口符号</param>
    /// <param name="context">源生成上下文</param>
    /// <returns>验证是否通过</returns>
    public static bool ValidateInterfaceConfiguration(INamedTypeSymbol interfaceSymbol, SourceProductionContext context)
    {
        if (interfaceSymbol == null)
        {
            HttpApiGenerationExceptionHandler.ReportWarning(
                context,
                "Unknown",
                "HTTPCLIENT012",
                "接口符号为空");
            return false;
        }

        if (!IsValidInterfaceName(interfaceSymbol.Name))
        {
            HttpApiGenerationExceptionHandler.ReportWarning(
                context,
                interfaceSymbol.Name,
                "HTTPCLIENT013",
                $"接口名称 '{interfaceSymbol.Name}' 不符合命名规范");
            return false;
        }

        return true;
    }

    /// <summary>
    /// 验证方法配置
    /// </summary>
    /// <param name="methodInfo">方法分析结果</param>
    /// <param name="context">源生成上下文</param>
    /// <returns>验证是否通过</returns>
    public static bool ValidateMethodConfiguration(MethodAnalysisResult methodInfo, SourceProductionContext context)
    {
        if (!HttpApiGenerationExceptionHandler.ValidateMethodAnalysisResult(methodInfo, context))
            return false;

        // 验证HTTP方法
        if (!IsValidHttpMethod(methodInfo.HttpMethod))
        {
            HttpApiGenerationExceptionHandler.ReportWarning(
                context,
                methodInfo.MethodName,
                "HTTPCLIENT014",
                $"不支持的HTTP方法: {methodInfo.HttpMethod}");
            return false;
        }

        // 验证URL模板
        if (!IsValidUrlTemplate(methodInfo.UrlTemplate))
        {
            HttpApiGenerationExceptionHandler.ReportWarning(
                context,
                methodInfo.MethodName,
                "HTTPCLIENT015",
                $"URL模板格式无效: {methodInfo.UrlTemplate}");
            return false;
        }

        // 验证返回类型
        if (!IsValidReturnType(methodInfo.ReturnType))
        {
            HttpApiGenerationExceptionHandler.ReportWarning(
                context,
                methodInfo.MethodName,
                "HTTPCLIENT016",
                $"不支持的返回类型: {methodInfo.ReturnType}");
            return false;
        }

        return true;
    }

    /// <summary>
    /// 验证参数配置
    /// </summary>
    /// <param name="parameters">参数列表</param>
    /// <param name="methodName">方法名</param>
    /// <param name="context">源生成上下文</param>
    /// <returns>验证是否通过</returns>
    public static bool ValidateParametersConfiguration(
        IReadOnlyList<ParameterInfo> parameters,
        string methodName,
        SourceProductionContext context)
    {
        if (parameters == null)
            return true;

        foreach (var parameter in parameters)
        {
            if (!HttpApiGenerationExceptionHandler.ValidateParameterConfig(parameter, methodName, context))
                return false;

            // 特定参数类型的验证
            if (!ValidateSpecificParameterType(parameter, methodName, context))
                return false;
        }

        // 验证参数组合
        if (!ValidateParameterCombination(parameters, methodName, context))
            return false;

        return true;
    }

    /// <summary>
    /// 验证特定参数类型
    /// </summary>
    private static bool ValidateSpecificParameterType(ParameterInfo parameter, string methodName, SourceProductionContext context)
    {
        // 验证Body参数
        if (parameter.Attributes.Any(a => a.Name == GeneratorConstants.BodyAttribute))
        {
            if (!IsValidBodyParameter(parameter))
            {
                HttpApiGenerationExceptionHandler.ReportWarning(
                    context,
                    methodName,
                    "HTTPCLIENT017",
                    $"Body参数 {parameter.Name} 配置无效");
                return false;
            }
        }

        // 验证FilePath参数
        if (parameter.Attributes.Any(a => a.Name == GeneratorConstants.FilePathAttribute))
        {
            if (!IsValidFilePathParameter(parameter))
            {
                HttpApiGenerationExceptionHandler.ReportWarning(
                    context,
                    methodName,
                    "HTTPCLIENT018",
                    $"FilePath参数 {parameter.Name} 配置无效");
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 验证参数组合
    /// </summary>
    private static bool ValidateParameterCombination(
        IReadOnlyList<ParameterInfo> parameters,
        string methodName,
        SourceProductionContext context)
    {
        // 检查是否有多个Body参数
        var bodyParameters = parameters.Where(p =>
            p.Attributes.Any(a => a.Name == GeneratorConstants.BodyAttribute)).ToList();

        if (bodyParameters.Count > 1)
        {
            HttpApiGenerationExceptionHandler.ReportWarning(
                context,
                methodName,
                "HTTPCLIENT019",
                "一个方法不能有多个Body参数");
            return false;
        }

        // 检查是否有多个FilePath参数
        var filePathParameters = parameters.Where(p =>
            p.Attributes.Any(a => a.Name == GeneratorConstants.FilePathAttribute)).ToList();

        if (filePathParameters.Count > 1)
        {
            HttpApiGenerationExceptionHandler.ReportWarning(
                context,
                methodName,
                "HTTPCLIENT020",
                "一个方法不能有多个FilePath参数");
            return false;
        }

        return true;
    }

    #region 验证辅助方法

    private static bool IsValidInterfaceName(string interfaceName)
    {
        return !string.IsNullOrEmpty(interfaceName) &&
               interfaceName.StartsWith("I", StringComparison.Ordinal) &&
               interfaceName.Length > 1 &&
               char.IsUpper(interfaceName[1]);
    }

    private static bool IsValidHttpMethod(string httpMethod)
    {
        var supportedMethods = new[]
        {
            "Get", "Post", "Put", "Delete", "Patch", "Head", "Options",
            "GetAttribute", "PostAttribute", "PutAttribute",
            "DeleteAttribute", "PatchAttribute", "HeadAttribute", "OptionsAttribute"
        };

        return supportedMethods.Contains(httpMethod);
    }

    private static bool IsValidUrlTemplate(string urlTemplate)
    {
        if (string.IsNullOrEmpty(urlTemplate))
            return false;

        // 基本URL格式验证
        if (!urlTemplate.StartsWith("/", StringComparison.Ordinal))
            return false;

        // 检查路径参数格式
        var pathParameters = System.Text.RegularExpressions.Regex.Matches(urlTemplate, @"\{([^}]+)\}");
        foreach (System.Text.RegularExpressions.Match match in pathParameters)
        {
            var paramName = match.Groups[1].Value;
            if (string.IsNullOrWhiteSpace(paramName))
                return false;
        }

        return true;
    }

    private static bool IsValidReturnType(string returnType)
    {
        // 检查是否为Task、Task<T>或其他支持的类型
        var supportedTypes = new[]
        {
            "Task", "void", "string", "int", "bool", "decimal", "double",
            "float", "long", "Guid", "DateTime", "byte[]"
        };

        var normalizedType = returnType.Replace("?", "").Trim();

        return supportedTypes.Any(supported =>
            normalizedType.StartsWith(supported, StringComparison.Ordinal) ||
            normalizedType.StartsWith($"Task<{supported}"));
    }

    private static bool IsValidBodyParameter(ParameterInfo parameter)
    {
        // Body参数不能是数组（特殊情况除外）
        if (IsArrayType(parameter.Type) &&
            !parameter.Type.Equals("byte[]", StringComparison.OrdinalIgnoreCase) &&
            !parameter.Type.Equals("string", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    private static bool IsValidFilePathParameter(ParameterInfo parameter)
    {
        // FilePath参数必须是string类型
        if (!parameter.Type.Equals("string", StringComparison.OrdinalIgnoreCase) &&
            !parameter.Type.Equals("string?", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    private static bool IsArrayType(string typeName)
    {
        return typeName.EndsWith("[]", StringComparison.OrdinalIgnoreCase) ||
               typeName.EndsWith("[]?", StringComparison.OrdinalIgnoreCase);
    }

    #endregion
}