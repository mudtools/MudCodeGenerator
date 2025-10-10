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

    protected override void GenerateCode(SourceProductionContext context, Compilation compilation, ClassDeclarationSyntax orgClassDeclaration)
    {
        try
        {
            var genClass = SyntaxHelper.GetClassAttributeValues(orgClassDeclaration, DtoGeneratorAttributeName, DtoGeneratorAttributeGenQueryInputClass, true);
            if (!genClass)
                return;

            var orgClassName = SyntaxHelper.GetClassName(orgClassDeclaration);

            var (localClass, dtoNameSpace, dtoClassName) = BuildLocalClass(orgClassDeclaration);

            localClass = BuildLocalClassProperty<PropertyDeclarationSyntax>(orgClassDeclaration, localClass,
                            member =>
                            {
                                if (IsIgnoreGenerator(member))
                                    return null;

                                return BuildProperty(member);
                            }, null);
            localClass = BuildLocalClassProperty<FieldDeclarationSyntax>(orgClassDeclaration, localClass,
                             member =>
                             {
                                 if (IsIgnoreGenerator(member))
                                     return null;

                                 return BuildProperty(member, false);
                             }, null);

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
            // 在最后统一格式化整个编译单元，确保代码格式正确
            compilationUnit = compilationUnit.NormalizeWhitespace();
            context.AddSource($"{dtoClassName}.g.cs", compilationUnit);
        }
        catch (Exception ex)
        {
            // 提高容错性，报告生成错误
            var className = orgClassDeclaration != null ? SyntaxHelper.GetClassName(orgClassDeclaration) : "Unknown";
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
}