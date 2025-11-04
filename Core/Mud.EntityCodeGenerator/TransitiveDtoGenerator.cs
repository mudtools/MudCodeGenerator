using Microsoft.CodeAnalysis.Diagnostics;
using Mud.EntityCodeGenerator.Helper;
using System.Collections.ObjectModel;

namespace Mud.EntityCodeGenerator;

/// <summary>
/// DTO代码生成基类。
/// </summary>
public abstract class TransitiveDtoGenerator : TransitiveCodeGenerator, IIncrementalGenerator
{
    /// <summary>
    /// DTO生成特性。
    /// </summary>
    protected const string DtoGeneratorAttributeName = "DtoGeneratorAttribute";

    /// <summary>
    /// 配置管理器实例。
    /// </summary>
    protected readonly GeneratorConfiguration Configuration = new GeneratorConfiguration();

    /// <summary>
    /// 是否生成实体类映射方法属性。
    /// </summary>
    protected const string DtoGeneratorAttributeGenMapMethod = "GenMapMethod";

    /// <summary>
    /// 是否生成VO类属性。
    /// </summary>
    protected const string DtoGeneratorAttributeGenVoClass = "GenVoClass";
    /// <summary>
    /// 是否生成QueryInput类属性。
    /// </summary>
    protected const string DtoGeneratorAttributeGenQueryInputClass = "GenQueryInputClass";
    /// <summary>
    /// 是否生成BO类属性。
    /// </summary>
    protected const string DtoGeneratorAttributeGenBoClass = "GenBoClass";
    /// <summary>
    /// DTO生成的命名空间。
    /// </summary>
    protected const string DtoGeneratorAttributeDtoNamespace = "DtoNamespace";
    /// <summary>
    /// 识别实体主键特性。
    /// </summary>
    protected readonly string[] PrimaryAttributes = new[] { "KeyAttribute", "Column|IsPrimary" };

    /// <summary>
    /// 获取类后缀
    /// </summary>
    protected override string ClassSuffix => GetConfiguredClassSuffix();

    /// <summary>
    /// 获取配置的类后缀
    /// </summary>
    /// <returns>配置的后缀名</returns>
    protected virtual string GetConfiguredClassSuffix()
    {
        return "";
    }

    /// <summary>
    /// 获取生成类的继承类。
    /// </summary>
    protected virtual string GetInheritClass(ClassDeclarationSyntax classNode) => "";

    /// <summary>
    /// 获取用户生成实体的属性。
    /// </summary>
    /// <returns></returns>
    protected virtual string[] GetPropertyAttributes() => [];

    /// <summary>
    /// 获取VO类属性配置。
    /// </summary>
    /// <returns></returns>
    protected string[] GetVoPropertyAttributes() => ConfigurationManager.Instance.GetPropertyAttributes("vo");

    /// <summary>
    /// 获取BO类属性配置。
    /// </summary>
    /// <returns></returns>
    protected string[] GetBoPropertyAttributes() => ConfigurationManager.Instance.GetPropertyAttributes("bo");

    /// <inheritdoc/>
    public override void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // 获取所有带有DtoGeneratorAttribute的类
        var classDeclarationProvider = GetClassDeclarationProvider(context, [DtoGeneratorAttributeName]);
        var compilationAndOptionsProvider = context.CompilationProvider
                                          .Combine(context.AnalyzerConfigOptionsProvider)
                                          .Select((s, _) => s);

        var providers = classDeclarationProvider.Combine(compilationAndOptionsProvider);
        // 生成DTO类
        context.RegisterSourceOutput(providers, (context, provider) =>
        {
            try
            {
                var (compiler, analyzer) = provider.Right;

                InitEntityPrefixValue(analyzer.GlobalOptions);
                //Debugger.Launch();

                // 读取并初始化所有配置项
                ReadConfigurationOptions(analyzer.GlobalOptions);

                var classes = provider.Left;
                foreach (var classNode in classes)
                {
                    GenerateCode(context, compiler, classNode);

                }
            }
            catch (Exception ex)
            {
                // 提高容错性，报告初始化错误
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.DtoInitializationError,
                    Location.None,
                    ex.Message));
            }
        });
    }

    /// <summary>
    /// 自动生成DTO类代码。
    /// </summary>
    /// <param name="context"><see cref="SourceProductionContext"/>对象</param>
    /// <param name="compilation"><see cref="Compilation"/>对象</param>
    /// <param name="orgClassDeclaration">原始的类声明语法<see cref="ClassDeclarationSyntax"/>对象。</param>
    protected abstract void GenerateCode(SourceProductionContext context, Compilation compilation, ClassDeclarationSyntax orgClassDeclaration);


    /// <summary>
    /// 读取项目配置选项并初始化内部字段。
    /// </summary>
    /// <param name="options">分析器配置选项</param>
    private void ReadConfigurationOptions(AnalyzerConfigOptions options)
    {
        ConfigurationManager.Instance.Initialize(options);
    }

    /// <summary>
    /// 获取生成DTO类所在的命名空间。
    /// </summary>
    /// <param name="orgClassDeclaration">原始的类声明语法<see cref="ClassDeclarationSyntax"/>对象。</param>
    /// <returns></returns>
    protected virtual string GetDtoNamespaceName(ClassDeclarationSyntax orgClassDeclaration)
    {
        // 提高容错性，处理空对象情况
        if (orgClassDeclaration == null)
            return "Dto";

        var cNamespace = GetNamespaceName(orgClassDeclaration);
        var dtoNamespace = SyntaxHelper.GetClassAttributeValues(orgClassDeclaration, DtoGeneratorAttributeName, DtoGeneratorAttributeDtoNamespace, "Dto");
        if (dtoNamespace.StartsWith(".", StringComparison.OrdinalIgnoreCase))
        {
            dtoNamespace = cNamespace + dtoNamespace;
        }
        else if (!string.IsNullOrEmpty(cNamespace))
        {
            dtoNamespace = cNamespace + "." + dtoNamespace;
        }
        return dtoNamespace;
    }

    /// <summary>
    /// 构建需要生成的类对象、命名空间名及类名。
    /// </summary>
    /// <param name="orgClassDeclaration">原始类的语法对象。</param>
    /// <returns></returns>
    protected (ClassDeclarationSyntax? classDeclaration, string namespaceName, string className) BuildLocalClass(ClassDeclarationSyntax orgClassDeclaration)
    {
        // 提高容错性，处理空对象情况
        if (orgClassDeclaration == null)
            return (null, "", "");
        var dtoNamespace = GetDtoNamespaceName(orgClassDeclaration);
        var dtoClassName = GetGeneratorClassName(orgClassDeclaration);
        var localClass = BuildLocalClass(orgClassDeclaration, dtoClassName);
        return (localClass, dtoNamespace, dtoClassName);
    }

    /// <summary>
    /// 通用成员处理器，用于处理类成员并执行指定的操作
    /// </summary>
    /// <typeparam name="T">成员类型</typeparam>
    /// <param name="orgClassDeclaration">原始类的语法对象</param>
    /// <param name="compilation">编译上下文</param>
    /// <param name="memberProcessor">成员处理委托</param>
    /// <param name="primaryKeyOnly">是否只处理主键属性</param>
    protected void ProcessMembers<T>(
        ClassDeclarationSyntax orgClassDeclaration,
        Compilation compilation,
        Action<T, string, string> memberProcessor,
        bool? primaryKeyOnly = null) where T : MemberDeclarationSyntax
    {
        MemberProcessor.ProcessMembers<T>(
            orgClassDeclaration,
            compilation,
            (member, orgPropertyName, propertyName, propertyType) =>
            {
                memberProcessor(member, orgPropertyName, propertyName);
            },
            primaryKeyOnly,
            IsIgnoreGenerator,
            IsPrimary,
            GetPropertyNames,
            GetPropertyType);
    }

    /// <summary>
    /// 通用成员处理器（包含属性类型），用于处理类成员并执行指定的操作
    /// </summary>
    /// <typeparam name="T">成员类型</typeparam>
    /// <param name="orgClassDeclaration">原始类的语法对象</param>
    /// <param name="compilation">编译上下文</param>
    /// <param name="memberProcessor">成员处理委托</param>
    /// <param name="primaryKeyOnly">是否只处理主键属性</param>
    protected void ProcessMembers<T>(
        ClassDeclarationSyntax orgClassDeclaration,
        Compilation compilation,
        Action<T, string, string, string> memberProcessor,
        bool? primaryKeyOnly = null) where T : MemberDeclarationSyntax
    {
        MemberProcessor.ProcessMembers<T>(
            orgClassDeclaration,
            compilation,
            memberProcessor,
            primaryKeyOnly,
            IsIgnoreGenerator,
            IsPrimary,
            GetPropertyNames,
            GetPropertyType);
    }

    /// <summary>
    /// 获取属性的原始名称和生成器名称
    /// </summary>
    protected (string orgPropertyName, string propertyName) GetPropertyNames<T>(T member) where T : MemberDeclarationSyntax
    {
        string orgPropertyName = "";
        string propertyName = "";

        if (member is PropertyDeclarationSyntax property)
        {
            orgPropertyName = GetPropertyName(property);
        }
        else if (member is FieldDeclarationSyntax field)
        {
            orgPropertyName = GetFirstUpperPropertyName(field);
        }

        // 统一使用GetGeneratorProperty返回的属性名
        propertyName = GetGeneratorProperty(member).propertyName;

        return (orgPropertyName, propertyName);
    }

    /// <summary>
    /// 类代码生成操作核心方法（依据<see cref="FieldDeclarationSyntax"/>生成）。
    /// </summary>
    /// <param name="orgClassDeclaration">原始类的语法对象。</param>
    /// <param name="localClass">本地类的语法对象。</param>
    /// <param name="genAttributeFunc">属性生成委托函数。</param>
    /// <param name="genExtAttributeFunc">扩展属性生成委托函数。</param>
    protected ClassDeclarationSyntax? BuildLocalClassProperty<T>(
        ClassDeclarationSyntax orgClassDeclaration,
        ClassDeclarationSyntax localClass,
        Compilation compilation,
        HashSet<string> propertes,
        Func<T, PropertyDeclarationSyntax?> genAttributeFunc,
        Func<T, PropertyDeclarationSyntax?>? genExtAttributeFunc = null)
         where T : MemberDeclarationSyntax
    {
        // 提高容错性，处理空对象情况
        if (orgClassDeclaration == null)
            return null;
        if (localClass == null)
            return null;

        ProcessMembers<T>(orgClassDeclaration, compilation, (member, orgPropertyName, propertyName) =>
        {
            //调用生成属性委托。
            if (genAttributeFunc != null)
            {
                var propertyDeclaration = genAttributeFunc(member);
                if (propertyDeclaration == null)
                    return;
                if (propertes.Contains(propertyName))
                    return;
                propertes.Add(propertyName);
                var attris = GetPropertyAttributes();
                if (attris.Any())
                {
                    var attributeList = GetAttributes(member, attris);
                    if (attributeList.Any())
                    {
                        var separatedAttributes = SyntaxFactory.SeparatedList(attributeList);
                        var attributeListSyntax = SyntaxFactory.AttributeList(separatedAttributes);
                        propertyDeclaration = propertyDeclaration.AddAttributeLists(attributeListSyntax);
                    }
                }
                //生成属性注释。
                var orgClassName = SyntaxHelper.GetClassName(orgClassDeclaration);

                // 为每个属性添加注释
                var inheritdoc = SyntaxFactory.ParseLeadingTrivia($"/// <inheritdoc cref=\"{orgClassName}.{orgPropertyName}\"/>\n");
                propertyDeclaration = propertyDeclaration.WithLeadingTrivia(inheritdoc);

                localClass = localClass.AddMembers(propertyDeclaration);

                //生成附加属性的方法
                if (genExtAttributeFunc != null)
                {
                    var extPropertyDeclaration = genExtAttributeFunc(member);
                    if (extPropertyDeclaration == null)
                        return;

                    // 为扩展属性也添加注释
                    var extInheritdoc = SyntaxFactory.ParseLeadingTrivia($"/// <inheritdoc cref=\"{orgClassName}.{orgPropertyName}\"/>\n");
                    extPropertyDeclaration = extPropertyDeclaration.WithLeadingTrivia(extInheritdoc);

                    localClass = localClass.AddMembers(extPropertyDeclaration);
                }
            }
        });

        // 不再在这里调用NormalizeWhitespace，避免覆盖手动添加的换行
        return localClass;
    }

    /// <summary>
    /// 生成本地类，也就是需要生成的类。
    /// </summary>
    /// <param name="localClassName">生成的类名。</param>
    /// <param name="orgClassDeclaration">原始类的语法<see cref="ClassDeclarationSyntax"/>对象。</param>
    /// <param name="isAttachAttribute">类上是否绑定默认注解</param>
    /// <returns></returns>
    protected ClassDeclarationSyntax BuildLocalClass(ClassDeclarationSyntax orgClassDeclaration, string localClassName, bool isAttachAttribute = true)
    {
        // 提高容错性，确保类名不为空
        if (string.IsNullOrEmpty(localClassName))
        {
            localClassName = "GeneratedClass";
        }

        var localClass = SyntaxFactory.ClassDeclaration(localClassName)
                                      .WithModifiers(SyntaxFactory.TokenList(
                                                 SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                                                 SyntaxFactory.Token(SyntaxKind.PartialKeyword)));

        //在类上加入自定义注解。
        if (isAttachAttribute && ConfigurationManager.Instance.Configuration.EntityAttachAttributes != null && ConfigurationManager.Instance.Configuration.EntityAttachAttributes.Any())
        {
            List<AttributeSyntax> attributeSyntaxs = new List<AttributeSyntax>();
            foreach (var attribute in ConfigurationManager.Instance.Configuration.EntityAttachAttributes)
            {
                // 提高容错性，跳过空的特性名称
                if (string.IsNullOrWhiteSpace(attribute))
                    continue;

                var newAttribute = SyntaxFactory.Attribute(SyntaxFactory.IdentifierName(attribute.Trim()));
                attributeSyntaxs.Add(newAttribute);
            }
            // 只有当有有效特性时才添加特性列表
            if (attributeSyntaxs.Any())
            {
                var attributeList = SyntaxFactory.AttributeList(
                                SyntaxFactory.SeparatedList(attributeSyntaxs));
                localClass = localClass.AddAttributeLists(attributeList);
            }
        }
        //在类上加上注释。
        var commentTrivia = orgClassDeclaration?.GetLeadingTrivia();
        if (commentTrivia != null)
            localClass = localClass.WithLeadingTrivia(commentTrivia);

        //添加继承的类。
        var inheritClass = GetInheritClass(orgClassDeclaration);
        if (!string.IsNullOrEmpty(inheritClass))
        {
            var baseType = SyntaxFactory.SimpleBaseType(SyntaxFactory.IdentifierName(inheritClass));
            localClass = localClass.WithBaseList(SyntaxFactory.BaseList(SyntaxFactory.SingletonSeparatedList<BaseTypeSyntax>(baseType)));
        }

        return localClass;
    }

    /// <summary>
    /// 获取属性上注解内容。
    /// </summary>
    /// <param name="propertyDeclaration">属性声明语法节点</param>
    /// <param name="attributes">要查找的特性名称数组</param>
    /// <returns>匹配的特性语法节点只读集合</returns>
    protected ReadOnlyCollection<AttributeSyntax> GetAttributes<T>(T propertyDeclaration, string[] attributes)
        where T : MemberDeclarationSyntax
    {
        var list = new List<AttributeSyntax>();
        if (propertyDeclaration == null)
            return new ReadOnlyCollection<AttributeSyntax>(list);

        // 提高容错性，检查attributes参数
        if (attributes == null || attributes.Length == 0)
            return new ReadOnlyCollection<AttributeSyntax>(list);

        // 获取所有特性
        var attrs = propertyDeclaration.AttributeLists
            .SelectMany(al => al.Attributes)
            .ToList();
        if (!attrs.Any())
            return new ReadOnlyCollection<AttributeSyntax>(list);

        // 过滤出匹配的特性
        List<AttributeSyntax>? propertyAttributes = null;

        if (propertyDeclaration is FieldDeclarationSyntax)//获取字段上的注解。
        {
            propertyAttributes = attrs
                                .Where(attr => IsRequiredAttribute(attr, attributes))
                                .ToList();
        }
        else//获取属性上的注解。
        {
            propertyAttributes = attrs
                                .Where(attr => IsPropertyAttribute(attr, attributes))
                                .ToList();
        }

        if (propertyAttributes == null || !propertyAttributes.Any())
            return new ReadOnlyCollection<AttributeSyntax>(list);

        return new ReadOnlyCollection<AttributeSyntax>(propertyAttributes);
    }

    /// <summary>
    /// 检查是否为所需的字段特性（带有property目标的特性）
    /// </summary>
    private bool IsRequiredAttribute(AttributeSyntax attribute, string[] attributes)
    {
        // 提高容错性，添加空值检查
        if (attribute?.Name == null || attributes == null)
            return false;

        if (attribute.Parent != null && attribute.Parent is AttributeListSyntax parent)
        {
            var x = parent.Target?.ToString();
            return parent.Target?.ToString()?.StartsWith("property", StringComparison.OrdinalIgnoreCase) == true &&
                   IsAttributeNameMatch(attribute, attributes);
        }
        return false;
    }

    /// <summary>
    /// 检查是否为所需的属性特性
    /// </summary>
    private bool IsPropertyAttribute(AttributeSyntax attribute, string[] attributes)
    {
        if (attribute?.Name == null || attributes == null)
            return false;

        return IsAttributeNameMatch(attribute, attributes);
    }

    /// <summary>
    /// 检查特性名称是否匹配给定的特性名称数组
    /// </summary>
    private bool IsAttributeNameMatch(AttributeSyntax attribute, string[] attributes)
    {
        if (attribute.Name == null || attributes == null)
            return false;

        string attributeName;

        // 处理不同类型的特性名称语法
        if (attribute.Name is IdentifierNameSyntax identifierName)
        {
            attributeName = identifierName.Identifier.Text;
        }
        else if (attribute.Name is QualifiedNameSyntax qualifiedName)
        {
            // 对于限定名称，取最后的标识符
            attributeName = qualifiedName.Right.Identifier.Text;
        }
        else
        {
            // 其他情况，使用字符串表示
            attributeName = attribute.Name.ToString();
        }

        return attributes.Contains(attributeName, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 是否忽略生成属性。
    /// </summary>
    /// <param name="memberDeclaration"></param>
    /// <returns></returns>
    protected bool IsIgnoreGenerator<T>(T memberDeclaration)
        where T : MemberDeclarationSyntax
    {
        // 提高容错性，处理空对象情况
        if (memberDeclaration == null)
            return true; // 空对象默认忽略生成

        var attributes = AttributeSyntaxHelper.GetAttributeSyntaxes(memberDeclaration, IgnoreGeneratorAttribute);
        return attributes != null && attributes.Any();
    }


    /// <summary>
    /// 判断属性是否为主键属性。
    /// </summary>
    /// <param name="memberDeclaration"></param>
    /// <returns></returns>
    protected bool IsPrimary<T>(T memberDeclaration)
        where T : MemberDeclarationSyntax
    {
        // 提高容错性，处理空对象情况
        if (memberDeclaration == null)
            return false;

        var isPrimary = false;
        // 提高容错性，检查PrimaryAttributes
        if (PrimaryAttributes == null)
            return false;

        foreach (var attr in PrimaryAttributes)
        {
            if (isPrimary)
                return true;

            // 提高容错性，处理attr为空的情况
            if (string.IsNullOrEmpty(attr))
                continue;

            var ps = attr.Split('|');
            if (ps == null || ps.Length < 1)
                continue;
            var attributes = AttributeSyntaxHelper.GetAttributeSyntaxes(memberDeclaration, ps[0]);
            if (ps.Length > 1)
            {
                isPrimary = AttributeSyntaxHelper.GetAttributeValue(attributes, ps[1], false);
            }
            else
            {
                if (attributes != null && attributes.Any())
                    isPrimary = true;
                else
                    isPrimary = false;
            }
        }
        return isPrimary;
    }

    /// <summary>
    /// 获取需要生成类名。
    /// </summary>
    /// <param name="classNode"></param>
    /// <param name="classSuffix">类的后缀</param>
    /// <returns></returns>
    protected string GetGeneratorClassName(ClassDeclarationSyntax classNode, string classSuffix = "")
    {
        // 提高容错性，处理空对象情况
        if (classNode == null)
            return "GeneratedClass";

        if (string.IsNullOrEmpty(classSuffix))
            classSuffix = ClassSuffix ?? ""; // 提高容错性，处理ClassSuffix为空的情况

        var className = SyntaxHelper.GetClassName(classNode).Replace(EntitySuffix ?? "", ""); // 提高容错性，处理EntitySuffix为空的情况
        var dtoClassName = $"{className}{classSuffix}";
        return dtoClassName;
    }

    /// <summary>
    /// 获取属性的原始名称（首字母大写）
    /// </summary>
    protected (string orgPropertyName, string propertyName) GetBuilderPropertyNames<T>(T member) where T : MemberDeclarationSyntax
    {
        string orgPropertyName = "";

        if (member is PropertyDeclarationSyntax property)
        {
            orgPropertyName = GetPropertyName(property);
        }
        else if (member is FieldDeclarationSyntax field)
        {
            orgPropertyName = GetFirstUpperPropertyName(field);
        }

        // BuilderGenerator 始终使用原始属性名（首字母大写）
        string propertyName = orgPropertyName;

        return (orgPropertyName, propertyName);
    }

    /// <summary>
    /// 根据原始的<see cref="FieldDeclarationSyntax"/>对象生成新的属性对象。
    /// </summary>
    /// <param name="member"></param>
    /// <param name="methBody">是否生成set、get方法体</param>
    /// <returns></returns>
    protected PropertyDeclarationSyntax? BuildProperty(FieldDeclarationSyntax member, bool methBody = true)
    {
        // 提高容错性，处理空对象情况
        if (member == null)
            return null;

        var (propertyName, propertyType) = GetGeneratorProperty(member);
        // 提高容错性，检查属性名和类型
        if (string.IsNullOrEmpty(propertyName) || string.IsNullOrEmpty(propertyType))
            return null;

        var varName = GetFieldName(member);
        if (methBody)
        {
            // 统一使用GetGeneratorProperty返回的属性名，确保大小写一致性
            return BuildProperty(propertyName, propertyType, varName);
        }
        // 统一使用GetGeneratorProperty返回的属性名，确保大小写一致性
        return BuildProperty(propertyName, propertyType);
    }


    /// <summary>
    /// 根据原始的<see cref="PropertyDeclarationSyntax"/>对象生成新的属性对象。
    /// </summary>
    /// <param name="member"></param>
    /// <returns></returns>
    protected PropertyDeclarationSyntax? BuildProperty(PropertyDeclarationSyntax member)
    {
        // 提高容错性，处理空对象情况
        if (member == null)
            return null;

        var (propertyName, propertyType) = GetGeneratorProperty(member);
        // 提高容错性，检查属性名和类型
        if (string.IsNullOrEmpty(propertyName) || string.IsNullOrEmpty(propertyType))
            return null;

        return BuildProperty(propertyName, propertyType);
    }

    /// <summary>
    /// 根据属性名及属性类型生成<see cref="PropertyDeclarationSyntax"/>属性。
    /// </summary>
    /// <param name="propertyName">属性名。</param>
    /// <param name="propertyType">属性类型。</param>
    /// <returns></returns>
    protected PropertyDeclarationSyntax BuildProperty(string propertyName, string propertyType)
    {
        // 提高容错性，确保参数不为空
        if (string.IsNullOrEmpty(propertyName))
            propertyName = "Property";
        if (string.IsNullOrEmpty(propertyType))
            propertyType = "object";

        var propertyDeclaration = SyntaxFactory.PropertyDeclaration(
                   SyntaxFactory.ParseTypeName(propertyType), propertyName)
                   .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword))) // 设置访问修饰符为 public
                   .WithAccessorList(SyntaxFactory.AccessorList(SyntaxFactory.List(new[]
                   {
                       SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration) // 添加 get 访问器
                               .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                           SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration) // 添加 set 访问器
                               .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                   })));
        return propertyDeclaration;
    }

    /// <summary>
    /// 根据属性名、属性类型及私有字段生成<see cref="PropertyDeclarationSyntax"/>属性。
    /// </summary>
    /// <param name="propertyName">属性名。</param>
    /// <param name="propertyType">属性类型。</param>
    /// <param name="priFieldName">私有字段</param>
    /// <returns></returns>
    protected PropertyDeclarationSyntax BuildProperty(
        string propertyName,
        string propertyType,
        string priFieldName)
    {
        // 提高容错性，确保参数不为空
        if (string.IsNullOrEmpty(propertyName))
            propertyName = "Property";
        if (string.IsNullOrEmpty(propertyType))
            propertyType = "object";
        if (string.IsNullOrEmpty(priFieldName))
            priFieldName = "_" + propertyName;

        // 创建get访问器
        var getAccessor = SyntaxFactory
            .AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
            .WithBody(
                SyntaxFactory.Block(
                    SyntaxFactory.SingletonList<StatementSyntax>(
                        SyntaxFactory.ReturnStatement(
                            SyntaxFactory.IdentifierName(priFieldName)
                        )
                    )
                )
            );

        // 创建set访问器
        var setAccessor = SyntaxFactory
            .AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
            .WithBody(
                SyntaxFactory.Block(
                    SyntaxFactory.SingletonList<StatementSyntax>(
                        SyntaxFactory.ExpressionStatement(
                            SyntaxFactory.AssignmentExpression(
                                SyntaxKind.SimpleAssignmentExpression,
                                SyntaxFactory.IdentifierName(priFieldName),
                                SyntaxFactory.IdentifierName("value")
                            )
                        )
                    )
                )
            );

        var propertyDeclaration = SyntaxFactory.PropertyDeclaration(
                   SyntaxFactory.ParseTypeName(propertyType), propertyName)
                   .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword))) // 设置访问修饰符为 public
                   .WithAccessorList(SyntaxFactory.AccessorList(SyntaxFactory.List(
                   [
                       getAccessor,
                       setAccessor
                   ])));
        return propertyDeclaration;
    }


    /// <summary>
    /// 获取成员的类型
    /// </summary>
    protected string GetPropertyType<T>(T member) where T : MemberDeclarationSyntax
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