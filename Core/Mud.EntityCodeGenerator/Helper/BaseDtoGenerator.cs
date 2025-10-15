namespace Mud.EntityCodeGenerator.Helper;

/// <summary>
/// DTO代码生成器抽象基类
/// </summary>
public abstract class BaseDtoGenerator : TransitiveDtoGenerator, IDtoGenerator
{
    protected GeneratorConfiguration Configuration { get; } = new GeneratorConfiguration();

    /// <summary>
    /// 生成器名称
    /// </summary>
    public abstract string GeneratorName { get; }

    /// <summary>
    /// 是否应该生成代码
    /// </summary>
    /// <param name="classDeclaration">类声明语法</param>
    /// <returns>是否应该生成</returns>
    public virtual bool ShouldGenerate(ClassDeclarationSyntax classDeclaration)
    {
        if (classDeclaration == null)
            return false;

        var attributeName = GetGeneratorAttributeName();
        var propertyName = GetGeneratorPropertyName();
        var defaultValue = GetDefaultGenerationValue();

        return SyntaxHelper.GetClassAttributeValues(classDeclaration, attributeName, propertyName, defaultValue);
    }

    /// <summary>
    /// 获取生成的类名
    /// </summary>
    /// <param name="classDeclaration">原始类声明</param>
    /// <returns>生成的类名</returns>
    public virtual string GetGeneratedClassName(ClassDeclarationSyntax classDeclaration)
    {
        return GetGeneratorClassName(classDeclaration);
    }

    /// <summary>
    /// 获取生成的命名空间
    /// </summary>
    /// <param name="classDeclaration">原始类声明</param>
    /// <returns>生成的命名空间</returns>
    public virtual string GetGeneratedNamespace(ClassDeclarationSyntax classDeclaration)
    {
        return GetDtoNamespaceName(classDeclaration);
    }

    /// <summary>
    /// 获取生成器特性名称
    /// </summary>
    /// <returns>特性名称</returns>
    protected virtual string GetGeneratorAttributeName()
    {
        return DtoGeneratorAttributeName;
    }

    /// <summary>
    /// 获取生成器属性名称
    /// </summary>
    /// <returns>属性名称</returns>
    protected abstract string GetGeneratorPropertyName();

    /// <summary>
    /// 获取默认生成值
    /// </summary>
    /// <returns>默认值</returns>
    protected virtual bool GetDefaultGenerationValue()
    {
        return true;
    }

    /// <summary>
    /// 生成代码的核心方法
    /// </summary>
    /// <param name="context">源码生成上下文</param>
    /// <param name="compilation">编译对象</param>
    /// <param name="classDeclaration">类声明语法</param>
    protected override void GenerateCode(SourceProductionContext context, Compilation compilation, ClassDeclarationSyntax classDeclaration)
    {
        var className = classDeclaration != null ? SyntaxHelper.GetClassName(classDeclaration) : "Unknown";

        ErrorHandler.SafeExecute(context, className, () =>
        {
            if (!ShouldGenerate(classDeclaration))
                return;

            var (localClass, namespaceName, generatedClassName) = BuildLocalClass(classDeclaration);

            // 使用属性生成器
            var propertyGenerator = CreatePropertyGenerator();
            var fieldGenerator = CreateFieldGenerator();

            // 使用安全的属性生成器
            var safePropertyGenerator = ErrorHandler.CreateSafePropertyGenerator(this, propertyGenerator);
            var safeFieldGenerator = ErrorHandler.CreateSafePropertyGenerator(this, fieldGenerator);

            localClass = BuildLocalClassProperty<PropertyDeclarationSyntax>(classDeclaration, localClass, compilation, safePropertyGenerator, null);

            // 验证生成结果
            if (!ErrorHandler.ValidateGenerationResult(context, localClass, className, GetFailureDescriptor()))
                return;

            var compilationUnit = GenCompilationUnitSyntax(localClass, namespaceName, generatedClassName);
            compilationUnit = compilationUnit.NormalizeWhitespace();
            context.AddSource($"{generatedClassName}.g.cs", compilationUnit);
        },
        GetFailureDescriptor(),
        GetErrorDescriptor());
    }

    /// <summary>
    /// 创建属性生成器
    /// </summary>
    /// <returns>属性生成委托</returns>
    protected abstract Func<PropertyDeclarationSyntax, PropertyDeclarationSyntax> CreatePropertyGenerator();

    /// <summary>
    /// 创建字段生成器
    /// </summary>
    /// <returns>字段生成委托</returns>
    protected abstract Func<FieldDeclarationSyntax, PropertyDeclarationSyntax> CreateFieldGenerator();

    /// <summary>
    /// 创建安全的属性生成器
    /// </summary>
    /// <returns>属性生成委托</returns>
    protected abstract Func<PropertyDeclarationSyntax, PropertyDeclarationSyntax> CreateSafePropertyGenerator();

    /// <summary>
    /// 创建安全的字段生成器
    /// </summary>
    /// <returns>字段生成委托</returns>
    protected abstract Func<FieldDeclarationSyntax, PropertyDeclarationSyntax> CreateSafeFieldGenerator();

    /// <summary>
    /// 获取失败描述符
    /// </summary>
    /// <returns>诊断描述符</returns>
    protected abstract Microsoft.CodeAnalysis.DiagnosticDescriptor GetFailureDescriptor();

    /// <summary>
    /// 获取错误描述符
    /// </summary>
    /// <returns>诊断描述符</returns>
    protected abstract Microsoft.CodeAnalysis.DiagnosticDescriptor GetErrorDescriptor();

    // 显式实现接口方法
    string[] IDtoGenerator.GetPropertyAttributes()
    {
        // 返回一个空的属性数组作为默认实现
        return Array.Empty<string>();
    }

    // 显式实现接口方法
    void ICodeGenerator.GenerateCode(SourceProductionContext context, Compilation compilation, ClassDeclarationSyntax classDeclaration)
    {
        GenerateCode(context, compilation, classDeclaration);
    }
}