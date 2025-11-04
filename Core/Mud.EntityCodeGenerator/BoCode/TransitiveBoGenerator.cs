using Mud.EntityCodeGenerator.Helper;

namespace Mud.EntityCodeGenerator;

/// <summary>
/// BO对象生成。
/// </summary>
/// <param name="generateNotPrimary">是否生成非主键属性</param>
/// <param name="generatePrimary">是否生成主键属性</param>
public abstract class TransitiveBoGenerator : BaseDtoGenerator
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
    public override string GeneratorName => "BO Generator";

    /// <inheritdoc/>
    protected override string GetGeneratorPropertyName() => DtoGeneratorAttributeGenBoClass;

    /// <inheritdoc/>
    protected override Func<PropertyDeclarationSyntax, PropertyDeclarationSyntax> CreatePropertyGenerator()
    {
        return member =>
        {
            if (IsIgnoreGenerator(member))
                return null;

            var isPrimary = IsPrimary(member);
            if (isPrimary && !_generatePrimary)
                return null;
            if (!isPrimary && !_generateNotPrimary)
                return null;

            return BuildProperty(member);
        };
    }

    /// <inheritdoc/>
    protected override Func<FieldDeclarationSyntax, PropertyDeclarationSyntax> CreateFieldGenerator()
    {
        return member =>
        {
            if (IsIgnoreGenerator(member))
                return null;

            var isPrimary = IsPrimary(member);
            if (isPrimary && !_generatePrimary)
                return null;
            if (!isPrimary && !_generateNotPrimary)
                return null;

            return BuildProperty(member, false);
        };
    }

    /// <inheritdoc/>
    protected override string[] GetPropertyAttributes()
    {
        var defaultAttributes = new[] { "Required", "Xss", "StringLength", "MaxLength", "MinLength",
                "EmailAddress", "DataValidation", "RegularExpression" };

        // 使用配置管理器的合并功能
        return ConfigurationManager.Instance.MergePropertyAttributes(defaultAttributes, "bo");
    }

    /// <inheritdoc/>
    protected override Microsoft.CodeAnalysis.DiagnosticDescriptor GetFailureDescriptor()
    {
        return DiagnosticDescriptors.BoGenerationFailure;
    }

    /// <inheritdoc/>
    protected override Microsoft.CodeAnalysis.DiagnosticDescriptor GetErrorDescriptor()
    {
        return DiagnosticDescriptors.BoGenerationError;
    }

    /// <inheritdoc/>
    protected override Func<PropertyDeclarationSyntax, PropertyDeclarationSyntax> CreateSafePropertyGenerator()
    {
        var propertyGenerator = CreatePropertyGenerator();
        return ErrorHandler.CreateSafePropertyGenerator(this, propertyGenerator);
    }

    /// <inheritdoc/>
    protected override Func<FieldDeclarationSyntax, PropertyDeclarationSyntax> CreateSafeFieldGenerator()
    {
        var fieldGenerator = CreateFieldGenerator();
        return ErrorHandler.CreateSafePropertyGenerator(this, fieldGenerator);
    }
}