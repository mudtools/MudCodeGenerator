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
        return Configuration.UpInputSuffix;
    }

    /// <inheritdoc/>
    protected override string GetInheritClass(ClassDeclarationSyntax classNode)
    {
        return GetGeneratorClassName(classNode, Configuration.CrInputSuffix);
    }
}