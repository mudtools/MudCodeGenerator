// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.CodeGenerator 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text;

namespace Mud.ServiceCodeGenerator.ApiSourceGenerator;

/// <summary>
/// 用于生成包装接口代码的代码生成器。
/// </summary>
[Generator]
public class HttpClientApiInterfaceWrapSourceGenerator : HttpClientApiWrapSourceGenerator
{
    protected override void GenerateWrapCode(Compilation compilation, InterfaceDeclarationSyntax interfaceDecl, INamedTypeSymbol interfaceSymbol, AttributeData wrapAttribute, SourceProductionContext context)
    {
        if (interfaceSymbol == null) return;

        // 生成包装接口代码
        var wrapInterfaceCode = GenerateWrapInterface(compilation, interfaceDecl, interfaceSymbol, wrapAttribute);
        if (!string.IsNullOrEmpty(wrapInterfaceCode))
        {
            var wrapFileName = $"{interfaceSymbol.Name}.Wrap.g.cs";
            context.AddSource(wrapFileName, wrapInterfaceCode);
        }
    }

    /// <summary>
    /// 生成包装接口代码
    /// </summary>
    protected string GenerateWrapInterface(Compilation compilation, InterfaceDeclarationSyntax interfaceDecl, INamedTypeSymbol interfaceSymbol, AttributeData wrapAttribute)
    {
        var sb = new StringBuilder();

        // 生成文件头部
        GenerateFileHeader(sb, interfaceDecl);

        // 获取包装接口名称
        var wrapInterfaceName = GetWrapInterfaceName(interfaceSymbol, wrapAttribute);

        // 添加接口注释
        var xmlDoc = GetXmlDocumentation(interfaceDecl);
        if (!string.IsNullOrEmpty(xmlDoc))
        {
            sb.Append(xmlDoc);
        }

        // 生成接口开始部分
        GenerateClassOrInterfaceStart(sb, wrapInterfaceName, "", isInterface: true);

        // 生成接口方法
        GenerateWrapMethods(compilation, interfaceDecl, interfaceSymbol, sb);

        sb.AppendLine("}");

        return sb.ToString();
    }


    /// <summary>
    /// 生成包装方法声明（接口方法）
    /// </summary>
    protected override string GenerateWrapMethod(MethodAnalysisResult methodInfo, MethodDeclarationSyntax methodSyntax, string interfaceName, string tokenManageInterfaceName)
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
            var tenantMethod = GenerateBothWrapMethod(methodInfo, methodSyntax, "_Tenant_");
            var userMethod = GenerateBothWrapMethod(methodInfo, methodSyntax, "_User_");

            return $"{tenantMethod}\n\n{userMethod}";
        }
        else
        {
            return GenerateSingleWrapMethod(methodInfo, methodSyntax);
        }
    }

    /// <summary>
    /// 生成单个包装方法声明
    /// </summary>
    private string GenerateSingleWrapMethod(MethodAnalysisResult methodInfo, MethodDeclarationSyntax methodSyntax)
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
        var methodSignature = GenerateMethodSignature(methodInfo, methodInfo.MethodName, filteredParameters, includeAsync: false);
        sb.Append(methodSignature);
        sb.Append(';');

        return sb.ToString();
    }

    /// <summary>
    /// 为TokenType.Both生成特定的方法声明
    /// </summary>
    private string GenerateBothWrapMethod(MethodAnalysisResult methodInfo, MethodDeclarationSyntax methodSyntax, string prefix)
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
        var methodSignature = GenerateMethodSignature(methodInfo, methodName, filteredParameters, includeAsync: false);
        sb.Append(methodSignature);
        sb.Append(';');

        return sb.ToString();
    }
}