using System.Text;

namespace Mud.ServiceCodeGenerator;

/// <summary>
/// 数据仓库实现代码生成器
/// </summary>
public class ServiceImpCodeGenerator : ServiceCodeGenerator
{
    /// <inheritdoc/>
    protected override (CompilationUnitSyntax? unitSyntax, string? className) GenerateCode(ClassDeclarationSyntax classNode)
    {
        var cNamespace = GetNamespaceName(classNode);
        var orgClassName = SyntaxHelper.GetClassName(classNode);
        var className = string.IsNullOrEmpty(EntitySuffix) ? orgClassName : orgClassName.Replace(EntitySuffix, "");
        var serviceClassName = $"{className}Service";

        var sb = new StringBuilder();
        GenerateCommonCode(sb, classNode);
        sb.AppendLine("{");
        sb.AppendLine($"    public partial class {serviceClassName}");
        sb.AppendLine("    {");

        sb.AppendLine("        /// <summary>");
        sb.AppendLine("        /// 构建标准的查询方法");
        sb.AppendLine("        /// </summary>");

        GenerateBuildQuerySelect(sb, classNode, orgClassName);

        sb.AppendLine("    }");
        sb.AppendLine("}");

        var syntaxTree = CSharpSyntaxTree.ParseText(sb.ToString());
        // 获取 CompilationUnitSyntax 对象
        var compilationUnit = syntaxTree.GetRoot() as CompilationUnitSyntax;
        return (compilationUnit, serviceClassName);
    }

    /// <summary>
    /// 生成BuildQuerySelect函数。
    /// </summary>
    /// <param name="sb"></param>
    /// <param name="classNode"></param>
    /// <param name="entityType"></param>
    private void GenerateBuildQuerySelect(StringBuilder sb, ClassDeclarationSyntax classNode, string entityType)
    {
        var dtoClassName = string.IsNullOrEmpty(EntitySuffix) ? entityType : entityType.Replace(EntitySuffix, "");

        sb.AppendLine($"        protected ISelect<{entityType}> BuildQuerySelect(IDefaultSqlRepository<{entityType}> repository, {dtoClassName}QueryInput bo)");
        sb.AppendLine("        {");
        sb.AppendLine($"            var lqw = repository.Orm.Select<{entityType}>();");

        List<OrderBy> orders = [];
        foreach (var member in classNode.Members.OfType<PropertyDeclarationSyntax>())
        {
            var orderBy = GetOrderByAttribute(member);
            if (orderBy != null)
                orders.Add(orderBy);

            if (IsIgnoreGenerator(member))
                continue;
            var isLikeQuery = IsLikeGenerator(member);

            var (propertyName, propertyType) = GetGeneratorProperty(member);
            var orgPropertyName = GetPropertyName(member);

            if (propertyType.StartsWith("string", StringComparison.CurrentCultureIgnoreCase))
            {
                if (!isLikeQuery)
                    sb.AppendLine($"            lqw = lqw.WhereIf(!string.IsNullOrEmpty(bo.{propertyName}), x => x.{orgPropertyName} == bo.{propertyName});");
                else
                    sb.AppendLine($"            lqw = lqw.WhereIf(!string.IsNullOrEmpty(bo.{propertyName}), x => x.{orgPropertyName}.Contains(bo.{propertyName}));");
            }
            else
                sb.AppendLine($"            lqw = lqw.WhereIf(bo.{propertyName}!=null, x => x.{orgPropertyName} == bo.{propertyName});");
        }

        if (orders.Count > 0)
        {
            var orderList = orders.OrderBy(o => o.OrderNum)
                                  .ThenBy(o => !o.IsAsc) // 先排升序再排降序
                                  .ToList();
            foreach (var order in orderList)
            {
                if (order.IsAsc)
                    sb.AppendLine($"            lqw = lqw.OrderBy(x => x.{order.PropertyName});");
                else
                    sb.AppendLine($"            lqw = lqw.OrderByDescending(x => x.{order.PropertyName});");
            }
        }
        sb.AppendLine("            return lqw;");
        sb.AppendLine("        }");
    }

    /// <summary>
    /// 检查属性是否为模糊查询
    /// </summary>
    /// <param name="propertyDeclaration">属性声明</param>
    /// <returns>是否为模糊查询</returns>
    private bool IsLikeGenerator(PropertyDeclarationSyntax propertyDeclaration)
    {
        if (propertyDeclaration == null)
            return false;

        var attributes = AttributeSyntaxHelper.GetAttributeSyntaxes(propertyDeclaration, "LikeQueryAttribute");
        return attributes != null && attributes.Any();
    }

    /// <inheritdoc/>
    protected override string GetNamespaceName(TypeDeclarationSyntax classNode)
    {
        var cNamespace = base.GetNamespaceName(classNode);
        return string.IsNullOrEmpty(EntitySuffix) ? cNamespace + "Services" : cNamespace.Replace(EntitySuffix, "Services");
    }
}