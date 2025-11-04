using Mud.EntityCodeGenerator.Helper;
using System.Text;

namespace Mud.EntityCodeGenerator;

/// <summary>
/// 实体扩展类生成器，用于生成实体与其他DTO类型之间的映射扩展方法。
/// </summary>
[Generator(LanguageNames.CSharp)]
public class EntityMappingGenerator : TransitiveDtoGenerator
{
    /// <summary>
    /// EntityMappingGenerator构造函数
    /// </summary>
    public EntityMappingGenerator() : base()
    {
    }

    /// <inheritdoc/>
    protected override void GenerateCode(SourceProductionContext context, Compilation compilation, ClassDeclarationSyntax orgClassDeclaration)
    {
        //Debugger.Launch();
        var className = orgClassDeclaration != null ? SyntaxHelper.GetClassName(orgClassDeclaration) : "Unknown";

        ErrorHandler.SafeExecute(context, className, () =>
        {
            // 获取原始类名
            var orgClassName = SyntaxHelper.GetClassName(orgClassDeclaration);

            // 检查是否需要生成扩展方法
            var genMapMethod = SyntaxHelper.GetClassAttributeValues(
                orgClassDeclaration,
                DtoGeneratorAttributeName,
                DtoGeneratorAttributeGenMapMethod,
                true);

            if (!genMapMethod)
                return;

            var entityNamespace = GetNamespaceName(orgClassDeclaration);
            var dtoNamespace = GetDtoNamespaceName(orgClassDeclaration);

            // 构建扩展类
            var extensionClassName = $"{orgClassName}Extensions";
            var extensionClass = BuildExtensionClass(orgClassDeclaration, compilation, orgClassName, extensionClassName);

            var compilationUnit = GenCompilationUnitSyntax(extensionClass, entityNamespace, extensionClassName);
            context.AddSource($"{extensionClassName}.g.cs", compilationUnit);
        });
    }


    /// <summary>
    /// 构建扩展类，包含所有映射方法
    /// </summary>
    private ClassDeclarationSyntax BuildExtensionClass(
        ClassDeclarationSyntax orgClassDeclaration,
        Compilation compilation,
        string orgClassName,
        string extensionClassName)
    {
        var extensionClass = BuildLocalClass(orgClassDeclaration, extensionClassName, false)
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                                                  SyntaxFactory.Token(SyntaxKind.StaticKeyword)));

        // 添加MapToEntityFromCrInput方法（从CrInput映射到实体）
        var mapFromCrInputMethod = GenerateMapToEntityFromCrInputMethod(orgClassDeclaration, compilation, orgClassName);
        if (mapFromCrInputMethod != null)
        {
            extensionClass = extensionClass.AddMembers(mapFromCrInputMethod);
        }

        // 添加MapToEntityFromUpInput方法（从UpInput映射到实体）
        var mapFromUpInputMethod = GenerateMapToEntityFromUpInputMethod(orgClassDeclaration, compilation, orgClassName);
        if (mapFromUpInputMethod != null)
        {
            extensionClass = extensionClass.AddMembers(mapFromUpInputMethod);
        }

        // 添加MapToCrInput方法（从实体映射到CrInput）
        var mapToCrInputMethod = GenerateMapToCrInputMethod(orgClassDeclaration, compilation, orgClassName);
        if (mapToCrInputMethod != null)
        {
            extensionClass = extensionClass.AddMembers(mapToCrInputMethod);
        }

        // 添加MapToUpInput方法（从实体映射到UpInput）
        var mapToUpInputMethod = GenerateMapToUpInputMethod(orgClassDeclaration, compilation, orgClassName);
        if (mapToUpInputMethod != null)
        {
            extensionClass = extensionClass.AddMembers(mapToUpInputMethod);
        }

        // 添加MapToListOutput方法（从实体映射到ListOutput/VO）
        var mapToListOutputMethod = GenerateMapToListOutputMethod(orgClassDeclaration, compilation, orgClassName);
        if (mapToListOutputMethod != null)
        {
            extensionClass = extensionClass.AddMembers(mapToListOutputMethod);
        }

        // 添加MapToInfoOutput方法（从实体映射到InfoOutput/VO）
        var mapToInfoOutputMethod = GenerateMapToInfoOutputMethod(orgClassDeclaration, compilation, orgClassName);
        if (mapToInfoOutputMethod != null)
        {
            extensionClass = extensionClass.AddMembers(mapToInfoOutputMethod);
        }

        // 添加MapToListOutputCollection方法（从实体集合映射到ListOutput集合）
        var mapToListOutputCollectionMethod = GenerateMapToListOutputCollectionMethod(orgClassDeclaration, orgClassName);
        if (mapToListOutputCollectionMethod != null)
        {
            extensionClass = extensionClass.AddMembers(mapToListOutputCollectionMethod);
        }

        // 添加MapToInfoOutputCollection方法（从实体集合映射到InfoOutput集合）
        var mapToInfoOutputCollectionMethod = GenerateMapToInfoOutputCollectionMethod(orgClassDeclaration, orgClassName);
        if (mapToInfoOutputCollectionMethod != null)
        {
            extensionClass = extensionClass.AddMembers(mapToInfoOutputCollectionMethod);
        }

        // 添加BuildQueryWhere方法（从QueryInput构建查询条件）
        var buildQueryWhereMethod = GenerateBuildQueryWhereMethod(orgClassDeclaration, orgClassName);
        if (buildQueryWhereMethod != null)
        {
            extensionClass = extensionClass.AddMembers(buildQueryWhereMethod);
        }

        return extensionClass;
    }


    /// <summary>
    /// 生成从CrInput映射到实体的扩展方法
    /// </summary>
    private MethodDeclarationSyntax GenerateMapToEntityFromCrInputMethod(
        ClassDeclarationSyntax orgClassDeclaration,
        Compilation compilation,
        string orgClassName)
    {
        var crInputClassName = GetGeneratorClassName(orgClassDeclaration, ConfigurationManager.Instance.GetClassSuffix("crinput"));
        Debug.WriteLine($"CRINPUT_CLASS_NAME: {crInputClassName}");

        // 添加命名空间前缀
        var fullCrInputClassName = $"{crInputClassName}";

        // 使用MappingGenerator生成映射方法
        var mappingLines = GenerateMappingLines<PropertyDeclarationSyntax>(
            orgClassDeclaration, compilation,
            (orgPropertyName, propertyName) => MappingGenerator.GeneratePropertyMappingLine(propertyName, orgPropertyName, "dto", "result"),
            false); // 只处理非主键属性

        var methodCode = MappingGenerator.GenerateDtoToEntityMapping(
            fullCrInputClassName,
            orgClassName,
            mappingLines,
            "MapToEntity");

        return SyntaxHelper.GetMethodDeclarationSyntax(methodCode);
    }

    /// <summary>
    /// 生成从UpInput映射到实体的扩展方法
    /// </summary>
    private MethodDeclarationSyntax GenerateMapToEntityFromUpInputMethod(
        ClassDeclarationSyntax orgClassDeclaration,
        Compilation compilation,
        string orgClassName)
    {
        var upInputClassName = GetGeneratorClassName(orgClassDeclaration, ConfigurationManager.Instance.GetClassSuffix("upinput"));
        System.Diagnostics.Debug.WriteLine($"UPINPUT_CLASS_NAME: {upInputClassName}");

        // 添加命名空间前缀
        var fullUpInputClassName = $"{upInputClassName}";

        // 使用MappingGenerator生成映射方法
        var mappingLines = GenerateMappingLines<PropertyDeclarationSyntax>(
            orgClassDeclaration, compilation,
            (orgPropertyName, propertyName) => MappingGenerator.GeneratePropertyMappingLine(propertyName, orgPropertyName, "dto", "result"),
            null); // 处理所有属性

        var methodCode = MappingGenerator.GenerateDtoToEntityMapping(
            fullUpInputClassName,
            orgClassName,
            mappingLines,
            "MapToEntity");

        return SyntaxHelper.GetMethodDeclarationSyntax(methodCode);
    }

    /// <summary>
    /// 生成BuildQueryWhere方法，用于构建查询条件
    /// </summary>
    private MethodDeclarationSyntax GenerateBuildQueryWhereMethod(
        ClassDeclarationSyntax orgClassDeclaration,
        string orgClassName)
    {
        var queryInputClassName = GetGeneratorClassName(orgClassDeclaration, ConfigurationManager.Instance.GetClassSuffix("queryinput"));
        System.Diagnostics.Debug.WriteLine($"QUERYINPUT_CLASS_NAME: {queryInputClassName}");

        // 添加命名空间前缀
        var fullQueryInputClassName = $"{queryInputClassName}";

        var sb = new StringBuilder();

        sb.AppendLine($"/// <summary>");
        sb.AppendLine($"/// 根据 <see cref=\"{queryInputClassName}\"/> 构建查询条件表达式。");
        sb.AppendLine($"/// </summary>");
        sb.AppendLine($"/// <param name=\"input\">输入的 <see cref=\"{queryInputClassName}\"/> 实例。</param>");
        sb.AppendLine($"/// <returns>查询条件表达式。</returns>");
        sb.AppendLine($"public static Expression<Func<{orgClassName}, bool>> BuildQueryWhere(this {fullQueryInputClassName} input)");
        sb.AppendLine("{");
        sb.AppendLine($"    if(input==null) return x => true;");
        sb.AppendLine($"    Expression<Func<{orgClassName}, bool>> where = x => true;");

        // 生成查询条件
        GenerateQueryConditions<PropertyDeclarationSyntax>(orgClassDeclaration, sb);

        sb.AppendLine("    return where;");
        sb.AppendLine("}");

        return SyntaxHelper.GetMethodDeclarationSyntax(sb);
    }

    /// <summary>
    /// 生成从实体映射到CrInput的扩展方法
    /// </summary>
    private MethodDeclarationSyntax GenerateMapToCrInputMethod(
        ClassDeclarationSyntax orgClassDeclaration,
        Compilation compilation,
        string orgClassName)
    {
        var crInputClassName = GetGeneratorClassName(orgClassDeclaration, ConfigurationManager.Instance.GetClassSuffix("crinput"));
        Debug.WriteLine($"CRINPUT_CLASS_NAME: {crInputClassName}");

        // 添加命名空间前缀
        var fullCrInputClassName = $"{crInputClassName}";

        // 使用MappingGenerator生成映射方法
        var mappingLines = GenerateMappingLines<PropertyDeclarationSyntax>(
            orgClassDeclaration, compilation,
            (orgPropertyName, propertyName) => MappingGenerator.GeneratePropertyMappingLine(orgPropertyName, propertyName, "entity", "result"),
            false); // 只处理非主键属性

        var methodCode = MappingGenerator.GenerateEntityToDtoMapping(
            orgClassName,
            fullCrInputClassName,
            mappingLines,
            "MapToCrInput");

        return SyntaxHelper.GetMethodDeclarationSyntax(methodCode);
    }

    /// <summary>
    /// 生成从实体映射到UpInput的扩展方法
    /// </summary>
    private MethodDeclarationSyntax GenerateMapToUpInputMethod(
         ClassDeclarationSyntax orgClassDeclaration,
         Compilation compilation,
         string orgClassName)
    {
        var upInputClassName = GetGeneratorClassName(orgClassDeclaration, ConfigurationManager.Instance.GetClassSuffix("upinput"));
        System.Diagnostics.Debug.WriteLine($"UPINPUT_CLASS_NAME: {upInputClassName}");

        // 添加命名空间前缀
        var fullUpInputClassName = $"{upInputClassName}";

        // 使用MappingGenerator生成映射方法
        var mappingLines = GenerateMappingLines<PropertyDeclarationSyntax>(
            orgClassDeclaration, compilation,
            (orgPropertyName, propertyName) => MappingGenerator.GeneratePropertyMappingLine(orgPropertyName, propertyName, "entity", "result"),
            null); // 处理所有属性

        var methodCode = MappingGenerator.GenerateEntityToDtoMapping(
            orgClassName,
            fullUpInputClassName,
            mappingLines,
            "MapToUpInput");

        return SyntaxHelper.GetMethodDeclarationSyntax(methodCode);
    }

    /// <summary>
    /// 生成从实体映射到ListOutput(VO)的扩展方法
    /// </summary>
    private MethodDeclarationSyntax GenerateMapToListOutputMethod(
        ClassDeclarationSyntax orgClassDeclaration,
        Compilation compilation,
        string orgClassName)
    {
        var voClassName = GetGeneratorClassName(orgClassDeclaration, ConfigurationManager.Instance.GetClassSuffix("vo"));
        System.Diagnostics.Debug.WriteLine($"VO_CLASS_NAME: {voClassName}");

        // 添加命名空间前缀
        var fullVoClassName = $"{voClassName}";

        // 使用MappingGenerator生成映射方法
        var mappingLines = GenerateMappingLines<PropertyDeclarationSyntax>(orgClassDeclaration, compilation,
            (orgPropertyName, propertyName) => MappingGenerator.GeneratePropertyMappingLine(orgPropertyName, propertyName, "entity", "result"));

        var methodCode = MappingGenerator.GenerateEntityToDtoMapping(
            orgClassName,
            fullVoClassName,
            mappingLines,
            "MapToListOutput");

        return SyntaxHelper.GetMethodDeclarationSyntax(methodCode);
    }

    /// <summary>
    /// 生成从实体映射到InfoOutput(VO)的扩展方法
    /// </summary>
    private MethodDeclarationSyntax GenerateMapToInfoOutputMethod(
         ClassDeclarationSyntax orgClassDeclaration,
         Compilation compilation,
         string orgClassName)
    {
        var voClassName = GetGeneratorClassName(orgClassDeclaration, ConfigurationManager.Instance.GetClassSuffix("infooutput"));
        System.Diagnostics.Debug.WriteLine($"VO_CLASS_NAME: {voClassName}");

        // 添加命名空间前缀
        var fullVoClassName = $"{voClassName}";

        // 使用MappingGenerator生成映射方法
        var mappingLines = GenerateMappingLines<PropertyDeclarationSyntax>(orgClassDeclaration, compilation,
            (orgPropertyName, propertyName) => MappingGenerator.GeneratePropertyMappingLine(orgPropertyName, propertyName, "entity", "result"));

        var methodCode = MappingGenerator.GenerateEntityToDtoMapping(
            orgClassName,
            fullVoClassName,
            mappingLines,
            "MapToInfoOutput");

        return SyntaxHelper.GetMethodDeclarationSyntax(methodCode);
    }

    /// <summary>
    /// 生成从实体集合映射到ListOutput集合的扩展方法
    /// </summary>
    private MethodDeclarationSyntax GenerateMapToListOutputCollectionMethod(
        ClassDeclarationSyntax orgClassDeclaration,
        string orgClassName)
    {
        var voClassName = GetGeneratorClassName(orgClassDeclaration, ConfigurationManager.Instance.GetClassSuffix("vo"));
        System.Diagnostics.Debug.WriteLine($"VO_CLASS_NAME: {voClassName}");

        // 添加命名空间前缀
        var fullVoClassName = $"{voClassName}";

        // 使用MappingGenerator生成集合映射方法
        var methodCode = MappingGenerator.GenerateCollectionMapping(
            orgClassName,
            fullVoClassName,
            "MapToListOutput",
            "MapToList");

        return SyntaxHelper.GetMethodDeclarationSyntax(methodCode);
    }

    /// <summary>
    /// 生成从实体集合映射到InfoOutput集合的扩展方法
    /// </summary>
    private MethodDeclarationSyntax GenerateMapToInfoOutputCollectionMethod(
        ClassDeclarationSyntax orgClassDeclaration,
        string orgClassName)
    {
        var voClassName = GetGeneratorClassName(orgClassDeclaration, ConfigurationManager.Instance.GetClassSuffix("infooutput"));

        // 添加命名空间前缀
        var fullVoClassName = $"{voClassName}";

        // 使用MappingGenerator生成集合映射方法
        var methodCode = MappingGenerator.GenerateCollectionMapping(
            orgClassName,
            fullVoClassName,
            "MapToInfoOutput",
            "MapToInfoList");

        return SyntaxHelper.GetMethodDeclarationSyntax(methodCode);
    }

    /// <summary>
    /// 生成映射行列表
    /// </summary>
    private List<string> GenerateMappingLines<T>(
        ClassDeclarationSyntax orgClassDeclaration,
        Compilation compilation,
        Func<string, string, string> generateMappingLine,
        bool? primaryKeyOnly = null) where T : MemberDeclarationSyntax
    {
        var mappingLines = new List<string>();

        ProcessMembers<T>(orgClassDeclaration, compilation, (member, orgPropertyName, propertyName) =>
        {
            var mappingLine = generateMappingLine(orgPropertyName, propertyName);
            if (!string.IsNullOrEmpty(mappingLine))
            {
                mappingLines.Add(mappingLine);
            }
        }, primaryKeyOnly);

        return mappingLines;
    }

    /// <summary>
    /// 生成查询条件代码
    /// </summary>
    private void GenerateQueryConditions<T>(ClassDeclarationSyntax orgClassDeclaration, StringBuilder sb)
        where T : MemberDeclarationSyntax
    {
        ProcessMembers<T>(orgClassDeclaration, null, (member, orgPropertyName, propertyName) =>
        {
            var isLikeQuery = IsLikeGenerator(member);
            var propertyType = GetPropertyType(member);

            if (string.IsNullOrEmpty(propertyType))
                propertyType = "object";

            // 实体属性名保持原始大写形式，查询输入参数名使用小写形式
            var entityPropertyName = orgPropertyName;

            if (propertyType.StartsWith("string", StringComparison.OrdinalIgnoreCase))
            {
                sb.AppendLine($"    if (!string.IsNullOrEmpty(input.{propertyName}?.Trim()))");
                if (!isLikeQuery)
                    sb.AppendLine($"        where = where.And(x => x.{entityPropertyName} == input.{propertyName}.Trim());");
                else
                    sb.AppendLine($"        where = where.And(x => x.{entityPropertyName}.Contains(input.{propertyName}.Trim()));");
            }
            else
            {
                sb.AppendLine($"    if (input.{propertyName} != null)");
                sb.AppendLine($"        where = where.And(x => x.{entityPropertyName} == input.{propertyName});");
            }
        });
    }
}