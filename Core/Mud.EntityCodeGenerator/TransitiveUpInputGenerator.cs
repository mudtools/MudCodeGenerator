using System.Text;

namespace Mud.EntityCodeGenerator;

/// <summary>
/// 生成业务数据更新操作UpInput类。
/// </summary>
[Generator(LanguageNames.CSharp)]
public class TransitiveUpInputGenerator() : TransitiveBoGenerator(false, true)
{
    /// <inheritdoc/>
    protected override string ClassSuffix => "UpInput";

    /// <inheritdoc/>
    protected override string GetInheritClass(ClassDeclarationSyntax classNode)
    {
        return GetGeneratorClassName(classNode, TransitiveCrInputGenerator.Suffix);
    }

    /// <inheritdoc/>
    protected override StringBuilder GenMethodStart(string orgClassName)
    {
        // 提高容错性，确保参数不为空
        if (string.IsNullOrEmpty(orgClassName))
            orgClassName = "Object";

        var sb = new StringBuilder();
        sb.AppendLine("class TestProgram{");
        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// 通用的BO对象映射至实体方法。");
        sb.AppendLine("/// </summary>");
        sb.AppendLine($"public override {orgClassName} MapTo()");
        sb.AppendLine("        {");
        sb.AppendLine($"           var entity=base.MapTo();");
        return sb;
    }

    /// <inheritdoc/>
    protected override void GeneratorMethodContent<T>(T member, StringBuilder sb, bool isPrimary)
    {
        // 提高容错性，处理空对象情况
        if (sb == null || member == null)
            return;

        if (!isPrimary)
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