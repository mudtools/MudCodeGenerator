
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
    protected override void ExecuteGenerator(Compilation compilation, ImmutableArray<InterfaceDeclarationSyntax> interfaces, SourceProductionContext context)
    {
        if (compilation == null || interfaces.IsDefaultOrEmpty)
            return;

        var httpClientApis = CollectHttpClientApis(compilation, interfaces, context);
        var httpClientWrapApis = CollectHttpClientWrapApis(compilation, interfaces, context);

        if (httpClientApis.Count == 0 && httpClientWrapApis.Count == 0)
            return;

        var sourceCode = GenerateSourceCode(compilation, httpClientApis, httpClientWrapApis);
        context.AddSource("HttpClientApiExtensions.g.cs", SourceText.From(sourceCode, Encoding.UTF8));
    }

    /// <inheritdoc/>
    protected override System.Collections.ObjectModel.Collection<string> GetFileUsingNameSpaces()
    {
        return ["System", "Microsoft.Extensions.DependencyInjection", "System.Runtime.CompilerServices", "System.Net.Http", "Microsoft.Extensions.Logging"];
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

    private List<HttpClientWrapApiInfo> CollectHttpClientWrapApis(Compilation compilation, ImmutableArray<InterfaceDeclarationSyntax> interfaces, SourceProductionContext context)
    {
        var httpClientWrapApis = new List<HttpClientWrapApiInfo>();

        foreach (var interfaceSyntax in interfaces)
        {
            if (interfaceSyntax == null)
                continue;

            try
            {
                var wrapApiInfo = ProcessWrapInterface(compilation, interfaceSyntax);
                if (wrapApiInfo != null)
                {
                    httpClientWrapApis.Add(wrapApiInfo);
                }
            }
            catch (Exception ex)
            {
                ReportInterfaceProcessingError(context, interfaceSyntax, ex);
            }
        }

        return httpClientWrapApis;
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
        var registryGroupName = GetRegistryGroupNameFromAttribute(httpClientApiAttribute);
        var implementationName = GetImplementationClassName(interfaceSymbol.Name);
        var namespaceName = GetNamespaceName(interfaceSyntax);

        return new HttpClientApiInfo(
            interfaceSymbol.Name,
            implementationName,
            namespaceName,
            baseUrl,
            timeout,
            registryGroupName);
    }

    private (string BaseUrl, int Timeout) ExtractAttributeParameters(AttributeData httpClientApiAttribute)
    {
        var baseUrl = GetBaseUrlFromAttribute(httpClientApiAttribute);
        var timeout = GetTimeoutFromAttribute(httpClientApiAttribute);
        return (baseUrl, timeout);
    }

    private HttpClientWrapApiInfo? ProcessWrapInterface(Compilation compilation, InterfaceDeclarationSyntax interfaceSyntax)
    {
        var semanticModel = compilation.GetSemanticModel(interfaceSyntax.SyntaxTree);
        if (semanticModel.GetDeclaredSymbol(interfaceSyntax) is not INamedTypeSymbol interfaceSymbol)
            return null;

        var httpClientApiWrapAttribute = GetHttpClientApiWrapAttribute(interfaceSymbol);
        if (httpClientApiWrapAttribute == null)
            return null;

        // 从原始的 HttpClientApi 特性中获取 RegistryGroupName
        var httpClientApiAttribute = GetHttpClientApiAttribute(interfaceSymbol);
        var registryGroupName = GetRegistryGroupNameFromAttribute(httpClientApiAttribute);
        
        var (baseUrl, timeout) = ExtractAttributeParameters(httpClientApiWrapAttribute);
        var wrapInterfaceName = GetWrapInterfaceName(interfaceSymbol, httpClientApiWrapAttribute);
        var wrapClassName = GetWrapClassName(wrapInterfaceName);
        var namespaceName = GetNamespaceName(interfaceSyntax);

        return new HttpClientWrapApiInfo(
            interfaceSymbol.Name,
            wrapInterfaceName,
            wrapClassName,
            namespaceName,
            baseUrl,
            timeout,
            registryGroupName);
    }

    private AttributeData? GetHttpClientApiWrapAttribute(INamedTypeSymbol interfaceSymbol)
    {
        if (interfaceSymbol == null)
            return null;

        return interfaceSymbol.GetAttributes()
            .FirstOrDefault(a => GeneratorConstants.HttpClientApiWrapAttributeNames.Contains(a.AttributeClass?.Name));
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

    private string GenerateSourceCode(Compilation compilation, List<HttpClientApiInfo> apis, List<HttpClientWrapApiInfo> wrapApis)
    {
        var codeBuilder = new StringBuilder();
        GenerateExtensionClass(compilation, codeBuilder, apis, wrapApis);
        return codeBuilder.ToString();
    }

    private void GenerateExtensionClass(Compilation compilation, StringBuilder codeBuilder, List<HttpClientApiInfo> apis, List<HttpClientWrapApiInfo> wrapApis)
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
        GenerateAddWebApiHttpClientMethod(codeBuilder, apis, wrapApis);
        codeBuilder.AppendLine("    }");
        codeBuilder.AppendLine("}");
    }

    private void GenerateAddWebApiHttpClientMethod(StringBuilder codeBuilder, List<HttpClientApiInfo> apis, List<HttpClientWrapApiInfo> wrapApis)
    {
        // 按RegistryGroupName分组APIs
        var groupedApis = apis.Where(api => !string.IsNullOrEmpty(api.RegistryGroupName))
                              .GroupBy(api => api.RegistryGroupName!)
                              .ToList();
        
        // 未分组的APIs
        var ungroupedApis = apis.Where(api => string.IsNullOrEmpty(api.RegistryGroupName)).ToList();

        // 生成默认注册函数（用于未分组的APIs）
        if (ungroupedApis.Count > 0)
        {
            codeBuilder.AppendLine("        /// <summary>");
            codeBuilder.AppendLine("        /// 注册所有未分组的标记了 [HttpClientApi] 特性的接口及其 HttpClient 实现");
            codeBuilder.AppendLine("        /// </summary>");
            codeBuilder.AppendLine("        /// <param name=\"services\">服务集合</param>");
            codeBuilder.AppendLine("        /// <returns>服务集合，用于链式调用</returns>");
            codeBuilder.AppendLine($"        {CompilerGeneratedAttribute}");
            codeBuilder.AppendLine($"        {GeneratedCodeAttribute}");
            codeBuilder.AppendLine("        public static IServiceCollection AddWebApiHttpClient(this IServiceCollection services)");
            codeBuilder.AppendLine("        {");

            foreach (var api in ungroupedApis)
            {
                GenerateHttpClientRegistration(codeBuilder, api);
            }

            codeBuilder.AppendLine("            return services;");
            codeBuilder.AppendLine("        }");
        }

        // 生成分组注册函数
        foreach (var group in groupedApis)
        {
            var groupName = group.Key;
            codeBuilder.AppendLine();
            codeBuilder.AppendLine("        /// <summary>");
            codeBuilder.AppendLine($"        /// 注册所有标记了 [HttpClientApi] 特性且 RegistryGroupName = \"{groupName}\" 的接口及其 HttpClient 实现");
            codeBuilder.AppendLine("        /// </summary>");
            codeBuilder.AppendLine("        /// <param name=\"services\">服务集合</param>");
            codeBuilder.AppendLine("        /// <returns>服务集合，用于链式调用</returns>");
            codeBuilder.AppendLine($"        {CompilerGeneratedAttribute}");
            codeBuilder.AppendLine($"        {GeneratedCodeAttribute}");
            codeBuilder.AppendLine($"        public static IServiceCollection Add{groupName}WebApiHttpClient(this IServiceCollection services)");
            codeBuilder.AppendLine("        {");

            foreach (var api in group)
            {
                GenerateHttpClientRegistration(codeBuilder, api);
            }

            codeBuilder.AppendLine("            return services;");
            codeBuilder.AppendLine("        }");
        }

        // 生成独立的包装API注册函数
        if (wrapApis.Count > 0)
        {
            GenerateAddWebApiHttpClientWrapMethod(codeBuilder, wrapApis);
        }
    }

    private void GenerateAddWebApiHttpClientWrapMethod(StringBuilder codeBuilder, List<HttpClientWrapApiInfo> wrapApis)
    {
        // 按RegistryGroupName分组Wrap APIs
        var groupedWrapApis = wrapApis.Where(api => !string.IsNullOrEmpty(api.RegistryGroupName))
                                     .GroupBy(api => api.RegistryGroupName!)
                                     .ToList();
        
        // 未分组的Wrap APIs
        var ungroupedWrapApis = wrapApis.Where(api => string.IsNullOrEmpty(api.RegistryGroupName)).ToList();

        // 生成默认包装注册函数（用于未分组的Wrap APIs）
        if (ungroupedWrapApis.Count > 0)
        {
            codeBuilder.AppendLine();
            codeBuilder.AppendLine("        /// <summary>");
            codeBuilder.AppendLine("        /// 注册所有未分组的包装接口及其包装实现类的瞬时服务");
            codeBuilder.AppendLine("        /// </summary>");
            codeBuilder.AppendLine("        /// <param name=\"services\">服务集合</param>");
            codeBuilder.AppendLine("        /// <returns>服务集合，用于链式调用</returns>");
            codeBuilder.AppendLine($"        {CompilerGeneratedAttribute}");
            codeBuilder.AppendLine($"        {GeneratedCodeAttribute}");
            codeBuilder.AppendLine("        public static IServiceCollection AddWebApiHttpClientWrap(this IServiceCollection services)");
            codeBuilder.AppendLine("        {");

            foreach (var wrapApi in ungroupedWrapApis)
            {
                GenerateHttpClientWrapRegistration(codeBuilder, wrapApi);
            }

            codeBuilder.AppendLine("            return services;");
            codeBuilder.AppendLine("        }");
        }

        // 生成分组包装注册函数
        foreach (var group in groupedWrapApis)
        {
            var groupName = group.Key;
            codeBuilder.AppendLine();
            codeBuilder.AppendLine("        /// <summary>");
            codeBuilder.AppendLine($"        /// 注册所有 RegistryGroupName = \"{groupName}\" 的包装接口及其包装实现类的瞬时服务");
            codeBuilder.AppendLine("        /// </summary>");
            codeBuilder.AppendLine("        /// <param name=\"services\">服务集合</param>");
            codeBuilder.AppendLine("        /// <returns>服务集合，用于链式调用</returns>");
            codeBuilder.AppendLine($"        {CompilerGeneratedAttribute}");
            codeBuilder.AppendLine($"        {GeneratedCodeAttribute}");
            codeBuilder.AppendLine($"        public static IServiceCollection Add{groupName}WebApiHttpClientWrap(this IServiceCollection services)");
            codeBuilder.AppendLine("        {");

            foreach (var wrapApi in group)
            {
                GenerateHttpClientWrapRegistration(codeBuilder, wrapApi);
            }

            codeBuilder.AppendLine("            return services;");
            codeBuilder.AppendLine("        }");
        }
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

    private void GenerateHttpClientWrapRegistration(StringBuilder codeBuilder, HttpClientWrapApiInfo wrapApi)
    {
        var fullyQualifiedWrapInterface = $"global::{wrapApi.Namespace}.{wrapApi.WrapInterfaceName}";
        var fullyQualifiedWrapClass = $"global::{wrapApi.Namespace}.{wrapApi.WrapClassName}";

        codeBuilder.AppendLine($"            // 注册 {wrapApi.WrapInterfaceName} 的包装实现类（瞬时服务）");
        codeBuilder.AppendLine($"            services.AddTransient<{fullyQualifiedWrapInterface}, {fullyQualifiedWrapClass}>();");
    }
}
