// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.CodeGenerator 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.EntityCodeGenerator.Helper;

namespace Mud.EntityCodeGenerator;

/// <summary>
/// 生成业务数据VO类。
/// </summary>
public class TransitiveVoGenerator : BaseDtoGenerator
{
    /// <inheritdoc/>
    protected override string GetConfiguredClassSuffix()
    {
        // 根据当前类名确定后缀
        var currentClassName = this.GetType().Name;
        return currentClassName switch
        {
            nameof(TransitiveListOutputGenerator) => ConfigurationManager.Instance.GetClassSuffix("vo"),
            nameof(TransitiveInfoOutputGenerator) => ConfigurationManager.Instance.GetClassSuffix("infooutput"),
            _ => ConfigurationManager.Instance.GetClassSuffix("vo")
        };
    }

    /// <inheritdoc/>
    protected override string[] GetPropertyAttributes()
    {
        var defaultAttributes = new[] { "DictFormat", "Translation", "Sensitive" };
        var extraAttributes = GetVoPropertyAttributes();

        if (extraAttributes != null && extraAttributes.Length > 0)
        {
            // 合并默认属性和额外属性，并去重
            return defaultAttributes.Concat(extraAttributes).Concat(["ExportProperty"])
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        return defaultAttributes.Concat(["ExportProperty"]).ToArray();
    }



    /// <summary>
    /// 生成属性。
    /// </summary>
    /// <param name="member"></param>
    /// <returns></returns>
    private PropertyDeclarationSyntax? GenAttributeFunc(PropertyDeclarationSyntax member)
    {
        if (AttributeDataHelper.IgnoreGenerator(member))
            return null;
        return BuildProperty(member);
    }

    /// <summary>
    /// 生成属性。
    /// </summary>
    /// <param name="member"></param>
    /// <returns></returns>
    private PropertyDeclarationSyntax? GenAttributeFunc(FieldDeclarationSyntax member)
    {
        if (AttributeDataHelper.IgnoreGenerator(member))
            return null;
        return BuildProperty(member, false);
    }

    /// <inheritdoc/>
    public override string GeneratorName => "VO Generator";

    /// <inheritdoc/>
    public override bool ShouldGenerate(ClassDeclarationSyntax classDeclaration)
    {
        return SyntaxHelper.GetClassAttributeValues<bool>(classDeclaration, DtoGeneratorAttributeName, DtoGeneratorAttributeGenVoClass, true);
    }

    /// <inheritdoc/>
    public override string GetGeneratedClassName(ClassDeclarationSyntax classDeclaration)
    {
        return GetGeneratorClassName(classDeclaration);
    }

    /// <inheritdoc/>
    public override string GetGeneratedNamespace(ClassDeclarationSyntax classDeclaration)
    {
        return GetDtoNamespaceName(classDeclaration);
    }

    /// <inheritdoc/>
    protected override string GetGeneratorPropertyName() => DtoGeneratorAttributeGenVoClass;

    /// <inheritdoc/>
    protected override Func<PropertyDeclarationSyntax, PropertyDeclarationSyntax> CreatePropertyGenerator()
    {
        return m => GenAttributeFunc(m);
    }

    /// <inheritdoc/>
    protected override Func<FieldDeclarationSyntax, PropertyDeclarationSyntax> CreateFieldGenerator()
    {
        return m => GenAttributeFunc(m);
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

    /// <inheritdoc/>
    protected override Microsoft.CodeAnalysis.DiagnosticDescriptor GetFailureDescriptor()
    {
        return Diagnostics.VoGenerationFailure;
    }

    /// <inheritdoc/>
    protected override Microsoft.CodeAnalysis.DiagnosticDescriptor GetErrorDescriptor()
    {
        return Diagnostics.VoGenerationError;
    }
}