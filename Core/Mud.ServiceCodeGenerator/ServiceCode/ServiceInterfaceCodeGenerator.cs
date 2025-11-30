// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.CodeGenerator 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text;

namespace Mud.ServiceCodeGenerator;

/// <summary>
/// 数据仓库接口代码生成器。
/// </summary>
public class ServiceInterfaceCodeGenerator : ServiceCodeBaseGenerator
{
    /// <inheritdoc/>
    protected override (CompilationUnitSyntax? unitSyntax, string? className) GenerateCode(ClassDeclarationSyntax classNode)
    {
        var cNamespace = GetNamespaceName(classNode);
        var orgClassName = SyntaxHelper.GetClassName(classNode);
        var className = string.IsNullOrEmpty(EntitySuffix) ? orgClassName : orgClassName.Replace(EntitySuffix, "");
        var interfaceClassName = $"I{className}Repository";

        var sb = new StringBuilder();
        GenerateCommonCode(sb, classNode);
        sb.AppendLine("{");
        sb.AppendLine($"    public partial interface {interfaceClassName}:IBaseSqlRepository<{orgClassName}>");
        sb.AppendLine("    {");

        sb.AppendLine("        /// <summary>");
        sb.AppendLine("        /// 构建标准的查询方法");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"        ISelect<{orgClassName}> BuildQuerySelect({className}QueryInput bo);");

        sb.AppendLine("    }");
        sb.AppendLine("}");

        var syntaxTree = CSharpSyntaxTree.ParseText(sb.ToString());
        // 获取 CompilationUnitSyntax 对象
        var compilationUnit = syntaxTree.GetRoot() as CompilationUnitSyntax;
        return (compilationUnit, interfaceClassName);
    }

    /// <inheritdoc/>
    public string GetNamespaceName(TypeDeclarationSyntax classNode)
    {
        var cNamespace = SyntaxHelper.GetNamespaceName(classNode);
        return string.IsNullOrEmpty(EntitySuffix) ? cNamespace + "Interface" : cNamespace.Replace(EntitySuffix, "Interface");
    }
}