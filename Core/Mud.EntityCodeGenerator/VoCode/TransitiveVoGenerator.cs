using Mud.EntityCodeGenerator.Diagnostics;
using Mud.EntityCodeGenerator.Helper;

namespace Mud.EntityCodeGenerator;

/// <summary>
/// 生成业务数据VO类。
/// </summary>
public class TransitiveVoGenerator : BaseDtoGenerator
{
    /// <inheritdoc/>
    protected override string GetConfiguredClassSuffix()
    {
        // 根据当前类名确定后缀
        var currentClassName = this.GetType().Name;
        return currentClassName switch
        {
            nameof(TransitiveListOutputGenerator) => ConfigurationManager.Instance.GetClassSuffix("vo"),
            nameof(TransitiveInfoOutputGenerator) => ConfigurationManager.Instance.GetClassSuffix("infooutput"),
            _ => ConfigurationManager.Instance.GetClassSuffix("vo")
        };
    }

    private const string ViewPropertyAttribute = "PropertyTranslation";

    private const string PropertyValueConvertAttributeName = "PropertyValueConvertAttribute";

    private readonly string[] ViewPropertyConvertAttributes = ["DictFormat", "Translation", "Sensitive"];

    /// <inheritdoc/>
    protected override string[] GetPropertyAttributes()
    {
        var defaultAttributes = new[] { "DictFormat", "Translation", "Sensitive" };
        var extraAttributes = GetVoPropertyAttributes();

        if (extraAttributes != null && extraAttributes.Length > 0)
        {
            // 合并默认属性和额外属性，并去重
            return defaultAttributes.Concat(extraAttributes).Concat(["ExportProperty"])
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        return defaultAttributes.Concat(["ExportProperty"]).ToArray();
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
        // 为扩展属性添加换行注释
        var extInheritdoc = SyntaxFactory.ParseLeadingTrivia($"\n\n///<inheritdoc cref=\"{propertyName}\"/>\n");
        property = property.WithLeadingTrivia(extInheritdoc);
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
        // 为扩展属性添加换行注释
        var extInheritdoc = SyntaxFactory.ParseLeadingTrivia($"\n\n///<inheritdoc cref=\"{propertyName}\"/>\n");
        property = property.WithLeadingTrivia(extInheritdoc);
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

    /// <inheritdoc/>
    public override string GeneratorName => "VO Generator";

    /// <inheritdoc/>
    public override bool ShouldGenerate(ClassDeclarationSyntax classDeclaration)
    {
        return SyntaxHelper.GetClassAttributeValues<bool>(classDeclaration, DtoGeneratorAttributeName, DtoGeneratorAttributeGenVoClass, true);
    }

    /// <inheritdoc/>
    public override string GetGeneratedClassName(ClassDeclarationSyntax classDeclaration)
    {
        return GetGeneratorClassName(classDeclaration);
    }

    /// <inheritdoc/>
    public override string GetGeneratedNamespace(ClassDeclarationSyntax classDeclaration)
    {
        return GetDtoNamespaceName(classDeclaration);
    }

    /// <inheritdoc/>
    protected override string GetGeneratorPropertyName() => DtoGeneratorAttributeGenVoClass;

    /// <inheritdoc/>
    protected override Func<PropertyDeclarationSyntax, PropertyDeclarationSyntax> CreatePropertyGenerator()
    {
        return m => GenAttributeFunc(m);
    }

    /// <inheritdoc/>
    protected override Func<FieldDeclarationSyntax, PropertyDeclarationSyntax> CreateFieldGenerator()
    {
        return m => GenAttributeFunc(m);
    }

    /// <inheritdoc/>
    protected override Func<PropertyDeclarationSyntax, PropertyDeclarationSyntax> CreateSafePropertyGenerator()
    {
        var propertyGenerator = CreatePropertyGenerator();
        return ErrorHandler.CreateSafePropertyGenerator(this, propertyGenerator);
    }

    /// <inheritdoc/>
    protected override Func<FieldDeclarationSyntax, PropertyDeclarationSyntax> CreateSafeFieldGenerator()
    {
        var fieldGenerator = CreateFieldGenerator();
        return ErrorHandler.CreateSafePropertyGenerator(this, fieldGenerator);
    }

    /// <inheritdoc/>
    protected override Microsoft.CodeAnalysis.DiagnosticDescriptor GetFailureDescriptor()
    {
        return DiagnosticDescriptors.VoGenerationFailure;
    }

    /// <inheritdoc/>
    protected override Microsoft.CodeAnalysis.DiagnosticDescriptor GetErrorDescriptor()
    {
        return DiagnosticDescriptors.VoGenerationError;
    }
}