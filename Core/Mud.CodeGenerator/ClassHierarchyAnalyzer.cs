namespace Mud.CodeGenerator;

public static class ClassHierarchyAnalyzer
{
    /// <summary>
    /// 分析类的完整继承层次结构
    /// </summary>
    /// <param name="classDeclaration">类声明语法节点</param>
    /// <param name="compilation">编译对象</param>
    /// <returns>继承链列表，从当前类开始到最顶层基类</returns>
    public static IReadOnlyList<ClassHierarchyInfo> GetClassHierarchy(
        ClassDeclarationSyntax classDeclaration,
        Compilation compilation)
    {
        if (classDeclaration == null || compilation == null)
            return [];

        var hierarchy = new List<ClassHierarchyInfo>();

        // 获取语义模型
        var semanticModel = compilation.GetSemanticModel(classDeclaration.SyntaxTree);

        // 获取当前类的符号
        var currentClassSymbol = semanticModel.GetDeclaredSymbol(classDeclaration);

        if (currentClassSymbol == null)
            return hierarchy;

        // 添加当前类信息
        hierarchy.Add(CreateClassInfo(currentClassSymbol, classDeclaration));

        // 递归分析基类
        AnalyzeBaseTypes(currentClassSymbol, hierarchy, compilation);

        return hierarchy;
    }

    /// <summary>
    /// 获取基类中的所有公共属性（包括继承链中的所有基类）
    /// </summary>
    /// <param name="classDeclaration">类声明语法节点</param>
    /// <param name="compilation">编译对象</param>
    /// <param name="includeCurrentClass">是否包含当前类的属性</param>
    /// <returns>基类的公共属性列表</returns>
    public static IReadOnlyList<PropertyInfo> GetPublicProperties(
        ClassDeclarationSyntax classDeclaration,
        Compilation compilation,
        bool includeCurrentClass = false)
    {
        if (classDeclaration == null || compilation == null)
            return [];
        var properties = new List<PropertyInfo>();

        // 获取语义模型
        var semanticModel = compilation.GetSemanticModel(classDeclaration.SyntaxTree);

        // 获取当前类的符号
        // 获取当前类的符号
        var currentClassSymbol = semanticModel.GetDeclaredSymbol(classDeclaration);
        if (currentClassSymbol == null)
            return properties;

        // 如果需要包含当前类的属性
        if (includeCurrentClass)
        {
            AddPublicPropertiesFromType(currentClassSymbol, properties, compilation);
        }

        // 递归获取基类的公共属性
        CollectBaseClassProperties(currentClassSymbol.BaseType, properties, compilation);

        return properties;
    }

    /// <summary>
    /// 递归收集基类的公共属性
    /// </summary>
    private static void CollectBaseClassProperties(
        INamedTypeSymbol baseType,
        List<PropertyInfo> properties,
        Compilation compilation)
    {
        // 基类为 null 或 System.Object 时停止递归
        if (baseType == null || baseType.SpecialType == SpecialType.System_Object)
            return;

        // 添加当前基类的公共属性
        AddPublicPropertiesFromType(baseType, properties, compilation);

        // 递归处理上一级基类
        CollectBaseClassProperties(baseType.BaseType, properties, compilation);
    }

    /// <summary>
    /// 从类型符号中添加公共属性
    /// </summary>
    private static void AddPublicPropertiesFromType(
        INamedTypeSymbol typeSymbol,
        List<PropertyInfo> properties,
        Compilation compilation)
    {
        // 获取类型的所有成员
        var members = typeSymbol.GetMembers();

        foreach (var member in members)
        {
            // 只处理公共属性
            if (member is IPropertySymbol propertySymbol &&
                propertySymbol.DeclaredAccessibility == Accessibility.Public &&
                !propertySymbol.IsStatic) // 排除静态属性
            {
                // 检查是否已经存在同名属性（避免重写属性重复）
                if (!properties.Any(p => p.Name == propertySymbol.Name && p.DeclaringType == typeSymbol.ToDisplayString()))
                {
                    properties.Add(CreatePropertyInfo(propertySymbol, typeSymbol, compilation));
                }
            }
        }
    }

    /// <summary>
    /// 创建属性信息
    /// </summary>
    private static PropertyInfo CreatePropertyInfo(
        IPropertySymbol propertySymbol,
        INamedTypeSymbol declaringType,
        Compilation compilation)
    {
        return new PropertyInfo
        {
            Name = propertySymbol.Name,
            Type = propertySymbol.Type.ToDisplayString(),
            DeclaringType = declaringType.ToDisplayString(),
            DeclaringTypeShortName = declaringType.Name,
            CanRead = propertySymbol.GetMethod != null,
            CanWrite = propertySymbol.SetMethod != null,
            IsAbstract = propertySymbol.IsAbstract,
            IsVirtual = propertySymbol.IsVirtual,
            IsOverride = propertySymbol.IsOverride,
            Accessibility = propertySymbol.DeclaredAccessibility,
            // 获取 XML 文档注释
            Documentation = GetPropertyDocumentation(propertySymbol, compilation)
        };
    }

    /// <summary>
    /// 获取属性的 XML 文档注释
    /// </summary>
    private static string GetPropertyDocumentation(IPropertySymbol propertySymbol, Compilation compilation)
    {
        try
        {
            // 获取属性的语法引用
            var syntaxReference = propertySymbol.DeclaringSyntaxReferences.FirstOrDefault();
            if (syntaxReference != null)
            {
                var syntaxNode = syntaxReference.GetSyntax();
                if (syntaxNode is PropertyDeclarationSyntax propertySyntax)
                {
                    // 获取前导 trivia 中的文档注释
                    var trivia = propertySyntax.GetLeadingTrivia();
                    var docComment = string.Join("\n", trivia
                        .Where(t => t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) ||
                                   t.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia))
                        .Select(t => t.ToString()));

                    return !string.IsNullOrWhiteSpace(docComment) ? docComment : null;
                }
            }
        }
        catch
        {
            // 如果无法获取文档注释，忽略错误
        }

        return null;
    }

    /// <summary>
    /// 获取基类公共属性的分组信息（按声明类型分组）
    /// </summary>
    public static Dictionary<string, List<PropertyInfo>> GetBaseClassPublicPropertiesGrouped(
        ClassDeclarationSyntax classDeclaration,
        Compilation compilation,
        bool includeCurrentClass = false)
    {
        var properties = GetPublicProperties(classDeclaration, compilation, includeCurrentClass);

        return properties
            .GroupBy(p => p.DeclaringType)
            .ToDictionary(
                g => g.Key,
                g => g.ToList());
    }

    /// <summary>
    /// 检查基类中是否存在特定名称的属性
    /// </summary>
    public static bool BaseClassHasProperty(
        ClassDeclarationSyntax classDeclaration,
        Compilation compilation,
        string propertyName,
        bool includeCurrentClass = false)
    {
        var properties = GetPublicProperties(classDeclaration, compilation, includeCurrentClass);
        return properties.Any(p => p.Name == propertyName);
    }

    /// <summary>
    /// 获取基类中特定类型的属性
    /// </summary>
    public static List<PropertyInfo> GetBaseClassPropertiesByType(
        ClassDeclarationSyntax classDeclaration,
        Compilation compilation,
        string typeName,
        bool includeCurrentClass = false)
    {
        var properties = GetPublicProperties(classDeclaration, compilation, includeCurrentClass);
        return properties.Where(p => p.Type == typeName).ToList();
    }

    /// <summary>
    /// 递归分析基类
    /// </summary>
    private static void AnalyzeBaseTypes(
        INamedTypeSymbol currentSymbol,
        List<ClassHierarchyInfo> hierarchy,
        Compilation compilation)
    {
        // 获取直接基类
        var baseType = currentSymbol.BaseType;

        // 基类为 null 或 System.Object 时停止递归
        if (baseType == null || baseType.SpecialType == SpecialType.System_Object)
            return;

        // 创建基类信息
        var baseClassInfo = CreateClassInfo(baseType);
        hierarchy.Add(baseClassInfo);

        // 递归分析上一级基类
        AnalyzeBaseTypes(baseType, hierarchy, compilation);
    }

    /// <summary>
    /// 创建类层次信息
    /// </summary>
    private static ClassHierarchyInfo CreateClassInfo(INamedTypeSymbol classSymbol, ClassDeclarationSyntax syntax = null)
    {
        return new ClassHierarchyInfo
        {
            ClassName = classSymbol.Name,
            FullName = classSymbol.ToDisplayString(),
            Accessibility = classSymbol.DeclaredAccessibility,
            IsAbstract = classSymbol.IsAbstract,
            IsSealed = classSymbol.IsSealed,
            Kind = classSymbol.TypeKind,
            Location = syntax?.GetLocation(),
            BaseTypeName = classSymbol.BaseType?.ToDisplayString() ?? "System.Object",
            Interfaces = classSymbol.Interfaces.Select(i => i.ToDisplayString()).ToList(),
            // 获取类所在的程序集信息
            AssemblyName = classSymbol.ContainingAssembly?.Name ?? "Unknown",
            Namespace = classSymbol.ContainingNamespace?.ToDisplayString() ?? "Global"
        };
    }

    /// <summary>
    /// 获取类的所有实现的接口（包括继承的）
    /// </summary>
    public static List<string> GetAllImplementedInterfaces(
        ClassDeclarationSyntax classDeclaration,
        Compilation compilation)
    {
        var semanticModel = compilation.GetSemanticModel(classDeclaration.SyntaxTree);
        var classSymbol = semanticModel.GetDeclaredSymbol(classDeclaration) as INamedTypeSymbol;

        if (classSymbol == null)
            return new List<string>();

        // 获取所有接口（包括基类实现的）
        return classSymbol.AllInterfaces
            .Select(i => i.ToDisplayString())
            .Distinct()
            .ToList();
    }

    /// <summary>
    /// 检查类是否继承自特定类型
    /// </summary>
    public static bool InheritsFrom(
        ClassDeclarationSyntax classDeclaration,
        Compilation compilation,
        string baseTypeFullName)
    {
        var hierarchy = GetClassHierarchy(classDeclaration, compilation);
        return hierarchy.Any(info => info.FullName == baseTypeFullName);
    }

    /// <summary>
    /// 获取继承深度（从当前类到Object的层级数）
    /// </summary>
    public static int GetInheritanceDepth(
        ClassDeclarationSyntax classDeclaration,
        Compilation compilation)
    {
        var hierarchy = GetClassHierarchy(classDeclaration, compilation);
        return hierarchy.Count;
    }
}