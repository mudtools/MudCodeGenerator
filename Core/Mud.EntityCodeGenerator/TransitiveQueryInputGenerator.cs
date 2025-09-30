using System.Text;

namespace Mud.EntityCodeGenerator;

/// <summary>
/// 生成业务数据查询类
/// </summary>
[Generator(LanguageNames.CSharp)]
public class TransitiveQueryInputGenerator : TransitiveDtoGenerator
{
    /// <inheritdoc/>
    protected override string ClassSuffix => "QueryInput";

    /// <inheritdoc/>
    protected override string GetInheritClass(ClassDeclarationSyntax classNode)
    {
        return "DataQueryInput";
    }

    /// <inheritdoc/>
    protected override void GenerateCode(SourceProductionContext context, ClassDeclarationSyntax orgClassDeclaration)
    {
        try
        {
            var genClass = GetClassAttributeValues(orgClassDeclaration, DtoGeneratorAttributeName, DtoGeneratorAttributeGenQueryInputClass, true);
            if (!genClass)
                return;

            var orgClassName = GetClassName(orgClassDeclaration);
            var sb = GetStartWherePart(orgClassName);

            var (localClass, dtoNameSpace, dtoClassName) = GenLocalClass(orgClassDeclaration);

            localClass = GenLocalClassProperty<PropertyDeclarationSyntax>(orgClassDeclaration, localClass,
                            member =>
                            {
                                if (IsIgnoreGenerator(member))
                                    return null;

                                GeneratorWhereContent(member, sb);

                                return GeneratorProperty(member);
                            }, null);
            localClass = GenLocalClassProperty<FieldDeclarationSyntax>(orgClassDeclaration, localClass,
                             member =>
                             {
                                 if (IsIgnoreGenerator(member))
                                     return null;

                                 GeneratorWhereContent(member, sb);

                                 return GeneratorProperty(member, false);
                             }, null);

            var methodDeclaration = GetMethodDeclaration(sb);
            if (methodDeclaration != null)
                localClass = localClass.AddMembers(methodDeclaration);

            // 提高容错性，检查生成的类是否为空
            if (localClass == null)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "EG002",
                        "QueryInput类生成失败",
                        $"无法为类 {orgClassName} 生成QueryInput类",
                        "代码生成",
                        DiagnosticSeverity.Warning,
                        true),
                    Location.None));
                return;
            }

            var compilationUnit = GenCompilationUnitSyntax(localClass, dtoNameSpace, dtoClassName);
            context.AddSource($"{dtoClassName}.g.cs", compilationUnit);
        }
        catch (Exception ex)
        {
            // 提高容错性，报告生成错误
            var className = orgClassDeclaration != null ? GetClassName(orgClassDeclaration) : "Unknown";
            context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor(
                    "QI002",
                    "QueryInput类生成错误",
                    $"生成类 {className} 的QueryInput类时发生错误: {ex.Message}",
                    "代码生成",
                    DiagnosticSeverity.Error,
                    true),
                Location.None));
        }
    }

    private StringBuilder GetStartWherePart(string orgClassName)
    {
        var sb = new StringBuilder();
        sb.AppendLine("class TestProgram{");
        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// 构建通用的查询条件。");
        sb.AppendLine("/// </summary>");
        sb.AppendLine($"public Expression<Func<{orgClassName}, bool>> BuildQueryWhere()");
        sb.AppendLine("        {");
        sb.AppendLine($"            Expression<Func<SysClientEntity, bool>> where = x => true;");
        return sb;
    }

    //生成条件方法逻辑
    private void GeneratorWhereContent<T>(T member, StringBuilder sb)
         where T : MemberDeclarationSyntax
    {
        // 提高容错性，处理空对象情况
        if (member == null || sb == null)
            return;

        var isLikeQuery = false;
        if (IsLikeGenerator(member))
            isLikeQuery = true;
        var (propertyName, propertyType) = GetGeneratorProperty(member);
        var orgPropertyName = "";
        if (member is PropertyDeclarationSyntax property)
            orgPropertyName = GetPropertyName(property);
        else if (member is FieldDeclarationSyntax field)
        {
            orgPropertyName = propertyName;
            propertyName = ToLowerFirstLetter(orgPropertyName);
        }

        GeneratorWhereContent(sb, isLikeQuery, propertyName, propertyType, orgPropertyName);
    }

    private void GeneratorWhereContent(StringBuilder sb, bool isLikeQuery, string propertyName, string propertyType, string orgPropertyName)
    {
        // 提高容错性，处理空对象情况
        if (sb == null)
            return;

        // 提高容错性，确保参数不为空
        if (string.IsNullOrEmpty(propertyName))
            propertyName = "Property";
        if (string.IsNullOrEmpty(propertyType))
            propertyType = "object";
        if (string.IsNullOrEmpty(orgPropertyName))
            orgPropertyName = propertyName;

        if (propertyType.StartsWith("string", StringComparison.OrdinalIgnoreCase))
        {
            sb.AppendLine($"         if(!string.IsNullOrEmpty(this.{propertyName}?.Trim()))");
            if (!isLikeQuery)
                sb.AppendLine($"            where = where.And( x => x.{orgPropertyName} == this.{propertyName}.Trim());");
            else
                sb.AppendLine($"            where = where.And( x => x.{orgPropertyName}.Contains(this.{propertyName}.Trim()));");
        }
        else
        {
            sb.AppendLine($"         if(this.{propertyName}!=null)");
            sb.AppendLine($"            where = where.And( x => x.{orgPropertyName} == this.{propertyName});");
        }
    }


    private MethodDeclarationSyntax GetMethodDeclaration(StringBuilder sb)
    {
        // 提高容错性，处理空对象情况
        if (sb == null)
            return null;

        sb.AppendLine("            return where;");
        sb.AppendLine("        }");
        sb.AppendLine("}");

        return GetMethodDeclarationSyntax(sb);
    }
}