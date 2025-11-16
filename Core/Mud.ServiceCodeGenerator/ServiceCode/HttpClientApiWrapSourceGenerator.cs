using System.Collections.Immutable;
using System.Text;

namespace Mud.ServiceCodeGenerator;

/// <summary>
/// HttpClientApiWrap 源代码生成器
/// 为标记有HttpClientApiWrap特性的接口生成二次包装实现类
/// </summary>
[Generator]
public class HttpClientApiWrapSourceGenerator : WebApiSourceGenerator
{
    /// <summary>
    /// HttpClientApiWrap特性名称数组
    /// </summary>
    private readonly string[] HttpClientApiWrapAttributeName = ["HttpClientApiWrapAttribute", "HttpClientApiWrap"];
    private readonly string[] TokenAttributeNames = new[] { "TokenAttribute", "Token" };

    /// <inheritdoc/>
    protected override System.Collections.ObjectModel.Collection<string> GetFileUsingNameSpaces()
    {
        return
        [
            "System",
            "System.Text",
            "System.Text.Json",
            "System.Threading.Tasks",
            "System.Collections.Generic",
            "System.Linq",
            "Microsoft.Extensions.Logging",
            "Microsoft.Extensions.Options"
        ];
    }

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
                var wrapInterfaceCode = GenerateWrapInterface(compilation, interfaceDecl, interfaceSymbol, wrapAttribute);
                if (!string.IsNullOrEmpty(wrapInterfaceCode))
                {
                    var wrapFileName = $"{interfaceSymbol.Name}.Wrap.g.cs";
                    context.AddSource(wrapFileName, wrapInterfaceCode);
                }

                // 生成包装实现类代码
                var wrapImplementationCode = GenerateWrapImplementation(compilation, interfaceDecl, interfaceSymbol, wrapAttribute);
                if (!string.IsNullOrEmpty(wrapImplementationCode))
                {
                    var implFileName = $"{interfaceSymbol.Name}.WrapImpl.g.cs";
                    context.AddSource(implFileName, wrapImplementationCode);
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
    /// 获取Token管理接口名称
    /// </summary>
    private string GetTokenManageInterfaceName(INamedTypeSymbol interfaceSymbol, AttributeData wrapAttribute)
    {
        // 检查特性参数中是否有指定的Token管理接口名称
        var tokenManageArg = wrapAttribute.NamedArguments.FirstOrDefault(a => a.Key == "TokenManage");
        if (!string.IsNullOrEmpty(tokenManageArg.Value.Value?.ToString()))
        {
            return tokenManageArg.Value.Value.ToString();
        }

        // 默认使用 ITokenManage
        return "ITokenManage";
    }

    /// <summary>
    /// 获取包装类名称
    /// </summary>
    private string GetWrapClassName(string wrapInterfaceName)
    {
        if (wrapInterfaceName.StartsWith("I", StringComparison.Ordinal) && wrapInterfaceName.Length > 1)
        {
            return wrapInterfaceName.Substring(1);
        }
        return wrapInterfaceName + "Wrap";
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
        sb.AppendLine($"public partial interface {wrapInterfaceName}");
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
                var wrapMethodCode = GenerateWrapMethodDeclaration(methodInfo, methodSyntax);
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
    /// 生成包装实现类代码
    /// </summary>
    private string GenerateWrapImplementation(Compilation compilation, InterfaceDeclarationSyntax interfaceDecl, INamedTypeSymbol interfaceSymbol, AttributeData wrapAttribute)
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

        // 获取包装类名称和接口名称
        
        var wrapInterfaceName = GetWrapInterfaceName(interfaceSymbol, wrapAttribute);
        var wrapClassName = GetWrapClassName(wrapInterfaceName);
        var tokenManageInterfaceName = GetTokenManageInterfaceName(interfaceSymbol, wrapAttribute);

        sb.AppendLine($"{CompilerGeneratedAttribute}");
        sb.AppendLine($"{GeneratedCodeAttribute}");
        sb.AppendLine($"internal partial class {wrapClassName} : {wrapInterfaceName}");
        sb.AppendLine("{");
        sb.AppendLine($"    private readonly {interfaceSymbol.Name} {PrivateFieldNamingHelper.GeneratePrivateFieldName(interfaceSymbol.Name)};");
        sb.AppendLine($"    private readonly {tokenManageInterfaceName} {PrivateFieldNamingHelper.GeneratePrivateFieldName(tokenManageInterfaceName)};");
        sb.AppendLine($"    private readonly ILogger<{wrapClassName}> _logger;");
        sb.AppendLine();
        
        // 生成构造函数
        sb.AppendLine($"    public {wrapClassName}({interfaceSymbol.Name} {wrapClassName.ToLowerInvariant()[0] + wrapClassName.Substring(1)}Api, {tokenManageInterfaceName} {tokenManageInterfaceName.ToLowerInvariant()[0] + tokenManageInterfaceName.Substring(1)}, ILogger<{wrapClassName}> logger)");
        sb.AppendLine("    {");
        sb.AppendLine($"        {PrivateFieldNamingHelper.GeneratePrivateFieldName(interfaceSymbol.Name)} = {wrapClassName.ToLowerInvariant()[0] + wrapClassName.Substring(1)}Api;");
        sb.AppendLine($"        {PrivateFieldNamingHelper.GeneratePrivateFieldName(tokenManageInterfaceName)} = {tokenManageInterfaceName.ToLowerInvariant()[0] + tokenManageInterfaceName.Substring(1)};");
        sb.AppendLine("        _logger = logger;");
        sb.AppendLine("    }");
        sb.AppendLine();

        // 生成包装方法实现
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
                var wrapMethodCode = GenerateWrapMethodImplementation(methodInfo, methodSyntax, interfaceSymbol.Name,tokenManageInterfaceName);
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
    /// 生成包装方法声明（接口方法）
    /// </summary>
    private string GenerateWrapMethodDeclaration(MethodAnalysisResult methodInfo, MethodDeclarationSyntax methodSyntax)
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
    /// 生成包装方法实现（具体实现）
    /// </summary>
    private string GenerateWrapMethodImplementation(MethodAnalysisResult methodInfo, MethodDeclarationSyntax methodSyntax, string interfaceName,string tokenManageInterfaceName)
    {
        var sb = new StringBuilder();

        // 添加方法注释
        var methodDoc = GetMethodXmlDocumentation(methodSyntax, methodInfo);
        if (!string.IsNullOrEmpty(methodDoc))
        {
            sb.AppendLine(methodDoc);
        }

        // 方法签名
        sb.Append($"    public async Task<{methodInfo.ReturnType}> {methodInfo.MethodName}(");
        
        // 过滤掉标记了[Token]特性的参数，保留其他所有参数
        var filteredParameters = FilterParametersByAttribute(methodInfo.Parameters, TokenAttributeNames, exclude: true);
        
        // 生成参数列表
        var parameterList = GenerateParameterList(filteredParameters);
        sb.AppendLine($"{parameterList})");
        
        sb.AppendLine("    {");
        
        // 生成方法体
        sb.AppendLine("        try");
        sb.AppendLine("        {");
        
        // 获取Token
        sb.AppendLine($"            var token = await {PrivateFieldNamingHelper.GeneratePrivateFieldName(tokenManageInterfaceName)}.GetTokenAsync();");
        sb.AppendLine();
        
        // Token空值检查
        sb.AppendLine("            if (string.IsNullOrEmpty(token))");
        sb.AppendLine("            {");
        sb.AppendLine("                _logger.LogWarning(\"获取到的Token为空！\");");
        sb.AppendLine("            }");
        sb.AppendLine();
        
        // 调用原始API方法
        sb.Append($"            return await {PrivateFieldNamingHelper.GeneratePrivateFieldName(interfaceName)}.{methodInfo.MethodName}(");
        
        // 生成调用参数列表（包含Token和过滤后的参数）
        var callParameters = new List<string> { "token" };
        callParameters.AddRange(filteredParameters.Select(p => p.Name));
        sb.Append(string.Join(", ", callParameters));
        
        sb.AppendLine(");");
        sb.AppendLine("        }");
        sb.AppendLine("        catch (Exception x)");
        sb.AppendLine("        {");
        sb.AppendLine($"            _logger.LogError(x, \"执行{methodInfo.MethodName}操作失败：{{message}}\", x.Message);");
        sb.AppendLine("            throw;");
        sb.AppendLine("        }");
        sb.AppendLine("    }");

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