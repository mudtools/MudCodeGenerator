using Mud.EntityCodeGenerator.Helper;
using System.Collections.ObjectModel;
using System.Text;

namespace Mud.EntityCodeGenerator
{
    /// <summary>
    /// 生成实体映射方法
    /// </summary>
    [Generator(LanguageNames.CSharp)]
    public class EntityMethodGenerator : TransitiveDtoGenerator
    {
        private string _dtoNameSpace = "";

        private string[] FieldAttributes = ["TableField", "Column", "Key"];

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

                var cNamespace = GetNamespaceName(orgClassDeclaration);
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
                    ReportFailureDiagnostic(context, DiagnosticDescriptors.EntityMethodGenerationFailure, orgClassName);
                    return;
                }

                var compilationUnit = GenCompilationUnitSyntax(localClass, cNamespace, orgClassName);
                context.AddSource($"{orgClassName}.g.cs", compilationUnit);
            }
            catch (Exception ex)
            {
                // 提高容错性，报告生成错误
                var className = orgClassDeclaration != null ? SyntaxHelper.GetClassName(orgClassDeclaration) : "Unknown";
                ReportErrorDiagnostic(context, DiagnosticDescriptors.EntityMethodGenerationError, className, ex);
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