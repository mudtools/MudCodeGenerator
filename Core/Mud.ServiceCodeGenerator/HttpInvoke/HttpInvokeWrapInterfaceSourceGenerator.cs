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
public class HttpInvokeWrapInterfaceSourceGenerator : HttpInvokeWrapSourceGenerator
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
        // 使用基类的公共逻辑，isInterface=true表示生成接口方法
        return GenerateWrapMethodCommon(methodInfo, methodSyntax, interfaceName, tokenManageInterfaceName, isInterface: true);
    }
}