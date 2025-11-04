using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Text;

namespace Mud.ServiceCodeGenerator;

[Generator]
public class HttpClientApiRegisterSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // 1. 收集所有接口声明
        var interfaces = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => s is InterfaceDeclarationSyntax,
                transform: static (ctx, _) => (InterfaceDeclarationSyntax)ctx.Node)
            .Where(static i => i != null)
            .Collect();

        // 2. 获取编译对象
        var compilation = context.CompilationProvider;

        // 3. 合并接口和编译信息
        var combined = interfaces.Combine(compilation);

        // 4. 生成代码
        context.RegisterSourceOutput(combined, static (spc, source) => Execute(source.Left, source.Right, spc));
    }

    private static void Execute(
        ImmutableArray<InterfaceDeclarationSyntax> interfaces,
        Compilation compilation,
        SourceProductionContext context)
    {
        if (interfaces.IsDefaultOrEmpty)
            return;

        var httpClientApis = new List<HttpClientApiInfo>();

        foreach (var interfaceSyntax in interfaces)
        {
            var semanticModel = compilation.GetSemanticModel(interfaceSyntax.SyntaxTree);
            var interfaceSymbol = semanticModel.GetDeclaredSymbol(interfaceSyntax) as INamedTypeSymbol;
            if (interfaceSymbol == null) continue;

            // 检查是否有有效的 [HttpClientApi] 特性
            var httpClientApiAttribute = GetHttpClientApiAttribute(interfaceSymbol);
            if (httpClientApiAttribute == null) continue;

            // 获取特性参数
            var baseUrl = GetBaseUrlFromAttribute(httpClientApiAttribute);
            var timeout = GetTimeoutFromAttribute(httpClientApiAttribute);

            // 生成实现类名称 (约定：移除接口前缀 "I")
            var implementationName = interfaceSymbol.Name.StartsWith("I") &&
                                     char.IsUpper(interfaceSymbol.Name[1])
                ? interfaceSymbol.Name.Substring(1)
                : interfaceSymbol.Name + "Impl";

            httpClientApis.Add(new HttpClientApiInfo(
                interfaceSymbol.Name,
                implementationName,
                interfaceSymbol.ContainingNamespace.ToString(),
                baseUrl,
                timeout));
        }

        if (httpClientApis.Count == 0)
            return;

        // 生成源代码
        var sourceCode = GenerateSourceCode(httpClientApis);
        context.AddSource("HttpClientApiExtensions.g.cs", SourceText.From(sourceCode, Encoding.UTF8));
    }

    private static AttributeData? GetHttpClientApiAttribute(INamedTypeSymbol interfaceSymbol)
    {
        return interfaceSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name is "HttpClientApiAttribute" or "HttpClientApi");
    }

    private static string GetBaseUrlFromAttribute(AttributeData attribute)
    {
        // 获取构造函数参数 (基地址)
        var baseUrlArg = attribute.ConstructorArguments.FirstOrDefault();
        return baseUrlArg.Value?.ToString() ?? string.Empty;
    }

    private static int GetTimeoutFromAttribute(AttributeData attribute)
    {
        // 获取命名参数 Timeout
        var timeoutArg = attribute.NamedArguments.FirstOrDefault(a => a.Key == "Timeout");
        return timeoutArg.Value.Value is int value ? value : 100; // 默认100秒
    }

    private static string GenerateSourceCode(List<HttpClientApiInfo> apis)
    {
        var sb = new StringBuilder();
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Net.Http;");
        sb.AppendLine("using Microsoft.Extensions.DependencyInjection;");
        sb.AppendLine();
        sb.AppendLine("namespace Microsoft.Extensions.DependencyInjection");
        sb.AppendLine("{");
        sb.AppendLine("    public static class HttpClientApiExtensions");
        sb.AppendLine("    {");
        sb.AppendLine("        public static IServiceCollection AddWebApiHttpClient(this IServiceCollection services)");
        sb.AppendLine("        {");

        foreach (var api in apis)
        {
            var fullyQualifiedInterface = $"global::{api.Namespace}.{api.InterfaceName}";
            var fullyQualifiedImplementation = $"global::{api.Namespace}.{api.ImplementationName}";

            sb.AppendLine($"            services.AddHttpClient<{fullyQualifiedInterface}, {fullyQualifiedImplementation}>(client =>");
            sb.AppendLine("            {");
            sb.AppendLine($"                client.BaseAddress = new Uri(\"{api.BaseUrl}\");");
            sb.AppendLine($"                client.Timeout = TimeSpan.FromSeconds({api.Timeout});");
            sb.AppendLine("            });");
        }

        sb.AppendLine("            return services;");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private class HttpClientApiInfo(
        string interfaceName,
        string implementationName,
        string namespaceName,
        string baseUrl,
        int timeout)
    {
        public string InterfaceName { get; set; } = interfaceName;
        public string ImplementationName { get; set; } = implementationName;
        public string Namespace { get; set; } = namespaceName;
        public string BaseUrl { get; set; } = baseUrl;
        public int Timeout { get; set; } = timeout;
    }
}
