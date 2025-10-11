using Mud.EntityCodeGenerator.Diagnostics;
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

            var entityNamespace = GetNamespaceName(orgClassDeclaration);

            // 构建建造者模式类
            var builderClassName = $"{orgClassName}Builder";
            var builderClass = BuildBuilderClass(orgClassDeclaration, orgClassName, builderClassName);

            var compilationUnit = GenCompilationUnitSyntax(builderClass, entityNamespace, builderClassName);
            context.AddSource($"{builderClassName}.g.cs", compilationUnit);
        }
        catch (Exception ex)
        {
            var className = orgClassDeclaration != null ? SyntaxHelper.GetClassName(orgClassDeclaration) : "Unknown";
            ReportErrorDiagnostic(context, DiagnosticDescriptors.EntityMethodGenerationError, className, ex);
        }
    }

    /// <summary>
    /// 构建扩展类，包含所有映射方法
    /// </summary>    
    private ClassDeclarationSyntax BuildBuilderClass(
        ClassDeclarationSyntax orgClassDeclaration,
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
        // 生成属性设置函数
        GeneratePropertyMappings<PropertyDeclarationSyntax>(
            orgClassDeclaration,
            sb,
            (orgPropertyName, propertyName, propertyType) => GenPropertySet(builderClassName, orgClassName, privateFieldName, orgPropertyName, propertyName, propertyType)
            );
        // 生成私有字段设置函数
        GeneratePropertyMappings<FieldDeclarationSyntax>(
            orgClassDeclaration,
            sb,
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
        Func<string, string, string, string> generateSetMethod)
         where T : MemberDeclarationSyntax
    {
        foreach (var member in orgClassDeclaration.Members.OfType<T>())
        {
            try
            {
                if (IsIgnoreGenerator(member))
                {
                    continue;
                }
                var orgPropertyName = "";
                var propertyType = "";
                if (member is PropertyDeclarationSyntax property)
                {
                    orgPropertyName = GetPropertyName(property);
                    propertyType = SyntaxHelper.GetPropertyType(property);
                }
                else if (member is FieldDeclarationSyntax field)
                {
                    orgPropertyName = GetFirstUpperPropertyName(field);
                    propertyType = SyntaxHelper.GetPropertyType(field);
                }

                if (string.IsNullOrEmpty(orgPropertyName))
                {
                    continue;
                }
                orgPropertyName = StringExtensions.ToUpperFirstLetter(orgPropertyName);
                var propertyName = ToUpperFirstLetter(orgPropertyName);
                var mappingLine = generateSetMethod(orgPropertyName, propertyName, propertyType);
                sb.AppendLine(mappingLine);
            }
            catch (Exception ex)
            {
                // 即使单个属性生成失败也不影响其他属性
            }
        }
    }
}