using Microsoft.CodeAnalysis.Diagnostics;

namespace Mud.ServiceCodeGenerator;


public partial class CodeInjectGenerator
{
    /// <inheritdoc/>
    public override void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var generationInfo = GetClassDeclarationProvider(context, [
            AttributeNames.ConstructorInject,
            AttributeNames.LoggerInject,
            AttributeNames.CacheManagerInject,
            AttributeNames.UserManagerInject,
            AttributeNames.CustomInject
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
                            $"Failed to generate code for {GetClassName(classDeclaration)}: {ex.Message}",
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
            DefaultCacheManagerType: ProjectConfigHelper.ReadConfigValue(globalOptions, ConfigKeys.DefaultCacheManagerType, DefaultValues.CacheManagerType),
            DefaultUserManagerType: ProjectConfigHelper.ReadConfigValue(globalOptions, ConfigKeys.DefaultUserManagerType, DefaultValues.UserManagerType),
            DefaultLoggerVariable: ProjectConfigHelper.ReadConfigValue(globalOptions, ConfigKeys.DefaultLoggerVariable, DefaultValues.LoggerVariable),
            DefaultCacheManagerVariable: ProjectConfigHelper.ReadConfigValue(globalOptions, ConfigKeys.DefaultCacheManagerVariable, DefaultValues.CacheManagerVariable),
            DefaultUserManagerVariable: ProjectConfigHelper.ReadConfigValue(globalOptions, ConfigKeys.DefaultUserManagerVariable, DefaultValues.UserManagerVariable)
        );
    }
    #endregion

    #region 代码生成主流程
    private void GenerateClassCode(SourceProductionContext context, ClassDeclarationSyntax classDeclaration, ProjectConfiguration config)
    {
        var className = GetClassName(classDeclaration);
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
        return new InjectionRequirements
        {
            ConstructorInject = GetAttributeSyntaxes(classDeclaration, AttributeNames.ConstructorInject) ?? Enumerable.Empty<AttributeSyntax>(),
            LoggerInject = GetAttributeSyntaxes(classDeclaration, AttributeNames.LoggerInject) ?? Enumerable.Empty<AttributeSyntax>(),
            OptionsInject = GetAttributeSyntaxes(classDeclaration, AttributeNames.OptionsInject) ?? Enumerable.Empty<AttributeSyntax>(),
            CacheManagerInject = GetAttributeSyntaxes(classDeclaration, AttributeNames.CacheManagerInject) ?? Enumerable.Empty<AttributeSyntax>(),
            UserManagerInject = GetAttributeSyntaxes(classDeclaration, AttributeNames.UserManagerInject) ?? Enumerable.Empty<AttributeSyntax>(),
            CustomInject = GetAttributeSyntaxes(classDeclaration, AttributeNames.CustomInject) ?? Enumerable.Empty<AttributeSyntax>()
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
