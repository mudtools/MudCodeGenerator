using System.Text;

namespace Mud.ServiceCodeGenerator;

/// <summary>
/// 服务类代码生成器基类。
/// </summary>
public abstract class ServiceCodeGenerator : TransitiveCodeGenerator
{
    /// <summary>
    /// 服务类生成特性。
    /// </summary>
    protected const string ServiceGeneratorAttributeName = "ServiceGeneratorAttribute";

    private const string IgnoreQueryAttributeName = "IgnoreQueryAttribute";
    private const string OrderByAttributeName = "OrderByAttribute";

    /// <summary>
    /// 是否生成服务端代码。
    /// </summary>
    private bool IsServiceGenerator = true;

    /// <inheritdoc/>
    public override void Initialize(IncrementalGeneratorInitializationContext context)
    {
        //Debugger.Launch();
        var generationInfo = GetClassDeclarationProvider(context, [ServiceGeneratorAttributeName]);

        var compilationAndOptionsProvider = context.CompilationProvider
                                          .Combine(context.AnalyzerConfigOptionsProvider)
                                          .Select((s, _) => s);

        var providers = generationInfo.Combine(compilationAndOptionsProvider);

        context.RegisterSourceOutput(providers, (spcontext, provider) =>
        {
            var (compiler, analyzer) = provider.Right;
            ReadProjectOptions(analyzer.GlobalOptions, "build_property.ServiceGenerator",
              val =>
              {
                  if (bool.TryParse(val, out var b))
                      IsServiceGenerator = b;
              });
            if (!IsServiceGenerator)
                return;

            var classDeclarations = provider.Left;
            if (!classDeclarations.Any())//没有需要生成的代码则直接退出，提高编译性能。
                return;
            var filePath = compiler.SyntaxTrees.Where(s => !string.IsNullOrEmpty(s.FilePath))
                                               .Select(s => s.FilePath).FirstOrDefault();

            if (string.IsNullOrEmpty(filePath))
                return;

            var paths = filePath.Split([compiler.AssemblyName], StringSplitOptions.RemoveEmptyEntries);
            filePath = paths[0];

            InitEntityPrefixValue(analyzer.GlobalOptions);
            var impAssembly = ProjectConfigHelper.ReadConfigValue(analyzer.GlobalOptions, "build_property.ImpAssembly", "Mud.System");

            var projectFiles = provider.Right;
            var fullPath = "";
            var referenceFilePath = Path.GetFullPath(Path.Combine(filePath, impAssembly));
            try
            {
                fullPath = Path.GetFullPath(referenceFilePath);
            }
            catch (Exception)
            {
            }

            foreach (var classNode in classDeclarations)
            {
                var (unitSyntax, className) = GenerateCode(classNode);
                WriteFile(fullPath, className, unitSyntax);
            }
        });
    }

    /// <summary>
    /// 自动生成服务类代码。
    /// </summary>
    protected abstract (CompilationUnitSyntax? unitSyntax, string? className) GenerateCode(ClassDeclarationSyntax classNode);

    /// <summary>
    /// 写入生成的代码到文件
    /// </summary>
    /// <param name="rootPath">根路径</param>
    /// <param name="className">类名</param>
    /// <param name="compilationUnit">编译单元</param>
    private void WriteFile(string rootPath, string className, CompilationUnitSyntax compilationUnit)
    {
        var classFileName = Path.Combine(rootPath, "AutoGenerator\\" + className + ".g.cs");
        var dir = Path.GetDirectoryName(classFileName);
        var dirInfo = new DirectoryInfo(dir);
        if (!dirInfo.Exists)
            dirInfo.Create();

        using var writer = new StreamWriter(classFileName);
        var codeContent = compilationUnit.ToFullString();
        writer.Write(codeContent);
        writer.Flush();
        writer.Close();
    }

    /// <summary>
    /// 是否忽略生成属性。
    /// </summary>
    /// <param name="propertyDeclaration"></param>
    /// <returns></returns>
    protected bool IsIgnoreGenerator(PropertyDeclarationSyntax propertyDeclaration)
    {
        if (propertyDeclaration == null)
            return false;

        var attributes = AttributeSyntaxHelper.GetAttributeSyntaxes(propertyDeclaration, IgnoreQueryAttributeName);
        return attributes != null && attributes.Any();
    }

    /// <summary>
    /// 获取实体的排序属性。
    /// </summary>
    /// <param name="propertyDeclaration"></param>
    /// <returns></returns>
    protected OrderBy GetOrderByAttribute(PropertyDeclarationSyntax propertyDeclaration)
    {
        if (propertyDeclaration == null)
            return null;

        var attributes = AttributeSyntaxHelper.GetAttributeSyntaxes(propertyDeclaration, OrderByAttributeName);
        if (!attributes.Any())
            return null;

        var orderBy = new OrderBy
        {
            PropertyName = GetPropertyName(propertyDeclaration),
            IsAsc = AttributeSyntaxHelper.GetPropertyAttributeValues(propertyDeclaration, OrderByAttributeName, "IsAsc", true),
            OrderNum = AttributeSyntaxHelper.GetPropertyAttributeValues(propertyDeclaration, OrderByAttributeName, "OrderNum", 0)
        };
        return orderBy;
    }

    /// <summary>
    /// 排序属性。
    /// </summary>
    protected class OrderBy
    {
        /// <summary>
        /// 排序属性名
        /// </summary>
        public string PropertyName { get; set; }
        /// <summary>
        /// 是否为升序查询。
        /// </summary>
        public bool IsAsc { get; set; } = true;

        /// <summary>
        /// 多个排序字段顺序。
        /// </summary>
        public int OrderNum { get; set; } = 0;
    }

    /// <summary>
    /// 生成公共代码。
    /// </summary>
    /// <param name="sb"></param>
    /// <param name="classNode"></param>
    protected void GenerateCommonCode(StringBuilder sb, ClassDeclarationSyntax classNode)
    {
        var namespaceName = GetNamespaceName(classNode);
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("// 这是一个自动生成的类，请勿手动修改。");
        sb.AppendLine("// 生成时间: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        sb.AppendLine("#pragma warning disable");
        sb.AppendLine("#nullable enable");
        sb.AppendLine("using System;");
        sb.AppendLine($"namespace {namespaceName}");

    }

    /// <summary>
    /// 获取属性名称
    /// </summary>
    /// <param name="propertyDeclaration">属性声明</param>
    /// <returns>属性名称</returns>
    protected string GetPropertyName(PropertyDeclarationSyntax propertyDeclaration)
    {
        return propertyDeclaration?.Identifier.Text ?? "";
    }
}