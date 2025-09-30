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
    /// 在实体类上绑定的特性。
    /// </summary>
    private string[] EntityAttachAttributes = [];

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
    protected readonly string[] PrimaryAttributes = ["KeyAttribute", "Column|IsPrimary"];

    /// <summary>
    /// 获取生成类的继承类。
    /// </summary>
    protected virtual string GetInheritClass(ClassDeclarationSyntax classNode) => "";

    /// <summary>
    /// 获取用户生成实体的属性。
    /// </summary>
    /// <returns></returns>
    protected virtual string[] GetPropertyAttributes() => [];

    /// <inheritdoc/>
    public override void Initialize(IncrementalGeneratorInitializationContext context)
    {
        //Debugger.Launch();
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

                ProjectConfigHelper.ReadProjectOptions(analyzer.GlobalOptions, "build_property.EntityAttachAttributes", val => EntityAttachAttributes = val.Split(','), "");

                var classes = provider.Left;
                foreach (var classNode in classes)
                {
                    try
                    {
                        GenerateCode(context, classNode);
                    }
                    catch (Exception ex)
                    {
                        // 提高容错性，即使单个类生成失败也不影响其他类
                        context.ReportDiagnostic(Diagnostic.Create(
                            new DiagnosticDescriptor(
                                "DTO001",
                                "DTO代码生成错误",
                                $"生成类 {GetClassName(classNode)} 时发生错误: {ex.Message}",
                                "代码生成",
                                DiagnosticSeverity.Error,
                                true),
                            Location.None));
                    }
                }
            }
            catch (Exception ex)
            {
                // 提高容错性，报告初始化错误
                context.ReportDiagnostic(Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "DTO002",
                        "DTO代码生成初始化错误",
                        $"初始化DTO代码生成器时发生错误: {ex.Message}",
                        "代码生成",
                        DiagnosticSeverity.Error,
                        true),
                    Location.None));
            }
        });
    }

    /// <summary>
    /// 自动生成DTO类代码。
    /// </summary>
    /// <param name="context"><see cref="SourceProductionContext"/>对象</param>
    /// <param name="orgClassDeclaration">原始的类声明语法<see cref="ClassDeclarationSyntax"/>对象。</param>
    protected abstract void GenerateCode(SourceProductionContext context, ClassDeclarationSyntax orgClassDeclaration);

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
    protected (ClassDeclarationSyntax? classDeclaration, string namespaceName, string className) GenLocalClass(ClassDeclarationSyntax orgClassDeclaration)
    {
        // 提高容错性，处理空对象情况
        if (orgClassDeclaration == null)
            return (null, "", "");
        var dtoNamespace = GetDtoNamespaceName(orgClassDeclaration);
        var dtoClassName = GetGeneratorClassName(orgClassDeclaration);
        var localClass = GenLocalClass(orgClassDeclaration, dtoClassName);
        return (localClass, dtoNamespace, dtoClassName);
    }

    /// <summary>
    /// 类代码生成操作核心方法（依据<see cref="FieldDeclarationSyntax"/>生成）。
    /// </summary>
    /// <param name="orgClassDeclaration">原始类的语法对象。</param>
    /// <param name="localClass">本地类的语法对象。</param>
    /// <param name="genAttributeFunc">属性生成委托函数。</param>
    /// <param name="genExtAttributeFunc">扩展属性生成委托函数。</param>
    protected ClassDeclarationSyntax? GenLocalClassProperty<T>(
        ClassDeclarationSyntax orgClassDeclaration,
         ClassDeclarationSyntax localClass,
        Func<T, PropertyDeclarationSyntax?> genAttributeFunc,
        Func<T, PropertyDeclarationSyntax?>? genExtAttributeFunc = null)
         where T : MemberDeclarationSyntax
    {
        // 提高容错性，处理空对象情况
        if (orgClassDeclaration == null)
            return null;
        if (localClass == null)
            return null;

        //循环添加类的成员属性。
        foreach (var member in orgClassDeclaration.Members.OfType<T>())
        {
            try
            {
                //调用生成属性委托。
                if (genAttributeFunc != null)
                {
                    var propertyDeclaration = genAttributeFunc(member);
                    if (propertyDeclaration == null)
                        continue;

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
                    var leadingTrivia = member.GetLeadingTrivia();
                    if (leadingTrivia != null)
                    {
                        propertyDeclaration = propertyDeclaration.WithLeadingTrivia(leadingTrivia);
                    }
                    localClass = localClass.AddMembers(propertyDeclaration);

                    //生成附加属性的方法
                    if (genExtAttributeFunc != null)
                    {
                        var extPropertyDeclaration = genExtAttributeFunc(member);
                        if (extPropertyDeclaration == null)
                            continue;

                        if (leadingTrivia != null)
                        {
                            extPropertyDeclaration = extPropertyDeclaration.WithLeadingTrivia(leadingTrivia);
                        }

                        //代码待补充。
                        localClass = localClass.AddMembers(extPropertyDeclaration);
                    }
                }
            }
            catch (Exception ex)
            {
                // 提高容错性，即使单个属性生成失败也不影响其他属性
                System.Diagnostics.Debug.WriteLine($"生成属性时发生错误: {ex.Message}");
            }
        }
        return localClass;
    }

    /// <summary>
    /// 生成本地类，也就是需要生成的类。
    /// </summary>
    /// <param name="localClassName">生成的类名。</param>
    /// <param name="orgClassDeclaration">原始类的语法<see cref="ClassDeclarationSyntax"/>对象。</param>
    /// <param name="isAttachAttribute">类上是否绑定默认注解</param>
    /// <returns></returns>
    protected ClassDeclarationSyntax GenLocalClass(ClassDeclarationSyntax orgClassDeclaration, string localClassName, bool isAttachAttribute = true)
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
        if (isAttachAttribute && EntityAttachAttributes != null && EntityAttachAttributes.Any())
        {
            List<AttributeSyntax> attributeSyntaxs = [];
            foreach (var attribute in EntityAttachAttributes)
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
    /// <param name="propertyDeclaration"></param>
    /// <param name="attributes"></param>
    protected ReadOnlyCollection<AttributeSyntax> GetAttributes<T>(T propertyDeclaration, string[] attributes)
        where T : MemberDeclarationSyntax
    {
        List<AttributeSyntax> list = [];
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

        // 过滤出名称以 "property" 开头的特性
        var propertyAttributes = attrs
            .Where(attr => IsRequiredAttribute(attr, attributes))
            .ToList();

        if (!propertyAttributes.Any())
            return new ReadOnlyCollection<AttributeSyntax>(list);

        return new ReadOnlyCollection<AttributeSyntax>(propertyAttributes);
    }

    private bool IsRequiredAttribute(AttributeSyntax attribute, string[] attributes)
    {
        // 提高容错性，添加空值检查
        if (attribute?.Name == null || attributes == null)
            return false;

        if (attribute.Parent != null && attribute.Parent is AttributeListSyntax parent)
        {
            var x = parent.Target?.ToString();
            return parent.Target?.ToString()?.StartsWith("property", StringComparison.OrdinalIgnoreCase) == true &&
                   attribute.Name is IdentifierNameSyntax identifierName &&
                   attributes.Contains(identifierName.Identifier.Text, StringComparer.OrdinalIgnoreCase);
        }
        return false;
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

        var className = GetClassName(classNode).Replace(EntitySuffix ?? "", ""); // 提高容错性，处理EntitySuffix为空的情况
        var dtoClassName = $"{className}{classSuffix}";
        return dtoClassName;
    }

    /// <summary>
    /// 根据原始的<see cref="FieldDeclarationSyntax"/>对象生成新的属性对象。
    /// </summary>
    /// <param name="member"></param>
    /// <param name="methBody">是否生成set、get方法体</param>
    /// <returns></returns>
    protected PropertyDeclarationSyntax? GeneratorProperty(FieldDeclarationSyntax member, bool methBody = true)
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
            return GeneratorProperty(propertyName, propertyType, varName);
        }
        propertyName = ToLowerFirstLetter(propertyName);
        return GeneratorProperty(propertyName, propertyType);
    }


    /// <summary>
    /// 根据原始的<see cref="PropertyDeclarationSyntax"/>对象生成新的属性对象。
    /// </summary>
    /// <param name="member"></param>
    /// <returns></returns>
    protected PropertyDeclarationSyntax? GeneratorProperty(PropertyDeclarationSyntax member)
    {
        // 提高容错性，处理空对象情况
        if (member == null)
            return null;

        var (propertyName, propertyType) = GetGeneratorProperty(member);
        // 提高容错性，检查属性名和类型
        if (string.IsNullOrEmpty(propertyName) || string.IsNullOrEmpty(propertyType))
            return null;

        return GeneratorProperty(propertyName, propertyType);
    }

    /// <summary>
    /// 根据属性名及属性类型生成<see cref="PropertyDeclarationSyntax"/>属性。
    /// </summary>
    /// <param name="propertyName">属性名。</param>
    /// <param name="propertyType">属性类型。</param>
    /// <returns></returns>
    protected PropertyDeclarationSyntax GeneratorProperty(string propertyName, string propertyType)
    {
        // 提高容错性，确保参数不为空
        if (string.IsNullOrEmpty(propertyName))
            propertyName = "Property";
        if (string.IsNullOrEmpty(propertyType))
            propertyType = "object";

        var propertyDeclaration = SyntaxFactory.PropertyDeclaration(
                   SyntaxFactory.ParseTypeName(propertyType), propertyName)
                   .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword))) // 设置访问修饰符为 public
                   .WithAccessorList(SyntaxFactory.AccessorList(SyntaxFactory.List(
                    [
                        SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration) // 添加 get 访问器
                                .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                            SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration) // 添加 set 访问器
                                .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                    ])));
        return propertyDeclaration;
    }

    /// <summary>
    /// 根据属性名、属性类型及私有字段生成<see cref="PropertyDeclarationSyntax"/>属性。
    /// </summary>
    /// <param name="propertyName">属性名。</param>
    /// <param name="propertyType">属性类型。</param>
    /// <param name="priFieldName">私有字段</param>
    /// <returns></returns>
    protected PropertyDeclarationSyntax GeneratorProperty(string propertyName, string propertyType, string priFieldName)
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
}