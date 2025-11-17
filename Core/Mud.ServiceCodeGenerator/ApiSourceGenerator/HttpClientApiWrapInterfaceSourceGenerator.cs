
using System.Text;

namespace Mud.ServiceCodeGenerator;
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
        GenerateWrapMethods(compilation, interfaceDecl, interfaceSymbol, sb, isInterface: true);

        sb.AppendLine("}");

        return sb.ToString();
    }

}