using System.Text;

namespace Mud.ServiceCodeGenerator;

/// <summary>
/// 数据仓库接口代码生成器。
/// </summary>
public class ServiceInterfaceCodeGenerator : ServiceCodeGenerator
{
    /// <inheritdoc/>
    protected override (CompilationUnitSyntax? unitSyntax, string? className) GenerateCode(ClassDeclarationSyntax classNode)
    {
        var cNamespace = GetNamespaceName(classNode);
        var orgClassName = GetClassName(classNode);
        var className = orgClassName.Replace(EntitySuffix, "");
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
    protected override string GetNamespaceName(ClassDeclarationSyntax classNode)
    {
        var cNamespace = base.GetNamespaceName(classNode);
        return cNamespace.Replace(EntitySuffix, "Interface");
    }
}