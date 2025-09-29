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
        protected override void GenerateCode(SourceProductionContext context, ClassDeclarationSyntax orgClassDeclaration)
        {
            try
            {
                //Debugger.Launch();
                var genMapMethod = GetClassAttributeValues(orgClassDeclaration, DtoGeneratorAttributeName, DtoGeneratorAttributeGenMapMethod, true);
                if (!genMapMethod)
                    return;

                var cNamespace = GetNamespaceName(orgClassDeclaration);
                _dtoNameSpace = GetDtoNamespaceName(orgClassDeclaration);

                var orgClassName = GetClassName(orgClassDeclaration);
                var voClassName = orgClassName.Replace(EntitySuffix, "") + TransitiveVoGenerator.VoSuffix;

                var localClass = GenLocalClass(orgClassDeclaration, orgClassName, false);

                localClass = GenProperty(localClass, orgClassDeclaration);

                var methodDeclaration = GenMapMethod(orgClassDeclaration, voClassName);
                if (methodDeclaration != null)
                    localClass = localClass.AddMembers(methodDeclaration);

                // 提高容错性，检查生成的类是否为空
                if (localClass == null)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor(
                            "EM001",
                            "实体映射方法生成失败",
                            $"无法为类 {orgClassName} 生成实体映射方法",
                            "代码生成",
                            DiagnosticSeverity.Warning,
                            true),
                        Location.None));
                    return;
                }

                var compilationUnit = GenCompilationUnitSyntax(localClass, cNamespace, orgClassName);
                context.AddSource($"{orgClassName}.g.cs", compilationUnit);
            }
            catch (Exception ex)
            {
                // 提高容错性，报告生成错误
                var className = orgClassDeclaration != null ? GetClassName(orgClassDeclaration) : "Unknown";
                context.ReportDiagnostic(Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "EM002",
                        "实体映射方法生成错误",
                        $"生成类 {className} 的实体映射方法时发生错误: {ex.Message}",
                        "代码生成",
                        DiagnosticSeverity.Error,
                        true),
                    Location.None));
            }
        }

        /// <summary>
        /// 根据字段生成属性。
        /// </summary>
        /// <param name="localClass">需要生成的类。</param>
        /// <param name="orgClassDeclaration">原始列。</param>
        /// <returns></returns>
        private ClassDeclarationSyntax GenProperty(ClassDeclarationSyntax localClass, ClassDeclarationSyntax orgClassDeclaration)
        {
            // 提高容错性，处理空对象情况
            if (localClass == null || orgClassDeclaration == null)
                return localClass;

            foreach (var member in orgClassDeclaration.Members.OfType<FieldDeclarationSyntax>())
            {
                try
                {
                    if (IsIgnoreGenerator(member))
                        continue;

                    var attributeList = GetAttributes(member, FieldAttributes);
                    var attributeListyntax = SyntaxFactory.SeparatedList(attributeList);

                    //生成属性注解。
                    var propertyDeclaration = GeneratorProperty(member);
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
                }
                catch (Exception ex)
                {
                    // 提高容错性，即使单个属性生成失败也不影响其他属性
                    System.Diagnostics.Debug.WriteLine($"生成实体属性时发生错误: {ex.Message}");
                }
            }
            return localClass;
        }

        #region 实体映射至VO对象方法
        private MethodDeclarationSyntax GenMapMethod(ClassDeclarationSyntax orgClassDeclaration, string voClassName)
        {
            // 提高容错性，处理空对象情况
            if (orgClassDeclaration == null)
                return null;

            var sb = GenMapMethodStart(voClassName);
            GenMapMethodBody<PropertyDeclarationSyntax>(orgClassDeclaration, sb);
            GenMapMethodBody<FieldDeclarationSyntax>(orgClassDeclaration, sb);
            var methodDeclaration = GenMapMethodEnd(sb);
            return methodDeclaration;

        }

        private void GenMapMethodBody<T>(ClassDeclarationSyntax orgClassDeclaration, StringBuilder sb)
            where T : MemberDeclarationSyntax
        {
            // 提高容错性，处理空对象情况
            if (orgClassDeclaration == null || sb == null)
                return;

            foreach (var member in orgClassDeclaration.Members.OfType<T>())
            {
                try
                {
                    if (IsIgnoreGenerator(member))
                        continue;

                    var orgPropertyName = "";
                    var propertyName = "";
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
                        continue;
                        
                    propertyName = ToLowerFirstLetter(orgPropertyName);
                    sb.AppendLine($"            voObj.{propertyName}=this.{orgPropertyName};");
                }
                catch (Exception ex)
                {
                    // 提高容错性，即使单个属性生成失败也不影响其他属性
                    System.Diagnostics.Debug.WriteLine($"生成映射方法体时发生错误: {ex.Message}");
                }
            }
        }

        private StringBuilder GenMapMethodStart(string voClassName)
        {
            // 提高容错性，确保参数不为空
            if (string.IsNullOrEmpty(voClassName))
                voClassName = "Object";

            var sb = new StringBuilder();
            sb.AppendLine("class TestProgram{");
            sb.AppendLine("/// <summary>");
            sb.AppendLine("/// 通用的实体映射至VO对象方法。");
            sb.AppendLine("/// </summary>");
            sb.AppendLine($"public virtual {voClassName} MapTo()");
            sb.AppendLine("        {");
            sb.AppendLine($"           var voObj=new {voClassName}();");
            return sb;
        }

        private MethodDeclarationSyntax GenMapMethodEnd(StringBuilder sb)
        {
            // 提高容错性，处理空对象情况
            if (sb == null)
                return null;

            sb.AppendLine("            ConverterUtils.RaiseMapAfter(voObj);");
            sb.AppendLine("            return voObj;");
            sb.AppendLine("        }");
            sb.AppendLine("}");
            return GetMethodDeclarationSyntax(sb);
        }
        #endregion
    }
}