using Microsoft.CodeAnalysis.Diagnostics;

namespace Mud.ServiceCodeGenerator;


public partial class CodeInjectGenerator
{
    /// <inheritdoc/>
    public override void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var generationInfo = GetClassDeclarationProvider<ClassDeclarationSyntax>(context, [
            CodeInjectGeneratorConstants.ConstructorInjectAttribute,
            CodeInjectGeneratorConstants.LoggerInjectAttribute,
            CodeInjectGeneratorConstants.CacheManagerInjectAttribute,
            CodeInjectGeneratorConstants.UserManagerInjectAttribute,
            CodeInjectGeneratorConstants.CustomInjectAttribute
        ]);

        var compilationAndOptionsProvider = context.CompilationProvider
            .Combine(context.AnalyzerConfigOptionsProvider);

        var providers = generationInfo.Combine(compilationAndOptionsProvider);

        context.RegisterSourceOutput(providers, (sourceContext, provider) =>
        {
            var (classDeclarations, (compilation, analyzerConfig)) = provider;

            if (!classDeclarations.Any())
                return;

            var config = ReadProjectConfiguration(analyzerConfig.GlobalOptions);

            foreach (var classDeclaration in classDeclarations)
            {
                try
                {
                    GenerateClassCode(sourceContext, classDeclaration, config);
                }
                catch (Exception ex)
                {
                    // 记录生成错误但不要中断编译
                    sourceContext.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor(
                            "SG001",
                            "Code generation failed",
                            $"Failed to generate code for {SyntaxHelper.GetClassName(classDeclaration)}: {ex.Message}",
                            "CodeGeneration",
                            DiagnosticSeverity.Warning,
                            true),
                        Location.None));
                }
            }
        });
    }

    #region 配置管理
    private ProjectConfiguration ReadProjectConfiguration(AnalyzerConfigOptions globalOptions)
    {
        return new ProjectConfiguration(
            DefaultCacheManagerType: ProjectConfigHelper.ReadConfigValue(globalOptions, CodeInjectGeneratorConstants.DefaultCacheManagerTypeKey, CodeInjectGeneratorConstants.DefaultCacheManagerType),
            DefaultUserManagerType: ProjectConfigHelper.ReadConfigValue(globalOptions, CodeInjectGeneratorConstants.DefaultUserManagerTypeKey, CodeInjectGeneratorConstants.DefaultUserManagerType),
            DefaultLoggerVariable: ProjectConfigHelper.ReadConfigValue(globalOptions, CodeInjectGeneratorConstants.DefaultLoggerVariableKey, CodeInjectGeneratorConstants.DefaultLoggerVariable),
            DefaultCacheManagerVariable: ProjectConfigHelper.ReadConfigValue(globalOptions, CodeInjectGeneratorConstants.DefaultCacheManagerVariableKey, CodeInjectGeneratorConstants.DefaultCacheManagerVariable),
            DefaultUserManagerVariable: ProjectConfigHelper.ReadConfigValue(globalOptions, CodeInjectGeneratorConstants.DefaultUserManagerVariableKey, CodeInjectGeneratorConstants.DefaultUserManagerVariable)
        );
    }
    #endregion

    #region 代码生成主流程
    private void GenerateClassCode(SourceProductionContext context, ClassDeclarationSyntax classDeclaration, ProjectConfiguration config)
    {
        var className = SyntaxHelper.GetClassName(classDeclaration);
        if (string.IsNullOrEmpty(className))
            return;

        var injectionContext = new InjectionContext(className, classDeclaration);

        // 收集所有注入需求
        var injectionRequirements = CollectInjectionRequirements(classDeclaration, config);

        if (!injectionRequirements.HasAnyInjection)
            return;

        // 应用所有注入
        ApplyInjections(injectionContext, injectionRequirements, config);

        // 检查是否有有效的注入内容
        if (!injectionContext.BuildParameterList().Parameters.Any() ||
            !injectionContext.BuildConstructorBody().Statements.Any())
            return;

        // 生成最终代码
        var compilationUnit = CreateCompilationUnit(injectionContext);
        context.AddSource($"{className}.g.cs", compilationUnit);
    }

    private InjectionRequirements CollectInjectionRequirements(ClassDeclarationSyntax classDeclaration, ProjectConfiguration config)
    {
        // 首先获取所有属性
        var allAttributes = classDeclaration.AttributeLists.SelectMany(al => al.Attributes).ToList();

        // 手动匹配CustomInject属性，包括泛型版本
        var customInjectAttributes = allAttributes.Where(attr =>
        {
            var attrName = attr.Name.ToString();

            // 匹配 CustomInjectAttribute
            if (attrName == CodeInjectGeneratorConstants.CustomInjectAttribute)
                return true;

            // 匹配 CustomInject（短名称）
            if (attrName == CodeInjectGeneratorConstants.CustomInject)
                return true;

            // 匹配泛型版本 CustomInject<IMenuRepository>
            if (attrName.StartsWith(CodeInjectGeneratorConstants.CustomInjectGenericPattern) && attrName.EndsWith(CodeInjectGeneratorConstants.GenericSuffix))
                return true;

            return false;
        }).ToList();

        // 手动匹配OptionsInject属性，包括泛型版本
        var optionsInjectAttributes = allAttributes.Where(attr =>
        {
            var attrName = attr.Name.ToString();

            // 匹配 OptionsInjectAttribute
            if (attrName == CodeInjectGeneratorConstants.OptionsInjectAttribute)
                return true;

            // 匹配 OptionsInject（短名称）
            if (attrName == CodeInjectGeneratorConstants.OptionsInject)
                return true;

            // 匹配泛型版本 OptionsInject<TenantOptions>
            if (attrName.StartsWith(CodeInjectGeneratorConstants.OptionsInjectGenericPattern) && attrName.EndsWith(CodeInjectGeneratorConstants.GenericSuffix))
                return true;

            return false;
        }).ToList();

        return new InjectionRequirements
        {
            ConstructorInject = AttributeSyntaxHelper.GetAttributeSyntaxes(classDeclaration, CodeInjectGeneratorConstants.ConstructorInjectAttribute) ?? Enumerable.Empty<AttributeSyntax>(),
            LoggerInject = AttributeSyntaxHelper.GetAttributeSyntaxes(classDeclaration, CodeInjectGeneratorConstants.LoggerInjectAttribute) ?? Enumerable.Empty<AttributeSyntax>(),
            OptionsInject = optionsInjectAttributes,
            CacheManagerInject = AttributeSyntaxHelper.GetAttributeSyntaxes(classDeclaration, CodeInjectGeneratorConstants.CacheManagerInjectAttribute) ?? Enumerable.Empty<AttributeSyntax>(),
            UserManagerInject = AttributeSyntaxHelper.GetAttributeSyntaxes(classDeclaration, CodeInjectGeneratorConstants.UserManagerInjectAttribute) ?? Enumerable.Empty<AttributeSyntax>(),
            CustomInject = customInjectAttributes
        };
    }

    private void ApplyInjections(InjectionContext context, InjectionRequirements requirements, ProjectConfiguration config)
    {
        var injectors = CreateInjectors(config);

        // 首先处理构造函数注入（字段已存在，只生成参数和赋值）
        var constructorInjector = injectors.OfType<ConstructorInjector>().FirstOrDefault();
        constructorInjector?.Inject(context, requirements);

        // 然后处理其他注入（需要生成新字段）
        foreach (var injector in injectors.Where(i => i is not ConstructorInjector))
        {
            injector.Inject(context, requirements);
        }
    }
    #endregion

    #region 工厂方法
    private IEnumerable<IInjector> CreateInjectors(ProjectConfiguration config)
    {
        yield return new ConstructorInjector();
        yield return new LoggerInjector(config.DefaultLoggerVariable);
        yield return new CacheManagerInjector(config.DefaultCacheManagerType, config.DefaultCacheManagerVariable);
        yield return new UserManagerInjector(config.DefaultUserManagerType, config.DefaultUserManagerVariable);
        yield return new OptionsInjector();
        yield return new CustomInjector();
    }

    private static FieldDeclarationSyntax ParseFieldDeclaration(string fieldCode)
    {
        try
        {
            var tree = CSharpSyntaxTree.ParseText(fieldCode);
            var root = tree.GetRoot() as CompilationUnitSyntax;
            return root?.DescendantNodes().OfType<FieldDeclarationSyntax>().FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }
    #endregion
}
