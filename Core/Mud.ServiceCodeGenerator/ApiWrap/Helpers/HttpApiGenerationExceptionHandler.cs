// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.CodeGenerator 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.ServiceCodeGenerator.ApiWrap.Helpers;

/// <summary>
/// HTTP API 生成器异常处理器
/// </summary>
/// <remarks>
/// 提供统一的异常处理和错误报告功能
/// </remarks>
internal static class HttpApiGenerationExceptionHandler
{
    /// <summary>
    /// 处理接口处理异常
    /// </summary>
    /// <param name="ex">异常信息</param>
    /// <param name="interfaceDecl">接口声明</param>
    /// <param name="context">源生成上下文</param>
    public static void HandleInterfaceProcessingException(Exception ex, InterfaceDeclarationSyntax interfaceDecl, SourceProductionContext context)
    {
        var descriptor = CreateDiagnosticDescriptor(ex);
        var message = FormatExceptionMessage(ex);
        var diagnostic = Diagnostic.Create(descriptor, interfaceDecl.Identifier.GetLocation(), message);
        context.ReportDiagnostic(diagnostic);
    }

    /// <summary>
    /// 创建诊断描述符
    /// </summary>
    /// <param name="ex">异常信息</param>
    /// <returns>诊断描述符</returns>
    private static DiagnosticDescriptor CreateDiagnosticDescriptor(Exception ex)
    {
        return ex switch
        {
            InvalidOperationException => new DiagnosticDescriptor(
                "HTTPCLIENT003",
                "HttpClient API语法错误",
                "接口{0}的语法分析失败: {1}",
                "Generation",
                DiagnosticSeverity.Error,
                true),

            ArgumentException => new DiagnosticDescriptor(
                "HTTPCLIENT004",
                "HttpClient API参数错误",
                "接口{0}的参数配置错误: {1}",
                "Generation",
                DiagnosticSeverity.Error,
                true),

            NotSupportedException => new DiagnosticDescriptor(
                "HTTPCLIENT005",
                "HttpClient API不支持的操作",
                "接口{0}包含不支持的操作: {1}",
                "Generation",
                DiagnosticSeverity.Error,
                true),

            _ => new DiagnosticDescriptor(
                "HTTPCLIENT001",
                "HttpClient API生成错误",
                "生成接口{0}的实现时发生错误: {1}",
                "Generation",
                DiagnosticSeverity.Error,
                true)
        };
    }

    /// <summary>
    /// 格式化异常消息
    /// </summary>
    /// <param name="ex">异常信息</param>
    /// <returns>格式化后的消息</returns>
    private static string FormatExceptionMessage(Exception ex)
    {
        var message = ex.Message;

        // 如果是内部异常，提取更有用的信息
        if (ex.InnerException != null)
        {
            message += $" (内部异常: {ex.InnerException.Message})";
        }

        return message;
    }

    /// <summary>
    /// 报告警告信息
    /// </summary>
    /// <param name="context">源生成上下文</param>
    /// <param name="interfaceName">接口名称</param>
    /// <param name="id">诊断ID</param>
    /// <param name="message">警告消息</param>
    /// <param name="location">位置信息</param>
    public static void ReportWarning(SourceProductionContext context, string interfaceName, string id, string message, Location? location = null)
    {
        var descriptor = new DiagnosticDescriptor(
            id,
            "HttpClient API警告",
            message,
            "Generation",
            DiagnosticSeverity.Warning,
            true);

        var diagnostic = location != null
            ? Diagnostic.Create(descriptor, location, interfaceName)
            : Diagnostic.Create(descriptor, Location.None, interfaceName);

        context.ReportDiagnostic(diagnostic);
    }

    /// <summary>
    /// 安全执行操作并处理异常
    /// </summary>
    /// <typeparam name="T">返回类型</typeparam>
    /// <param name="operation">要执行的操作</param>
    /// <param name="interfaceDecl">接口声明</param>
    /// <param name="context">源生成上下文</param>
    /// <param name="defaultValue">默认值</param>
    /// <returns>操作结果或默认值</returns>
    public static T SafeExecute<T>(
        Func<T> operation,
        InterfaceDeclarationSyntax interfaceDecl,
        SourceProductionContext context,
        T defaultValue = default)
    {
        try
        {
            return operation();
        }
        catch (InvalidOperationException ex)
        {
            HandleInterfaceProcessingException(ex, interfaceDecl, context);
            return defaultValue;
        }
        catch (ArgumentException ex)
        {
            HandleInterfaceProcessingException(ex, interfaceDecl, context);
            return defaultValue;
        }
        catch (NotSupportedException ex)
        {
            HandleInterfaceProcessingException(ex, interfaceDecl, context);
            return defaultValue;
        }
        catch (Exception ex)
        {
            // 对于未预期的异常，记录并重新抛出
            HandleInterfaceProcessingException(ex, interfaceDecl, context);
            throw;
        }
    }

    /// <summary>
    /// 验证方法分析结果
    /// </summary>
    /// <param name="methodInfo">方法分析结果</param>
    /// <param name="context">源生成上下文</param>
    /// <returns>验证是否通过</returns>
    public static bool ValidateMethodAnalysisResult(MethodAnalysisResult methodInfo, SourceProductionContext context)
    {
        if (!methodInfo.IsValid)
        {
            ReportWarning(context, methodInfo.InterfaceName, "HTTPCLIENT006",
                $"方法 {methodInfo.MethodName} 无效，跳过生成");
            return false;
        }

        if (string.IsNullOrEmpty(methodInfo.HttpMethod))
        {
            ReportWarning(context, methodInfo.InterfaceName, "HTTPCLIENT007",
                $"方法 {methodInfo.MethodName} 缺少HTTP方法特性");
            return false;
        }

        if (string.IsNullOrEmpty(methodInfo.UrlTemplate))
        {
            ReportWarning(context, methodInfo.InterfaceName, "HTTPCLIENT008",
                $"方法 {methodInfo.MethodName} 缺少URL模板");
            return false;
        }

        return true;
    }

    /// <summary>
    /// 验证参数配置
    /// </summary>
    /// <param name="parameter">参数信息</param>
    /// <param name="methodName">方法名</param>
    /// <param name="context">源生成上下文</param>
    /// <returns>验证是否通过</returns>
    public static bool ValidateParameterConfig(ParameterInfo parameter, string methodName, SourceProductionContext context)
    {
        if (string.IsNullOrEmpty(parameter.Name))
        {
            ReportWarning(context, methodName, "HTTPCLIENT009",
                $"参数名不能为空");
            return false;
        }

        if (string.IsNullOrEmpty(parameter.Type))
        {
            ReportWarning(context, methodName, "HTTPCLIENT010",
                $"参数 {parameter.Name} 的类型不能为空");
            return false;
        }

        // 检查是否有冲突的特性
        var attributeNames = parameter.Attributes.Select(a => a.Name).ToList();
        var conflictingAttributes = new[]
        {
            (GeneratorConstants.QueryAttribute, GeneratorConstants.ArrayQueryAttribute),
            (GeneratorConstants.BodyAttribute, GeneratorConstants.QueryAttribute),
            (GeneratorConstants.HeaderAttribute, GeneratorConstants.BodyAttribute)
        };

        foreach (var (attr1, attr2) in conflictingAttributes)
        {
            if (attributeNames.Contains(attr1) && attributeNames.Contains(attr2))
            {
                ReportWarning(context, methodName, "HTTPCLIENT011",
                    $"参数 {parameter.Name} 不能同时使用 [{attr1}] 和 [{attr2}] 特性");
                return false;
            }
        }

        return true;
    }
}