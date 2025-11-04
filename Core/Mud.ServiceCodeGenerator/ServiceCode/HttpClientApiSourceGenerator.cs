using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Mud.ServiceCodeGenerator;

[Generator]
public class HttpClientApiSourceGenerator : TransitiveCodeGenerator
{
    private const string HttpClientApiAttributeName = "HttpClientApiAttribute";
    private static readonly string[] SupportedHttpMethods = ["Get", "Post", "Put", "Delete", "Patch", "Head", "Options"];

    public override void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // 使用自定义方法查找标记了[HttpClientApi]的接口
        var interfaceDeclarations = GetInterfaceDeclarationProvider(context, [HttpClientApiAttributeName, "HttpClientApi"]);

        // 组合编译和接口声明
        var compilationAndInterfaces = context.CompilationProvider.Combine(interfaceDeclarations);

        // 注册源生成
        context.RegisterSourceOutput(compilationAndInterfaces,
             (spc, source) => Execute(source.Left, source.Right, spc));
    }

    /// <summary>
    /// 根据注解名获取需要进行代码辅助生成的接口。
    /// </summary>
    /// <param name="context"><see cref="IncrementalGeneratorInitializationContext"/>对象。</param>
    /// <param name="attributeNames">需要查找的特性名称数组。</param>
    /// <returns></returns>
    protected IncrementalValueProvider<ImmutableArray<InterfaceDeclarationSyntax>> GetInterfaceDeclarationProvider(IncrementalGeneratorInitializationContext context, string[] attributeNames)
    {
        // 获取所有带有指定特性的接口
        var generationInfo = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: (node, c) => node is InterfaceDeclarationSyntax,
                transform: (ctx, c) =>
                {
                    var interfaceNode = (InterfaceDeclarationSyntax)ctx.Node;
                    var semanticModel = ctx.SemanticModel;
                    var symbol = semanticModel.GetDeclaredSymbol(interfaceNode, cancellationToken: default);

                    if (symbol?.GetAttributes().Any(a => attributeNames.Contains(a.AttributeClass?.Name)) ?? false)
                    {
                        return interfaceNode;
                    }

                    return null;
                })
            .Where(static s => s is not null)
            .Select(static (s, c) => s!)
            .Collect();
        return generationInfo;
    }

    // 重写获取文件使用的命名空间
    protected override System.Collections.ObjectModel.Collection<string> GetFileUsingNameSpaces()
    {
        return ["System", "System.Net.Http", "System.Text", "System.Text.Json",
                "System.Threading.Tasks", "System.Collections.Generic", "System.Linq",
                "Microsoft.Extensions.Logging"];
    }

    private void Execute(Compilation compilation, ImmutableArray<InterfaceDeclarationSyntax> interfaces, SourceProductionContext context)
    {
        if (interfaces.IsDefaultOrEmpty)
            return;

        foreach (var interfaceDecl in interfaces)
        {
            try
            {
                var model = compilation.GetSemanticModel(interfaceDecl.SyntaxTree);
                var interfaceSymbol = model.GetDeclaredSymbol(interfaceDecl) as INamedTypeSymbol;

                if (interfaceSymbol != null)
                {
                    var sourceCode = GenerateImplementationClass(interfaceSymbol, interfaceDecl);
                    var className = GetImplementationClassName(interfaceSymbol.Name);
                    context.AddSource($"{className}.g.cs", SourceText.From(sourceCode, Encoding.UTF8));
                }
            }
            catch (System.Exception ex)
            {
                ReportErrorDiagnostic(context,
                    new DiagnosticDescriptor(
                        "HTTPCLIENT001",
                        "HttpClient API生成错误",
                        $"生成接口{interfaceDecl.Identifier.Text}的实现时发生错误: {0}",
                        "Generation",
                        DiagnosticSeverity.Error,
                        true),
                    interfaceDecl.Identifier.Text, ex);
            }
        }
    }

    private string GenerateImplementationClass(INamedTypeSymbol interfaceSymbol, InterfaceDeclarationSyntax interfaceDecl)
    {
        var className = GetImplementationClassName(interfaceSymbol.Name);
        var namespaceName = GetNamespaceName(interfaceDecl);

        var sb = new StringBuilder();
        GenerateFileHeader(sb, className, namespaceName, interfaceSymbol.Name);

        // 为每个方法生成实现
        foreach (var methodSymbol in interfaceSymbol.GetMembers().OfType<IMethodSymbol>())
        {
            GenerateMethodImplementation(sb, methodSymbol, interfaceDecl);
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private static void GenerateFileHeader(StringBuilder sb, string className, string namespaceName, string interfaceName)
    {
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("// 此代码由HttpClient API源生成器自动生成，请勿手动修改");
        sb.AppendLine($"// 生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine();
        sb.AppendLine("#nullable enable");

        // 使用基类的命名空间
        var usingNamespaces = new HttpClientApiSourceGenerator().GetFileUsingNameSpaces();
        foreach (var usingNamespace in usingNamespaces)
        {
            sb.AppendLine($"using {usingNamespace};");
        }

        sb.AppendLine();
        sb.AppendLine($"namespace {namespaceName}");
        sb.AppendLine("{");
        sb.AppendLine($"    /// <summary>");
        sb.AppendLine($"    /// {interfaceName}的HttpClient实现类");
        sb.AppendLine($"    /// </summary>");
        sb.AppendLine($"    public partial class {className} : {interfaceName}");
        sb.AppendLine("    {");
        sb.AppendLine("        private readonly HttpClient _httpClient;");
        sb.AppendLine($"        private readonly ILogger<{className}> _logger;");
        sb.AppendLine("        private readonly JsonSerializerOptions _jsonSerializerOptions;");
        sb.AppendLine();
        sb.AppendLine("        /// <summary>");
        sb.AppendLine("        /// 构造函数");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine("        /// <param name=\"httpClient\">HttpClient实例</param>");
        sb.AppendLine("        /// <param name=\"logger\">日志记录器</param>");
        sb.AppendLine($"        public {className}(HttpClient httpClient, ILogger<{className}> logger)");
        sb.AppendLine("        {");
        sb.AppendLine("            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));");
        sb.AppendLine("            _logger = logger ?? throw new ArgumentNullException(nameof(logger));");
        sb.AppendLine("            _jsonSerializerOptions = new JsonSerializerOptions");
        sb.AppendLine("            {");
        sb.AppendLine("                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,");
        sb.AppendLine("                WriteIndented = false,");
        sb.AppendLine("                PropertyNameCaseInsensitive = true");
        sb.AppendLine("            };");
        sb.AppendLine("        }");
    }

    private void GenerateMethodImplementation(StringBuilder sb, IMethodSymbol methodSymbol, InterfaceDeclarationSyntax interfaceDecl)
    {
        var methodInfo = AnalyzeMethod(methodSymbol, interfaceDecl);
        if (!methodInfo.IsValid) return;

        sb.AppendLine();
        sb.AppendLine($"        /// <summary>");
        sb.AppendLine($"        /// 实现 {methodSymbol.Name} 方法");
        sb.AppendLine($"        /// </summary>");
        sb.AppendLine($"        public async {methodSymbol.ReturnType} {methodSymbol.Name}({GetParameterList(methodSymbol)})");
        sb.AppendLine("        {");

        GenerateRequestSetup(sb, methodInfo);
        GenerateParameterHandling(sb, methodInfo);
        GenerateRequestExecution(sb, methodInfo);

        sb.AppendLine("        }");
    }

    private MethodAnalysisResult AnalyzeMethod(IMethodSymbol methodSymbol, InterfaceDeclarationSyntax interfaceDecl)
    {
        var methodSyntax = interfaceDecl.Members
            .OfType<MethodDeclarationSyntax>()
            .FirstOrDefault(m => m.Identifier.ValueText == methodSymbol.Name);

        if (methodSyntax == null) return MethodAnalysisResult.Invalid;

        // 分析HTTP方法特性
        var httpMethodAttr = methodSyntax.AttributeLists
            .SelectMany(a => a.Attributes)
            .FirstOrDefault(a => SupportedHttpMethods.Contains(a.Name.ToString()));

        if (httpMethodAttr == null) return MethodAnalysisResult.Invalid;

        var httpMethod = httpMethodAttr.Name.ToString().ToUpperInvariant();
        var urlTemplate = GetAttributeArgumentValue(httpMethodAttr, 0)?.ToString().Trim('"') ?? "";

        // 分析参数
        var parameters = methodSymbol.Parameters.Select(p => new ParameterInfo
        {
            Name = p.Name,
            Type = p.Type.ToDisplayString(),
            Attributes = p.GetAttributes().Select(attr => new ParameterAttributeInfo
            {
                Name = attr.AttributeClass?.Name ?? "",
                Arguments = attr.ConstructorArguments.Select(arg => arg.Value).ToArray(),
                NamedArguments = attr.NamedArguments.ToDictionary(na => na.Key, na => na.Value.Value)
            }).ToList()
        }).ToList();

        return new MethodAnalysisResult
        {
            IsValid = true,
            MethodName = methodSymbol.Name,
            HttpMethod = httpMethod,
            UrlTemplate = urlTemplate,
            ReturnType = GetReturnTypeDisplayString(methodSymbol.ReturnType),
            Parameters = parameters
        };
    }

    private void GenerateRequestSetup(StringBuilder sb, MethodAnalysisResult methodInfo)
    {
        // 日志记录：开始请求
        sb.AppendLine($"            _logger.LogDebug(\"开始HTTP {methodInfo.HttpMethod}请求: {{Url}}\", \"{methodInfo.UrlTemplate}\");");

        // 构建URL
        sb.AppendLine($"            var url = $\"{methodInfo.UrlTemplate}\";");

        // 创建HttpRequestMessage
        sb.AppendLine($"            using var request = new HttpRequestMessage(HttpMethod.{methodInfo.HttpMethod}, url);");
    }

    private void GenerateParameterHandling(StringBuilder sb, MethodAnalysisResult methodInfo)
    {
        var queryParams = methodInfo.Parameters
            .Where(p => p.Attributes.Any(attr => attr.Name == "QueryAttribute"))
            .ToList();

        if (queryParams.Any())
        {
            sb.AppendLine("            var queryParams = new List<string>();");
            foreach (var param in queryParams)
            {
                sb.AppendLine($"            if ({param.Name} != null)");
                sb.AppendLine($"                queryParams.Add($\"{param.Name}={{{param.Name}}}\");");
            }
            sb.AppendLine("            if (queryParams.Any())");
            sb.AppendLine("                url += \"?\" + string.Join(\"&\", queryParams);");
        }

        // 处理Header参数
        var headerParams = methodInfo.Parameters
            .Where(p => p.Attributes.Any(attr => attr.Name == "HeaderAttribute"))
            .ToList();

        foreach (var param in headerParams)
        {
            var headerAttr = param.Attributes.First(a => a.Name == "HeaderAttribute");
            var headerName = headerAttr.Arguments.FirstOrDefault()?.ToString() ?? param.Name;

            sb.AppendLine($"            if (!string.IsNullOrEmpty({param.Name}))");
            sb.AppendLine($"                request.Headers.Add(\"{headerName}\", {param.Name});");
        }

        // 处理Body参数
        var bodyParam = methodInfo.Parameters
            .FirstOrDefault(p => p.Attributes.Any(attr => attr.Name == "BodyAttribute"));

        if (bodyParam != null)
        {
            var bodyAttr = bodyParam.Attributes.First(a => a.Name == "BodyAttribute");
            var contentType = bodyAttr.NamedArguments.TryGetValue("ContentType", out var contentTypeArg) ? (contentTypeArg?.ToString() ?? "application/json") : "application/json";
            var useStringContent = bodyAttr.NamedArguments.TryGetValue("UseStringContent", out var useStringContentArg) ? bool.Parse((useStringContentArg?.ToString() ?? "false")) : false;

            sb.AppendLine($"            if ({bodyParam.Name} != null)");
            sb.AppendLine("            {");

            if (useStringContent)
            {
                sb.AppendLine($"                request.Content = new StringContent({bodyParam.Name}.ToString() ?? \"\", Encoding.UTF8, \"{contentType}\");");
            }
            else
            {
                sb.AppendLine($"                var jsonContent = JsonSerializer.Serialize({bodyParam.Name}, _jsonSerializerOptions);");
                sb.AppendLine($"                request.Content = new StringContent(jsonContent, Encoding.UTF8, \"{contentType}\");");
            }

            sb.AppendLine("            }");
        }
    }

    private void GenerateRequestExecution(StringBuilder sb, MethodAnalysisResult methodInfo)
    {
        sb.AppendLine("            try");
        sb.AppendLine("            {");
        sb.AppendLine("                using var response = await _httpClient.SendAsync(request);");
        sb.AppendLine("                var responseContent = await response.Content.ReadAsStringAsync();");
        sb.AppendLine();
        sb.AppendLine("                _logger.LogDebug(\"HTTP请求完成: {{StatusCode}}, 响应长度: {{ContentLength}}\", " +
                     "(int)response.StatusCode, responseContent?.Length ?? 0);");
        sb.AppendLine();
        sb.AppendLine("                if (!response.IsSuccessStatusCode)");
        sb.AppendLine("                {");
        sb.AppendLine("                    _logger.LogError(\"HTTP请求失败: {{StatusCode}}, 响应: {{Response}}\", " +
                     "(int)response.StatusCode, responseContent);");
        sb.AppendLine("                    throw new HttpRequestException($\"HTTP请求失败: {(int)response.StatusCode} - {response.ReasonPhrase}\");");
        sb.AppendLine("                }");
        sb.AppendLine();
        sb.AppendLine("                if (string.IsNullOrEmpty(responseContent))");
        sb.AppendLine("                {");
        sb.AppendLine("                    return default;");
        sb.AppendLine("                }");
        sb.AppendLine();
        sb.AppendLine($"                var result = JsonSerializer.Deserialize<{methodInfo.ReturnType}>(responseContent, _jsonSerializerOptions);");
        sb.AppendLine("                return result;");
        sb.AppendLine("            }");
        sb.AppendLine("            catch (System.Exception ex)");
        sb.AppendLine("            {");
        sb.AppendLine("                _logger.LogError(ex, \"HTTP请求异常: {{Url}}\", url);");
        sb.AppendLine("                throw;");
        sb.AppendLine("            }");
    }

    private string GetImplementationClassName(string interfaceName)
    {
        return interfaceName.StartsWith("I", StringComparison.Ordinal) && interfaceName.Length > 1 && char.IsUpper(interfaceName[1])
            ? interfaceName.Substring(1)
            : interfaceName + "Impl";
    }

    private string GetParameterList(IMethodSymbol methodSymbol)
    {
        return string.Join(", ", methodSymbol.Parameters.Select(p =>
            $"{p.Type.ToDisplayString()} {p.Name}"));
    }

    private object? GetAttributeArgumentValue(AttributeSyntax attribute, int index)
    {
        if (attribute.ArgumentList == null || index >= attribute.ArgumentList.Arguments.Count)
            return null;

        return attribute.ArgumentList.Arguments[index].Expression switch
        {
            LiteralExpressionSyntax literal => literal.Token.Value,
            _ => null
        };
    }

    private string GetReturnTypeDisplayString(ITypeSymbol returnType)
    {
        if (returnType is INamedTypeSymbol namedType && namedType.IsGenericType)
        {
            if (namedType.Name == "Task" && namedType.TypeArguments.Length == 1)
            {
                // 处理 Task<T> 的情况
                var genericType = namedType.TypeArguments[0];

                // 处理可空类型 Task<T?>
                if (genericType is INamedTypeSymbol genericNamedType &&
                    genericNamedType.IsGenericType &&
                    genericNamedType.Name == "Nullable")
                {
                    return $"{genericNamedType.TypeArguments[0].ToDisplayString()}?";
                }

                return genericType.ToDisplayString();
            }
        }

        return returnType.ToDisplayString();
    }

    // 辅助类
    private class MethodAnalysisResult
    {
        public bool IsValid { get; set; }
        public string MethodName { get; set; } = string.Empty;
        public string HttpMethod { get; set; } = string.Empty;
        public string UrlTemplate { get; set; } = string.Empty;
        public string ReturnType { get; set; } = string.Empty;
        public List<ParameterInfo> Parameters { get; set; } = [];

        public static MethodAnalysisResult Invalid => new() { IsValid = false };
    }

    private class ParameterInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public List<ParameterAttributeInfo> Attributes { get; set; } = [];
    }

    private class ParameterAttributeInfo
    {
        public string Name { get; set; } = string.Empty;
        public object?[] Arguments { get; set; } = [];
        public Dictionary<string, object?> NamedArguments { get; set; } = [];
    }
}