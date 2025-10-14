using Mud.EntityCodeGenerator.Diagnostics;
using System.Text;

namespace Mud.EntityCodeGenerator;

/// <summary>
/// 实体扩展类生成器，用于生成实体与其他DTO类型之间的映射扩展方法。
/// </summary>
[Generator(LanguageNames.CSharp)]
public class EntityExtensionsGenerator : TransitiveDtoGenerator
{
    /// <summary>
    /// EntityExtensionsGenerator构造函数
    /// </summary>
    public EntityExtensionsGenerator() : base()
    {
    }

    /// <inheritdoc/>
    protected override void GenerateCode(SourceProductionContext context, Compilation compilation, ClassDeclarationSyntax orgClassDeclaration)
    {
        try
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
        var crInputClassName = GetGeneratorClassName(orgClassDeclaration, TransitiveCrInputGenerator.Suffix);
        Debug.WriteLine($"CRINPUT_CLASS_NAME: {crInputClassName}");

        // 添加命名空间前缀
        var fullCrInputClassName = $"{crInputClassName}";

        var sb = new StringBuilder();

        sb.AppendLine($"/// <summary>");
        sb.AppendLine($"/// 将 <see cref=\"{crInputClassName}\"/> 映射到 <see cref=\"{orgClassName}\"/> 实例。");
        sb.AppendLine($"/// </summary>");
        sb.AppendLine($"/// <param name=\"input\">输入的 <see cref=\"{crInputClassName}\"/> 实例。</param>");
        sb.AppendLine($"/// <param name=\"action\">映射后对实体执行的操作。</param>");
        sb.AppendLine($"/// <returns>映射后的 <see cref=\"{orgClassName}\"/> 实例。</returns>");
        sb.AppendLine($"public static {orgClassName} MapToEntity(this {fullCrInputClassName} input, Action<{orgClassName}>? action = null)");
        sb.AppendLine("{");
        sb.AppendLine($"    if(input==null)return null;");
        sb.AppendLine($"    var entity = new {orgClassName}();");

        // 生成属性映射
        GeneratePropertyMappings<PropertyDeclarationSyntax>(
            orgClassDeclaration,
            sb, compilation,
            (orgPropertyName, propertyName) => $"    entity.{orgPropertyName} = input.{propertyName};",
            false); // 只处理非主键属性

        GeneratePropertyMappings<FieldDeclarationSyntax>(
            orgClassDeclaration,
            sb, compilation,
            (orgPropertyName, propertyName) => $"    entity.{orgPropertyName} = input.{propertyName};",
            false); // 只处理非主键属性

        sb.AppendLine($"    if(action!=null)");
        sb.AppendLine($"        action(entity);");
        sb.AppendLine("    return entity;");
        sb.AppendLine("}");

        return SyntaxHelper.GetMethodDeclarationSyntax(sb);
    }

    /// <summary>
    /// 生成从UpInput映射到实体的扩展方法
    /// </summary>
    private MethodDeclarationSyntax GenerateMapToEntityFromUpInputMethod(
        ClassDeclarationSyntax orgClassDeclaration,
        Compilation compilation,
        string orgClassName)
    {
        var upInputClassName = GetGeneratorClassName(orgClassDeclaration, "UpInput");
        System.Diagnostics.Debug.WriteLine($"UPINPUT_CLASS_NAME: {upInputClassName}");

        // 添加命名空间前缀
        var fullUpInputClassName = $"{upInputClassName}";

        var sb = new StringBuilder();

        sb.AppendLine($"/// <summary>");
        sb.AppendLine($"/// 将 <see cref=\"{upInputClassName}\"/> 映射到 <see cref=\"{orgClassName}\"/> 实例。");
        sb.AppendLine($"/// </summary>");
        sb.AppendLine($"/// <param name=\"input\">输入的 <see cref=\"{upInputClassName}\"/> 实例。</param>");
        sb.AppendLine($"/// <param name=\"action\">映射后对实体执行的操作。</param>");
        sb.AppendLine($"/// <returns>映射后的 <see cref=\"{orgClassName}\"/> 实例。</returns>");
        sb.AppendLine($"public static {orgClassName} MapToEntity(this {fullUpInputClassName} input, Action<{orgClassName}>? action = null)");
        sb.AppendLine("{");
        sb.AppendLine($"    if(input==null)return null;");
        sb.AppendLine($"    var entity = new {orgClassName}();");

        // 生成属性映射（所有属性）
        GeneratePropertyMappings<PropertyDeclarationSyntax>(
            orgClassDeclaration,
            sb, compilation,
            (orgPropertyName, propertyName) => $"    entity.{orgPropertyName} = input.{propertyName};",
            null); // 处理所有属性

        GeneratePropertyMappings<FieldDeclarationSyntax>(
            orgClassDeclaration,
            sb, compilation,
            (orgPropertyName, propertyName) => $"    entity.{orgPropertyName} = input.{propertyName};",
            null); // 处理所有属性

        sb.AppendLine($"    if(action!=null)");
        sb.AppendLine($"        action(entity);");
        sb.AppendLine("    return entity;");
        sb.AppendLine("}");

        return SyntaxHelper.GetMethodDeclarationSyntax(sb);
    }

    /// <summary>
    /// 生成BuildQueryWhere方法，用于构建查询条件
    /// </summary>
    private MethodDeclarationSyntax GenerateBuildQueryWhereMethod(
        ClassDeclarationSyntax orgClassDeclaration,
        string orgClassName)
    {
        var queryInputClassName = GetGeneratorClassName(orgClassDeclaration, "QueryInput");
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
        GenerateQueryConditions<FieldDeclarationSyntax>(orgClassDeclaration, sb);

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
        var crInputClassName = GetGeneratorClassName(orgClassDeclaration, TransitiveCrInputGenerator.Suffix);
        Debug.WriteLine($"CRINPUT_CLASS_NAME: {crInputClassName}");

        // 添加命名空间前缀
        var fullCrInputClassName = $"{crInputClassName}";

        var sb = new StringBuilder();

        sb.AppendLine($"/// <summary>");
        sb.AppendLine($"/// 将 <see cref=\"{orgClassName}\"/> 映射到 <see cref=\"{crInputClassName}\"/> 实例。");
        sb.AppendLine($"/// </summary>");
        sb.AppendLine($"/// <param name=\"entity\">输入的 <see cref=\"{orgClassName}\"/> 实例。</param>");
        sb.AppendLine($"/// <param name=\"action\">映射后对DTO执行的操作。</param>");
        sb.AppendLine($"/// <returns>映射后的 <see cref=\"{crInputClassName}\"/> 实例。</returns>");
        sb.AppendLine($"public static {fullCrInputClassName} MapToCrInput(this {orgClassName} entity, Action<{fullCrInputClassName}>? action = null)");
        sb.AppendLine("{");
        sb.AppendLine($"    if(entity==null)return null;");
        sb.AppendLine($"    var input = new {fullCrInputClassName}();");

        // 生成属性映射（只处理非主键属性）
        GeneratePropertyMappings<PropertyDeclarationSyntax>(
            orgClassDeclaration,
            sb, compilation,
            (orgPropertyName, propertyName) => $"    input.{propertyName} = entity.{orgPropertyName};",
            false); // 只处理非主键属性

        GeneratePropertyMappings<FieldDeclarationSyntax>(
            orgClassDeclaration,
            sb, compilation,
            (orgPropertyName, propertyName) => $"    input.{propertyName} = entity.{orgPropertyName};",
            false); // 只处理非主键属性

        sb.AppendLine($"    if(action!=null)");
        sb.AppendLine($"        action(input);");
        sb.AppendLine("    return input;");
        sb.AppendLine("}");

        return SyntaxHelper.GetMethodDeclarationSyntax(sb);
    }

    /// <summary>
    /// 生成从实体映射到UpInput的扩展方法
    /// </summary>
    private MethodDeclarationSyntax GenerateMapToUpInputMethod(
         ClassDeclarationSyntax orgClassDeclaration,
         Compilation compilation,
         string orgClassName)
    {
        var upInputClassName = GetGeneratorClassName(orgClassDeclaration, "UpInput");
        System.Diagnostics.Debug.WriteLine($"UPINPUT_CLASS_NAME: {upInputClassName}");

        // 添加命名空间前缀
        var fullUpInputClassName = $"{upInputClassName}";

        var sb = new StringBuilder();

        sb.AppendLine($"/// <summary>");
        sb.AppendLine($"/// 将 <see cref=\"{orgClassName}\"/> 映射到 <see cref=\"{upInputClassName}\"/> 实例。");
        sb.AppendLine($"/// </summary>");
        sb.AppendLine($"/// <param name=\"entity\">输入的 <see cref=\"{orgClassName}\"/> 实例。</param>");
        sb.AppendLine($"/// <param name=\"action\">映射后对DTO执行的操作。</param>");
        sb.AppendLine($"/// <returns>映射后的 <see cref=\"{upInputClassName}\"/> 实例。</returns>");
        sb.AppendLine($"public static {fullUpInputClassName} MapToUpInput(this {orgClassName} entity, Action<{fullUpInputClassName}>? action = null)");
        sb.AppendLine("{");
        sb.AppendLine($"    if(entity==null)return null;");
        sb.AppendLine($"    var input = new {fullUpInputClassName}();");

        // 生成属性映射（所有属性）
        GeneratePropertyMappings<PropertyDeclarationSyntax>(
            orgClassDeclaration,
            sb, compilation,
            (orgPropertyName, propertyName) => $"    input.{propertyName} = entity.{orgPropertyName};",
            null); // 处理所有属性

        GeneratePropertyMappings<FieldDeclarationSyntax>(
            orgClassDeclaration,
            sb, compilation,
            (orgPropertyName, propertyName) => $"    input.{propertyName} = entity.{orgPropertyName};",
            null); // 处理所有属性

        sb.AppendLine($"    if(action!=null)");
        sb.AppendLine($"        action(input);");
        sb.AppendLine("    return input;");
        sb.AppendLine("}");

        return SyntaxHelper.GetMethodDeclarationSyntax(sb);
    }

    /// <summary>
    /// 生成从实体映射到ListOutput(VO)的扩展方法
    /// </summary>
    private MethodDeclarationSyntax GenerateMapToListOutputMethod(
        ClassDeclarationSyntax orgClassDeclaration,
        Compilation compilation,
        string orgClassName)
    {
        var voClassName = orgClassName.Replace(EntitySuffix, "") + TransitiveVoGenerator.VoSuffix;
        System.Diagnostics.Debug.WriteLine($"VO_CLASS_NAME: {voClassName}");

        // 添加命名空间前缀
        var fullVoClassName = $"{voClassName}";

        var sb = new StringBuilder();

        sb.AppendLine($"/// <summary>");
        sb.AppendLine($"/// 将 <see cref=\"{orgClassName}\"/> 映射到 <see cref=\"{voClassName}\"/> 实例。");
        sb.AppendLine($"/// </summary>");
        sb.AppendLine($"/// <param name=\"entity\">输入的 <see cref=\"{orgClassName}\"/> 实例。</param>");
        sb.AppendLine($"/// <param name=\"action\">映射后对DTO执行的操作。</param>");
        sb.AppendLine($"/// <returns>映射后的 <see cref=\"{voClassName}\"/> 实例。</returns>");
        sb.AppendLine($"public static {fullVoClassName} MapToListOutput(this {orgClassName} entity, Action<{fullVoClassName}>? action = null)");
        sb.AppendLine("{");
        sb.AppendLine($"    if(entity==null)return null;");
        sb.AppendLine($"    var output = new {fullVoClassName}();");

        // 生成属性映射（所有属性）
        GeneratePropertyMappings<PropertyDeclarationSyntax>(
            orgClassDeclaration,
            sb, compilation,
            (orgPropertyName, propertyName) => $"    output.{propertyName} = entity.{orgPropertyName};",
            null); // 处理所有属性

        GeneratePropertyMappings<FieldDeclarationSyntax>(
            orgClassDeclaration,
            sb, compilation,
            (orgPropertyName, propertyName) => $"    output.{propertyName} = entity.{orgPropertyName};",
            null); // 处理所有属性

        sb.AppendLine($"    if(action!=null)");
        sb.AppendLine($"        action(output);");
        sb.AppendLine("    return output;");
        sb.AppendLine("}");

        return SyntaxHelper.GetMethodDeclarationSyntax(sb);
    }

    /// <summary>
    /// 生成从实体映射到InfoOutput(VO)的扩展方法
    /// </summary>
    private MethodDeclarationSyntax GenerateMapToInfoOutputMethod(
         ClassDeclarationSyntax orgClassDeclaration,
         Compilation compilation,
         string orgClassName)
    {
        var voClassName = orgClassName.Replace(EntitySuffix, "") + TransitiveVoGenerator.InfoSuffix;
        System.Diagnostics.Debug.WriteLine($"VO_CLASS_NAME: {voClassName}");

        // 添加命名空间前缀
        var fullVoClassName = $"{voClassName}";

        var sb = new StringBuilder();

        sb.AppendLine($"/// <summary>");
        sb.AppendLine($"/// 将 <see cref=\"{orgClassName}\"/> 映射到 <see cref=\"{voClassName}\"/> 实例。");
        sb.AppendLine($"/// </summary>");
        sb.AppendLine($"/// <param name=\"entity\">输入的 <see cref=\"{orgClassName}\"/> 实例。</param>");
        sb.AppendLine($"/// <param name=\"action\">映射后对DTO执行的操作。</param>");
        sb.AppendLine($"/// <returns>映射后的 <see cref=\"{voClassName}\"/> 实例。</returns>");
        sb.AppendLine($"public static {fullVoClassName} MapToInfoOutput(this {orgClassName} entity, Action<{fullVoClassName}>? action = null)");
        sb.AppendLine("{");
        sb.AppendLine($"    if(entity==null)return null;");
        sb.AppendLine($"    var output = new {fullVoClassName}();");

        // 生成属性映射（所有属性）
        GeneratePropertyMappings<PropertyDeclarationSyntax>(
            orgClassDeclaration,
            sb, compilation,
            (orgPropertyName, propertyName) => $"    output.{propertyName} = entity.{orgPropertyName};",
            null); // 处理所有属性

        GeneratePropertyMappings<FieldDeclarationSyntax>(
            orgClassDeclaration,
            sb, compilation,
            (orgPropertyName, propertyName) => $"    output.{propertyName} = entity.{orgPropertyName};",
            null); // 处理所有属性

        sb.AppendLine($"    if(action!=null)");
        sb.AppendLine($"        action(output);");
        sb.AppendLine("    return output;");
        sb.AppendLine("}");

        return SyntaxHelper.GetMethodDeclarationSyntax(sb);
    }

    /// <summary>
    /// 生成从实体集合映射到ListOutput集合的扩展方法
    /// </summary>
    private MethodDeclarationSyntax GenerateMapToListOutputCollectionMethod(
        ClassDeclarationSyntax orgClassDeclaration,
        string orgClassName)
    {
        var voClassName = orgClassName.Replace(EntitySuffix, "") + TransitiveVoGenerator.VoSuffix;
        System.Diagnostics.Debug.WriteLine($"VO_CLASS_NAME: {voClassName}");

        // 添加命名空间前缀
        var fullVoClassName = $"{voClassName}";

        var sb = new StringBuilder();

        sb.AppendLine($"/// <summary>");
        sb.AppendLine($"/// 将 <see cref=\"{orgClassName}\"/> 集合映射到 <see cref=\"{voClassName}\"/> 集合。");
        sb.AppendLine($"/// </summary>");
        sb.AppendLine($"/// <param name=\"entities\">输入的 <see cref=\"{orgClassName}\"/> 集合。</param>");
        sb.AppendLine($"/// <param name=\"action\">映射后对每个DTO执行的操作。</param>");
        sb.AppendLine($"/// <returns>映射后的 <see cref=\"{voClassName}\"/> 集合。</returns>");
        sb.AppendLine($"public static List<{fullVoClassName}> MapToList(this IEnumerable<{orgClassName}> entities, Action<{fullVoClassName}>? action=null)");
        sb.AppendLine("{");
        sb.AppendLine($"    if (entities == null)");
        sb.AppendLine($"        return [];");
        sb.AppendLine();
        sb.AppendLine($"    var listOutputs = new List<{fullVoClassName}>();");
        sb.AppendLine($"    foreach (var entity in entities)");
        sb.AppendLine($"    {{");
        sb.AppendLine($"        var listOutput = entity.MapToListOutput();");
        sb.AppendLine($"        if(action!=null)");
        sb.AppendLine($"            action(listOutput);");
        sb.AppendLine($"        listOutputs.Add(listOutput);");
        sb.AppendLine($"    }}");
        sb.AppendLine($"    return listOutputs;");
        sb.AppendLine("}");

        return SyntaxHelper.GetMethodDeclarationSyntax(sb);
    }

    /// <summary>
    /// 生成从实体集合映射到InfoOutput集合的扩展方法
    /// </summary>
    private MethodDeclarationSyntax GenerateMapToInfoOutputCollectionMethod(
        ClassDeclarationSyntax orgClassDeclaration,
        string orgClassName)
    {
        var voClassName = orgClassName.Replace(EntitySuffix, "") + TransitiveVoGenerator.InfoSuffix;
        System.Diagnostics.Debug.WriteLine($"VO_CLASS_NAME: {voClassName}");

        // 添加命名空间前缀
        var fullVoClassName = $"{voClassName}";

        var sb = new StringBuilder();

        sb.AppendLine($"/// <summary>");
        sb.AppendLine($"/// 将 <see cref=\"{orgClassName}\"/> 集合映射到 <see cref=\"{voClassName}\"/> 集合。");
        sb.AppendLine($"/// </summary>");
        sb.AppendLine($"/// <param name=\"entities\">输入的 <see cref=\"{orgClassName}\"/> 集合。</param>");
        sb.AppendLine($"/// <param name=\"action\">映射后对每个DTO执行的操作。</param>");
        sb.AppendLine($"/// <returns>映射后的 <see cref=\"{voClassName}\"/> 集合。</returns>");
        sb.AppendLine($"public static List<{fullVoClassName}> MapToInfoList(this IEnumerable<{orgClassName}> entities, Action<{fullVoClassName}>? action=null)");
        sb.AppendLine("{");
        sb.AppendLine($"    if (entities == null)");
        sb.AppendLine($"        return [];");
        sb.AppendLine();
        sb.AppendLine($"    var listOutputs = new List<{fullVoClassName}>();");
        sb.AppendLine($"    foreach (var entity in entities)");
        sb.AppendLine($"    {{");
        sb.AppendLine($"        var listOutput = entity.MapToInfoOutput();");
        sb.AppendLine($"        if(action!=null)");
        sb.AppendLine($"            action(listOutput);");
        sb.AppendLine($"        listOutputs.Add(listOutput);");
        sb.AppendLine($"    }}");
        sb.AppendLine($"    return listOutputs;");
        sb.AppendLine("}");

        return SyntaxHelper.GetMethodDeclarationSyntax(sb);
    }

    /// <summary>
    /// 生成属性映射代码
    /// </summary>
    private void GeneratePropertyMappings<T>(
        ClassDeclarationSyntax orgClassDeclaration,
        StringBuilder sb,
        Compilation compilation,
        Func<string, string, string> generateMappingLine,
        bool? primaryKeyOnly) where T : MemberDeclarationSyntax
    {
        ProcessMembers<T>(orgClassDeclaration, compilation, (member, orgPropertyName, propertyName) =>
        {
            var mappingLine = generateMappingLine(orgPropertyName, propertyName);
            sb.AppendLine(mappingLine);
        }, primaryKeyOnly);
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

    /// <summary>
    /// 获取成员的类型
    /// </summary>
    private string GetPropertyType<T>(T member) where T : MemberDeclarationSyntax
    {
        if (member is PropertyDeclarationSyntax property)
        {
            return SyntaxHelper.GetPropertyType(property);
        }
        else if (member is FieldDeclarationSyntax field)
        {
            return SyntaxHelper.GetPropertyType(field);
        }
        return "object";
    }
}