// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.CodeGenerator 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.EntityCodeGenerator.Helper;
using System.Text;

namespace Mud.EntityCodeGenerator;

/// <summary>
/// 生成实体类的建造者模式代码。
/// </summary>
[Generator(LanguageNames.CSharp)]
public class BuilderGenerator : TransitiveDtoGenerator
{
    internal const string BuilderGeneratorAttributeName = "BuilderAttribute";

    /// <summary>
    /// BuilderGenerator构造函数
    /// </summary>
    public BuilderGenerator() : base()
    {
    }


    /// <inheritdoc/>
    protected override void GenerateCode(SourceProductionContext context, Compilation compilation, ClassDeclarationSyntax orgClassDeclaration)
    {
        try
        {
            // 获取原始类名
            var orgClassName = SyntaxHelper.GetClassName(orgClassDeclaration);

            // 检查是否需要生成建造者模式代码。
            var genBuilderCode = SyntaxHelper.GetAttributeSyntaxes(
                orgClassDeclaration,
                BuilderGeneratorAttributeName);

            if (!genBuilderCode.Any())
                return;

            var entityNamespace = SyntaxHelper.GetNamespaceName(orgClassDeclaration);

            // 构建建造者模式类
            var builderClassName = $"{orgClassName}Builder";
            var builderClass = BuildBuilderClass(orgClassDeclaration, compilation, orgClassName, builderClassName);

            var compilationUnit = GenCompilationUnitSyntax(builderClass, entityNamespace, builderClassName);
            context.AddSource($"{builderClassName}.g.cs", compilationUnit);
        }
        catch (Exception ex)
        {
            var className = orgClassDeclaration != null ? SyntaxHelper.GetClassName(orgClassDeclaration) : "Unknown";
            ReportErrorDiagnostic(context, Diagnostics.EntityBuilderGenerationError, className, ex);
        }
    }

    /// <summary>
    /// 构建扩展类，包含所有映射方法
    /// </summary>    
    private ClassDeclarationSyntax BuildBuilderClass(
        ClassDeclarationSyntax orgClassDeclaration,
        Compilation compilation,
        string orgClassName,
        string builderClassName)
    {
        string privateFieldName = PrivateFieldNamingHelper.GeneratePrivateFieldName(orgClassName, FieldNamingStyle.UnderscoreCamel);

        var sb = new StringBuilder();
        sb.AppendLine($"/// <summary>");
        sb.AppendLine($"/// <see cref=\"{orgClassName}\"/> 的构建者。");
        sb.AppendLine($"/// </summary>");
        sb.AppendLine($"public class {builderClassName}");
        sb.AppendLine("{");
        sb.AppendLine($"    private {orgClassName} {privateFieldName} = new {orgClassName}();");
        sb.Append("\n\n\n");
        // 生成属性设置函数（只处理属性声明，避免重复生成）
        GeneratePropertyMappings<PropertyDeclarationSyntax>(
            orgClassDeclaration,
            sb, compilation,
            (orgPropertyName, propertyName, propertyType) => GenPropertySet(builderClassName, orgClassName, privateFieldName, orgPropertyName, propertyName, propertyType)
            );

        sb.AppendLine($"    /// <summary>");
        sb.AppendLine($"    /// 构建 <see cref=\"{orgClassName}\"/> 类的实例。");
        sb.AppendLine($"    /// </summary>");
        sb.AppendLine($"    public {orgClassName} Build()");
        sb.AppendLine("    {");
        sb.AppendLine($"        return this.{privateFieldName};");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return SyntaxHelper.GetClassDeclarationSyntax(sb);
    }

    private string GenPropertySet(string builderClassName, string orgClassName, string privateFieldName, string orgPropertyName, string propertyName, string propertyType)
    {
        string parameterName = PrivateFieldNamingHelper.GeneratePrivateFieldName(orgPropertyName, FieldNamingStyle.PureCamel);
        var sb = new StringBuilder();
        sb.AppendLine($"    /// <summary>");
        sb.AppendLine($"    /// 设置 <see cref=\"{orgClassName}.{orgPropertyName}\"/> 属性值。");
        sb.AppendLine($"    /// </summary>");
        sb.AppendLine($"    /// <param name=\"{parameterName}\">属性值</param>");
        sb.AppendLine($"    /// <returns>返回 <see cref=\"{builderClassName}\"/> 实例</returns>");
        sb.AppendLine($"    public {builderClassName} Set{orgPropertyName}({propertyType} {parameterName})");
        sb.AppendLine("    {");
        sb.AppendLine($"        this.{privateFieldName}.{orgPropertyName} = {parameterName};");
        sb.AppendLine($"        return this;");
        sb.AppendLine("    }");
        return sb.ToString();
    }


    /// <summary>
    /// 生成属性映射代码
    /// </summary>
    private void GeneratePropertyMappings<T>(
        ClassDeclarationSyntax orgClassDeclaration,
        StringBuilder sb,
        Compilation compilation,
        Func<string, string, string, string> generateSetMethod)
         where T : MemberDeclarationSyntax
    {
        MemberProcessor.GeneratePropertyMappings<T>(
            orgClassDeclaration,
            sb,
            compilation,
            generateSetMethod,
            AttributeDataHelper.IgnoreGenerator,
            IsPrimary,
            GetBuilderPropertyNames,
            GetPropertyType);
    }
}