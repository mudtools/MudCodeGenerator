using Mud.EntityCodeGenerator.Helper;

namespace Mud.EntityCodeGenerator;

/// <summary>
/// 生成业务数据更新操作UpInput类。
/// </summary>
[Generator(LanguageNames.CSharp)]
public class TransitiveUpInputGenerator : TransitiveBoGenerator
{
    public TransitiveUpInputGenerator() : base(false, true) { }

    /// <inheritdoc/>
    protected override string GetConfiguredClassSuffix()
    {
        return ConfigurationManager.Instance.GetClassSuffix("upinput");
    }

    /// <inheritdoc/>
    protected override string GetInheritClass(ClassDeclarationSyntax classNode)
    {
        return GetGeneratorClassName(classNode, ConfigurationManager.Instance.GetClassSuffix("crinput"));
    }
}