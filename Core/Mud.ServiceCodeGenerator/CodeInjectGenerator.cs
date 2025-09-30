namespace Mud.ServiceCodeGenerator;

/// <summary>
/// 字段注入、日志注入、缓存注入、用户注入代码生成器。
/// </summary>
[Generator(LanguageNames.CSharp)]
public class CodeInjectGenerator : TransitiveCodeGenerator
{
    private const string ConstructorInjectAttributeName = "ConstructorInjectAttribute";
    private const string LoggerInjectAttributeName = "LoggerInjectAttribute";
    private const string OptionsInjectAttributeName = "OptionsInjectAttribute";
    private const string CacheManagerInjectAttributeName = "CacheInjectAttribute";
    private const string UserManagerInjectAttributeName = "UserInjectAttribute";
    private const string CustomInjectAttributeName = "CustomInjectAttribute";
    private const string loggerVariable = "_logger";

    /// <inheritdoc/>
    public override void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var generationInfo = GetClassDeclarationProvider(context, [ConstructorInjectAttributeName, LoggerInjectAttributeName, CacheManagerInjectAttributeName, UserManagerInjectAttributeName, CustomInjectAttributeName]);
        var compilationAndOptionsProvider = context.CompilationProvider
                                          .Combine(context.AnalyzerConfigOptionsProvider)
                                          .Select((s, _) => s);

        var providers = generationInfo.Combine(compilationAndOptionsProvider);

        context.RegisterSourceOutput(providers, (sourceContext, provider) =>
        {
            var classDeclarations = provider.Left;
            var (compilation, analyzer) = provider.Right;

            if (!classDeclarations.Any())//没有需要生成的代码则直接退出，提高编译性能。
                return;

            // 从项目配置中读取默认值
            string defaultCacheManagerType = "ICacheManager";
            string defaultUserManagerType = "IUserManager";
            string defaultLoggerVariable = "_logger";
            string defaultCacheManagerVariable = "_cacheManager";
            string defaultUserManagerVariable = "_userManager";

            ReadProjectOptions(analyzer.GlobalOptions, "build_property.DefaultCacheManagerType", val => defaultCacheManagerType = val, "ICacheManager");
            ReadProjectOptions(analyzer.GlobalOptions, "build_property.DefaultUserManagerType", val => defaultUserManagerType = val, "IUserManager");
            ReadProjectOptions(analyzer.GlobalOptions, "build_property.DefaultLoggerVariable", val => defaultLoggerVariable = val, "_logger");
            ReadProjectOptions(analyzer.GlobalOptions, "build_property.DefaultCacheManagerVariable", val => defaultCacheManagerVariable = val, "_cacheManager");
            ReadProjectOptions(analyzer.GlobalOptions, "build_property.DefaultUserManagerVariable", val => defaultUserManagerVariable = val, "_userManager");

            //Debugger.Launch();
            foreach (var classDeclaration in classDeclarations)
            {
                GenerateCode(sourceContext, classDeclaration, defaultCacheManagerType, defaultUserManagerType,
                    defaultLoggerVariable, defaultCacheManagerVariable, defaultUserManagerVariable);
            }
        });
    }

    private void GenerateCode(SourceProductionContext context, ClassDeclarationSyntax classDeclaration,
        string defaultCacheManagerType, string defaultUserManagerType,
        string defaultLoggerVariable, string defaultCacheManagerVariable, string defaultUserManagerVariable)
    {
        var className = GetClassName(classDeclaration);
        ParameterListSyntax parameterListSyntax = null;
        BlockSyntax constructorBody = null;
        List<FieldDeclarationSyntax> fieldDeclarationSyntaxs = [];
        var constructInject = GetAttributeSyntaxes(classDeclaration, ConstructorInjectAttributeName);
        var loggerInject = GetAttributeSyntaxes(classDeclaration, LoggerInjectAttributeName);
        var optionsInject = GetAttributeSyntaxes(classDeclaration, OptionsInjectAttributeName);
        var cacheManagerInject = GetAttributeSyntaxes(classDeclaration, CacheManagerInjectAttributeName);
        var userManagerInject = GetAttributeSyntaxes(classDeclaration, UserManagerInjectAttributeName);
        var customInject = GetAttributeSyntaxes(classDeclaration, CustomInjectAttributeName);

        #region 循环所有字段
        //注入普通定义字段的构造函数。
        if (constructInject.Any())
        {
            (parameterListSyntax, constructorBody) = GeneratorFieldInject(classDeclaration, className, parameterListSyntax, constructorBody);
        }
        //注入日志对象。
        if (loggerInject.Any())
        {
            (parameterListSyntax, constructorBody) = GeneratorLoggerInject(classDeclaration, className, parameterListSyntax, constructorBody);

            var fieldCode = $"private readonly ILogger<{className}> {defaultLoggerVariable};";
            var fieldDeclaration = GeneratorPrivateField(fieldCode);
            fieldDeclarationSyntaxs.Add(fieldDeclaration);
        }

        //注入缓存管理对象。
        if (cacheManagerInject.Any())
        {
            (parameterListSyntax, constructorBody) = GeneratorStandardInject(classDeclaration, className, defaultCacheManagerType, defaultCacheManagerVariable, parameterListSyntax, constructorBody);
            var fieldDeclaration = GeneratorPrivateField(defaultCacheManagerType, defaultCacheManagerVariable);
            fieldDeclarationSyntaxs.Add(fieldDeclaration);
        }
        //注入用户管理对象。
        if (userManagerInject.Any())
        {
            (parameterListSyntax, constructorBody) = GeneratorStandardInject(classDeclaration, className, defaultUserManagerType, defaultUserManagerVariable, parameterListSyntax, constructorBody);
            var fieldDeclaration = GeneratorPrivateField(defaultUserManagerType, defaultUserManagerVariable);
            fieldDeclarationSyntaxs.Add(fieldDeclaration);
        }
        //注入配置项对象。
        if (optionsInject.Any())
        {
            foreach (var option in optionsInject)
            {
                var variableType = option.GetPropertyValue("OptionType")?.ToString();
                if (string.IsNullOrWhiteSpace(variableType))
                    continue;
                var varName = option.GetPropertyValue("VarName")?.ToString();
                if (string.IsNullOrWhiteSpace(varName))
                    varName = PrivateFieldNamingHelper.GeneratePrivateFieldName(variableType, FieldNamingStyle.UnderscoreCamel);

                (parameterListSyntax, constructorBody) = GeneratorOptionsInject(classDeclaration, variableType, varName, parameterListSyntax, constructorBody);

                var fieldCode = $"private readonly IOptions<{variableType}> {varName};";
                var fieldDeclaration = GeneratorPrivateField(fieldCode);
                fieldDeclarationSyntaxs.Add(fieldDeclaration);
            }
        }

        if (customInject.Any())
        {
            foreach (var customType in customInject)
            {
                var variableType = customType.GetPropertyValue("VarType")?.ToString();
                var varName = customType.GetPropertyValue("VarName")?.ToString();
                if (string.IsNullOrWhiteSpace(variableType))
                    continue;
                if (string.IsNullOrWhiteSpace(varName))
                    varName = PrivateFieldNamingHelper.GeneratePrivateFieldName(variableType, FieldNamingStyle.UnderscoreCamel);

                (parameterListSyntax, constructorBody) = GeneratorStandardInject(classDeclaration, className, variableType, varName, parameterListSyntax, constructorBody);
                var fieldDeclaration = GeneratorPrivateField(variableType, varName);
                fieldDeclarationSyntaxs.Add(fieldDeclaration);

            }
        }
        #endregion

        if (parameterListSyntax == null || constructorBody == null)
            return;

        var compilationUnit = CreateCompilationUnitSyntax(className, classDeclaration, parameterListSyntax, constructorBody, fieldDeclarationSyntaxs);
        context.AddSource($"{className}.g.cs", compilationUnit);
    }

    /// <summary>
    /// 创建代码编译<see cref="CompilationUnitSyntax"/>对象。
    /// </summary>
    /// <param name="className">生成代码的类名。</param>
    /// <param name="classDeclaration">原有类</param>
    /// <param name="parameterListSyntax">构造函数参数体</param>
    /// <param name="constructorBody">构造函数体。</param>
    /// <param name="fieldDeclarationSyntaxs">私有字段集合。</param>
    /// <returns></returns>
    private CompilationUnitSyntax CreateCompilationUnitSyntax(
        string className, ClassDeclarationSyntax classDeclaration,
        ParameterListSyntax parameterListSyntax, BlockSyntax constructorBody,
        List<FieldDeclarationSyntax> fieldDeclarationSyntaxs)
    {
        // 创建构造函数声明
        var constructor = SyntaxFactory.ConstructorDeclaration(className)
                                       .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
                                       .WithParameterList(parameterListSyntax)
                                       .WithBody(constructorBody);

        var cNamespace = GetNamespaceName(classDeclaration);

        var localClass = SyntaxFactory.ClassDeclaration(className)
                           .WithModifiers(SyntaxFactory.TokenList(
                                         SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                                         SyntaxFactory.Token(SyntaxKind.PartialKeyword)));

        //在类上加上注释。
        var commentTrivia = classDeclaration.GetLeadingTrivia();
        if (commentTrivia != null)
        {
            localClass = localClass.WithLeadingTrivia(commentTrivia);
            constructor = constructor.WithLeadingTrivia(commentTrivia);
        }
        if (fieldDeclarationSyntaxs != null && fieldDeclarationSyntaxs.Count > 0)
        {
            localClass = localClass.AddMembers([.. fieldDeclarationSyntaxs]);
        }

        localClass = localClass.AddMembers(constructor);


        var compilationUnit = SyntaxFactory.CompilationUnit();
        var root = classDeclaration.SyntaxTree.GetCompilationUnitRoot();
        if (root.Usings.Count > 0)
        {
            foreach (var u in root.Usings)
            {
                compilationUnit = compilationUnit.AddUsings(u);
            }
        }

        compilationUnit = compilationUnit.AddMembers(SyntaxFactory.NamespaceDeclaration(SyntaxFactory.IdentifierName(cNamespace)).AddMembers(localClass))
                                         .NormalizeWhitespace()
                                         .AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.IdentifierName("System")))
                                         .AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.IdentifierName("Microsoft.Extensions.Logging")))
                                         .NormalizeWhitespace()
                                         .WithLeadingTrivia(SyntaxFactory.TriviaList([
                                                                           SyntaxFactory.Comment("// <auto-generated/>\n"),
                                                                                        SyntaxFactory.Comment("// 自动生成代码，请不要手动修改。\n"),
                                                                                        SyntaxFactory.Comment("// 生成时间: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\n"),
                                                                                        SyntaxFactory.Comment("")]));
        return compilationUnit;
    }

    #region 生成类的私有字段
    private FieldDeclarationSyntax GeneratorPrivateField(string className, string varName)
    {
        var fieldCode = $"private readonly {className} {varName};";
        return GeneratorPrivateField(fieldCode);
    }

    private FieldDeclarationSyntax GeneratorPrivateField(string fieldCode)
    {
        var tree = CSharpSyntaxTree.ParseText(fieldCode);
        var root = tree.GetRoot() as CompilationUnitSyntax;
        var fieldDeclaration = root.DescendantNodes().OfType<FieldDeclarationSyntax>().FirstOrDefault();
        return fieldDeclaration;
    }
    #endregion

    #region 生成构造函数的函数签名及函数体
    /// <summary>
    /// 生成日志注入代码。
    /// </summary>
    /// <param name="classDeclaration"></param>
    /// <param name="className"></param>
    /// <param name="parameterListSyntax"></param>
    /// <param name="constructorBody"></param>
    /// <returns></returns>
    private (ParameterListSyntax parameterListSyntax, BlockSyntax constructorBody)
        GeneratorLoggerInject(ClassDeclarationSyntax classDeclaration, string className, ParameterListSyntax parameterListSyntax, BlockSyntax constructorBody)
    {
        var parameter = SyntaxFactory.Parameter(
                               SyntaxFactory.List<AttributeListSyntax>(),
                               SyntaxFactory.TokenList(),
                               SyntaxFactory.ParseTypeName("ILoggerFactory"),
                               SyntaxFactory.Identifier("loggerFactory"),
                               null);
        if (parameterListSyntax == null)
            parameterListSyntax = SyntaxFactory.ParameterList(SyntaxFactory.SingletonSeparatedList(parameter));
        else
            parameterListSyntax = parameterListSyntax.AddParameters(parameter);

        var block = CreateLogCode(className, loggerVariable);
        if (constructorBody == null)
        {
            constructorBody = SyntaxFactory.Block(block);
        }
        else
        {
            constructorBody = constructorBody.AddStatements(block);
        }

        return (parameterListSyntax, constructorBody);
    }

    /// <summary>
    /// 生成配置项注入代码。
    /// </summary>
    /// <param name="classDeclaration"></param>
    /// <param name="optionClassName"></param>
    /// <param name="parameterListSyntax"></param>
    /// <param name="constructorBody"></param>
    /// <returns></returns>
    private (ParameterListSyntax parameterListSyntax, BlockSyntax constructorBody)
        GeneratorOptionsInject(ClassDeclarationSyntax classDeclaration,
        string optionClassName, string optionFieldVariable, ParameterListSyntax parameterListSyntax,
        BlockSyntax constructorBody)
    {
        var optionVariable = PrivateFieldNamingHelper.GeneratePrivateFieldName(optionClassName, FieldNamingStyle.PureCamel);
        var parameter = SyntaxFactory.Parameter(
                               SyntaxFactory.List<AttributeListSyntax>(),
                               SyntaxFactory.TokenList(),
                               SyntaxFactory.ParseTypeName($"IOptions<{optionClassName}>"),
                               SyntaxFactory.Identifier(optionVariable),
                               null);
        if (parameterListSyntax == null)
            parameterListSyntax = SyntaxFactory.ParameterList(SyntaxFactory.SingletonSeparatedList(parameter));
        else
            parameterListSyntax = parameterListSyntax.AddParameters(parameter);

        var block = CreateOptionCode(optionVariable, optionFieldVariable);
        if (constructorBody == null)
        {
            constructorBody = SyntaxFactory.Block(block);
        }
        else
        {
            constructorBody = constructorBody.AddStatements(block);
        }

        return (parameterListSyntax, constructorBody);
    }

    /// <summary>
    /// 生成构造函数注入代码。
    /// </summary>
    /// <returns></returns>
    private (ParameterListSyntax parameterListSyntax, BlockSyntax constructorBody) GeneratorStandardInject(ClassDeclarationSyntax classDeclaration,
        string className,
        string paramTypeName,
        string valVariable,
        ParameterListSyntax parameterListSyntax,
        BlockSyntax constructorBody)
    {
        var pValVariable = valVariable.Remove(0, 1);
        var parameter = SyntaxFactory.Parameter(
                              SyntaxFactory.List<AttributeListSyntax>(),
                              SyntaxFactory.TokenList(),
                              SyntaxFactory.ParseTypeName(paramTypeName),
                              SyntaxFactory.Identifier(pValVariable),
                              null);

        if (parameterListSyntax == null)
            parameterListSyntax = SyntaxFactory.ParameterList(SyntaxFactory.SingletonSeparatedList(parameter));
        else
            parameterListSyntax = parameterListSyntax.AddParameters(parameter);

        var block = SyntaxFactory.ExpressionStatement(
                         SyntaxFactory.AssignmentExpression(
                             SyntaxKind.SimpleAssignmentExpression,
                             SyntaxFactory.MemberAccessExpression(
                                 SyntaxKind.SimpleMemberAccessExpression,
                                 SyntaxFactory.ThisExpression(),
                                 SyntaxFactory.IdentifierName(valVariable)),
                             SyntaxFactory.IdentifierName(pValVariable)));
        if (constructorBody == null)
        {
            constructorBody = SyntaxFactory.Block(block);
        }
        else
        {
            constructorBody = constructorBody.AddStatements(block);
        }

        return (parameterListSyntax, constructorBody);
    }

    /// <summary>
    /// 在构造函数中注入字段。
    /// </summary>
    /// <param name="classDeclaration"></param>
    /// <param name="className"></param>
    /// <param name="parameterListSyntax"></param>
    /// <param name="constructorBody"></param>
    /// <returns></returns>
    private (ParameterListSyntax parameterListSyntax, BlockSyntax constructorBody) GeneratorFieldInject(ClassDeclarationSyntax classDeclaration, string className, ParameterListSyntax parameterListSyntax, BlockSyntax constructorBody)
    {
        var fields = GetClassMemberField(classDeclaration);
        foreach (var field in fields)
        {
            var attr = GetAttributeSyntaxes(field, IgnoreGeneratorAttribute);
            if (attr != null && attr.Any())
                continue;

            var variable = GetFieldName(field.Declaration);
            var valVariable = variable;
            if (valVariable.Contains("_"))
            {
                var t = valVariable.Split(['_'], StringSplitOptions.RemoveEmptyEntries);
                valVariable = t.Length > 1 ? t[1] : t[0];
            }
            else
            {
                valVariable = "_" + valVariable;
            }
            #region 循环生成参数
            ParameterSyntax parameter = null;
            var paramTypeName = GetTypeSyntaxName(field.Declaration.Type);
            if (paramTypeName.StartsWith("ILogger<", StringComparison.CurrentCultureIgnoreCase))//日志类型字段特殊处理
            {
                parameter = SyntaxFactory.Parameter(
                             SyntaxFactory.List<AttributeListSyntax>(),
                             SyntaxFactory.TokenList(),
                             SyntaxFactory.ParseTypeName("ILoggerFactory"),
                             SyntaxFactory.Identifier("loggerFactory"),
                             null);
            }
            else
            {
                parameter = SyntaxFactory.Parameter(
                              SyntaxFactory.List<AttributeListSyntax>(),
                              SyntaxFactory.TokenList(),
                              field.Declaration.Type,
                              SyntaxFactory.Identifier(valVariable),
                              null);
            }

            if (parameterListSyntax == null)
                parameterListSyntax = SyntaxFactory.ParameterList(SyntaxFactory.SingletonSeparatedList(parameter));
            else
                parameterListSyntax = parameterListSyntax.AddParameters(parameter);
            #endregion

            #region 循环生成方法体
            ExpressionStatementSyntax block = null;
            if (paramTypeName.StartsWith("ILogger<", StringComparison.CurrentCultureIgnoreCase))//日志类型字段特殊处理
            {
                block = CreateLogCode(className, variable);
            }
            else
            {
                block = SyntaxFactory.ExpressionStatement(
                         SyntaxFactory.AssignmentExpression(
                             SyntaxKind.SimpleAssignmentExpression,
                             SyntaxFactory.MemberAccessExpression(
                                 SyntaxKind.SimpleMemberAccessExpression,
                                 SyntaxFactory.ThisExpression(),
                                 SyntaxFactory.IdentifierName(variable)),
                             SyntaxFactory.IdentifierName(valVariable)));
            }

            if (constructorBody == null)
            {
                constructorBody = SyntaxFactory.Block(block);
            }
            else
            {
                constructorBody = constructorBody.AddStatements(block);
            }
            #endregion
        }

        return (parameterListSyntax, constructorBody);
    }
    #endregion

    /// <summary>
    /// 创建日志代码块。
    /// </summary>
    /// <param name="className"></param>
    /// <param name="loggerVarName"></param>
    /// <returns></returns>
    private ExpressionStatementSyntax CreateLogCode(string className, string loggerVarName)
    {
        var statementText = $"{loggerVarName} = loggerFactory.CreateLogger<{className}>();";
        var newStatement = SyntaxFactory.ParseStatement(statementText) as ExpressionStatementSyntax;
        return newStatement;
    }

    /// <summary>
    /// 创建日志代码块。
    /// </summary>
    /// <param name="optionFiledVarName"></param>
    /// <param name="optionVarName"></param>
    /// <returns></returns>
    private ExpressionStatementSyntax CreateOptionCode(string optionVarName, string optionFiledVarName)
    {
        var statementText = $"{optionFiledVarName} = {optionVarName}.Value;";
        var newStatement = SyntaxFactory.ParseStatement(statementText) as ExpressionStatementSyntax;
        return newStatement;
    }
}
