using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Text;

namespace Mud.ServiceCodeGenerator;

/// <summary>
/// HttpClient API 源生成器
/// <para>基于 Roslyn 技术，自动为标记了 [HttpClientApi] 特性的接口生成 HttpClient 实现类。</para>
/// <para>支持 HTTP 方法：Get, Post, Put, Delete, Patch, Head, Options。</para>
/// </summary>
/// <remarks>
/// <para>使用方式：</para>
/// <code>
/// [HttpClientApi]
/// public interface IDingTalkApi
/// {
///     [Get("/api/v1/user/{id}")]
///     Task&lt;UserDto&gt; GetUserAsync([Query] string id);
///     
///     [Post("/api/v1/user")]
///     Task&lt;UserDto&gt; CreateUserAsync([Body] UserDto user);
/// }
/// </code>
/// </remarks>
[Generator(LanguageNames.CSharp)]
public class HttpClientApiSourceGenerator : TransitiveCodeGenerator
{
    private const string HttpClientApiAttributeName = "HttpClientApiAttribute";
    private static readonly string[] SupportedHttpMethods = ["Get", "Post", "Put", "Delete", "Patch", "Head", "Options"];

    /// <summary>
    /// 初始化源生成器
    /// </summary>
    /// <param name="context">增量生成器初始化上下文</param>
    /// <remarks>
    /// 此方法设置源生成器的管道，包括：
    /// <list type="bullet">
    /// <item><description>查找标记了 [HttpClientApi] 特性的接口</description></item>
    /// <item><description>组合编译信息和接口声明</description></item>
    /// <item><description>注册源生成输出</description></item>
    /// </list>
    /// </remarks>
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

    /// <summary>
    /// 获取生成代码文件需要使用的命名空间
    /// </summary>
    /// <returns>命名空间集合</returns>
    /// <remarks>
    /// 重写基类方法，提供 HttpClient API 实现类所需的命名空间。
    /// </remarks>
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
                    // 验证接口是否包含有效的HTTP方法
                    if (!HasValidHttpMethods(interfaceSymbol, interfaceDecl))
                    {
                        ReportWarningDiagnostic(context,
                            new DiagnosticDescriptor(
                                "HTTPCLIENT002",
                                "HttpClient API警告",
                                $"接口{interfaceDecl.Identifier.Text}不包含有效的HTTP方法特性，跳过生成",
                                "Generation",
                                DiagnosticSeverity.Warning,
                                true),
                            interfaceDecl.Identifier.Text);
                        continue;
                    }

                    var sourceCode = GenerateImplementationClass(interfaceSymbol, interfaceDecl);
                    var className = GetImplementationClassName(interfaceSymbol.Name);
                    context.AddSource($"{className}.g.cs", SourceText.From(sourceCode, Encoding.UTF8));
                }
            }
            catch (InvalidOperationException ex)
            {
                // 特定异常：语法分析错误
                ReportErrorDiagnostic(context,
                    new DiagnosticDescriptor(
                        "HTTPCLIENT003",
                        "HttpClient API语法错误",
                        $"接口{interfaceDecl.Identifier.Text}的语法分析失败: {0}",
                        "Generation",
                        DiagnosticSeverity.Error,
                        true),
                    interfaceDecl.Identifier.Text, ex);
            }
            catch (ArgumentException ex)
            {
                // 特定异常：参数错误
                ReportErrorDiagnostic(context,
                    new DiagnosticDescriptor(
                        "HTTPCLIENT004",
                        "HttpClient API参数错误",
                        $"接口{interfaceDecl.Identifier.Text}的参数配置错误: {0}",
                        "Generation",
                        DiagnosticSeverity.Error,
                        true),
                    interfaceDecl.Identifier.Text, ex);
            }
            catch (System.Exception ex)
            {
                // 通用异常处理
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

    private void GenerateFileHeader(StringBuilder sb, string className, string namespaceName, string interfaceName)
    {
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("// 此代码由Mud.ServiceCodeGenerator源生成器自动生成，请勿手动修改");
        sb.AppendLine($"// 生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine();
        sb.AppendLine("#nullable enable");

        // 使用基类的命名空间
        var usingNamespaces = GetFileUsingNameSpaces();
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

        var httpMethod = httpMethodAttr.Name.ToString();
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

        // 构建URL - 处理路径参数替换
        var urlBuilder = new StringBuilder($"            var url = $\"{methodInfo.UrlTemplate}\";");

        // 处理路径参数替换
        var pathParams = methodInfo.Parameters
            .Where(p => p.Attributes.Any(attr => attr.Name == "PathAttribute" || attr.Name == "RouteAttribute"))
            .ToList();

        if (pathParams.Any())
        {
            urlBuilder.Clear();
            urlBuilder.AppendLine($"            var url = $\"{methodInfo.UrlTemplate}\";");

            foreach (var param in pathParams)
            {
                // 替换URL模板中的占位符 {paramName}
                urlBuilder.AppendLine($"            url = url.Replace(\"{{{param.Name}}}\", {param.Name}?.ToString() ?? \"\");");
            }
        }

        sb.AppendLine(urlBuilder.ToString());

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
                // 处理简单类型和复杂类型的查询参数
                if (IsSimpleType(param.Type))
                {
                    sb.AppendLine($"            if ({param.Name} != null)");
                    sb.AppendLine($"                queryParams.Add($\"{param.Name}={{{param.Name}}}\");");
                }
                else
                {
                    // 处理复杂类型（如对象）的查询参数
                    sb.AppendLine($"            if ({param.Name} != null)");
                    sb.AppendLine("            {");
                    sb.AppendLine($"                var properties = {param.Name}.GetType().GetProperties();");
                    sb.AppendLine("                foreach (var prop in properties)");
                    sb.AppendLine("                {");
                    sb.AppendLine("                    var value = prop.GetValue(param);");
                    sb.AppendLine("                    if (value != null)");
                    sb.AppendLine($"                        queryParams.Add($\"{{prop.Name}}={{value}}\");");
                    sb.AppendLine("                }");
                    sb.AppendLine("            }");
                }
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

    /// <summary>
    /// 判断是否为简单类型
    /// </summary>
    /// <param name="typeName">类型名称</param>
    /// <returns>是否为简单类型</returns>
    private bool IsSimpleType(string typeName)
    {
        var simpleTypes = new[] { "string", "int", "long", "float", "double", "decimal", "bool", "DateTime", "Guid" };
        return simpleTypes.Contains(typeName) ||
               typeName.StartsWith("string?", StringComparison.OrdinalIgnoreCase) || typeName.StartsWith("int?", StringComparison.OrdinalIgnoreCase) ||
               typeName.StartsWith("long?", StringComparison.OrdinalIgnoreCase) || typeName.StartsWith("float?", StringComparison.OrdinalIgnoreCase) ||
               typeName.StartsWith("double?", StringComparison.OrdinalIgnoreCase) || typeName.StartsWith("decimal?", StringComparison.OrdinalIgnoreCase) ||
               typeName.StartsWith("bool?", StringComparison.OrdinalIgnoreCase) || typeName.StartsWith("DateTime?", StringComparison.OrdinalIgnoreCase) ||
               typeName.StartsWith("Guid?", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 验证接口是否包含有效的HTTP方法
    /// </summary>
    /// <param name="interfaceSymbol">接口符号</param>
    /// <param name="interfaceDecl">接口声明语法</param>
    /// <returns>是否包含有效的HTTP方法</returns>
    private bool HasValidHttpMethods(INamedTypeSymbol interfaceSymbol, InterfaceDeclarationSyntax interfaceDecl)
    {
        var methods = interfaceSymbol.GetMembers().OfType<IMethodSymbol>();

        foreach (var methodSymbol in methods)
        {
            var methodSyntax = interfaceDecl.Members
                .OfType<MethodDeclarationSyntax>()
                .FirstOrDefault(m => m.Identifier.ValueText == methodSymbol.Name);

            if (methodSyntax != null)
            {
                var hasHttpMethod = methodSyntax.AttributeLists
                    .SelectMany(a => a.Attributes)
                    .Any(a => SupportedHttpMethods.Contains(a.Name.ToString()));

                if (hasHttpMethod)
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// 方法分析结果
    /// </summary>
    /// <remarks>
    /// 用于存储接口方法的分析信息，包括 HTTP 方法、URL 模板、参数等。
    /// </remarks>
    private class MethodAnalysisResult
    {
        /// <summary>
        /// 方法是否有效
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// 方法名称
        /// </summary>
        public string MethodName { get; set; } = string.Empty;

        /// <summary>
        /// HTTP 方法（GET, POST, PUT, DELETE, PATCH, HEAD, OPTIONS）
        /// </summary>
        public string HttpMethod { get; set; } = string.Empty;

        /// <summary>
        /// URL 模板，支持参数占位符
        /// </summary>
        public string UrlTemplate { get; set; } = string.Empty;

        /// <summary>
        /// 返回类型显示字符串
        /// </summary>
        public string ReturnType { get; set; } = string.Empty;

        /// <summary>
        /// 方法参数列表
        /// </summary>
        public List<ParameterInfo> Parameters { get; set; } = [];

        /// <summary>
        /// 无效的分析结果实例
        /// </summary>
        public static MethodAnalysisResult Invalid => new() { IsValid = false };
    }

    /// <summary>
    /// 参数信息
    /// </summary>
    /// <remarks>
    /// 存储方法参数的详细信息，包括参数名、类型和特性。
    /// </remarks>
    private class ParameterInfo
    {
        /// <summary>
        /// 参数名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 参数类型显示字符串
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// 参数特性列表
        /// </summary>
        public List<ParameterAttributeInfo> Attributes { get; set; } = [];
    }

    /// <summary>
    /// 参数特性信息
    /// </summary>
    /// <remarks>
    /// 存储参数特性的详细信息，包括特性名称、构造函数参数和命名参数。
    /// </remarks>
    private class ParameterAttributeInfo
    {
        /// <summary>
        /// 特性名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 构造函数参数数组
        /// </summary>
        public object?[] Arguments { get; set; } = [];

        /// <summary>
        /// 命名参数字典
        /// </summary>
        public Dictionary<string, object?> NamedArguments { get; set; } = [];
    }
}