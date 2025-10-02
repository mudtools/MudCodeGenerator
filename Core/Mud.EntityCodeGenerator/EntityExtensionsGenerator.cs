using Mud.EntityCodeGenerator.Diagnostics;
using System.Text;

namespace Mud.EntityCodeGenerator;

/// <summary>
/// 实体扩展类生成器，用于生成实体与其他DTO类型之间的映射扩展方法。
/// </summary>
[Generator]
public class EntityExtensionsGenerator : TransitiveDtoGenerator
{
    /// <summary>
    /// DTO生成特性。
    /// </summary>
    protected new const string DtoGeneratorAttributeName = "DtoGeneratorAttribute";

    /// <summary>
    /// EntityExtensionsGenerator构造函数
    /// </summary>
    public EntityExtensionsGenerator() : base()
    {
    }

    /// <inheritdoc/>
    public override void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // 获取所有带有DtoGeneratorAttribute的类
        var classDeclarationProvider = GetClassDeclarationProvider(context, [DtoGeneratorAttributeName]);

        // 生成扩展类
        context.RegisterSourceOutput(classDeclarationProvider, (context, classNodes) =>
        {
            foreach (var classNode in classNodes)
            {
                //Debugger.Launch();
                if (classNode != null)
                {
                    try
                    {
                        GenerateCode(context, classNode);
                    }
                    catch (Exception ex)
                    {
                        // 提高容错性，即使单个类生成失败也不影响其他类
                        var className = SyntaxHelper.GetClassName(classNode);
                        context.ReportDiagnostic(Diagnostic.Create(
                            DiagnosticDescriptors.DtoGenerationError,
                            Location.None,
                            className,
                            ex.Message));
                    }
                }
            }
        });
    }

    /// <inheritdoc/>
    protected override void GenerateCode(SourceProductionContext context, ClassDeclarationSyntax orgClassDeclaration)
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
            var extensionClass = BuildExtensionClass(orgClassDeclaration, orgClassName, extensionClassName);

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
        string orgClassName,
        string extensionClassName)
    {
        var extensionClass = BuildLocalClass(orgClassDeclaration, extensionClassName, false)
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                                                  SyntaxFactory.Token(SyntaxKind.StaticKeyword)));

        // 添加MapToEntityFromCrInput方法（从CrInput映射到实体）
        var mapFromCrInputMethod = GenerateMapToEntityFromCrInputMethod(orgClassDeclaration, orgClassName);
        if (mapFromCrInputMethod != null)
        {
            extensionClass = extensionClass.AddMembers(mapFromCrInputMethod);
        }

        // 添加MapToEntityFromUpInput方法（从UpInput映射到实体）
        var mapFromUpInputMethod = GenerateMapToEntityFromUpInputMethod(orgClassDeclaration, orgClassName);
        if (mapFromUpInputMethod != null)
        {
            extensionClass = extensionClass.AddMembers(mapFromUpInputMethod);
        }

        // 添加BuildQueryWhere方法（从QueryInput构建查询条件）
        var buildQueryWhereMethod = GenerateBuildQueryWhereMethod(orgClassDeclaration, orgClassName);
        if (buildQueryWhereMethod != null)
        {
            extensionClass = extensionClass.AddMembers(buildQueryWhereMethod);
        }

        // 添加MapToCrInput方法（从实体映射到CrInput）
        var mapToCrInputMethod = GenerateMapToCrInputMethod(orgClassDeclaration, orgClassName);
        if (mapToCrInputMethod != null)
        {
            extensionClass = extensionClass.AddMembers(mapToCrInputMethod);
        }

        // 添加MapToUpInput方法（从实体映射到UpInput）
        var mapToUpInputMethod = GenerateMapToUpInputMethod(orgClassDeclaration, orgClassName);
        if (mapToUpInputMethod != null)
        {
            extensionClass = extensionClass.AddMembers(mapToUpInputMethod);
        }

        // 添加MapToListOutput方法（从实体映射到ListOutput/VO）
        var mapToListOutputMethod = GenerateMapToListOutputMethod(orgClassDeclaration, orgClassName);
        if (mapToListOutputMethod != null)
        {
            extensionClass = extensionClass.AddMembers(mapToListOutputMethod);
        }

        return extensionClass;
    }

    /// <summary>
    /// 生成从CrInput映射到实体的扩展方法
    /// </summary>
    private MethodDeclarationSyntax GenerateMapToEntityFromCrInputMethod(
        ClassDeclarationSyntax orgClassDeclaration,
        string orgClassName)
    {
        var crInputClassName = GetGeneratorClassName(orgClassDeclaration, TransitiveCrInputGenerator.Suffix);
        Debug.WriteLine($"CRINPUT_CLASS_NAME: {crInputClassName}");

        // 添加命名空间前缀
        var fullCrInputClassName = $"{crInputClassName}";

        var sb = new StringBuilder();

        sb.AppendLine($"public static {orgClassName} MapToEntity(this {fullCrInputClassName} input)");
        sb.AppendLine("{");
        sb.AppendLine($"    var entity = new {orgClassName}();");

        // 生成属性映射
        GeneratePropertyMappings<PropertyDeclarationSyntax>(
            orgClassDeclaration,
            sb,
            (orgPropertyName, propertyName) => $"    entity.{orgPropertyName} = input.{propertyName};",
            false); // 只处理非主键属性

        GeneratePropertyMappings<FieldDeclarationSyntax>(
            orgClassDeclaration,
            sb,
            (orgPropertyName, propertyName) => $"    entity.{orgPropertyName} = input.{propertyName};",
            false); // 只处理非主键属性

        sb.AppendLine("    return entity;");
        sb.AppendLine("}");

        return GetMethodDeclarationSyntax(sb);
    }

    /// <summary>
    /// 生成从UpInput映射到实体的扩展方法
    /// </summary>
    private MethodDeclarationSyntax GenerateMapToEntityFromUpInputMethod(
        ClassDeclarationSyntax orgClassDeclaration,
        string orgClassName)
    {
        var upInputClassName = GetGeneratorClassName(orgClassDeclaration, "UpInput");
        System.Diagnostics.Debug.WriteLine($"UPINPUT_CLASS_NAME: {upInputClassName}");

        // 添加命名空间前缀
        var fullUpInputClassName = $"{upInputClassName}";

        var sb = new StringBuilder();

        sb.AppendLine($"public static {orgClassName} MapToEntity(this {fullUpInputClassName} input)");
        sb.AppendLine("{");
        sb.AppendLine($"    var entity = new {orgClassName}();");

        // 生成属性映射（所有属性）
        GeneratePropertyMappings<PropertyDeclarationSyntax>(
            orgClassDeclaration,
            sb,
            (orgPropertyName, propertyName) => $"    entity.{orgPropertyName} = input.{propertyName};",
            null); // 处理所有属性

        GeneratePropertyMappings<FieldDeclarationSyntax>(
            orgClassDeclaration,
            sb,
            (orgPropertyName, propertyName) => $"    entity.{orgPropertyName} = input.{propertyName};",
            null); // 处理所有属性

        sb.AppendLine("    return entity;");
        sb.AppendLine("}");

        return GetMethodDeclarationSyntax(sb);
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

        sb.AppendLine($"public static Expression<Func<{orgClassName}, bool>> BuildQueryWhere(this {fullQueryInputClassName} input)");
        sb.AppendLine("{");
        sb.AppendLine($"    Expression<Func<{orgClassName}, bool>> where = x => true;");

        // 生成查询条件
        GenerateQueryConditions<PropertyDeclarationSyntax>(orgClassDeclaration, sb);
        GenerateQueryConditions<FieldDeclarationSyntax>(orgClassDeclaration, sb);

        sb.AppendLine("    return where;");
        sb.AppendLine("}");

        return GetMethodDeclarationSyntax(sb);
    }

    /// <summary>
    /// 生成从实体映射到CrInput的扩展方法
    /// </summary>
    private MethodDeclarationSyntax GenerateMapToCrInputMethod(
        ClassDeclarationSyntax orgClassDeclaration,
        string orgClassName)
    {
        var crInputClassName = GetGeneratorClassName(orgClassDeclaration, TransitiveCrInputGenerator.Suffix);
        System.Diagnostics.Debug.WriteLine($"CRINPUT_CLASS_NAME: {crInputClassName}");

        // 添加命名空间前缀
        var fullCrInputClassName = $"{crInputClassName}";

        var sb = new StringBuilder();

        sb.AppendLine($"public static {fullCrInputClassName} MapToCrInput(this {orgClassName} entity)");
        sb.AppendLine("{");
        sb.AppendLine($"    var input = new {fullCrInputClassName}();");

        // 生成属性映射（只处理非主键属性）
        GeneratePropertyMappings<PropertyDeclarationSyntax>(
            orgClassDeclaration,
            sb,
            (orgPropertyName, propertyName) => $"    input.{propertyName} = entity.{orgPropertyName};",
            false); // 只处理非主键属性

        GeneratePropertyMappings<FieldDeclarationSyntax>(
            orgClassDeclaration,
            sb,
            (orgPropertyName, propertyName) => $"    input.{propertyName} = entity.{orgPropertyName};",
            false); // 只处理非主键属性

        sb.AppendLine("    return input;");
        sb.AppendLine("}");

        return GetMethodDeclarationSyntax(sb);
    }

    /// <summary>
    /// 生成从实体映射到UpInput的扩展方法
    /// </summary>
    private MethodDeclarationSyntax GenerateMapToUpInputMethod(
        ClassDeclarationSyntax orgClassDeclaration,
        string orgClassName)
    {
        var upInputClassName = GetGeneratorClassName(orgClassDeclaration, "UpInput");
        System.Diagnostics.Debug.WriteLine($"UPINPUT_CLASS_NAME: {upInputClassName}");

        // 添加命名空间前缀
        var fullUpInputClassName = $"{upInputClassName}";

        var sb = new StringBuilder();

        sb.AppendLine($"public static {fullUpInputClassName} MapToUpInput(this {orgClassName} entity)");
        sb.AppendLine("{");
        sb.AppendLine($"    var input = new {fullUpInputClassName}();");

        // 生成属性映射（所有属性）
        GeneratePropertyMappings<PropertyDeclarationSyntax>(
            orgClassDeclaration,
            sb,
            (orgPropertyName, propertyName) => $"    input.{propertyName} = entity.{orgPropertyName};",
            null); // 处理所有属性

        GeneratePropertyMappings<FieldDeclarationSyntax>(
            orgClassDeclaration,
            sb,
            (orgPropertyName, propertyName) => $"    input.{propertyName} = entity.{orgPropertyName};",
            null); // 处理所有属性

        sb.AppendLine("    return input;");
        sb.AppendLine("}");

        return GetMethodDeclarationSyntax(sb);
    }

    /// <summary>
    /// 生成从实体映射到ListOutput(VO)的扩展方法
    /// </summary>
    private MethodDeclarationSyntax GenerateMapToListOutputMethod(
        ClassDeclarationSyntax orgClassDeclaration,
        string orgClassName)
    {
        var voClassName = orgClassName.Replace(EntitySuffix, "") + TransitiveVoGenerator.VoSuffix;
        System.Diagnostics.Debug.WriteLine($"VO_CLASS_NAME: {voClassName}");

        // 添加命名空间前缀
        var fullVoClassName = $"{voClassName}";

        var sb = new StringBuilder();

        sb.AppendLine($"public static {fullVoClassName} MapToListOutput(this {orgClassName} entity)");
        sb.AppendLine("{");
        sb.AppendLine($"    var output = new {fullVoClassName}();");

        // 生成属性映射（所有属性）
        GeneratePropertyMappings<PropertyDeclarationSyntax>(
            orgClassDeclaration,
            sb,
            (orgPropertyName, propertyName) => $"    output.{propertyName} = entity.{orgPropertyName};",
            null); // 处理所有属性

        GeneratePropertyMappings<FieldDeclarationSyntax>(
            orgClassDeclaration,
            sb,
            (orgPropertyName, propertyName) => $"    output.{propertyName} = entity.{orgPropertyName};",
            null); // 处理所有属性

        sb.AppendLine("    return output;");
        sb.AppendLine("}");

        return GetMethodDeclarationSyntax(sb);
    }

    /// <summary>
    /// 生成属性映射代码
    /// </summary>
    private void GeneratePropertyMappings<T>(
        ClassDeclarationSyntax orgClassDeclaration,
        StringBuilder sb,
        Func<string, string, string> generateMappingLine,
        bool? primaryKeyOnly) where T : MemberDeclarationSyntax
    {
        foreach (var member in orgClassDeclaration.Members.OfType<T>())
        {
            try
            {
                if (IsIgnoreGenerator(member))
                {
                    continue;
                }

                var isPrimary = IsPrimary(member);

                // 根据primaryKeyOnly参数决定是否处理该属性
                if (primaryKeyOnly.HasValue)
                {
                    if (primaryKeyOnly.Value && !isPrimary)
                    {
                        continue;
                    }

                    if (!primaryKeyOnly.Value && isPrimary)
                    {
                        continue;
                    }
                }

                var orgPropertyName = "";
                if (member is PropertyDeclarationSyntax property)
                {
                    orgPropertyName = GetPropertyName(property);
                }
                else if (member is FieldDeclarationSyntax field)
                {
                    orgPropertyName = GetFirstUpperPropertyName(field);
                }

                if (string.IsNullOrEmpty(orgPropertyName))
                {
                    continue;
                }

                var propertyName = ToLowerFirstLetter(orgPropertyName);
                var mappingLine = generateMappingLine(orgPropertyName, propertyName);
                sb.AppendLine(mappingLine);
            }
            catch (Exception ex)
            {
                // 即使单个属性生成失败也不影响其他属性
            }
        }
    }

    /// <summary>
    /// 生成查询条件代码
    /// </summary>
    private void GenerateQueryConditions<T>(ClassDeclarationSyntax orgClassDeclaration, StringBuilder sb)
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

                var isLikeQuery = IsLikeGenerator(member);
                var (propertyName, propertyType) = GetGeneratorProperty(member);
                var orgPropertyName = "";

                if (member is PropertyDeclarationSyntax property)
                {
                    orgPropertyName = GetPropertyName(property);
                }
                else if (member is FieldDeclarationSyntax field)
                {
                    orgPropertyName = propertyName;
                    propertyName = ToLowerFirstLetter(orgPropertyName);
                }

                if (string.IsNullOrEmpty(propertyName))
                    propertyName = "Property";

                if (string.IsNullOrEmpty(propertyType))
                    propertyType = "object";

                if (string.IsNullOrEmpty(orgPropertyName))
                    orgPropertyName = propertyName;

                if (propertyType.StartsWith("string", StringComparison.OrdinalIgnoreCase))
                {
                    sb.AppendLine($"    if (!string.IsNullOrEmpty(input.{propertyName}?.Trim()))");
                    if (!isLikeQuery)
                        sb.AppendLine($"        where = where.And(x => x.{orgPropertyName} == input.{propertyName}.Trim());");
                    else
                        sb.AppendLine($"        where = where.And(x => x.{orgPropertyName}.Contains(input.{propertyName}.Trim()));");
                }
                else
                {
                    sb.AppendLine($"    if (input.{propertyName} != null)");
                    sb.AppendLine($"        where = where.And(x => x.{orgPropertyName} == input.{propertyName});");
                }
            }
            catch (Exception ex)
            {
                // 即使单个属性生成失败也不影响其他属性
            }
        }
    }
}