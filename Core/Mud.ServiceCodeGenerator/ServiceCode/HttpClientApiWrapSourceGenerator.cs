using System.Collections.Immutable;
using System.Text;

namespace Mud.ServiceCodeGenerator;

/// <summary>
/// HttpClientApiWrap 源代码生成器
/// 为标记有HttpClientApiWrap特性的接口生成二次包装接口
/// </summary>
[Generator]
public class HttpClientApiWrapSourceGenerator : WebApiSourceGenerator
{
    /// <summary>
    /// HttpClientApiWrap特性名称数组
    /// </summary>
    private readonly string[] HttpClientApiWrapAttributeName = ["HttpClientApiWrapAttribute", "HttpClientApiWrap"];
    private readonly string[] TokenAttributeNames = new[] { "TokenAttribute", "Token" };

    /// <summary>
    /// 初始化源代码生成器
    /// </summary>
    /// <param name="context">初始化上下文</param>
    public override void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // 使用自定义方法查找标记了[HttpClientApiWrap]的接口
        var interfaceDeclarations = GetClassDeclarationProvider<InterfaceDeclarationSyntax>(context, HttpClientApiWrapAttributeName);

        // 组合编译和接口声明
        var compilationAndInterfaces = context.CompilationProvider.Combine(interfaceDeclarations);

        // 注册源生成
        context.RegisterSourceOutput(compilationAndInterfaces,
             (spc, source) => Execute(source.Left, source.Right, spc));
    }

    /// <summary>
    /// 执行源代码生成逻辑
    /// </summary>
    /// <param name="compilation">编译信息</param>
    /// <param name="interfaces">接口声明数组</param>
    /// <param name="context">源代码生成上下文</param>
    protected override void Execute(Compilation compilation, ImmutableArray<InterfaceDeclarationSyntax> interfaces, SourceProductionContext context)
    {
        if (compilation == null || interfaces.IsDefaultOrEmpty)
            return;

        foreach (var interfaceDecl in interfaces)
        {
            try
            {
                var semanticModel = compilation.GetSemanticModel(interfaceDecl.SyntaxTree);
                var interfaceSymbol = semanticModel.GetDeclaredSymbol(interfaceDecl);

                if (interfaceSymbol == null || !HasValidHttpMethods(interfaceSymbol))
                    continue;

                var wrapAttribute = GetHttpClientApiWrapAttribute(interfaceSymbol);
                if (wrapAttribute == null)
                    continue;

                // 生成包装接口代码
                var generatedCode = GenerateWrapInterface(compilation, interfaceDecl, interfaceSymbol, wrapAttribute);
                if (!string.IsNullOrEmpty(generatedCode))
                {
                    var fileName = $"{interfaceSymbol.Name}.Wrap.g.cs";
                    context.AddSource(fileName, generatedCode);
                }
            }
            catch (Exception ex)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "MCG001",
                        "HttpClientApiWrap Source Generator Error",
                        $"Error generating wrap interface for {interfaceDecl.Identifier}: {ex.Message}",
                        "Generation",
                        DiagnosticSeverity.Error,
                        true),
                    interfaceDecl.GetLocation()));
            }
        }
    }

    /// <summary>
    /// 获取HttpClientApiWrap特性
    /// </summary>
    private AttributeData? GetHttpClientApiWrapAttribute(INamedTypeSymbol interfaceSymbol)
    {
        if (interfaceSymbol == null)
            return null;
        return interfaceSymbol.GetAttributes()
            .FirstOrDefault(a => HttpClientApiWrapAttributeName.Contains(a.AttributeClass?.Name));
    }

    /// <summary>
    /// 生成包装接口代码
    /// </summary>
    private string GenerateWrapInterface(Compilation compilation, InterfaceDeclarationSyntax interfaceDecl, INamedTypeSymbol interfaceSymbol, AttributeData wrapAttribute)
    {
        var sb = new StringBuilder();
        GenerateFileHeader(sb);
        // 获取命名空间
        var namespaceName = GetNamespaceName(interfaceDecl);
        if (!string.IsNullOrEmpty(namespaceName))
        {
            sb.AppendLine();
            sb.AppendLine($"namespace {namespaceName};");
            sb.AppendLine();
        }

        // 获取包装接口名称
        var wrapInterfaceName = GetWrapInterfaceName(interfaceSymbol, wrapAttribute);

        // 添加接口注释
        var xmlDoc = GetXmlDocumentation(interfaceDecl);
        if (!string.IsNullOrEmpty(xmlDoc))
        {
            sb.Append(xmlDoc);
        }

        sb.AppendLine($"{CompilerGeneratedAttribute}");
        sb.AppendLine($"{GeneratedCodeAttribute}");
        sb.AppendLine($"public interface {wrapInterfaceName}");
        sb.AppendLine("{");

        // 生成接口方法
        var methods = interfaceSymbol.GetMembers()
                                     .OfType<IMethodSymbol>()
                                     .Where(m => IsValidMethod(m));
        foreach (var methodSymbol in methods)
        {
            var methodInfo = AnalyzeMethod(compilation, methodSymbol, interfaceDecl);
            if (!methodInfo.IsValid)
                continue;

            var methodSyntax = GetMethodDeclarationSyntax(methodSymbol, interfaceDecl);
            if (methodSyntax != null)
            {
                var wrapMethodCode = GenerateWrapMethod(methodInfo, methodSyntax);
                if (!string.IsNullOrEmpty(wrapMethodCode))
                {
                    sb.AppendLine(wrapMethodCode);
                    sb.AppendLine();
                }
            }
        }

        sb.AppendLine("}");

        return sb.ToString();
    }

    /// <summary>
    /// 生成包装方法
    /// </summary>
    private string GenerateWrapMethod(MethodAnalysisResult methodInfo, MethodDeclarationSyntax methodSyntax)
    {
        var sb = new StringBuilder();

        // 添加方法注释
        var methodDoc = GetMethodXmlDocumentation(methodSyntax, methodInfo);
        if (!string.IsNullOrEmpty(methodDoc))
        {
            sb.AppendLine(methodDoc);
        }

        // 方法签名
        sb.Append($"    {methodInfo.ReturnType} {methodInfo.MethodName}(");

        // 过滤掉标记了[Token]特性的参数，保留其他所有参数
        var filteredParameters = FilterParametersByAttribute(methodInfo.Parameters, TokenAttributeNames, exclude: true);

        // 生成参数列表
        var parameterList = GenerateParameterList(filteredParameters);
        sb.Append(parameterList);

        sb.Append(");");

        return sb.ToString();
    }

    /// <summary>
    /// 获取方法的XML文档注释
    /// </summary>
    private string GetMethodXmlDocumentation(MethodDeclarationSyntax methodSyntax, MethodAnalysisResult methodInfo)
    {
        var xmlDoc = GetXmlDocumentation(methodSyntax);
        if (string.IsNullOrEmpty(xmlDoc))
            return string.Empty;

        // 获取标记了Token特性的参数名称
        var tokenParameterNames = FilterParametersByAttribute(methodInfo.Parameters, TokenAttributeNames)
            .Select(p => p.Name)
            .ToList();

        // 处理XML文档注释，移除token参数的注释
        var docLines = xmlDoc.Split('\n');
        var result = new StringBuilder();

        int i = 0;
        foreach (var line in docLines)
        {
            var trimmedLine = line.TrimEnd();
            if (!string.IsNullOrWhiteSpace(trimmedLine))
            {
                // 检查是否是token参数的注释行
                var isTokenParameterComment = tokenParameterNames.Any(name =>
                    trimmedLine.Contains($"<param name=\"{name}\">") ||
                    trimmedLine.Contains($"<param name=\"{name}\""));

                if (!isTokenParameterComment)
                {
                    if (i == 0)
                        result.AppendLine($"    {trimmedLine}");
                    else
                        result.AppendLine($"{trimmedLine}");
                }

                i++;
            }
        }

        return result.ToString().TrimEnd();
    }
}