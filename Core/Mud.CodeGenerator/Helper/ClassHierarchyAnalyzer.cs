namespace Mud.CodeGenerator;

public static class ClassHierarchyAnalyzer
{
    /// <summary>
    /// 分析类的完整继承层次结构
    /// </summary>
    public static IReadOnlyList<ClassHierarchyInfo> AnalyzeClassHierarchy(
        ClassDeclarationSyntax classDeclaration,
        Compilation compilation)
    {
        if (classDeclaration == null || compilation == null)
            return [];
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
    /// 获取基类中的所有公共属性的语法节点（包括继承链中的所有基类）
    /// </summary>
    public static IReadOnlyList<PropertyDeclarationSyntax> GetBaseClassPublicPropertyDeclarations(
        ClassDeclarationSyntax classDeclaration,
        Compilation compilation,
        bool includeCurrentClass = false)
    {
        if (classDeclaration == null || compilation == null)
            return [];
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
            AddPublicPropertyDeclarationsFromType(currentClassSymbol, propertyDeclarations, compilation, currentClassSymbol);
        }

        // 递归获取基类的公共属性语法节点，传递当前类符号用于类型解析
        CollectBaseClassPropertyDeclarations(currentClassSymbol.BaseType, propertyDeclarations, compilation, currentClassSymbol);

        return propertyDeclarations;
    }

    /// <summary>
    /// 获取基类公共属性语法节点的分组信息（按声明类型分组）
    /// </summary>
    public static IReadOnlyDictionary<string, List<PropertyDeclarationSyntax>> GetBaseClassPublicPropertyDeclarationsGrouped(
        ClassDeclarationSyntax classDeclaration,
        Compilation compilation,
        bool includeCurrentClass = false)
    {
        if (classDeclaration == null || compilation == null)
            return new Dictionary<string, List<PropertyDeclarationSyntax>>();
        var properties = GetBaseClassPublicPropertyDeclarations(classDeclaration, compilation, includeCurrentClass);

        // 需要创建一个字典来按声明类型分组
        var groupedProperties = new Dictionary<string, List<PropertyDeclarationSyntax>>();

        foreach (var property in properties)
        {
            // 获取属性的声明类型名称 - 使用语义模型获取准确的类型信息
            var semanticModel = compilation.GetSemanticModel(property.SyntaxTree);
            var propertySymbol = semanticModel.GetDeclaredSymbol(property) as IPropertySymbol;

            if (propertySymbol != null)
            {
                var declaringTypeName = propertySymbol.ContainingType.ToDisplayString();
                if (!groupedProperties.ContainsKey(declaringTypeName))
                {
                    groupedProperties[declaringTypeName] = new List<PropertyDeclarationSyntax>();
                }
                groupedProperties[declaringTypeName].Add(property);
            }
            else
            {
                // 回退方案：使用语法分析
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
        }

        return groupedProperties;
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
        if (classDeclaration == null || compilation == null)
            return false;
        var properties = GetBaseClassPublicPropertyDeclarations(classDeclaration, compilation, includeCurrentClass);
        return properties.Any(p => p.Identifier.ValueText == propertyName);
    }

    /// <summary>
    /// 获取基类中特定类型的属性语法节点
    /// </summary>
    public static IReadOnlyList<PropertyDeclarationSyntax> GetBaseClassPropertyDeclarationsByType(
        ClassDeclarationSyntax classDeclaration,
        Compilation compilation,
        string typeName,
        bool includeCurrentClass = false)
    {
        if (classDeclaration == null || compilation == null)
            return [];
        var properties = GetBaseClassPublicPropertyDeclarations(classDeclaration, compilation, includeCurrentClass);
        return properties.Where(p =>
            p.Type is IdentifierNameSyntax identifierName &&
            identifierName.Identifier.ValueText == typeName).ToList();
    }

    /// <summary>
    /// 获取继承深度（从当前类到Object的层级数）
    /// </summary>
    public static int GetInheritanceDepth(
        ClassDeclarationSyntax classDeclaration,
        Compilation compilation)
    {
        if (classDeclaration == null || compilation == null)
            return 0;
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
        if (classDeclaration == null || compilation == null)
            return false;
        var hierarchy = AnalyzeClassHierarchy(classDeclaration, compilation);
        return hierarchy.Any(info => info.FullName == baseTypeFullName);
    }

    /// <summary>
    /// 获取类的所有实现的接口（包括继承的）
    /// </summary>
    public static IReadOnlyList<string> GetAllImplementedInterfaces(
        ClassDeclarationSyntax classDeclaration,
        Compilation compilation)
    {
        if (classDeclaration == null || compilation == null)
            return [];
        var semanticModel = compilation.GetSemanticModel(classDeclaration.SyntaxTree);
        var classSymbol = semanticModel.GetDeclaredSymbol(classDeclaration) as INamedTypeSymbol;

        if (classSymbol == null)
            return [];

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


    private static void CollectBaseClassPropertyDeclarations(
        INamedTypeSymbol baseType,
        List<PropertyDeclarationSyntax> propertyDeclarations,
        Compilation compilation,
        INamedTypeSymbol contextClassSymbol)
    {
        // 基类为 null 或 System.Object 时停止递归
        if (baseType == null || baseType.SpecialType == SpecialType.System_Object)
            return;

        // 添加当前基类的公共属性语法节点，传递上下文类符号用于类型解析
        AddPublicPropertyDeclarationsFromType(baseType, propertyDeclarations, compilation, contextClassSymbol);

        // 递归处理上一级基类
        CollectBaseClassPropertyDeclarations(baseType.BaseType, propertyDeclarations, compilation, contextClassSymbol);
    }

    private static void AddPublicPropertyDeclarationsFromType(
        INamedTypeSymbol typeSymbol,
        List<PropertyDeclarationSyntax> propertyDeclarations,
        Compilation compilation,
        INamedTypeSymbol contextClassSymbol)
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
                    // 对于泛型类型参数，我们需要创建一个修改后的语法节点来反映具体类型
                    var resolvedPropertySyntax = ResolvePropertyTypeInSyntax(propertySyntax, propertySymbol, contextClassSymbol, compilation);

                    // 检查是否已经存在同名属性（避免重写属性重复）
                    if (!propertyDeclarations.Any(p => p.Identifier.ValueText == resolvedPropertySyntax.Identifier.ValueText))
                    {
                        propertyDeclarations.Add(resolvedPropertySyntax);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 解析属性语法节点中的类型，将泛型类型参数替换为具体类型
    /// </summary>
    private static PropertyDeclarationSyntax ResolvePropertyTypeInSyntax(
        PropertyDeclarationSyntax propertySyntax,
        IPropertySymbol propertySymbol,
        INamedTypeSymbol contextClassSymbol,
        Compilation compilation)
    {
        // 如果属性类型不是泛型类型参数，直接返回原语法节点
        if (!(propertySymbol.Type is ITypeParameterSymbol))
        {
            return propertySyntax;
        }

        // 解析具体的类型名称
        var resolvedTypeName = ResolvePropertyType(propertySymbol, contextClassSymbol);
        if (resolvedTypeName == null || resolvedTypeName == propertySymbol.Type.ToDisplayString())
        {
            return propertySyntax;
        }

        // 创建新的类型语法节点
        var newTypeSyntax = SyntaxFactory.ParseTypeName(resolvedTypeName)
            .WithLeadingTrivia(propertySyntax.Type.GetLeadingTrivia())
            .WithTrailingTrivia(propertySyntax.Type.GetTrailingTrivia());

        // 替换类型节点
        return propertySyntax.WithType(newTypeSyntax);
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

        while (currentType != null && currentType.SpecialType != SpecialType.System_Object)
        {
            // 检查当前类型是否实现了包含该类型参数的泛型基类
            var resolvedType = ResolveTypeParameterFromBaseChain(typeParameter, currentType);
            if (resolvedType != null)
            {
                return resolvedType;
            }

            currentType = currentType.BaseType;
        }

        return null;
    }

    private static string ResolveTypeParameterFromBaseChain(ITypeParameterSymbol typeParameter, INamedTypeSymbol currentType)
    {
        if (currentType == null || currentType.SpecialType == SpecialType.System_Object)
            return null;

        // 检查当前类型的所有基类
        var baseType = currentType.BaseType;
        while (baseType != null && baseType.SpecialType != SpecialType.System_Object)
        {
            // 如果基类是泛型类型，检查类型参数映射
            if (baseType.IsGenericType)
            {
                var originalDefinition = baseType.OriginalDefinition;
                var typeParameters = originalDefinition.TypeParameters;

                // 在基类的类型参数中查找匹配
                for (int i = 0; i < typeParameters.Length; i++)
                {
                    if (typeParameters[i].Name == typeParameter.Name)
                    {
                        // 找到匹配的类型参数，返回对应的具体类型
                        if (i < baseType.TypeArguments.Length)
                        {
                            return baseType.TypeArguments[i].ToDisplayString();
                        }
                    }
                }

                // 递归检查基类的类型参数中是否包含我们要找的类型参数
                for (int i = 0; i < baseType.TypeArguments.Length; i++)
                {
                    if (baseType.TypeArguments[i] is ITypeParameterSymbol baseTypeParam)
                    {
                        // 如果基类类型参数本身也是类型参数，需要继续解析
                        if (baseTypeParam.Name == typeParameter.Name)
                        {
                            // 在当前类型的类型参数中查找对应的具体类型
                            if (currentType.IsGenericType && i < currentType.TypeArguments.Length)
                            {
                                return currentType.TypeArguments[i].ToDisplayString();
                            }
                        }
                    }
                }
            }

            // 检查当前类型是否直接实现了包含该类型参数的接口
            foreach (var interfaceType in currentType.Interfaces)
            {
                if (interfaceType.IsGenericType)
                {
                    var originalInterface = interfaceType.OriginalDefinition;
                    var interfaceTypeParameters = originalInterface.TypeParameters;

                    for (int i = 0; i < interfaceTypeParameters.Length; i++)
                    {
                        if (interfaceTypeParameters[i].Name == typeParameter.Name)
                        {
                            if (i < interfaceType.TypeArguments.Length)
                            {
                                return interfaceType.TypeArguments[i].ToDisplayString();
                            }
                        }
                    }
                }
            }

            baseType = baseType.BaseType;
        }

        return null;
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
            else if (arg is INamedTypeSymbol namedTypeArg && namedTypeArg.IsGenericType)
            {
                // 递归处理嵌套的泛型类型
                return ResolveConstructedGenericType(namedTypeArg, contextClassSymbol);
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
    #endregion
}