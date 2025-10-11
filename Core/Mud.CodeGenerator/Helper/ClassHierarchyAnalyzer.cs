namespace Mud.CodeGenerator;
public static class ClassHierarchyAnalyzer
{
    /// <summary>
    /// 分析类的完整继承层次结构
    /// </summary>
    public static List<ClassHierarchyInfo> AnalyzeClassHierarchy(
        ClassDeclarationSyntax classDeclaration,
        Compilation compilation)
    {
        var hierarchy = new List<ClassHierarchyInfo>();

        // 获取语义模型
        var semanticModel = compilation.GetSemanticModel(classDeclaration.SyntaxTree);

        // 获取当前类的符号
        var currentClassSymbol = semanticModel.GetDeclaredSymbol(classDeclaration) as INamedTypeSymbol;

        if (currentClassSymbol == null)
            return hierarchy;

        // 添加当前类信息
        hierarchy.Add(CreateClassInfo(currentClassSymbol, classDeclaration));

        // 递归分析基类
        AnalyzeBaseTypes(currentClassSymbol, hierarchy, compilation);

        return hierarchy;
    }

    /// <summary>
    /// 获取基类中的所有公共属性（包括继承链中的所有基类），处理泛型类型的具体化
    /// </summary>
    public static List<PropertyInfo> GetBaseClassPublicProperties(
        ClassDeclarationSyntax classDeclaration,
        Compilation compilation,
        bool includeCurrentClass = false)
    {
        var properties = new List<PropertyInfo>();

        var semanticModel = compilation.GetSemanticModel(classDeclaration.SyntaxTree);
        var currentClassSymbol = semanticModel.GetDeclaredSymbol(classDeclaration) as INamedTypeSymbol;

        if (currentClassSymbol == null)
            return properties;

        if (includeCurrentClass)
        {
            AddPublicPropertiesFromType(currentClassSymbol, properties, compilation, currentClassSymbol);
        }

        // 递归分析基类，传递当前类符号用于类型参数解析
        CollectBaseClassProperties(currentClassSymbol.BaseType, properties, compilation, currentClassSymbol);

        return properties;
    }

    /// <summary>
    /// 获取基类中的所有公共属性的语法节点（包括继承链中的所有基类）
    /// </summary>
    public static List<PropertyDeclarationSyntax> GetBaseClassPublicPropertyDeclarations(
        ClassDeclarationSyntax classDeclaration,
        Compilation compilation,
        bool includeCurrentClass = false)
    {
        var propertyDeclarations = new List<PropertyDeclarationSyntax>();

        // 获取语义模型
        var semanticModel = compilation.GetSemanticModel(classDeclaration.SyntaxTree);

        // 获取当前类的符号
        var currentClassSymbol = semanticModel.GetDeclaredSymbol(classDeclaration) as INamedTypeSymbol;

        if (currentClassSymbol == null)
            return propertyDeclarations;

        // 如果需要包含当前类的属性
        if (includeCurrentClass)
        {
            AddPublicPropertyDeclarationsFromType(currentClassSymbol, propertyDeclarations);
        }

        // 递归获取基类的公共属性语法节点
        CollectBaseClassPropertyDeclarations(currentClassSymbol.BaseType, propertyDeclarations);

        return propertyDeclarations;
    }

    /// <summary>
    /// 获取基类公共属性的分组信息（按声明类型分组）
    /// </summary>
    public static Dictionary<string, List<PropertyInfo>> GetBaseClassPublicPropertiesGrouped(
        ClassDeclarationSyntax classDeclaration,
        Compilation compilation,
        bool includeCurrentClass = false)
    {
        var properties = GetBaseClassPublicProperties(classDeclaration, compilation, includeCurrentClass);

        return properties
            .GroupBy(p => p.DeclaringType)
            .ToDictionary(
                g => g.Key,
                g => g.ToList());
    }

    /// <summary>
    /// 获取基类公共属性语法节点的分组信息（按声明类型分组）
    /// </summary>
    public static Dictionary<string, List<PropertyDeclarationSyntax>> GetBaseClassPublicPropertyDeclarationsGrouped(
        ClassDeclarationSyntax classDeclaration,
        Compilation compilation,
        bool includeCurrentClass = false)
    {
        var properties = GetBaseClassPublicPropertyDeclarations(classDeclaration, compilation, includeCurrentClass);

        // 需要创建一个字典来按声明类型分组
        var groupedProperties = new Dictionary<string, List<PropertyDeclarationSyntax>>();

        foreach (var property in properties)
        {
            // 获取属性的声明类型名称
            var parentClass = property.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();
            if (parentClass != null)
            {
                var className = parentClass.Identifier.ValueText;
                if (!groupedProperties.ContainsKey(className))
                {
                    groupedProperties[className] = new List<PropertyDeclarationSyntax>();
                }
                groupedProperties[className].Add(property);
            }
        }

        return groupedProperties;
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
        var properties = GetBaseClassPublicProperties(classDeclaration, compilation, includeCurrentClass);
        return properties.Any(p => p.Name == propertyName);
    }

    /// <summary>
    /// 检查基类中是否存在特定名称的属性语法节点
    /// </summary>
    public static bool BaseClassHasPropertyDeclaration(
        ClassDeclarationSyntax classDeclaration,
        Compilation compilation,
        string propertyName,
        bool includeCurrentClass = false)
    {
        var properties = GetBaseClassPublicPropertyDeclarations(classDeclaration, compilation, includeCurrentClass);
        return properties.Any(p => p.Identifier.ValueText == propertyName);
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
        var properties = GetBaseClassPublicProperties(classDeclaration, compilation, includeCurrentClass);
        return properties.Where(p => p.Type == typeName).ToList();
    }

    /// <summary>
    /// 获取基类中特定类型的属性语法节点
    /// </summary>
    public static List<PropertyDeclarationSyntax> GetBaseClassPropertyDeclarationsByType(
        ClassDeclarationSyntax classDeclaration,
        Compilation compilation,
        string typeName,
        bool includeCurrentClass = false)
    {
        var properties = GetBaseClassPublicPropertyDeclarations(classDeclaration, compilation, includeCurrentClass);
        return properties.Where(p =>
            p.Type is IdentifierNameSyntax identifierName &&
            identifierName.Identifier.ValueText == typeName).ToList();
    }

    /// <summary>
    /// 从属性声明语法节点创建属性信息对象
    /// </summary>
    public static PropertyInfo CreatePropertyInfoFromDeclaration(
        PropertyDeclarationSyntax propertyDeclaration,
        Compilation compilation)
    {
        var semanticModel = compilation.GetSemanticModel(propertyDeclaration.SyntaxTree);
        var propertySymbol = semanticModel.GetDeclaredSymbol(propertyDeclaration) as IPropertySymbol;

        if (propertySymbol == null)
            return null;

        // 解析属性类型，考虑泛型类型参数的具体化
        var resolvedType = ResolvePropertyType(propertySymbol, propertySymbol.ContainingType);

        return new PropertyInfo
        {
            Name = propertySymbol.Name,
            Type = resolvedType ?? propertySymbol.Type.ToDisplayString(),
            DeclaringType = propertySymbol.ContainingType.ToDisplayString(),
            DeclaringTypeShortName = propertySymbol.ContainingType.Name,
            CanRead = propertySymbol.GetMethod != null,
            CanWrite = propertySymbol.SetMethod != null,
            IsAbstract = propertySymbol.IsAbstract,
            IsVirtual = propertySymbol.IsVirtual,
            IsOverride = propertySymbol.IsOverride,
            Accessibility = propertySymbol.DeclaredAccessibility,
            OriginalType = propertySymbol.Type.ToDisplayString(),
            Documentation = GetPropertyDocumentation(propertySymbol, compilation)
        };
    }

    /// <summary>
    /// 获取继承深度（从当前类到Object的层级数）
    /// </summary>
    public static int GetInheritanceDepth(
        ClassDeclarationSyntax classDeclaration,
        Compilation compilation)
    {
        var hierarchy = AnalyzeClassHierarchy(classDeclaration, compilation);
        return hierarchy.Count;
    }

    /// <summary>
    /// 检查类是否继承自特定类型
    /// </summary>
    public static bool InheritsFrom(
        ClassDeclarationSyntax classDeclaration,
        Compilation compilation,
        string baseTypeFullName)
    {
        var hierarchy = AnalyzeClassHierarchy(classDeclaration, compilation);
        return hierarchy.Any(info => info.FullName == baseTypeFullName);
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

    #region Private Methods

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

    private static void CollectBaseClassProperties(
        INamedTypeSymbol baseType,
        List<PropertyInfo> properties,
        Compilation compilation,
        INamedTypeSymbol originalClassSymbol)
    {
        if (baseType == null || baseType.SpecialType == SpecialType.System_Object)
            return;

        AddPublicPropertiesFromType(baseType, properties, compilation, originalClassSymbol);
        CollectBaseClassProperties(baseType.BaseType, properties, compilation, originalClassSymbol);
    }

    private static void CollectBaseClassPropertyDeclarations(
        INamedTypeSymbol baseType,
        List<PropertyDeclarationSyntax> propertyDeclarations)
    {
        // 基类为 null 或 System.Object 时停止递归
        if (baseType == null || baseType.SpecialType == SpecialType.System_Object)
            return;

        // 添加当前基类的公共属性语法节点
        AddPublicPropertyDeclarationsFromType(baseType, propertyDeclarations);

        // 递归处理上一级基类
        CollectBaseClassPropertyDeclarations(baseType.BaseType, propertyDeclarations);
    }

    private static void AddPublicPropertiesFromType(
        INamedTypeSymbol typeSymbol,
        List<PropertyInfo> properties,
        Compilation compilation,
        INamedTypeSymbol contextClassSymbol)
    {
        var members = typeSymbol.GetMembers();

        foreach (var member in members)
        {
            if (member is IPropertySymbol propertySymbol &&
                propertySymbol.DeclaredAccessibility == Accessibility.Public &&
                !propertySymbol.IsStatic)
            {
                // 解析属性类型，考虑泛型类型参数的具体化
                var resolvedType = ResolvePropertyType(propertySymbol, contextClassSymbol);

                if (!properties.Any(p => p.Name == propertySymbol.Name && p.DeclaringType == typeSymbol.ToDisplayString()))
                {
                    properties.Add(CreatePropertyInfo(propertySymbol, typeSymbol, resolvedType, compilation));
                }
            }
        }
    }

    private static void AddPublicPropertyDeclarationsFromType(
        INamedTypeSymbol typeSymbol,
        List<PropertyDeclarationSyntax> propertyDeclarations)
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
                // 获取属性的语法节点
                var propertySyntax = GetPropertyDeclarationSyntax(propertySymbol);
                if (propertySyntax != null)
                {
                    // 检查是否已经存在同名属性（避免重写属性重复）
                    if (!propertyDeclarations.Any(p => p.Identifier.ValueText == propertySyntax.Identifier.ValueText))
                    {
                        propertyDeclarations.Add(propertySyntax);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 从属性符号获取属性声明语法节点
    /// </summary>
    private static PropertyDeclarationSyntax GetPropertyDeclarationSyntax(IPropertySymbol propertySymbol)
    {
        // 获取属性的语法引用
        var syntaxReferences = propertySymbol.DeclaringSyntaxReferences;
        if (syntaxReferences.Length == 0)
            return null;

        // 获取第一个语法节点（对于部分类可能有多个）
        var syntaxNode = syntaxReferences[0].GetSyntax();

        // 返回属性声明语法节点
        return syntaxNode as PropertyDeclarationSyntax;
    }

    private static string ResolvePropertyType(IPropertySymbol propertySymbol, INamedTypeSymbol contextClassSymbol)
    {
        // 如果属性类型是类型参数（泛型），尝试解析具体类型
        if (propertySymbol.Type is ITypeParameterSymbol typeParameter)
        {
            // 在继承链中查找类型参数的具体化
            return ResolveTypeParameter(typeParameter, contextClassSymbol) ?? propertySymbol.Type.ToDisplayString();
        }

        // 如果属性类型是构造的泛型类型，递归解析类型参数
        if (propertySymbol.Type is INamedTypeSymbol namedType && namedType.IsGenericType)
        {
            return ResolveConstructedGenericType(namedType, contextClassSymbol);
        }

        // 普通类型直接返回
        return propertySymbol.Type.ToDisplayString();
    }

    private static string ResolveTypeParameter(ITypeParameterSymbol typeParameter, INamedTypeSymbol contextClassSymbol)
    {
        var currentType = contextClassSymbol;

        while (currentType != null)
        {
            // 如果当前类型是泛型类型，检查类型参数
            if (currentType.IsGenericType)
            {
                var typeParameters = currentType.OriginalDefinition.TypeParameters;
                for (int i = 0; i < typeParameters.Length; i++)
                {
                    if (typeParameters[i].Name == typeParameter.Name)
                    {
                        return currentType.TypeArguments[i].ToDisplayString();
                    }
                }
            }

            // 检查基类链中的类型参数具体化
            if (currentType.BaseType != null)
            {
                var baseTypeResolution = ResolveTypeParameterInBaseChain(typeParameter, currentType.BaseType);
                if (baseTypeResolution != null)
                    return baseTypeResolution;
            }

            currentType = currentType.BaseType;
        }

        return null;
    }

    private static string ResolveTypeParameterInBaseChain(ITypeParameterSymbol typeParameter, INamedTypeSymbol baseType)
    {
        if (baseType == null || baseType.SpecialType == SpecialType.System_Object)
            return null;

        // 如果基类是构造的泛型类型，查找类型参数的映射
        if (baseType.IsGenericType)
        {
            var originalDefinition = baseType.OriginalDefinition;
            var typeParameters = originalDefinition.TypeParameters;

            for (int i = 0; i < typeParameters.Length; i++)
            {
                if (typeParameters[i].Name == typeParameter.Name)
                {
                    return baseType.TypeArguments[i].ToDisplayString();
                }
            }
        }

        return ResolveTypeParameterInBaseChain(typeParameter, baseType.BaseType);
    }

    private static string ResolveConstructedGenericType(INamedTypeSymbol constructedType, INamedTypeSymbol contextClassSymbol)
    {
        var typeName = constructedType.OriginalDefinition.ToDisplayString();
        var typeArguments = constructedType.TypeArguments.Select(arg =>
        {
            if (arg is ITypeParameterSymbol typeParam)
            {
                return ResolveTypeParameter(typeParam, contextClassSymbol) ?? arg.ToDisplayString();
            }
            return arg.ToDisplayString();
        }).ToArray();

        return $"{typeName}<{string.Join(", ", typeArguments)}>";
    }

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
            AssemblyName = classSymbol.ContainingAssembly?.Name ?? "Unknown",
            Namespace = classSymbol.ContainingNamespace?.ToDisplayString() ?? "Global"
        };
    }

    private static PropertyInfo CreatePropertyInfo(
        IPropertySymbol propertySymbol,
        INamedTypeSymbol declaringType,
        string resolvedType,
        Compilation compilation)
    {
        return new PropertyInfo
        {
            Name = propertySymbol.Name,
            Type = resolvedType ?? propertySymbol.Type.ToDisplayString(),
            DeclaringType = declaringType.ToDisplayString(),
            DeclaringTypeShortName = declaringType.Name,
            CanRead = propertySymbol.GetMethod != null,
            CanWrite = propertySymbol.SetMethod != null,
            IsAbstract = propertySymbol.IsAbstract,
            IsVirtual = propertySymbol.IsVirtual,
            IsOverride = propertySymbol.IsOverride,
            Accessibility = propertySymbol.DeclaredAccessibility,
            OriginalType = propertySymbol.Type.ToDisplayString(),
            Documentation = GetPropertyDocumentation(propertySymbol, compilation)
        };
    }

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

    #endregion
}