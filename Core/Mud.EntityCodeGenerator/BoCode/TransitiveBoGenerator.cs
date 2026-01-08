// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.CodeGenerator 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

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
            if (AttributeDataHelper.IgnoreGenerator(member))
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
            if (AttributeDataHelper.IgnoreGenerator(member))
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
        return Diagnostics.BoGenerationFailure;
    }

    /// <inheritdoc/>
    protected override Microsoft.CodeAnalysis.DiagnosticDescriptor GetErrorDescriptor()
    {
        return Diagnostics.BoGenerationError;
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