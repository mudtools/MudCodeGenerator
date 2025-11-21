// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.CodeGenerator 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.EntityCodeGenerator.Helper;

namespace Mud.EntityCodeGenerator;

/// <summary>
/// 生成业务数据查询类
/// </summary>
[Generator(LanguageNames.CSharp)]
public class TransitiveQueryInputGenerator : TransitiveDtoGenerator
{
    /// <inheritdoc/>
    protected override string GetConfiguredClassSuffix()
    {
        return ConfigurationManager.Instance.GetClassSuffix("queryinput");
    }

    /// <inheritdoc/>
    protected override string GetInheritClass(ClassDeclarationSyntax classNode)
    {
        return "DataQueryInput";
    }

    protected override void GenerateCode(SourceProductionContext context, Compilation compilation, ClassDeclarationSyntax orgClassDeclaration)
    {
        try
        {
            var genClass = SyntaxHelper.GetClassAttributeValues(orgClassDeclaration, DtoGeneratorAttributeName, DtoGeneratorAttributeGenQueryInputClass, true);
            if (!genClass)
                return;

            var orgClassName = SyntaxHelper.GetClassName(orgClassDeclaration);

            var (localClass, dtoNameSpace, dtoClassName) = BuildLocalClass(orgClassDeclaration);

            HashSet<string> propertes = [];//用于保存属性名集合，防止重复生成。
            // 先处理属性声明
            localClass = BuildLocalClassProperty<PropertyDeclarationSyntax>(orgClassDeclaration, localClass, compilation, propertes,
                            member =>
                            {
                                if (IsIgnoreGenerator(member))
                                    return null;

                                return BuildProperty(member);
                            }, null);

            // 再处理字段声明，但需要避免重复生成
            localClass = BuildLocalClassProperty<FieldDeclarationSyntax>(orgClassDeclaration, localClass, compilation, propertes,
                            member =>
                            {
                                if (IsIgnoreGenerator(member))
                                    return null;
                                return BuildProperty(member, false);
                            }, null);

            // 提高容错性，检查生成的类是否为空
            if (localClass == null)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "EG002",
                        "QueryInput类生成失败",
                        $"无法为类 {orgClassName} 生成QueryInput类",
                        "代码生成",
                        DiagnosticSeverity.Warning,
                        true),
                    Location.None));
                return;
            }

            var compilationUnit = GenCompilationUnitSyntax(localClass, dtoNameSpace, dtoClassName);
            // 在最后统一格式化整个编译单元，确保代码格式正确
            compilationUnit = compilationUnit.NormalizeWhitespace();
            context.AddSource($"{dtoClassName}.g.cs", compilationUnit);
        }
        catch (Exception ex)
        {
            // 提高容错性，报告生成错误
            var className = orgClassDeclaration != null ? SyntaxHelper.GetClassName(orgClassDeclaration) : "Unknown";
            context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor(
                    "QI002",
                    "QueryInput类生成错误",
                    $"生成类 {className} 的QueryInput类时发生错误: {ex.Message}",
                    "代码生成",
                    DiagnosticSeverity.Error,
                    true),
                Location.None));
        }
    }
}