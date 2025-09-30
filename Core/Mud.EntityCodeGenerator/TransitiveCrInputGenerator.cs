using System.Text;

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
    protected override void GeneratorMethodContent<T>(T member, StringBuilder sb, bool isPrimary)
    {
        // 基础验证
        if (sb == null || member == null || isPrimary)
            return;

        // 获取原始属性名
        string orgPropertyName = member switch
        {
            PropertyDeclarationSyntax property => GetPropertyName(property),
            FieldDeclarationSyntax field => GetFirstUpperPropertyName(field),
            _ => ""
        };

        if (string.IsNullOrEmpty(orgPropertyName))
            return;

        // 生成赋值语句
        GenerateAssignmentStatement(orgPropertyName, sb);
    }

    /// <summary>
    /// 生成属性赋值语句
    /// </summary>
    /// <param name="orgPropertyName">原始属性名</param>
    /// <param name="sb">字符串构建器</param>
    private void GenerateAssignmentStatement(string orgPropertyName, StringBuilder sb)
    {
        string lowerCasePropertyName = ToLowerFirstLetter(orgPropertyName);
        sb.AppendLine($"            entity.{orgPropertyName}=this.{lowerCasePropertyName};");
    }
}