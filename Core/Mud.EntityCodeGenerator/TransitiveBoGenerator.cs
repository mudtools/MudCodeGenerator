using Mud.EntityCodeGenerator.Diagnostics;

namespace Mud.EntityCodeGenerator;

/// <summary>
/// BO对象生成。
/// </summary>
/// <param name="generateNotPrimary">是否生成非主键属性</param>
/// <param name="generatePrimary">是否生成主键属性</param>
public abstract class TransitiveBoGenerator : TransitiveDtoGenerator
{
    /// <summary>
    /// 是否生成主键属性。
    /// </summary>
    private readonly bool _generatePrimary;

    /// <summary>
    /// 是否生成非主键属性。
    /// </summary>
    private readonly bool _generateNotPrimary;

    protected TransitiveBoGenerator(bool generateNotPrimary, bool generatePrimary)
    {
        _generatePrimary = generatePrimary;
        _generateNotPrimary = generateNotPrimary;
    }

    /// <inheritdoc/>
    protected override string[] GetPropertyAttributes()
    {
        var defaultAttributes = new[] { "Required", "Xss", "StringLength", "MaxLength", "MinLength",
                "EmailAddress", "DataValidation", "RegularExpression" };
        var extraAttributes = GetBoPropertyAttributes();

        if (extraAttributes != null && extraAttributes.Length > 0)
        {
            // 合并默认属性和额外属性，并去重
            return defaultAttributes.Concat(extraAttributes)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        return defaultAttributes;
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
            var (localClass, dtoNameSpace, dtoClassName) = BuildLocalClass(orgClassDeclaration);

            localClass = BuildLocalClassProperty<PropertyDeclarationSyntax>(orgClassDeclaration, localClass, member =>
            {
                if (IsIgnoreGenerator(member))
                    return null;
                var isPrimary = IsPrimary(member);
                if (isPrimary && !_generatePrimary)
                    return null;
                if (!isPrimary && !_generateNotPrimary)
                    return null;
                return BuildProperty(member);
            }, null);
            localClass = BuildLocalClassProperty<FieldDeclarationSyntax>(orgClassDeclaration, localClass, member =>
            {
                if (IsIgnoreGenerator(member))
                    return null;
                var isPrimary = IsPrimary(member);
                if (isPrimary && !_generatePrimary)
                    return null;
                if (!isPrimary && !_generateNotPrimary)
                    return null;
                return BuildProperty(member, false);
            }, null);

            // 提高容错性，检查生成的类是否为空
            if (localClass == null)
            {
                ReportFailureDiagnostic(context, DiagnosticDescriptors.BoGenerationFailure, orgClassName);
                return;
            }

            var compilationUnit = GenCompilationUnitSyntax(localClass, dtoNameSpace, dtoClassName);
            context.AddSource($"{dtoClassName}.g.cs", compilationUnit);
        }
        catch (Exception ex)
        {
            // 提高容错性，报告生成错误
            var className = orgClassDeclaration != null ? SyntaxHelper.GetClassName(orgClassDeclaration) : "Unknown";
            ReportErrorDiagnostic(context, DiagnosticDescriptors.BoGenerationError, className, ex);
        }
    }
}