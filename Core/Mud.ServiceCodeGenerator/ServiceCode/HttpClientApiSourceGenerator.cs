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
public class HttpClientApiSourceGenerator : WebApiSourceGenerator
{
    private readonly string[] PathAttributes = ["PathAttribute", "Path", "RouteAttribute", "Route"];
    private const string QueryAttribute = "QueryAttribute";
    private const string HeaderAttribute = "HeaderAttribute";
    private const string BodyAttribute = "BodyAttribute";

    /// <summary>
    /// 获取生成代码文件需要使用的命名空间
    /// </summary>
    /// <returns>命名空间集合</returns>
    /// <remarks>
    /// 重写基类方法，提供 HttpClient API 实现类所需的命名空间。
    /// </remarks>
    protected override System.Collections.ObjectModel.Collection<string> GetFileUsingNameSpaces()
    {
        return ["System", "System.Web","System.Net.Http", "System.Text", "System.Text.Json",
                "System.Threading.Tasks", "System.Collections.Generic", "System.Linq",
                "Microsoft.Extensions.Logging", "Microsoft.Extensions.Options"];
    }

    /// <summary>
    /// 执行HttpClient API源代码生成逻辑
    /// </summary>
    /// <param name="compilation">编译信息</param>
    /// <param name="interfaces">接口声明数组</param>
    /// <param name="context">源代码生成上下文</param>
    /// <summary>
    /// 执行HttpClient API源代码生成逻辑
    /// </summary>
    /// <param name="compilation">编译信息</param>
    /// <param name="interfaces">接口声明数组</param>
    /// <param name="context">源代码生成上下文</param>
    protected override void Execute(Compilation compilation, ImmutableArray<InterfaceDeclarationSyntax> interfaces, SourceProductionContext context)
    {
        //Debugger.Launch();

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

                    var sourceCode = GenerateImplementationClass(compilation, interfaceSymbol, interfaceDecl);
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

    private string GenerateImplementationClass(Compilation compilation, INamedTypeSymbol interfaceSymbol, InterfaceDeclarationSyntax interfaceDecl)
    {
        var className = GetImplementationClassName(interfaceSymbol.Name);
        var namespaceName = GetNamespaceName(interfaceDecl);

        var sb = new StringBuilder();
        GenerateFileHeader(sb, className, namespaceName, interfaceSymbol.Name);

        // 为每个方法生成实现
        foreach (var methodSymbol in interfaceSymbol.GetMembers().OfType<IMethodSymbol>())
        {
            GenerateMethodImplementation(compilation, sb, methodSymbol, interfaceDecl);
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private void GenerateFileHeader(StringBuilder sb, string className, string namespaceName, string interfaceName)
    {
        GenerateHttpClientFileHeader(sb, className, namespaceName, interfaceName);
    }

    /// <summary>
    /// 生成HttpClient实现类的文件头部
    /// </summary>
    protected void GenerateHttpClientFileHeader(StringBuilder sb, string className, string namespaceName, string interfaceName)
    {
        GenerateFileHeader(sb);

        sb.AppendLine();
        sb.AppendLine($"namespace {namespaceName}");
        sb.AppendLine("{");
        sb.AppendLine($"    /// <summary>");
        sb.AppendLine($"    /// {interfaceName}的HttpClient实现类");
        sb.AppendLine($"    /// </summary>");
        sb.AppendLine("    [global::System.Runtime.CompilerServices.CompilerGenerated]");
        sb.AppendLine($"    internal partial class {className} : {interfaceName}");
        sb.AppendLine("    {");
        sb.AppendLine("        private readonly HttpClient _httpClient;");
        sb.AppendLine($"        private readonly ILogger<{className}> _logger;");
        sb.AppendLine("        private readonly JsonSerializerOptions _jsonSerializerOptions;");
        sb.AppendLine();
        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// 构建 <see cref = \"{className}\"/> 类的实例。");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine("        /// <param name=\"httpClient\">HttpClient实例</param>");
        sb.AppendLine("        /// <param name=\"logger\">日志记录器</param>");
        sb.AppendLine("        /// <param name=\"option\">Json序列化参数</param>");
        sb.AppendLine($"        public {className}(HttpClient httpClient, ILogger<{className}> logger, IOptions<JsonSerializerOptions> option)");
        sb.AppendLine("        {");
        sb.AppendLine("            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));");
        sb.AppendLine("            _logger = logger ?? throw new ArgumentNullException(nameof(logger));");
        sb.AppendLine("            _jsonSerializerOptions = option.Value;");
        sb.AppendLine("        }");
    }

    private void GenerateMethodImplementation(Compilation compilation, StringBuilder sb, IMethodSymbol methodSymbol, InterfaceDeclarationSyntax interfaceDecl)
    {
        //Debugger.Launch();

        var methodInfo = AnalyzeMethod(compilation, methodSymbol, interfaceDecl);
        if (!methodInfo.IsValid) return;

        sb.AppendLine();
        sb.AppendLine($"        /// <summary>");
        sb.AppendLine($"        /// 实现 {methodSymbol.Name} 方法");
        sb.AppendLine($"        /// </summary>");
        sb.AppendLine("        [global::System.Runtime.CompilerServices.CompilerGenerated]");
        sb.AppendLine($"        public async {methodSymbol.ReturnType} {methodSymbol.Name}({GetParameterList(methodSymbol)})");
        sb.AppendLine("        {");

        GenerateRequestSetup(sb, methodInfo);
        GenerateParameterHandling(sb, methodInfo);
        GenerateRequestExecution(sb, methodInfo);

        sb.AppendLine("        }");
    }

    private MethodAnalysisResult AnalyzeMethod(Compilation compilation, IMethodSymbol methodSymbol, InterfaceDeclarationSyntax interfaceDecl)
    {
        var methodSyntax = interfaceDecl.Members
     .OfType<MethodDeclarationSyntax>()
     .FirstOrDefault(m =>
     {
         var model = compilation.GetSemanticModel(m.SyntaxTree);
         var methodSymbolFromSyntax = model.GetDeclaredSymbol(m);
         return methodSymbolFromSyntax != null &&
                methodSymbolFromSyntax.Equals(methodSymbol, SymbolEqualityComparer.Default);
     });

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
        // 构建URL - 处理路径参数替换
        var urlTemplate = methodInfo.UrlTemplate;
        var pathParams = methodInfo.Parameters
            .Where(p => p.Attributes.Any(attr => PathAttributes.Contains(attr.Name)))
            .ToList();

        // 检查URL模板中是否包含路径参数
        var urlBuilder = new StringBuilder();
        if (pathParams.Any())
        {
            // 如果URL模板包含路径参数占位符，使用字符串插值构建URL
            var interpolatedUrl = urlTemplate;
            foreach (var param in pathParams)
            {
                // 检查URL模板是否包含该参数占位符
                if (urlTemplate.Contains($"{{{param.Name}}}"))
                {
                    interpolatedUrl = interpolatedUrl.Replace($"{{{param.Name}}}", $"{{{param.Name}}}");
                }
            }

            // 构建URL字符串插值表达式
            urlBuilder.Append($"            var url = $\"{interpolatedUrl}\";");
        }
        else
        {
            // 如果没有路径参数，直接使用模板
            urlBuilder.Append($"            var url = \"{urlTemplate}\";");
        }
        sb.AppendLine(urlBuilder.ToString());
        // 日志记录：开始请求
        sb.AppendLine($"            _logger.LogDebug(\"开始HTTP {methodInfo.HttpMethod}请求: {{Url}}\", url);");

        // 创建HttpRequestMessage
        sb.AppendLine($"            using var request = new HttpRequestMessage(HttpMethod.{methodInfo.HttpMethod}, url);");
    }

    private void GenerateParameterHandling(StringBuilder sb, MethodAnalysisResult methodInfo)
    {
        var queryParams = methodInfo.Parameters
            .Where(p => p.Attributes.Any(attr => attr.Name == QueryAttribute))
            .ToList();

        if (queryParams.Any())
        {
            sb.AppendLine($"            var queryParams = HttpUtility.ParseQueryString(string.Empty);");
            foreach (var param in queryParams)
            {
                // 处理简单类型和复杂类型的查询参数
                if (IsSimpleType(param.Type))
                {
                    if (IsStringType(param.Type))
                    {
                        sb.AppendLine($"            if (!string.IsNullOrEmpty({param.Name}))");
                        sb.AppendLine("            {");
                        sb.AppendLine($"                {param.Name} = HttpUtility.UrlEncode({param.Name});");
                        sb.AppendLine($"                queryParams.Add(\"{param.Name}\",{param.Name});");
                        sb.AppendLine("            }");
                    }
                    else
                    {
                        sb.AppendLine($"            if ({param.Name} != null)");
                        sb.AppendLine($"                queryParams.Add(\"{param.Name}\",{param.Name}.ToString());");
                    }
                }
                else
                {
                    // 处理复杂类型（如对象）的查询参数
                    sb.AppendLine($"            if ({param.Name} != null)");
                    sb.AppendLine("            {");
                    sb.AppendLine($"                var properties = {param.Name}.GetType().GetProperties();");
                    sb.AppendLine();
                    sb.AppendLine("                foreach (var prop in properties)");
                    sb.AppendLine("                {");
                    sb.AppendLine($"                    var value = prop.GetValue({param.Name});");
                    sb.AppendLine("                    if (value != null)");
                    sb.AppendLine("                    {");
                    sb.AppendLine($"                        queryParams.Add(\"{param.Name}\", HttpUtility.UrlEncode(value.ToString()));");
                    sb.AppendLine("                    }");
                    sb.AppendLine("                }");
                    sb.AppendLine("            }");
                }
            }
            sb.AppendLine("            if (queryParams.Count > 0)");
            sb.AppendLine("                url += \"?\" + string.Join(\"&\", queryParams);");
        }

        // 处理Header参数
        var headerParams = methodInfo.Parameters
            .Where(p => p.Attributes.Any(attr => attr.Name == HeaderAttribute))
            .ToList();

        foreach (var param in headerParams)
        {
            var headerAttr = param.Attributes.First(a => a.Name == HeaderAttribute);
            var headerName = headerAttr.Arguments.FirstOrDefault()?.ToString() ?? param.Name;

            sb.AppendLine($"            if (!string.IsNullOrEmpty({param.Name}))");
            sb.AppendLine($"                request.Headers.Add(\"{headerName}\", {param.Name});");
        }

        // 处理Body参数
        var bodyParam = methodInfo.Parameters
            .FirstOrDefault(p => p.Attributes.Any(attr => attr.Name == BodyAttribute));

        if (bodyParam != null)
        {
            var bodyAttr = bodyParam.Attributes.First(a => a.Name == BodyAttribute);
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

    private string GetCancellationTokenParam(MethodAnalysisResult methodInfo)
    {
        var hasCancellationToken = methodInfo.Parameters.Any(p => p.Type.Contains("CancellationToken"));
        var cancellationTokenParam = methodInfo.Parameters.FirstOrDefault(p => p.Type.Contains("CancellationToken"));
        var cancellationTokenArg = hasCancellationToken ? $", {cancellationTokenParam?.Name ?? "CancellationToken.None"}" : "";
        return cancellationTokenArg;
    }

    private void GenerateRequestExecution(StringBuilder sb, MethodAnalysisResult methodInfo)
    {
        var cancellationTokenArg = GetCancellationTokenParam(methodInfo);

        sb.AppendLine("            try");
        sb.AppendLine("            {");
        sb.AppendLine($"                using var response = await _httpClient.SendAsync(request{cancellationTokenArg});");
        sb.AppendLine("                _logger.LogDebug(\"HTTP请求完成: {StatusCode}\", (int)response.StatusCode);");
        sb.AppendLine();
        sb.AppendLine("                if (!response.IsSuccessStatusCode)");
        sb.AppendLine("                {");
        sb.AppendLine("                    var errorContent = await response.Content.ReadAsStringAsync();");
        sb.AppendLine("                    _logger.LogError(\"HTTP请求失败: {StatusCode}, 响应: {Response}\", (int)response.StatusCode, errorContent);");
        sb.AppendLine("                    throw new HttpRequestException($\"HTTP请求失败: {(int)response.StatusCode}\");");
        sb.AppendLine("                }");
        sb.AppendLine("                using var stream = await response.Content.ReadAsStreamAsync();");
        sb.AppendLine();
        sb.AppendLine("                if (stream.Length == 0)");
        sb.AppendLine("                {");
        sb.AppendLine("                    return default;");
        sb.AppendLine("                }");
        sb.AppendLine();
        sb.AppendLine($"                var result = await JsonSerializer.DeserializeAsync<{methodInfo.ReturnType}>(stream, _jsonSerializerOptions{cancellationTokenArg});");
        sb.AppendLine("                return result;");
        sb.AppendLine("            }");
        sb.AppendLine("            catch (System.Exception ex)");
        sb.AppendLine("            {");
        sb.AppendLine("                _logger.LogError(ex, \"HTTP请求异常: {{Url}}\", url);");
        sb.AppendLine("                throw;");
        sb.AppendLine("            }");
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

    private bool IsStringType(string typeName)
    {
        return typeName.Equals("string", StringComparison.OrdinalIgnoreCase) || typeName.Equals("string?", StringComparison.OrdinalIgnoreCase);
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