using System.Text;

namespace Mud.EntityCodeGenerator;

/// <summary>
/// 生成业务数据新增操作CrInput类。
/// </summary>
[Generator(LanguageNames.CSharp)]
public class TransitiveCrInputGenerator() : TransitiveBoGenerator(true, false)
{
    /// <inheritdoc/>
    public const string Suffix = "CrInput";

    /// <inheritdoc/>
    protected override string ClassSuffix => Suffix;

    /// <inheritdoc/>
    protected override void GeneratorMethodContent<T>(T member, StringBuilder sb, bool isPrimary)
    {
        // 提高容错性，处理空对象情况
        if (sb == null || member == null)
            return;

        if (isPrimary)
            return;

        var orgPropertyName = "";
        if (member is PropertyDeclarationSyntax property)
        {
            orgPropertyName = GetPropertyName(property);
        }
        else if (member is FieldDeclarationSyntax field)
        {
            orgPropertyName = GetFirstUpperPropertyName(field);
        }
        
        // 提高容错性，确保属性名不为空
        if (string.IsNullOrEmpty(orgPropertyName))
            return;
            
        var propertyName = ToLowerFirstLetter(orgPropertyName);
        sb.AppendLine($"            entity.{orgPropertyName}=this.{propertyName};");
    }
}