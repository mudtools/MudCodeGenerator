using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Mud.EntityCodeGenerator;

/// <summary>
/// 生成业务数据新增操作CrInput类。
/// </summary>
[Generator(LanguageNames.CSharp)]
public class TransitiveCrInputGenerator : TransitiveBoGenerator
{
    public TransitiveCrInputGenerator() : base(true, false) { }

    /// <inheritdoc/>
    public const string Suffix = "CrInput";

    /// <inheritdoc/>
    protected override string ClassSuffix => Suffix;

    /// <inheritdoc/>
    protected override StringBuilder GenMethodStart(string orgClassName)
    {
        // 不生成MapTo方法，只创建测试类结构
        var sb = new StringBuilder();
        sb.AppendLine("class TestProgram{");
        return sb;
    }

    /// <inheritdoc/>
    protected override void GeneratorMethodContent<T>(T member, StringBuilder sb, bool isPrimary)
    {
        // 不生成MapTo方法中的属性赋值逻辑
        return;
    }
}