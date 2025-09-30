using System.Text;

namespace Mud.EntityCodeGenerator;

/// <summary>
/// BO对象生成。
/// </summary>
/// <param name="generateNotPrimary">是否生成非主键属性</param>
/// <param name="generatePrimary">是否生成主键属性</param>
public abstract class TransitiveBoGenerator(bool generateNotPrimary, bool generatePrimary) : TransitiveDtoGenerator
{
    /// <summary>
    /// 是否生成主键属性。
    /// </summary>
    private bool _generatePrimary { get; } = generatePrimary;

    /// <summary>
    /// 是否生成非主键属性。
    /// </summary>
    private bool _generateNotPrimary { get; } = generateNotPrimary;

    /// <inheritdoc/>
    protected override string[] GetPropertyAttributes()
    {
        return ["Required", "Xss", "StringLength", "MaxLength", "MinLength", "EmailAddress", "DataValidation", "RegularExpression"];
    }

    /// <inheritdoc/>
    protected override void GenerateCode(SourceProductionContext context, ClassDeclarationSyntax orgClassDeclaration)
    {
        try
        {
            var genBoClass = SyntaxHelper.GetClassAttributeValues(orgClassDeclaration, DtoGeneratorAttributeName, DtoGeneratorAttributeGenBoClass, true);
            if (!genBoClass)
                return;
            //Debugger.Launch();

            var orgClassName = SyntaxHelper.GetClassName(orgClassDeclaration);
            var sb = GenMethodStart(orgClassName);
            var (localClass, dtoNameSpace, dtoClassName) = GenLocalClass(orgClassDeclaration);

            localClass = GenLocalClassProperty<PropertyDeclarationSyntax>(orgClassDeclaration, localClass, member =>
            {
                if (IsIgnoreGenerator(member))
                    return null;
                var isPrimary = IsPrimary(member);
                GeneratorMethodContent(member, sb, isPrimary);
                if (isPrimary && !_generatePrimary)
                    return null;
                if (!isPrimary && !_generateNotPrimary)
                    return null;
                return GeneratorProperty(member);
            }, null);
            localClass = GenLocalClassProperty<FieldDeclarationSyntax>(orgClassDeclaration, localClass, member =>
            {
                if (IsIgnoreGenerator(member))
                    return null;
                var isPrimary = IsPrimary(member);
                GeneratorMethodContent(member, sb, isPrimary);
                if (isPrimary && !_generatePrimary)
                    return null;
                if (!isPrimary && !_generateNotPrimary)
                    return null;
                return GeneratorProperty(member, false);
            }, null);

            var methodDeclaration = GenMethodEnd(sb);
            if (methodDeclaration != null)
                localClass = localClass.AddMembers(methodDeclaration);

            // 提高容错性，检查生成的类是否为空
            if (localClass == null)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "EG001",
                        "BO类生成失败",
                        $"无法为类 {orgClassName} 生成BO类",
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
            var className = orgClassDeclaration != null ? SyntaxHelper.GetClassName(orgClassDeclaration) : "Unknown";
            context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor(
                    "BO002",
                    "BO类生成错误",
                    $"生成类 {className} 的BO类时发生错误: {ex.Message}",
                    "代码生成",
                    DiagnosticSeverity.Error,
                    true),
                Location.None));
        }
    }

    /// <summary>
    /// 根据属性<see cref="PropertyDeclarationSyntax"/>生成方法内容
    /// </summary>
    protected abstract void GeneratorMethodContent<T>(T member, StringBuilder sb, bool isPrimary)
        where T : MemberDeclarationSyntax;


    /// <summary>
    /// 生成方法起始部分
    /// </summary>
    /// <param name="orgClassName"></param>
    /// <returns></returns>
    protected virtual StringBuilder GenMethodStart(string orgClassName)
    {
        // 提高容错性，确保参数不为空
        if (string.IsNullOrEmpty(orgClassName))
            orgClassName = "Object";

        var sb = new StringBuilder();
        sb.AppendLine("class TestProgram{");
        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// 通用的BO对象映射至实体方法。");
        sb.AppendLine("/// </summary>");
        sb.AppendLine($"public virtual {orgClassName} MapTo()");
        sb.AppendLine("        {");
        sb.AppendLine($"           var entity=new {orgClassName}();");
        return sb;
    }

    private MethodDeclarationSyntax GenMethodEnd(StringBuilder sb)
    {
        // 提高容错性，处理空对象情况
        if (sb == null)
            return null;

        sb.AppendLine("            return entity;");
        sb.AppendLine("        }");
        sb.AppendLine("}");
        return GetMethodDeclarationSyntax(sb);
    }
}