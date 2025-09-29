namespace Mud.EntityCodeGenerator;

/// <summary>
/// 生成业务数据VO类。
/// </summary>
[Generator(LanguageNames.CSharp)]
public class TransitiveVoGenerator : TransitiveDtoGenerator
{
    internal const string VoSuffix = "ListOutput";

    /// <inheritdoc/>
    protected override string ClassSuffix => VoSuffix;

    private const string ViewPropertyAttribute = "PropertyTranslation";

    private const string PropertyValueConvertAttributeName = "PropertyValueConvertAttribute";

    private readonly string[] ViewPropertyConvertAttributes = ["DictFormat", "Translation", "Sensitive"];

    /// <inheritdoc/>
    protected override string[] GetPropertyAttributes()
    {
        return [.. ViewPropertyConvertAttributes, "ExportProperty"];
    }

    /// <inheritdoc/>
    protected override void GenerateCode(SourceProductionContext context, ClassDeclarationSyntax orgClassDeclaration)
    {
        try
        {
            //Debugger.Launch();
            var genVoClass = GetClassAttributeValues<bool>(orgClassDeclaration, DtoGeneratorAttributeName, DtoGeneratorAttributeGenVoClass, true);
            if (!genVoClass)
                return;

            var (localClass, dtoNameSpace, dtoClassName) = GenLocalClass(orgClassDeclaration);
            localClass = GenLocalClassProperty<PropertyDeclarationSyntax>(orgClassDeclaration, localClass,
                                                                m => GenAttributeFunc(m),
                                                                m => GenExtAttributeFunc(m));
            localClass = GenLocalClassProperty<FieldDeclarationSyntax>(orgClassDeclaration, localClass,
                                                               m => GenAttributeFunc(m),
                                                               m => GenExtAttributeFunc(m));
            
            // 提高容错性，检查生成的类是否为空
            if (localClass == null)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "VO001",
                        "VO类生成失败",
                        $"无法为类 {GetClassName(orgClassDeclaration)} 生成VO类",
                        "代码生成",
                        DiagnosticSeverity.Warning,
                        true),
                    Location.None));
                return;
            }

            var compilationUnit = GenCompilationUnitSyntax(localClass, dtoNameSpace, dtoClassName);
            context.AddSource($"{dtoClassName}.g.cs", compilationUnit);
        }
        catch (Exception ex)
        {
            // 提高容错性，报告生成错误
            context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor(
                    "VO002",
                    "VO类生成错误",
                    $"生成类 {GetClassName(orgClassDeclaration)} 的VO类时发生错误: {ex.Message}",
                    "代码生成",
                    DiagnosticSeverity.Error,
                    true),
                Location.None));
        }
    }

    /// <summary>
    /// 生成属性。
    /// </summary>
    /// <param name="member"></param>
    /// <returns></returns>
    private PropertyDeclarationSyntax GenAttributeFunc(PropertyDeclarationSyntax member)
    {
        if (IsIgnoreGenerator(member))
            return null;
        return GeneratorProperty(member);
    }

    /// <summary>
    /// 生成属性。
    /// </summary>
    /// <param name="member"></param>
    /// <returns></returns>
    private PropertyDeclarationSyntax GenAttributeFunc(FieldDeclarationSyntax member)
    {
        if (IsIgnoreGenerator(member))
            return null;
        return GeneratorProperty(member, false);
    }

    /// <summary>
    /// 生成扩展属性。
    /// </summary>
    /// <param name="member"></param>
    /// <returns></returns>
    private PropertyDeclarationSyntax GenExtAttributeFunc(PropertyDeclarationSyntax member)
    {
        if (IsIgnoreGenerator(member))
            return null;

        var viewAttributes = GetAttributes(member, [ViewPropertyAttribute]);
        if (!viewAttributes.Any())
            return null;

        var propertyName = GetFirstLowerPropertyName(member);
        var viewAttribute = viewAttributes[0];
        var extPropertyName = GetGenPropertyName(viewAttribute, propertyName + "Str");
        var convertType = GetConvertPropety(viewAttribute);
        //加入映射到属性
        var linkPropertyName = SyntaxFactory.AttributeArgument(
                    SyntaxFactory.NameEquals("MapperFrom"),
                    null,
                    SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression,
                        SyntaxFactory.Literal(propertyName)));
        var argsList = SyntaxFactory.SeparatedList(
            [
                linkPropertyName,
            ]);
        //加入转换类型属性
        if (convertType != null)
            argsList = argsList.Add(convertType);

        //生成新的属性
        var nameSyntax = SyntaxFactory.ParseName(PropertyValueConvertAttributeName);
        var attributeSyntax = SyntaxFactory.Attribute(nameSyntax);
        var argumentList = SyntaxFactory.AttributeArgumentList(argsList);
        attributeSyntax = attributeSyntax.WithArgumentList(argumentList);
        //新的属性
        var propertyAttributes = SyntaxFactory.SeparatedList([attributeSyntax]);

        //获取原属性上需要迁移到新属性上特性。
        var attributeList = GetAttributes(member, ViewPropertyConvertAttributes);
        if (attributeList.Any())
        {
            var sttributes = SyntaxFactory.SeparatedList(attributeList);
            propertyAttributes = propertyAttributes.AddRange(sttributes);
        }
        var attributeListSyntax = SyntaxFactory.AttributeList(propertyAttributes);

        var property = GeneratorProperty(extPropertyName, "string");
        property = property.AddAttributeLists(attributeListSyntax);
        return property;
    }

    /// <summary>
    /// 生成扩展属性。
    /// </summary>
    /// <param name="member"></param>
    /// <returns></returns>
    private PropertyDeclarationSyntax GenExtAttributeFunc(FieldDeclarationSyntax member)
    {
        if (IsIgnoreGenerator(member))
            return null;

        var viewAttributes = GetAttributes(member, [ViewPropertyAttribute]);
        if (!viewAttributes.Any())
            return null;

        var propertyName = GetFirstUpperPropertyName(member);
        var viewAttribute = viewAttributes[0];
        var extPropertyName = GetGenPropertyName(viewAttribute, propertyName + "Str");
        var convertType = GetConvertPropety(viewAttribute);
        //加入映射到属性
        var linkPropertyName = SyntaxFactory.AttributeArgument(
                    SyntaxFactory.NameEquals("MapperFrom"),
                    null,
                    SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression,
                        SyntaxFactory.Literal(propertyName)));
        var argsList = SyntaxFactory.SeparatedList(
            [
                linkPropertyName,
            ]);
        //加入转换类型属性
        if (convertType != null)
            argsList = argsList.Add(convertType);

        //生成新的属性
        var nameSyntax = SyntaxFactory.ParseName(PropertyValueConvertAttributeName);
        var attributeSyntax = SyntaxFactory.Attribute(nameSyntax);
        var argumentList = SyntaxFactory.AttributeArgumentList(argsList);
        attributeSyntax = attributeSyntax.WithArgumentList(argumentList);
        //新的属性
        var propertyAttributes = SyntaxFactory.SeparatedList([attributeSyntax]);

        //获取原属性上需要迁移到新属性上特性。
        var attributeList = GetAttributes(member, ViewPropertyConvertAttributes);
        if (attributeList.Any())
        {
            var sttributes = SyntaxFactory.SeparatedList(attributeList);
            propertyAttributes = propertyAttributes.AddRange(sttributes);
        }
        var attributeListSyntax = SyntaxFactory.AttributeList(propertyAttributes);

        var property = GeneratorProperty(extPropertyName, "string");
        property = property.AddAttributeLists(attributeListSyntax);
        return property;
    }

    private AttributeArgumentSyntax GetConvertPropety(AttributeSyntax attributeSyntax)
    {
        // 提高容错性，处理空对象情况
        if (attributeSyntax?.ArgumentList == null)
            return null;
            
        if (!attributeSyntax.ArgumentList.Arguments.Any())
            return null;

        foreach (var item in attributeSyntax.ArgumentList.Arguments)
        {
            // 提高容错性，检查NameEquals和Identifier
            if (item.NameEquals?.Name?.Identifier.Text?.Equals("convertertype", StringComparison.OrdinalIgnoreCase) == true)
            {
                return item;
            }
        }
        return null;
    }

    private string GetGenPropertyName(AttributeSyntax attributeSyntax, string defaultName)
    {
        // 提高容错性，处理空对象情况
        if (attributeSyntax?.ArgumentList == null)
            return defaultName;
            
        if (!attributeSyntax.ArgumentList.Arguments.Any())
            return defaultName;
            
        foreach (var item in attributeSyntax.ArgumentList.Arguments)
        {
            // 提高容错性，检查NameEquals和Identifier
            if (item.NameEquals?.Name?.Identifier.Text?.Equals("propertyname", StringComparison.OrdinalIgnoreCase) == true)
            {
                var propertyValue = ParseAttributeValue(item.Expression);
                return propertyValue != null ? ToLowerFirstLetter(propertyValue.ToString()) : defaultName;
            }
        }
        return defaultName;

    }
}