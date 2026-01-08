// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.CodeGenerator 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.EntityCodeGenerator.Helper;
using System.Collections.ObjectModel;
using System.Text;

namespace Mud.EntityCodeGenerator
{
    /// <summary>
    /// 生成私有字段的公共属性。
    /// </summary>
    [Generator(LanguageNames.CSharp)]
    public class EntityPropertyGenerator : TransitiveDtoGenerator
    {
        private string _dtoNameSpace = "";

        private string[] FieldAttributes = ["TableField", "Column", "SugarColumn", "Key"];

        /// <summary>
        /// EntityPropertyGenerator构造函数
        /// </summary>
        public EntityPropertyGenerator() : base()
        {
        }

        /// <inheritdoc/>
        protected override Collection<string> GetFileUsingNameSpaces()
        {
            var nameSpaces = base.GetFileUsingNameSpaces();
            if (!string.IsNullOrEmpty(_dtoNameSpace))
                nameSpaces.Add(_dtoNameSpace);
            return nameSpaces;
        }

        /// <inheritdoc/>
        protected override void GenerateCode(SourceProductionContext context, Compilation compilation, ClassDeclarationSyntax orgClassDeclaration)
        {
            try
            {
                //Debugger.Launch();
                var genMapMethod = SyntaxHelper.GetClassAttributeValues(orgClassDeclaration, DtoGeneratorAttributeName, DtoGeneratorAttributeGenMapMethod, true);
                if (!genMapMethod)
                    return;

                var cNamespace = SyntaxHelper.GetNamespaceName(orgClassDeclaration);
                _dtoNameSpace = GetDtoNamespaceName(orgClassDeclaration);

                var orgClassName = SyntaxHelper.GetClassName(orgClassDeclaration);
                var voClassName = (string.IsNullOrEmpty(EntitySuffix) ? orgClassName : orgClassName.Replace(EntitySuffix, "")) + ConfigurationManager.Instance.GetClassSuffix("vo");

                var localClass = BuildLocalClass(orgClassDeclaration, orgClassName, false);

                var methodDeclaration = GenerateBuildMethod(orgClassDeclaration, orgClassName);
                if (methodDeclaration != null)
                    localClass = localClass.AddMembers(methodDeclaration);

                (localClass, var success) = BuildProperty(localClass, orgClassDeclaration);
                if (!success && methodDeclaration == null)//如果没有任何属性生成，则不生成类
                    return;

                //var methodDeclaration = GenMapMethod(orgClassDeclaration, voClassName);
                //if (methodDeclaration != null)
                //    localClass = localClass.AddMembers(methodDeclaration);

                // 提高容错性，检查生成的类是否为空
                if (localClass == null)
                {
                    ReportFailureDiagnostic(context, Diagnostics.EntityMethodGenerationFailure, orgClassName);
                    return;
                }

                var compilationUnit = GenCompilationUnitSyntax(localClass, cNamespace, orgClassName);
                context.AddSource($"{orgClassName}.g.cs", compilationUnit);
            }
            catch (Exception ex)
            {
                // 提高容错性，报告生成错误
                var className = orgClassDeclaration != null ? SyntaxHelper.GetClassName(orgClassDeclaration) : "Unknown";
                ReportErrorDiagnostic(context, Diagnostics.EntityMethodGenerationError, className, ex);
            }
        }

        /// <summary>
        /// 根据字段生成属性。
        /// </summary>
        /// <param name="localClass">需要生成的类。</param>
        /// <param name="orgClassDeclaration">原始列。</param>
        /// <returns></returns>
        private (ClassDeclarationSyntax? classDeclaration, bool success) BuildProperty(ClassDeclarationSyntax localClass, ClassDeclarationSyntax orgClassDeclaration)
        {
            //Debugger.Launch();
            // 提高容错性，处理空对象情况
            if (localClass == null || orgClassDeclaration == null)
                return (localClass, false);

            bool success = false;

            // 获取所有已存在的属性名，避免重复生成
            var existingPropertyNames = new HashSet<string>(orgClassDeclaration.Members
                .OfType<PropertyDeclarationSyntax>()
                .Select(p => p.Identifier.Text), StringComparer.OrdinalIgnoreCase);

            foreach (var member in orgClassDeclaration.Members.OfType<FieldDeclarationSyntax>())
            {
                try
                {
                    if (!SyntaxHelper.IsValidPrivateField(member))
                        continue;

                    if (IsIgnoreGenerator(member))
                        continue;

                    // 检查字段对应的属性是否已存在
                    var fieldName = GetFieldName(member);
                    var propertyName = ToPropertyName(fieldName);

                    if (existingPropertyNames.Contains(propertyName))
                    {
                        // 如果属性已存在，跳过生成
                        continue;
                    }

                    var attributeList = GetAttributes(member, FieldAttributes);
                    var attributeListyntax = SyntaxFactory.SeparatedList(attributeList);

                    //生成属性注解。
                    var propertyDeclaration = BuildProperty(member);
                    // 提高容错性，检查生成的属性是否为空
                    if (propertyDeclaration == null)
                        continue;

                    if (attributeListyntax.Any())
                        propertyDeclaration = propertyDeclaration.AddAttributeLists(SyntaxFactory.AttributeList(attributeListyntax));

                    //生成属性注释。
                    var leadingTrivia = member.GetLeadingTrivia();
                    if (leadingTrivia != null)
                    {
                        propertyDeclaration = propertyDeclaration.WithLeadingTrivia(leadingTrivia);
                    }

                    localClass = localClass.AddMembers(propertyDeclaration);
                    success = true;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"生成实体属性时发生错误: {ex.Message}");
                }
            }
            return (localClass, success);
        }

        private MethodDeclarationSyntax GenerateBuildMethod(ClassDeclarationSyntax orgClassDeclaration,
            string orgClassName)
        {
            // 检查是否需要生成建造者模式代码。
            var genBuilderCode = SyntaxHelper.GetAttributeSyntaxes(
                orgClassDeclaration,
                BuilderGenerator.BuilderGeneratorAttributeName);

            if (!genBuilderCode.Any())
                return null;
            var builderClassName = $"{orgClassName}Builder";
            var sb = new StringBuilder();
            sb.AppendLine($"/// <summary>");
            sb.AppendLine($"/// 创建 <see cref=\"{orgClassName}\"/> 类的 <see cref=\"{builderClassName}\"/> 构造者实例。");
            sb.AppendLine($"/// </summary>");
            sb.AppendLine($"public static {builderClassName} Builder()");
            sb.AppendLine("{");
            sb.AppendLine($"    return new {builderClassName}();");
            sb.AppendLine("}");
            return SyntaxHelper.GetMethodDeclarationSyntax(sb);
        }
    }
}