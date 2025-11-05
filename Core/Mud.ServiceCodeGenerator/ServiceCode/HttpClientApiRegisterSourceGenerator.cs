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
public class HttpClientApiRegisterSourceGenerator : WebApiSourceGenerator
{
    /// <summary>
    /// 执行HttpClient API注册源代码生成逻辑
    /// </summary>
    /// <param name="compilation">编译信息</param>
    /// <param name="interfaces">接口声明数组</param>
    /// <param name="context">源代码生成上下文</param>
    /// <summary>
    /// 执行HttpClient API注册源代码生成逻辑
    /// </summary>
    /// <param name="compilation">编译信息</param>
    /// <param name="interfaces">接口声明数组</param>
    /// <param name="context">源代码生成上下文</param>
    protected override void Execute(Compilation compilation, ImmutableArray<InterfaceDeclarationSyntax> interfaces, SourceProductionContext context)
    {
        if (compilation==null||interfaces.IsDefaultOrEmpty)
            return;

        var httpClientApis = new List<HttpClientApiInfo>();

        foreach (var interfaceSyntax in interfaces)
        {
            if (interfaceSyntax == null) continue;

            try
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
                var implementationName = GetImplementationClassName(interfaceSymbol.Name);

                // 使用基类方法获取命名空间
                var namespaceName = GetNamespaceName(interfaceSyntax);

                httpClientApis.Add(new HttpClientApiInfo(
                    interfaceSymbol.Name,
                    implementationName,
                    namespaceName,
                    baseUrl,
                    timeout));
            }
            catch (Exception ex)
            {
                // 使用基类的错误报告方法
                var errorDescriptor = new DiagnosticDescriptor(
                    "HTTPCLIENTREG001",
                    "HttpClient API Register Generation Error",
                    "Error generating HttpClient API registration for interface '{0}': {1}",
                    "Generation",
                    DiagnosticSeverity.Error,
                    true);

                ReportErrorDiagnostic(context, errorDescriptor, interfaceSyntax.Identifier.Text, ex);
            }
        }

        if (httpClientApis.Count == 0)
            return;

        // 生成源代码
        var sourceCode = GenerateSourceCode(httpClientApis);
        context.AddSource("HttpClientApiExtensions.g.cs", SourceText.From(sourceCode, Encoding.UTF8));
    }


    private string GenerateSourceCode(List<HttpClientApiInfo> apis)
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
