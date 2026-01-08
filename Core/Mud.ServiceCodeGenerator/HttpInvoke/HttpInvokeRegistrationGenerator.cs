// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.CodeGenerator 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text;

namespace Mud.ServiceCodeGenerator;

/// <summary>
/// HttpClient API 注册源生成器
/// </summary>
/// <remarks>
/// 基于 Roslyn 技术，自动为标记了 [HttpClientApi] 特性的接口生成依赖注入注册代码
/// </remarks>
[Generator(LanguageNames.CSharp)]
public class HttpInvokeRegistrationGenerator : HttpInvokeBaseSourceGenerator
{
    /// <summary>
    /// 语义模型缓存，使用弱引用避免内存泄漏
    /// </summary>
    private static readonly ConditionalWeakTable<SyntaxTree, SemanticModel> _semanticModelCache = new();



    /// <inheritdoc/>
    protected override void ExecuteGenerator(Compilation compilation,
        ImmutableArray<InterfaceDeclarationSyntax> interfaces,
        SourceProductionContext context,
        AnalyzerConfigOptionsProvider configOptionsProvider)
    {
        if (compilation == null || interfaces.IsDefaultOrEmpty)
            return;

        var httpClientApis = CollectHttpClientApis(compilation, interfaces, context);
        var httpClientWrapApis = CollectHttpClientWrapApis(compilation, interfaces, context);

        if (httpClientApis.Count == 0 && httpClientWrapApis.Count == 0)
            return;

        var sourceCode = GenerateSourceCode(compilation, httpClientApis, httpClientWrapApis, context);
        context.AddSource("HttpClientApiExtensions.g.cs", SourceText.From(sourceCode, Encoding.UTF8));
    }

    /// <summary>
    /// 获取或创建语义模型，使用缓存提高性能
    /// 使用ConditionalWeakTable避免内存泄漏
    /// </summary>
    private static SemanticModel GetOrCreateSemanticModel(Compilation compilation, SyntaxTree syntaxTree)
    {
        if (_semanticModelCache.TryGetValue(syntaxTree, out var model))
            return model;

        var newModel = compilation.GetSemanticModel(syntaxTree);
        _semanticModelCache.Add(syntaxTree, newModel);
        return newModel;
    }

    /// <inheritdoc/>
    protected override System.Collections.ObjectModel.Collection<string> GetFileUsingNameSpaces()
    {
        return ["System", "Microsoft.Extensions.DependencyInjection", "System.Runtime.CompilerServices", "System.Net.Http", "Microsoft.Extensions.Logging"];
    }

    private List<HttpClientApiInfo> CollectHttpClientApis(Compilation compilation, ImmutableArray<InterfaceDeclarationSyntax> interfaces, SourceProductionContext context)
    {
        return CollectApiInfos<HttpClientApiInfo>(compilation, interfaces, context, (compilation, interfaceSyntax) => ProcessInterface(compilation, interfaceSyntax, context));
    }

    private List<HttpClientWrapApiInfo> CollectHttpClientWrapApis(Compilation compilation, ImmutableArray<InterfaceDeclarationSyntax> interfaces, SourceProductionContext context)
    {
        return CollectApiInfos<HttpClientWrapApiInfo>(compilation, interfaces, context, (compilation, interfaceSyntax) => ProcessWrapInterface(compilation, interfaceSyntax, context));
    }

    /// <summary>
    /// 通用的 API 信息收集方法，消除重复代码
    /// </summary>
    private List<T> CollectApiInfos<T>(Compilation compilation,
        ImmutableArray<InterfaceDeclarationSyntax> interfaces,
        SourceProductionContext context,
        Func<Compilation, InterfaceDeclarationSyntax, T?> processor)
    {
        var apiInfos = new List<T>();

        foreach (var interfaceSyntax in interfaces)
        {
            if (interfaceSyntax == null)
                continue;

            try
            {
                var apiInfo = processor(compilation, interfaceSyntax);
                if (apiInfo != null)
                {
                    apiInfos.Add(apiInfo);
                }
            }
            catch (Exception ex)
            {
                ReportInterfaceProcessingError(context, interfaceSyntax, ex);
            }
        }

        return apiInfos;
    }

    private HttpClientApiInfo? ProcessInterface(Compilation compilation, InterfaceDeclarationSyntax interfaceSyntax, SourceProductionContext context)
    {
        var semanticModel = GetOrCreateSemanticModel(compilation, interfaceSyntax.SyntaxTree);
        if (semanticModel.GetDeclaredSymbol(interfaceSyntax) is not INamedTypeSymbol interfaceSymbol)
            return null;

        var httpClientApiAttribute = AttributeDataHelper.GetAttributeDataFromSymbol(interfaceSymbol, HttpClientGeneratorConstants.HttpClientApiAttributeNames);
        if (httpClientApiAttribute == null)
            return null;

        // 检查是否标记为忽略实现
        var ignoreImplementAttribute = AttributeDataHelper.GetAttributeDataFromSymbol(interfaceSymbol, HttpClientGeneratorConstants.IgnoreImplementAttributeNames);
        if (ignoreImplementAttribute != null)
            return null;

        var isAbstract = AttributeDataHelper.GetBoolValueFromAttribute(httpClientApiAttribute, HttpClientGeneratorConstants.IsAbstractProperty, false);
        if (isAbstract)
            return null;

        var (baseUrl, timeout) = ExtractAttributeParameters(httpClientApiAttribute);
        var registryGroupName = AttributeDataHelper.GetStringValueFromAttribute(httpClientApiAttribute, HttpClientGeneratorConstants.RegistryGroupNameProperty);

        // 验证 RegistryGroupName
        if (!CSharpCodeValidator.ValidateAndReportRegistryGroupName(context, interfaceSyntax.GetLocation(), registryGroupName))
            return null;

        var implementationName = TypeSymbolHelper.GetImplementationClassName(interfaceSymbol.Name);
        var namespaceName = SyntaxHelper.GetNamespaceName(interfaceSyntax);

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
        var baseUrl = AttributeDataHelper.GetStringValueFromAttributeConstructor(httpClientApiAttribute, HttpClientGeneratorConstants.BaseAddressProperty) ?? string.Empty;
        var timeout = AttributeDataHelper.GetIntValueFromAttribute(httpClientApiAttribute, HttpClientGeneratorConstants.TimeoutProperty, 100);
        return (baseUrl, timeout);
    }

    private HttpClientWrapApiInfo? ProcessWrapInterface(Compilation compilation, InterfaceDeclarationSyntax interfaceSyntax, SourceProductionContext context)
    {
        var semanticModel = GetOrCreateSemanticModel(compilation, interfaceSyntax.SyntaxTree);
        if (semanticModel.GetDeclaredSymbol(interfaceSyntax) is not INamedTypeSymbol interfaceSymbol)
            return null;

        var httpClientApiWrapAttribute = GetHttpClientApiWrapAttribute(interfaceSymbol);
        if (httpClientApiWrapAttribute == null)
            return null;

        // 检查是否标记为忽略包装接口
        var ignoreWrapInterfaceAttribute = AttributeDataHelper.GetAttributeDataFromSymbol(interfaceSymbol, HttpClientGeneratorConstants.IgnoreWrapInterfaceAttributeNames);
        if (ignoreWrapInterfaceAttribute != null)
            return null;

        // 从原始的 HttpClientApi 特性中获取 RegistryGroupName
        var httpClientApiAttribute = AttributeDataHelper.GetAttributeDataFromSymbol(interfaceSymbol, HttpClientGeneratorConstants.HttpClientApiAttributeNames);
        var registryGroupName = httpClientApiAttribute != null
            ? AttributeDataHelper.GetStringValueFromAttribute(httpClientApiAttribute, HttpClientGeneratorConstants.RegistryGroupNameProperty)
            : null;

        // 验证 RegistryGroupName
        if (!CSharpCodeValidator.ValidateAndReportRegistryGroupName(context, interfaceSyntax.GetLocation(), registryGroupName))
            return null;

        var (baseUrl, timeout) = ExtractAttributeParameters(httpClientApiWrapAttribute);
        var nonNullBaseUrl = baseUrl ?? string.Empty;
        var wrapInterfaceName = GetWrapInterfaceName(interfaceSymbol, httpClientApiWrapAttribute);
        var wrapClassName = TypeSymbolHelper.GetWrapClassName(wrapInterfaceName);
        var namespaceName = SyntaxHelper.GetNamespaceName(interfaceSyntax);

        return new HttpClientWrapApiInfo(
            interfaceSymbol.Name,
            wrapInterfaceName,
            wrapClassName,
            namespaceName,
            nonNullBaseUrl,
            timeout,
            registryGroupName);
    }

    private AttributeData? GetHttpClientApiWrapAttribute(INamedTypeSymbol interfaceSymbol)
    {
        if (interfaceSymbol == null)
            return null;

        return interfaceSymbol.GetAttributes()
            .FirstOrDefault(a => HttpClientGeneratorConstants.HttpClientApiWrapAttributeNames.Contains(a.AttributeClass?.Name));
    }

    private void ReportInterfaceProcessingError(SourceProductionContext context, InterfaceDeclarationSyntax interfaceSyntax, Exception ex)
    {
        ReportErrorDiagnostic(context, Diagnostics.HttpClientRegistrationGenerationError, interfaceSyntax.Identifier.Text, ex);
    }

    private string GenerateSourceCode(Compilation compilation, List<HttpClientApiInfo> apis, List<HttpClientWrapApiInfo> wrapApis, SourceProductionContext context)
    {
        var codeBuilder = new StringBuilder();
        GenerateExtensionClass(compilation, codeBuilder, apis, wrapApis, context);
        return codeBuilder.ToString();
    }

    private void GenerateExtensionClass(Compilation compilation, StringBuilder codeBuilder, List<HttpClientApiInfo> apis, List<HttpClientWrapApiInfo> wrapApis, SourceProductionContext context)
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
        GenerateAddWebApiHttpClientMethod(codeBuilder, apis, wrapApis, context);
        codeBuilder.AppendLine("    }");
        codeBuilder.AppendLine("}");
    }

    private void GenerateAddWebApiHttpClientMethod(StringBuilder codeBuilder, List<HttpClientApiInfo> apis, List<HttpClientWrapApiInfo> wrapApis, SourceProductionContext context)
    {
        GenerateDefaultRegistrationMethod(codeBuilder, apis);
        GenerateGroupedRegistrationMethods(codeBuilder, apis, context);

        // 生成独立的包装API注册函数
        if (wrapApis.Count > 0)
        {
            GenerateAddWebApiHttpClientWrapMethod(codeBuilder, wrapApis, context);
        }
    }

    /// <summary>
    /// 生成默认注册函数（用于未分组的APIs）
    /// </summary>
    private void GenerateDefaultRegistrationMethod(StringBuilder codeBuilder, List<HttpClientApiInfo> apis)
    {
        GenerateDefaultRegistrationMethodInternal(codeBuilder, apis, HttpClientApiInfoBaseExtensions, "AddWebApiHttpClient");
    }

    /// <summary>
    /// 通用的默认注册函数生成方法
    /// </summary>
    private void GenerateDefaultRegistrationMethodInternal<T>(StringBuilder codeBuilder, List<T> apis, Action<StringBuilder, T> registrationGenerator, string methodName)
    {
        var ungroupedApis = apis.Where(api => string.IsNullOrEmpty((api as HttpClientApiInfoBase)?.RegistryGroupName)).ToList();

        if (ungroupedApis.Count == 0)
            return;

        codeBuilder.AppendLine("        /// <summary>");
        codeBuilder.AppendLine("        /// 注册所有未分组的API服务");
        codeBuilder.AppendLine("        /// </summary>");
        codeBuilder.AppendLine("        /// <param name=\"services\">服务集合</param>");
        codeBuilder.AppendLine("        /// <returns>服务集合，用于链式调用</returns>");
        codeBuilder.AppendLine($"        {CompilerGeneratedAttribute}");
        codeBuilder.AppendLine($"        {GeneratedCodeAttribute}");
        codeBuilder.AppendLine($"        public static IServiceCollection {methodName}(this IServiceCollection services)");
        codeBuilder.AppendLine("        {");

        foreach (var api in ungroupedApis)
        {
            registrationGenerator(codeBuilder, api);
        }

        codeBuilder.AppendLine("            return services;");
        codeBuilder.AppendLine("        }");
    }

    /// <summary>
    /// HttpClientApiInfo 注册生成的委托
    /// </summary>
    private void HttpClientApiInfoBaseExtensions(StringBuilder codeBuilder, HttpClientApiInfo api)
    {
        GenerateHttpClientRegistration(codeBuilder, api);
    }

    /// <summary>
    /// 生成分组注册函数
    /// </summary>
    private void GenerateGroupedRegistrationMethods(StringBuilder codeBuilder, List<HttpClientApiInfo> apis, SourceProductionContext context)
    {
        GenerateGroupedRegistrations<HttpClientApiInfo>(
            codeBuilder,
            apis,
            HttpClientApiInfoBaseExtensions,
            "HttpClient",
            "注册所有标记了 [HttpClientApi] 特性且 RegistryGroupName = \"{0}\" 的接口及其 HttpClient 实现",
            context);
    }

    /// <summary>
    /// 通用的分组注册生成方法，用于处理 HttpClientApi 和 WrapApi
    /// </summary>
    private void GenerateGroupedRegistrations<T>(StringBuilder codeBuilder,
        List<T> apiInfos,
        Action<StringBuilder, T> registrationGenerator,
        string serviceType,
        string descriptionTemplate,
        SourceProductionContext context)
    {
        var groupedApis = apiInfos
            .Where(api => !string.IsNullOrEmpty((api as HttpClientApiInfoBase)?.RegistryGroupName))
            .GroupBy(api => (api as HttpClientApiInfoBase)!.RegistryGroupName!)
            .ToList();

        foreach (var group in groupedApis)
        {
            var description = descriptionTemplate.Replace("{0}", group.Key);
            GenerateGroupedRegistrationMethod(
                codeBuilder,
                group.Key,
                group,
                registrationGenerator,
                serviceType,
                description,
                context);
        }
    }

    /// <summary>
    /// 生成单个分组注册方法
    /// </summary>
    private void GenerateGroupedRegistrationMethod<T>(StringBuilder codeBuilder,
        string groupName,
        IEnumerable<T> apiInfos,
        Action<StringBuilder, T> registrationGenerator,
        string serviceType,
        string description,
        SourceProductionContext context)
    {
        // 验证 RegistryGroupName 是否为合法的 C# 标识符
        if (!CSharpCodeValidator.IsValidCSharpIdentifier(groupName))
        {
            CSharpCodeValidator.ValidateAndReportRegistryGroupName(context, Location.None, groupName);
            return;
        }

        codeBuilder.AppendLine();
        codeBuilder.AppendLine("        /// <summary>");
        codeBuilder.AppendLine($"        /// {description}");
        codeBuilder.AppendLine("        /// </summary>");
        codeBuilder.AppendLine("        /// <param name=\"services\">服务集合</param>");
        codeBuilder.AppendLine("        /// <returns>服务集合，用于链式调用</returns>");
        codeBuilder.AppendLine($"        {CompilerGeneratedAttribute}");
        codeBuilder.AppendLine($"        {GeneratedCodeAttribute}");
        codeBuilder.AppendLine($"        public static IServiceCollection Add{groupName}WebApi{serviceType}(this IServiceCollection services)");
        codeBuilder.AppendLine("        {");

        foreach (var api in apiInfos)
        {
            registrationGenerator(codeBuilder, api);
        }

        codeBuilder.AppendLine("            return services;");
        codeBuilder.AppendLine("        }");
    }

    private void GenerateAddWebApiHttpClientWrapMethod(StringBuilder codeBuilder, List<HttpClientWrapApiInfo> wrapApis, SourceProductionContext context)
    {
        GenerateDefaultWrapRegistrationMethod(codeBuilder, wrapApis);
        GenerateGroupedWrapRegistrationMethods(codeBuilder, wrapApis, context);
    }

    /// <summary>
    /// 生成默认包装注册函数（用于未分组的Wrap APIs）
    /// </summary>
    private void GenerateDefaultWrapRegistrationMethod(StringBuilder codeBuilder, List<HttpClientWrapApiInfo> wrapApis)
    {
        GenerateDefaultRegistrationMethodInternal(codeBuilder, wrapApis, HttpClientWrapApiInfoBaseExtensions, "AddWebApiHttpClientWrap");
    }

    /// <summary>
    /// HttpClientWrapApiInfo 注册生成的委托
    /// </summary>
    private void HttpClientWrapApiInfoBaseExtensions(StringBuilder codeBuilder, HttpClientWrapApiInfo wrapApi)
    {
        GenerateHttpClientWrapRegistration(codeBuilder, wrapApi);
    }

    /// <summary>
    /// 生成分组包装注册函数
    /// </summary>
    private void GenerateGroupedWrapRegistrationMethods(StringBuilder codeBuilder, List<HttpClientWrapApiInfo> wrapApis, SourceProductionContext context)
    {
        GenerateGroupedRegistrations<HttpClientWrapApiInfo>(
            codeBuilder,
            wrapApis,
            HttpClientWrapApiInfoBaseExtensions,
            "HttpClientWrap",
            "注册所有 RegistryGroupName = \"{0}\" 的包装接口及其包装实现类的瞬时服务",
            context);
    }

    private void GenerateHttpClientRegistration(StringBuilder codeBuilder, HttpClientApiInfo api)
    {
        var fullyQualifiedInterface = $"global::{api.Namespace}.{api.InterfaceName}";
        var fullyQualifiedImplementation = $"global::{api.Namespace}.Internal.{api.ImplementationName}";

        codeBuilder.AppendLine($"            // 注册 {api.InterfaceName} 的 HttpClient 包装实现类（瞬时服务）");
        codeBuilder.AppendLine($"            services.AddTransient<{fullyQualifiedInterface}, {fullyQualifiedImplementation}>();");
    }

    private void GenerateHttpClientWrapRegistration(StringBuilder codeBuilder, HttpClientWrapApiInfo wrapApi)
    {
        var fullyQualifiedWrapInterface = $"global::{wrapApi.Namespace}.{wrapApi.WrapInterfaceName}";
        var fullyQualifiedWrapClass = $"global::{wrapApi.Namespace}.{wrapApi.WrapClassName}";

        codeBuilder.AppendLine($"            // 注册 {wrapApi.WrapInterfaceName} 的包装实现类（瞬时服务）");
        codeBuilder.AppendLine($"            services.AddTransient<{fullyQualifiedWrapInterface}, {fullyQualifiedWrapClass}>();");
    }
}
