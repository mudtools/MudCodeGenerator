// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.CodeGenerator 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Text;

namespace Mud.ServiceCodeGenerator.ApiSourceGenerator;

/// <summary>
/// HttpClientApiWrap 源代码生成器
/// 为标记有HttpClientApiWrap特性的接口生成二次包装实现类
/// </summary>
public abstract class HttpInvokeWrapSourceGenerator : HttpInvokeBaseSourceGenerator
{
    protected override string[] ApiWrapAttributeNames() => GeneratorConstants.HttpClientApiWrapAttributeNames;

    /// <summary>
    /// 更新Execute方法以使用验证
    /// </summary>
    protected override void ExecuteGenerator(
        Compilation compilation,
        ImmutableArray<InterfaceDeclarationSyntax> interfaces,
        SourceProductionContext context,
        AnalyzerConfigOptionsProvider configOptionsProvider)
    {
        if (compilation == null || interfaces.IsDefaultOrEmpty)
            return;

        foreach (var interfaceDecl in interfaces)
        {
            try
            {
                var semanticModel = compilation.GetSemanticModel(interfaceDecl.SyntaxTree);
                var interfaceSymbol = semanticModel.GetDeclaredSymbol(interfaceDecl);
                var wrapAttribute = GetHttpClientApiWrapAttribute(interfaceSymbol);

                if (!ValidateInterfaceConfiguration(interfaceSymbol, wrapAttribute, context, interfaceDecl))
                    continue;

                GenerateWrapCode(compilation, interfaceDecl, interfaceSymbol, wrapAttribute, context);
            }
            catch (Exception ex)
            {
                ReportDiagnosticError(context, interfaceDecl, ex);
            }
        }
    }

    protected abstract void GenerateWrapCode(Compilation compilation, InterfaceDeclarationSyntax interfaceDecl, INamedTypeSymbol interfaceSymbol, AttributeData wrapAttribute, SourceProductionContext context);

    /// <summary>
    /// 获取HttpClientApiWrap特性
    /// </summary>
    private AttributeData? GetHttpClientApiWrapAttribute(INamedTypeSymbol interfaceSymbol)
    {
        if (interfaceSymbol == null)
            return null;
        return interfaceSymbol.GetAttributes()
            .FirstOrDefault(a => GeneratorConstants.HttpClientApiWrapAttributeNames.Contains(a.AttributeClass?.Name));
    }

    /// <summary>
    /// 生成文件头部（命名空间和头部注释）
    /// </summary>
    protected void GenerateFileHeader(StringBuilder sb, InterfaceDeclarationSyntax interfaceDecl)
    {
        if (interfaceDecl == null || sb == null)
            return;

        GenerateFileHeader(sb);

        // 获取命名空间
        var namespaceName = GetNamespaceName(interfaceDecl);
        if (!string.IsNullOrEmpty(namespaceName))
        {
            sb.AppendLine();
            sb.AppendLine($"namespace {namespaceName};");
            sb.AppendLine();
        }
    }

    /// <summary>
    /// 生成接口或类的开始部分
    /// </summary>
    protected void GenerateClassOrInterfaceStart(StringBuilder sb, string typeName, string interfaceName, bool isInterface = false)
    {
        if (sb == null)
            return;
        var parentName = isInterface ? " " : $" : {interfaceName}";
        sb.AppendLine($"{CompilerGeneratedAttribute}");
        sb.AppendLine($"{GeneratedCodeAttribute}");
        sb.AppendLine($"{(isInterface ? "public partial interface" : "internal partial class")} {typeName}{parentName}");
        sb.AppendLine("{");
    }

    /// <summary>
    /// 根据 Token 类型获取对应的方法名
    /// </summary>
    protected string GetTokenMethodName(ParameterInfo tokenParameter)
    {
        if (tokenParameter == null || string.IsNullOrEmpty(tokenParameter.TokenType))
        {
            return "GetTokenAsync";
        }

        return tokenParameter.TokenType switch
        {
            "TenantAccessToken" => "GetTenantAccessTokenAsync",
            "UserAccessToken" => "GetUserAccessTokenAsync",
            "Both" => "GetTokenAsync", // 默认方法
            _ => "GetTokenAsync"
        };
    }

    /// <summary>
    /// 生成包装方法（接口声明或实现）
    /// </summary>
    protected void GenerateWrapMethods(Compilation compilation, InterfaceDeclarationSyntax interfaceDecl, INamedTypeSymbol interfaceSymbol, StringBuilder sb, string tokenManageInterfaceName = null, string interfaceName = null)
    {
        if (interfaceSymbol == null || sb == null) return;

        var allMethods = GetAllInterfaceMethods(interfaceSymbol);

        foreach (var methodSymbol in allMethods)
        {
            if (!IsValidMethod(methodSymbol))
                continue;

            var methodInfo = AnalyzeMethod(compilation, methodSymbol, interfaceDecl);
            if (!methodInfo.IsValid)
                continue;

            // 检查是否忽略生成包装接口
            if (methodInfo.IgnoreWrapInterface)
                continue;

            // 使用更精确的方法查找，确保正确处理重载方法
            var methodSyntax = FindMethodSyntax(compilation, methodSymbol, interfaceDecl);
            if (methodSyntax != null)
            {
                string wrapMethodCode = GenerateWrapMethod(methodInfo, methodSyntax, interfaceName, tokenManageInterfaceName);
                if (!string.IsNullOrEmpty(wrapMethodCode))
                {
                    sb.AppendLine(wrapMethodCode);
                    sb.AppendLine();
                }
            }
        }
    }

    protected abstract string GenerateWrapMethod(MethodAnalysisResult methodInfo, MethodDeclarationSyntax methodSyntax, string interfaceName, string tokenManageInterfaceName);

    /// <summary>
    /// 生成包装方法的通用逻辑
    /// </summary>
    protected string GenerateWrapMethodCommon(MethodAnalysisResult methodInfo, MethodDeclarationSyntax methodSyntax, string interfaceName, string tokenManageInterfaceName, bool isInterface)
    {
        if (methodInfo == null || methodSyntax == null)
            return string.Empty;

        // 检查是否有TokenType.Both的Token参数
        var bothTokenParameter = methodInfo.Parameters.FirstOrDefault(p =>
            HasAttribute(p, GeneratorConstants.TokenAttributeNames) &&
            p.TokenType.Equals("Both", StringComparison.OrdinalIgnoreCase));

        if (bothTokenParameter != null)
        {
            // 为Both类型生成两个方法
            var tenantMethod = GenerateBothWrapMethod(methodInfo, methodSyntax, interfaceName, tokenManageInterfaceName, "_Tenant_", "GetTenantAccessTokenAsync", isInterface);
            var userMethod = GenerateBothWrapMethod(methodInfo, methodSyntax, interfaceName, tokenManageInterfaceName, "_User_", "GetUserAccessTokenAsync", isInterface);

            return $"{tenantMethod}\r\n\r\n{userMethod}";
        }
        else
        {
            return GenerateSingleWrapMethod(methodInfo, methodSyntax, interfaceName, tokenManageInterfaceName, isInterface);
        }
    }

    /// <summary>
    /// 生成单个包装方法
    /// </summary>
    private string GenerateSingleWrapMethod(MethodAnalysisResult methodInfo, MethodDeclarationSyntax methodSyntax, string interfaceName, string tokenManageInterfaceName, bool isInterface)
    {
        var sb = new StringBuilder();

        // 添加方法注释
        var methodDoc = GetMethodXmlDocumentation(methodSyntax, methodInfo);
        if (!string.IsNullOrEmpty(methodDoc))
        {
            sb.AppendLine(methodDoc);
        }

        // 过滤掉标记了[Token]特性的参数，保留其他所有参数
        var filteredParameters = FilterParametersByAttribute(methodInfo.Parameters, GeneratorConstants.TokenAttributeNames, exclude: true);

        // 生成方法签名 - 接口方法不需要async关键字
        var methodSignature = GenerateMethodSignature(methodInfo, methodInfo.MethodName, filteredParameters, includeAsync: !isInterface);
        sb.AppendLine(methodSignature);

        if (!isInterface)
        {
            // 生成方法体
            sb.AppendLine("    {");
            var methodBody = GenerateMethodBody(methodInfo, interfaceName, tokenManageInterfaceName, methodInfo.Parameters, filteredParameters);
            sb.Append(methodBody);
        }
        else
        {
            sb.Append(';');
        }

        return sb.ToString();
    }

    /// <summary>
    /// 为TokenType.Both生成特定的方法
    /// </summary>
    private string GenerateBothWrapMethod(MethodAnalysisResult methodInfo, MethodDeclarationSyntax methodSyntax, string interfaceName, string tokenManageInterfaceName, string prefix, string tokenMethodName, bool isInterface)
    {
        var sb = new StringBuilder();

        // 添加方法注释
        var methodDoc = GetMethodXmlDocumentation(methodSyntax, methodInfo);
        if (!string.IsNullOrEmpty(methodDoc))
        {
            sb.AppendLine(methodDoc);
        }

        // 生成方法名
        var methodName = GenerateBothMethodName(methodInfo.MethodName, prefix);

        // 过滤掉标记了[Token]特性的参数，保留其他所有参数
        var filteredParameters = FilterParametersByAttribute(methodInfo.Parameters, GeneratorConstants.TokenAttributeNames, exclude: true);

        // 生成方法签名 - 接口方法不需要async关键字
        var methodSignature = GenerateMethodSignature(methodInfo, methodName, filteredParameters, includeAsync: !isInterface);
        sb.AppendLine(methodSignature);

        if (!isInterface)
        {
            // 生成方法体
            sb.AppendLine("    {");
            sb.AppendLine("        try");
            sb.AppendLine("        {");

            // 生成Token获取逻辑 - 使用指定的token方法名
            GenerateTokenAcquisition(sb, tokenManageInterfaceName, tokenMethodName, methodInfo.Parameters);

            // 生成API调用逻辑
            GenerateApiCall(sb, methodInfo, interfaceName, methodInfo.MethodName, methodInfo.Parameters, filteredParameters);

            // 生成异常处理逻辑
            GenerateExceptionHandling(sb, methodInfo.MethodName);
        }
        else
        {
            sb.Append(';');
        }

        return sb.ToString();
    }

    /// <summary>
    /// 获取方法的XML文档注释
    /// </summary>
    protected string GetMethodXmlDocumentation(MethodDeclarationSyntax methodSyntax, MethodAnalysisResult methodInfo)
    {
        var xmlDoc = GetXmlDocumentation(methodSyntax);
        if (string.IsNullOrEmpty(xmlDoc) || methodInfo == null)
            return string.Empty;

        // 获取标记了Token特性的参数名称
        var tokenParameterNames = FilterParametersByAttribute(methodInfo.Parameters, GeneratorConstants.TokenAttributeNames)
            .Select(p => p.Name)
            .ToList();

        // 处理XML文档注释，移除token参数的注释
        var docLines = xmlDoc.Split('\n');
        var result = new StringBuilder();

        int i = 0;
        foreach (var line in docLines)
        {
            var trimmedLine = line.TrimEnd();
            if (!string.IsNullOrWhiteSpace(trimmedLine))
            {
                // 检查是否是token参数的注释行
                var isTokenParameterComment = tokenParameterNames.Any(name =>
                    trimmedLine.Contains($"<param name=\"{name}\">") ||
                    trimmedLine.Contains($"<param name=\"{name}\""));

                if (!isTokenParameterComment)
                {
                    if (i == 0)
                        result.AppendLine($"    {trimmedLine}");
                    else
                        result.AppendLine($"{trimmedLine}");
                }

                i++;
            }
        }

        return result.ToString().TrimEnd();
    }

    /// <summary>
    /// 报告诊断错误
    /// </summary>
    private void ReportDiagnosticError(SourceProductionContext context, InterfaceDeclarationSyntax interfaceDecl, Exception ex)
    {
        context.ReportDiagnostic(Diagnostic.Create(
            new DiagnosticDescriptor(
                GeneratorConstants.GeneratorErrorDiagnosticId,
                "HttpClientApiWrap Source Generator Error",
                $"Error generating wrap interface for {interfaceDecl.Identifier}: {ex.Message}",
                "Generation",
                DiagnosticSeverity.Error,
                true),
            interfaceDecl.GetLocation()));
    }

    /// <summary>
    /// 报告诊断警告
    /// </summary>
    private void ReportDiagnosticWarning(SourceProductionContext context, InterfaceDeclarationSyntax interfaceDecl, string message)
    {
        context.ReportDiagnostic(Diagnostic.Create(
            new DiagnosticDescriptor(
                GeneratorConstants.GeneratorWarningDiagnosticId,
                "HttpClientApiWrap Source Generator Warning",
                message,
                "Generation",
                DiagnosticSeverity.Warning,
                true),
            interfaceDecl.GetLocation()));
    }

    /// <summary>
    /// 验证接口配置
    /// </summary>
    private bool ValidateInterfaceConfiguration(INamedTypeSymbol interfaceSymbol, AttributeData wrapAttribute, SourceProductionContext context, InterfaceDeclarationSyntax interfaceDecl)
    {
        if (interfaceSymbol == null)
        {
            ReportDiagnosticWarning(context, interfaceDecl, "Interface symbol is null, skipping generation.");
            return false;
        }

        if (!HasValidHttpMethods(interfaceSymbol))
        {
            ReportDiagnosticWarning(context, interfaceDecl, $"Interface {interfaceSymbol.Name} has no valid HTTP methods, skipping generation.");
            return false;
        }

        if (wrapAttribute == null)
        {
            ReportDiagnosticWarning(context, interfaceDecl, $"Interface {interfaceSymbol.Name} is missing HttpClientApiWrap attribute, skipping generation.");
            return false;
        }

        return true;
    }

    #region 共享的代码生成方法

    /// <summary>
    /// 获取Token管理接口名称
    /// </summary>
    protected string GetTokenManageInterfaceName(AttributeData wrapAttribute)
    {
        if (wrapAttribute == null)
            return string.Empty;

        // 检查特性参数中是否有指定的Token管理接口名称
        var tokenManageArg = wrapAttribute.NamedArguments.FirstOrDefault(a => a.Key == "TokenManage");
        if (!string.IsNullOrEmpty(tokenManageArg.Value.Value?.ToString()))
        {
            return tokenManageArg.Value.Value.ToString();
        }

        // 默认使用 ITokenManage
        return GeneratorConstants.DefaultTokenManageInterface;
    }

    /// <summary>
    /// 生成包装方法签名的通用方法
    /// </summary>
    protected string GenerateMethodSignature(MethodAnalysisResult methodInfo, string methodName, IReadOnlyList<ParameterInfo> parameters, string accessibility = "public", bool includeAsync = true)
    {
        if (methodInfo == null)
            return string.Empty;

        var sb = new StringBuilder();

        sb.AppendLine($"    {GeneratedCodeAttribute}");
        // 添加访问修饰符和async关键字（如果需要）
        sb.Append($"    {accessibility}");
        if (includeAsync && methodInfo.IsAsyncMethod)
        {
            sb.Append(" async");
        }

        sb.Append($" {methodInfo.ReturnType} {methodName}(");

        // 生成参数列表
        var parameterList = GenerateParameterList(parameters);
        sb.Append($"{parameterList})");

        return sb.ToString();
    }

    /// <summary>
    /// 为TokenType.Both生成方法名
    /// </summary>
    protected string GenerateBothMethodName(string originalMethodName, string prefix)
    {
        if (string.IsNullOrEmpty(originalMethodName))
            return string.Empty;

        if (originalMethodName.EndsWith("Async", StringComparison.OrdinalIgnoreCase))
        {
            return originalMethodName.Insert(originalMethodName.Length - 5, prefix);
        }
        else
        {
            return prefix.Trim('_') + originalMethodName;
        }
    }

    /// <summary>
    /// 生成方法体中的Token获取逻辑
    /// </summary>
    protected void GenerateTokenAcquisition(StringBuilder sb, string tokenManageInterfaceName, string tokenMethodName, IReadOnlyList<ParameterInfo> parameters)
    {
        if (sb == null) return;
        // 检查是否有CancellationToken参数
        var cancellationTokenParameter = parameters.FirstOrDefault(p => p.Type.Contains("CancellationToken"));
        var hasCancellationToken = cancellationTokenParameter != null;

        // Token获取方法都是异步的，需要使用await
        if (hasCancellationToken)
        {
            sb.AppendLine($"            var token = await {PrivateFieldNamingHelper.GeneratePrivateFieldName(tokenManageInterfaceName)}.{tokenMethodName}({cancellationTokenParameter.Name});");
        }
        else
        {
            sb.AppendLine($"            var token = await {PrivateFieldNamingHelper.GeneratePrivateFieldName(tokenManageInterfaceName)}.{tokenMethodName}();");
        }
        sb.AppendLine();

        // Token空值检查
        sb.AppendLine("            if (string.IsNullOrEmpty(token))");
        sb.AppendLine("            {");
        sb.AppendLine("                _logger.LogWarning(\"获取到的Token为空！\");");
        sb.AppendLine("            }");
        sb.AppendLine();
    }

    /// <summary>
    /// 生成方法体中的API调用逻辑
    /// </summary>
    protected void GenerateApiCall(StringBuilder sb, MethodAnalysisResult methodInfo, string interfaceName, string methodName, IReadOnlyList<ParameterInfo> originalParameters, IReadOnlyList<ParameterInfo> filteredParameters)
    {
        if (sb == null || methodInfo == null) return;

        // 检查是否为文件下载场景（有FilePath参数）
        var hasFilePathParam = originalParameters.Any(p => p.Attributes.Any(attr => attr.Name == GeneratorConstants.FilePathAttribute));

        // 调用原始API方法
        if (methodInfo.IsAsyncMethod)
        {
            // 对于异步方法，需要检查是否有返回值
            if (hasFilePathParam || (methodInfo.AsyncInnerReturnType.Equals("void", StringComparison.OrdinalIgnoreCase)))
            {
                // 对于有FilePath参数或返回void的方法，不使用return
                sb.Append($"            await {PrivateFieldNamingHelper.GeneratePrivateFieldName(interfaceName)}.{methodName}(");
            }
            else
            {
                // 对于有返回值的异步方法，使用return await
                sb.Append($"            return await {PrivateFieldNamingHelper.GeneratePrivateFieldName(interfaceName)}.{methodName}(");
            }
        }
        else
        {
            sb.Append($"            return {PrivateFieldNamingHelper.GeneratePrivateFieldName(interfaceName)}.{methodName}(");
        }

        // 生成调用参数列表（包含Token和过滤后的参数）
        var callParameters = GenerateCorrectParameterCallList(originalParameters, filteredParameters, "token");
        sb.Append(string.Join(", ", callParameters));
        sb.AppendLine(");");
    }

    /// <summary>
    /// 生成方法体中的异常处理逻辑
    /// </summary>
    protected void GenerateExceptionHandling(StringBuilder sb, string methodName)
    {
        if (sb == null) return;
        sb.AppendLine("        }");
        sb.AppendLine("        catch (Exception x)");
        sb.AppendLine("        {");
        sb.AppendLine($"            _logger.LogError(x, \"执行{methodName}操作失败：{{message}}\", x.Message);");
        sb.AppendLine("            throw;");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
    }

    /// <summary>
    /// 生成完整的实现方法体
    /// </summary>
    protected string GenerateMethodBody(MethodAnalysisResult methodInfo, string interfaceName, string tokenManageInterfaceName, IReadOnlyList<ParameterInfo> originalParameters, IReadOnlyList<ParameterInfo> filteredParameters)
    {
        if (methodInfo == null) return string.Empty;
        var sb = new StringBuilder();

        // 获取Token - 根据 Token 类型调用不同的方法
        var tokenParameter = originalParameters.FirstOrDefault(p => HasAttribute(p, GeneratorConstants.TokenAttributeNames));
        var tokenMethodName = GetTokenMethodName(tokenParameter);

        // 生成方法体
        sb.AppendLine("        try");
        sb.AppendLine("        {");

        // 生成Token获取逻辑
        GenerateTokenAcquisition(sb, tokenManageInterfaceName, tokenMethodName, originalParameters);

        // 生成API调用逻辑
        GenerateApiCall(sb, methodInfo, interfaceName, methodInfo.MethodName, originalParameters, filteredParameters);

        // 生成异常处理逻辑
        GenerateExceptionHandling(sb, methodInfo.MethodName);

        return sb.ToString();
    }

    #endregion
}