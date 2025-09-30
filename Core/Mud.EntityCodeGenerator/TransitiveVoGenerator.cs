using Mud.CodeGenerator;
using Mud.EntityCodeGenerator.Diagnostics;

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

    private readonly string[] ViewPropertyConvertAttributes = new[] { "DictFormat", "Translation", "Sensitive" };

    /// <inheritdoc/>
    protected override string[] GetPropertyAttributes()
    {
        return (new[] { "DictFormat", "Translation", "Sensitive" }).Concat(new[] { "ExportProperty" }).ToArray();
    }

    /// <inheritdoc/>
    protected override void GenerateCode(SourceProductionContext context, ClassDeclarationSyntax orgClassDeclaration)
    {
        try
        {
            //Debugger.Launch();
            var genVoClass = SyntaxHelper.GetClassAttributeValues<bool>(orgClassDeclaration, DtoGeneratorAttributeName, DtoGeneratorAttributeGenVoClass, true);
            if (!genVoClass)
                return;

            var (localClass, dtoNameSpace, dtoClassName) = BuildLocalClass(orgClassDeclaration);
            localClass = BuildLocalClassProperty<PropertyDeclarationSyntax>(orgClassDeclaration, localClass,
                                                                m => GenAttributeFunc(m),
                                                                m => GenExtAttributeFunc(m));
            localClass = BuildLocalClassProperty<FieldDeclarationSyntax>(orgClassDeclaration, localClass,
                                                               m => GenAttributeFunc(m),
                                                               m => GenExtAttributeFunc(m));

            // 提高容错性，检查生成的类是否为空
            if (localClass == null)
            {
                var className = SyntaxHelper.GetClassName(orgClassDeclaration);
                ReportFailureDiagnostic(context, DiagnosticDescriptors.VoGenerationFailure, className);
                return;
            }

            var compilationUnit = GenCompilationUnitSyntax(localClass, dtoNameSpace, dtoClassName);
            context.AddSource($"{dtoClassName}.g.cs", compilationUnit);
        }
        catch (Exception ex)
        {
            // 提高容错性，报告生成错误
            var className = SyntaxHelper.GetClassName(orgClassDeclaration);
            ReportErrorDiagnostic(context, DiagnosticDescriptors.VoGenerationError, className, ex);
        }
    }

    /// <summary>
    /// 生成属性。
    /// </summary>
    /// <param name="member"></param>
    /// <returns></returns>
    private PropertyDeclarationSyntax? GenAttributeFunc(PropertyDeclarationSyntax member)
    {
        if (IsIgnoreGenerator(member))
            return null;
        return BuildProperty(member);
    }

    /// <summary>
    /// 生成属性。
    /// </summary>
    /// <param name="member"></param>
    /// <returns></returns>
    private PropertyDeclarationSyntax? GenAttributeFunc(FieldDeclarationSyntax member)
    {
        if (IsIgnoreGenerator(member))
            return null;
        return BuildProperty(member, false);
    }

    /// <summary>
    /// 生成扩展属性。
    /// </summary>
    /// <param name="member"></param>
    /// <returns></returns>
    private PropertyDeclarationSyntax? GenExtAttributeFunc(PropertyDeclarationSyntax member)
    {
        if (IsIgnoreGenerator(member))
            return null;
        var viewAttributes = GetAttributes(member, new[] { ViewPropertyAttribute });
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
        var argsList = SyntaxFactory.SeparatedList(new[] { linkPropertyName });
        //加入转换类型属性
        if (convertType != null)
            argsList = argsList.Add(convertType);

        //生成新的属性
        var nameSyntax = SyntaxFactory.ParseName(PropertyValueConvertAttributeName);
        var attributeSyntax = SyntaxFactory.Attribute(nameSyntax);
        var argumentList = SyntaxFactory.AttributeArgumentList(argsList);
        attributeSyntax = attributeSyntax.WithArgumentList(argumentList);
        //新的属性
        var propertyAttributes = SyntaxFactory.SeparatedList(new[] { attributeSyntax });

        //获取原属性上需要迁移到新属性上特性。
        var attributeList = GetAttributes(member, ViewPropertyConvertAttributes);
        if (attributeList.Any())
        {
            var sttributes = SyntaxFactory.SeparatedList(attributeList);
            propertyAttributes = propertyAttributes.AddRange(sttributes);
        }
        var attributeListSyntax = SyntaxFactory.AttributeList(propertyAttributes);

        var property = BuildProperty(extPropertyName, "string");
        property = property.AddAttributeLists(attributeListSyntax);
        return property;
    }

    /// <summary>
    /// 生成扩展属性。
    /// </summary>
    /// <param name="member"></param>
    /// <returns></returns>
    private PropertyDeclarationSyntax? GenExtAttributeFunc(FieldDeclarationSyntax member)
    {
        if (IsIgnoreGenerator(member))
            return null;

        var viewAttributes = GetAttributes(member, new[] { ViewPropertyAttribute });
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
        var argsList = SyntaxFactory.SeparatedList([linkPropertyName]);
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

        var property = BuildProperty(extPropertyName, "string");
        property = property.AddAttributeLists(attributeListSyntax);
        return property;
    }

    private AttributeArgumentSyntax? GetConvertPropety(AttributeSyntax attributeSyntax)
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
                var propertyValue = AttributeSyntaxHelper.ExtractValueFromSyntax(item.Expression);
                return propertyValue != null ? ToLowerFirstLetter(propertyValue.ToString()) : defaultName;
            }
        }
        return defaultName;

    }
}