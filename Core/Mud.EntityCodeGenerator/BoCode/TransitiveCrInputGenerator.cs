namespace Mud.EntityCodeGenerator;

using Mud.EntityCodeGenerator.Helper;

/// <summary>
/// 生成业务数据新增操作CrInput类。
/// </summary>
[Generator(LanguageNames.CSharp)]
public class TransitiveCrInputGenerator : TransitiveBoGenerator
{
    public TransitiveCrInputGenerator() : base(true, false) { }

    /// <inheritdoc/>
    protected override string GetConfiguredClassSuffix()
    {
        return ConfigurationManager.Instance.GetClassSuffix("crinput");
    }
}