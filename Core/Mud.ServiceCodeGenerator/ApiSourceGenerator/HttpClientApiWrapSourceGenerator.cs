using System.Collections.Immutable;
using System.Text;

namespace Mud.ServiceCodeGenerator;


/// <summary>
/// HttpClientApiWrap 源代码生成器
/// 为标记有HttpClientApiWrap特性的接口生成二次包装实现类
/// </summary>
public abstract class HttpClientApiWrapSourceGenerator : WebApiSourceGenerator
{

    protected override string[] ApiWrapAttributeNames() => GeneratorConstants.HttpClientApiWrapAttributeNames;


    /// <summary>
    /// 更新Execute方法以使用验证
    /// </summary>
    protected override void ExecuteGenerator(Compilation compilation, ImmutableArray<InterfaceDeclarationSyntax> interfaces, SourceProductionContext context)
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
    protected void GenerateWrapMethods(Compilation compilation, InterfaceDeclarationSyntax interfaceDecl, INamedTypeSymbol interfaceSymbol, StringBuilder sb, bool isInterface, string tokenManageInterfaceName = null, string interfaceName = null)
    {
        if (interfaceSymbol == null) return;

        var methods = interfaceSymbol.GetMembers()
                                     .OfType<IMethodSymbol>()
                                     .Where(m => IsValidMethod(m));

        foreach (var methodSymbol in methods)
        {
            var methodInfo = AnalyzeMethod(compilation, methodSymbol, interfaceDecl);
            if (!methodInfo.IsValid)
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
    /// 获取方法的XML文档注释
    /// </summary>
    protected string GetMethodXmlDocumentation(MethodDeclarationSyntax methodSyntax, MethodAnalysisResult methodInfo)
    {
        var xmlDoc = GetXmlDocumentation(methodSyntax);
        if (string.IsNullOrEmpty(xmlDoc))
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
}