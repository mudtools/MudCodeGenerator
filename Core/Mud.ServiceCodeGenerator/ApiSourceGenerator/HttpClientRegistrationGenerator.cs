
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Text;

namespace Mud.ServiceCodeGenerator;

/// <summary>
/// HttpClient API 注册源生成器
/// </summary>
/// <remarks>
/// 基于 Roslyn 技术，自动为标记了 [HttpClientApi] 特性的接口生成依赖注入注册代码
/// </remarks>
[Generator(LanguageNames.CSharp)]
public class HttpClientRegistrationGenerator : WebApiSourceGenerator
{
    /// <inheritdoc/>
    protected override void Execute(Compilation compilation, ImmutableArray<InterfaceDeclarationSyntax> interfaces, SourceProductionContext context)
    {
        if (compilation == null || interfaces.IsDefaultOrEmpty)
            return;

        var httpClientApis = CollectHttpClientApis(compilation, interfaces, context);
        if (httpClientApis.Count == 0)
            return;

        var sourceCode = GenerateSourceCode(compilation, httpClientApis);
        context.AddSource("HttpClientApiExtensions.g.cs", SourceText.From(sourceCode, Encoding.UTF8));
    }

    /// <inheritdoc/>
    protected override System.Collections.ObjectModel.Collection<string> GetFileUsingNameSpaces()
    {
        return ["System", "Microsoft.Extensions.DependencyInjection", "System.Runtime.CompilerServices", "System.Net.Http"];
    }

    private List<HttpClientApiInfo> CollectHttpClientApis(Compilation compilation, ImmutableArray<InterfaceDeclarationSyntax> interfaces, SourceProductionContext context)
    {
        var httpClientApis = new List<HttpClientApiInfo>();

        foreach (var interfaceSyntax in interfaces)
        {
            if (interfaceSyntax == null)
                continue;

            try
            {
                var apiInfo = ProcessInterface(compilation, interfaceSyntax);
                if (apiInfo != null)
                {
                    httpClientApis.Add(apiInfo);
                }
            }
            catch (Exception ex)
            {
                ReportInterfaceProcessingError(context, interfaceSyntax, ex);
            }
        }

        return httpClientApis;
    }

    private HttpClientApiInfo? ProcessInterface(Compilation compilation, InterfaceDeclarationSyntax interfaceSyntax)
    {
        var semanticModel = compilation.GetSemanticModel(interfaceSyntax.SyntaxTree);
        if (semanticModel.GetDeclaredSymbol(interfaceSyntax) is not INamedTypeSymbol interfaceSymbol)
            return null;

        var httpClientApiAttribute = GetHttpClientApiAttribute(interfaceSymbol);
        if (httpClientApiAttribute == null)
            return null;

        var (baseUrl, timeout) = ExtractAttributeParameters(httpClientApiAttribute);
        var implementationName = GetImplementationClassName(interfaceSymbol.Name);
        var namespaceName = GetNamespaceName(interfaceSyntax);

        return new HttpClientApiInfo(
            interfaceSymbol.Name,
            implementationName,
            namespaceName,
            baseUrl,
            timeout);
    }

    private (string BaseUrl, int Timeout) ExtractAttributeParameters(AttributeData httpClientApiAttribute)
    {
        var baseUrl = GetBaseUrlFromAttribute(httpClientApiAttribute);
        var timeout = GetTimeoutFromAttribute(httpClientApiAttribute);
        return (baseUrl, timeout);
    }

    private void ReportInterfaceProcessingError(SourceProductionContext context, InterfaceDeclarationSyntax interfaceSyntax, Exception ex)
    {
        var errorDescriptor = new DiagnosticDescriptor(
            "HTTPCLIENTREG001",
            "HttpClient API Register Generation Error",
            $"Error generating HttpClient API registration for interface '{{0}}': {{1}}",
            "Generation",
            DiagnosticSeverity.Error,
            true);

        ReportErrorDiagnostic(context, errorDescriptor, interfaceSyntax.Identifier.Text, ex);
    }

    private string GenerateSourceCode(Compilation compilation, List<HttpClientApiInfo> apis)
    {
        var codeBuilder = new StringBuilder();
        GenerateExtensionClass(compilation, codeBuilder, apis);
        return codeBuilder.ToString();
    }

    private void GenerateExtensionClass(Compilation compilation, StringBuilder codeBuilder, List<HttpClientApiInfo> apis)
    {
        GenerateFileHeader(codeBuilder);

        codeBuilder.AppendLine();
        var @namespace = compilation.AssemblyName;
        var targetNamespace = string.IsNullOrEmpty(@namespace) ? "Microsoft.Extensions.DependencyInjection" : @namespace;

        codeBuilder.AppendLine($"namespace {targetNamespace}");
        codeBuilder.AppendLine("{");
        codeBuilder.AppendLine($"    {CompilerGeneratedAttribute}");
        codeBuilder.AppendLine($"    {GeneratedCodeAttribute}");
        codeBuilder.AppendLine("    internal static class HttpClientApiExtensions");
        codeBuilder.AppendLine("    {");
        GenerateAddWebApiHttpClientMethod(codeBuilder, apis);
        codeBuilder.AppendLine("    }");
        codeBuilder.AppendLine("}");
    }

    private void GenerateAddWebApiHttpClientMethod(StringBuilder codeBuilder, List<HttpClientApiInfo> apis)
    {
        codeBuilder.AppendLine("        /// <summary>");
        codeBuilder.AppendLine("        /// 注册所有标记了 [HttpClientApi] 特性的接口及其 HttpClient 实现");
        codeBuilder.AppendLine("        /// </summary>");
        codeBuilder.AppendLine("        /// <param name=\"services\">服务集合</param>");
        codeBuilder.AppendLine("        /// <returns>服务集合，用于链式调用</returns>");
        codeBuilder.AppendLine($"        {CompilerGeneratedAttribute}");
        codeBuilder.AppendLine($"        {GeneratedCodeAttribute}");
        codeBuilder.AppendLine("        public static IServiceCollection AddWebApiHttpClient(this IServiceCollection services)");
        codeBuilder.AppendLine("        {");

        foreach (var api in apis)
        {
            GenerateHttpClientRegistration(codeBuilder, api);
        }

        codeBuilder.AppendLine("            return services;");
        codeBuilder.AppendLine("        }");
    }

    private void GenerateHttpClientRegistration(StringBuilder codeBuilder, HttpClientApiInfo api)
    {
        var fullyQualifiedInterface = $"global::{api.Namespace}.{api.InterfaceName}";
        var fullyQualifiedImplementation = $"global::{api.Namespace}.{api.ImplementationName}";

        codeBuilder.AppendLine($"            // 注册 {api.InterfaceName} 的 HttpClient 实现");
        codeBuilder.AppendLine($"            services.AddHttpClient<{fullyQualifiedInterface}, {fullyQualifiedImplementation}>(client =>");
        codeBuilder.AppendLine("            {");

        if (!string.IsNullOrEmpty(api.BaseUrl))
        {
            codeBuilder.AppendLine($"                client.BaseAddress = new Uri(\"{api.BaseUrl}\");");
        }

        codeBuilder.AppendLine($"                client.Timeout = TimeSpan.FromSeconds({api.Timeout});");
        codeBuilder.AppendLine("            });");
    }

    /// <summary>
    /// 表示 HttpClient API 的元数据信息
    /// </summary>
    private sealed class HttpClientApiInfo
    {
        public HttpClientApiInfo(string interfaceName, string implementationName, string namespaceName, string baseUrl, int timeout)
        {
            InterfaceName = interfaceName ?? throw new ArgumentNullException(nameof(interfaceName));
            ImplementationName = implementationName ?? throw new ArgumentNullException(nameof(implementationName));
            Namespace = namespaceName ?? throw new ArgumentNullException(nameof(namespaceName));
            BaseUrl = baseUrl ?? throw new ArgumentNullException(nameof(baseUrl));
            Timeout = timeout;
        }

        public string InterfaceName { get; }
        public string ImplementationName { get; }
        public string Namespace { get; }
        public string BaseUrl { get; }
        public int Timeout { get; }
    }
}
