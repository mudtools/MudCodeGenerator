// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.CodeGenerator 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.CodeGenerator;
using Mud.HttpUtils.Models;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace Mud.HttpUtils;

/// <summary>
/// 生成Http调用代码的源代码生成器基类
/// </summary>
/// <remarks>
/// 提供Web API相关的公共功能，包括HttpClient特性处理、HTTP方法验证等
/// </remarks>
internal abstract class HttpInvokeBaseSourceGenerator : TransitiveCodeGenerator
{

    #region Configuration

    /// <inheritdoc/>
    protected override System.Collections.ObjectModel.Collection<string> GetFileUsingNameSpaces()
    {
        return
        [
            "System",
            "System.Web",
            "System.Net.Http",
            "System.Text",
            "System.Text.Json",
            "System.Threading.Tasks",
            "System.Collections.Generic",
            "System.Linq",
            "Microsoft.Extensions.Logging",
            "Microsoft.Extensions.Options",
        ];
    }

    protected virtual string[] ApiWrapAttributeNames() => HttpClientGeneratorConstants.HttpClientApiAttributeNames;

    #endregion

    #region Generator Initialization and Execution

    /// <summary>
    /// 初始化源代码生成器
    /// </summary>
    /// <param name="context">初始化上下文</param>
    public override void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // 使用 SyntaxProvider 作为主要触发源
        // 不做任何过滤，让所有接口声明都通过，在 ExecuteGenerator 中再进行过滤
        var httpClientApiInterfaces = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: (node, c) => node is InterfaceDeclarationSyntax,
                transform: (ctx, c) => (InterfaceDeclarationSyntax?)ctx.Node)
            .Collect();

        // 组合 Compilation 和 AnalyzerConfigOptions
        var compilationAndOptions = context.CompilationProvider
            .Combine(context.AnalyzerConfigOptionsProvider);

        // 将接口列表与编译信息组合
        var completeData = httpClientApiInterfaces.Combine(compilationAndOptions);

        // 注册源代码生成器
        context.RegisterSourceOutput(completeData,
            (ctx, provider) => ExecuteGenerator(
                compilation: provider.Right.Left,
                interfaces: provider.Left,
                context: ctx,
                configOptionsProvider: provider.Right.Right));
    }

    /// <summary>
    /// 接口信息结构，包含语法节点和符号
    /// </summary>
    protected readonly struct InterfaceInfo
    {
        public readonly InterfaceDeclarationSyntax Syntax;
        public readonly INamedTypeSymbol Symbol;

        public InterfaceInfo(InterfaceDeclarationSyntax syntax, INamedTypeSymbol symbol)
        {
            Syntax = syntax;
            Symbol = symbol;
        }
    }

    /// <summary>
    /// 获取所有需要生成代码的接口声明（已弃用，保留兼容性）
    /// </summary>
    [Obsolete("Use SyntaxProvider directly in Initialize instead")]
    protected IncrementalValueProvider<ImmutableArray<InterfaceDeclarationSyntax?>> GetInterfaceDeclarationProvider(
        IncrementalGeneratorInitializationContext context,
        string[] attributeNames)
    {
        // 主要使用 SyntaxProvider 进行接口检测（编辑模式下立即响应）
        var syntaxInterfaces = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: (node, c) =>
                {
                    // 基础检查：是否为带特性的接口
                    if (node is not InterfaceDeclarationSyntax interfaceDecl)
                        return false;

                    return interfaceDecl.AttributeLists.Count > 0;
                },
                transform: (ctx, c) =>
                {
                    var interfaceNode = (InterfaceDeclarationSyntax)ctx.Node;

                    // 语法级别检查（编辑模式下最可靠）
                    if (HasTargetAttributeSyntax(interfaceNode, attributeNames))
                    {
                        return interfaceNode;
                    }

                    return null;
                })
            .Where(static s => s is not null)
            .Collect();

        // 返回 SyntaxProvider 的结果，作为主要的触发源
        return syntaxInterfaces;
    }

    /// <summary>
    /// 在语法级别检查接口是否有目标特性（不依赖语义模型，编辑模式下更可靠）
    /// </summary>
    protected bool HasTargetAttributeSyntax(InterfaceDeclarationSyntax interfaceNode, string[] attributeNames)
    {
        foreach (var attributeList in interfaceNode.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                // 获取特性名称，处理限定名（如 Mud.Common.CodeGenerator.HttpClientApi）
                var attributeName = attribute.Name.ToString();
                var originalName = attributeName;

                // 处理命名空间前缀（取最后一部分）
                var lastDotIndex = attributeName.LastIndexOf('.');
                if (lastDotIndex >= 0)
                {
                    attributeName = attributeName.Substring(lastDotIndex + 1);
                }

                // 移除Attribute后缀进行比较
                if (attributeName.EndsWith("Attribute"))
                {
                    attributeName = attributeName.Substring(0, attributeName.Length - 9);
                }

                foreach (var targetName in attributeNames)
                {
                    var cleanTargetName = targetName;
                    if (cleanTargetName.EndsWith("Attribute"))
                    {
                        cleanTargetName = cleanTargetName.Substring(0, cleanTargetName.Length - 9);
                    }

                    if (attributeName.Equals(cleanTargetName, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    /// <summary>
    /// 检查接口是否继承了带有目标特性的接口（仅检查继承关系）
    /// </summary>
    private bool InheritsTargetInterface(INamedTypeSymbol interfaceSymbol, string[] attributeNames)
    {
        if (interfaceSymbol == null || interfaceSymbol.TypeKind != TypeKind.Interface)
            return false;

        // 遍历所有基接口
        foreach (var baseInterface in interfaceSymbol.AllInterfaces)
        {
            var baseHasAttribute = baseInterface.GetAttributes()
                .Any(a => attributeNames.Contains(a.AttributeClass?.Name));

            if (baseHasAttribute)
                return true;
        }

        return false;
    }

    /// <summary>
    /// 判断是否应该为接口生成代码
    /// 条件：1. 接口直接标记了目标特性；2. 接口继承了标记了目标特性的接口
    /// </summary>
    /// <param name="interfaceSymbol">接口符号</param>
    /// <param name="attributeNames">目标特性名称数组</param>
    /// <returns>是否应该生成代码</returns>
    protected bool ShouldGenerateForInterface(INamedTypeSymbol interfaceSymbol, string[] attributeNames)
    {
        if (interfaceSymbol == null || interfaceSymbol.TypeKind != TypeKind.Interface)
            return false;

        // 检查接口是否直接标记了目标特性
        var hasDirectAttribute = interfaceSymbol.GetAttributes()
            .Any(a => attributeNames.Contains(a.AttributeClass?.Name));

        if (hasDirectAttribute)
            return true;

        // 检查接口是否继承了带有目标特性的接口
        // 遍历所有基接口
        foreach (var baseInterface in interfaceSymbol.AllInterfaces)
        {
            try
            {
                var baseHasAttribute = baseInterface.GetAttributes()
                    .Any(a => attributeNames.Contains(a.AttributeClass?.Name));

                if (baseHasAttribute)
                    return true;
            }
            catch
            {
                // 忽略无法解析的基接口（例如来自其他程序集，在设计时可能无法访问）
                // 继续检查其他基接口
                continue;
            }
        }

        return false;
    }

    /// <summary>
    /// 执行源代码生成逻辑
    /// </summary>
    /// <param name="compilation">编译信息</param>
    /// <param name="interfaces">接口声明数组</param>
    /// <param name="context">源代码生成上下文</param>
    /// <param name="configOptionsProvider">配置选项提供者</param>
    protected abstract void ExecuteGenerator(
        Compilation compilation,
        ImmutableArray<InterfaceDeclarationSyntax?> interfaces,
        SourceProductionContext context,
        AnalyzerConfigOptionsProvider configOptionsProvider);

    #endregion

    #region Interface and Method Helpers

    /// <summary>
    /// 获取包装接口名称
    /// </summary>
    protected string GetWrapInterfaceName(INamedTypeSymbol interfaceSymbol, AttributeData wrapAttribute)
    {
        string GetDefalultWrapInterfaceName(string interfaceName)
        {
            if (string.IsNullOrEmpty(interfaceName))
                return "NullOrEmptyWrapInterfaceName";

            if (interfaceName.EndsWith("api", StringComparison.OrdinalIgnoreCase))
                return interfaceName.Substring(0, interfaceName.Length - 3) + "Wrap";
            return interfaceName + "Wrap";
        }

        if (interfaceSymbol == null) return null;
        if (wrapAttribute == null) return GetDefalultWrapInterfaceName(interfaceSymbol.Name);

        // 检查特性参数中是否有指定的包装接口名称
        var wrapInterface = wrapAttribute.NamedArguments.FirstOrDefault(a => a.Key == "WrapInterface").Value.Value?.ToString();
        if (!string.IsNullOrEmpty(wrapInterface))
        {
            return wrapInterface;
        }

        // 默认在接口名称后添加"Wrap"
        return GetDefalultWrapInterfaceName(interfaceSymbol.Name);
    }

    /// <summary>
    /// 检查方法是否有效
    /// </summary>
    protected bool IsValidMethod(IMethodSymbol method)
    {
        if (method == null)
            return false;
        return method.GetAttributes()
            .Any(attr => HttpClientGeneratorConstants.SupportedHttpMethods.Contains(attr.AttributeClass?.Name));
    }

    /// <summary>
    /// 查找HTTP方法特性
    /// </summary>
    protected AttributeSyntax? FindHttpMethodAttribute(MethodDeclarationSyntax methodSyntax)
    {
        if (methodSyntax == null)
            return null;
        return methodSyntax.AttributeLists
            .SelectMany(a => a.Attributes)
            .FirstOrDefault(a => HttpClientGeneratorConstants.SupportedHttpMethods.Contains(a.Name.ToString()));
    }

    #endregion

    #region Common Utility Methods
    /// <summary>
    /// 检查参数是否具有指定的特性
    /// </summary>
    protected bool HasAttribute(ParameterInfo parameter, params string[] attributeNames)
    {
        if (parameter == null)
            return false;
        if (parameter.Attributes == null || parameter.Attributes.Count == 0)
            return false;
        return parameter.Attributes
            .Any(attr => attributeNames.Contains(attr.Name));
    }

    /// <summary>
    /// 根据特性名称过滤参数
    /// </summary>
    protected IReadOnlyList<ParameterInfo> FilterParametersByAttribute(IReadOnlyList<ParameterInfo> parameters, string[] attributeNames, bool exclude = false)
    {
        return exclude
            ? parameters.Where(p => !HasAttribute(p, attributeNames)).ToList()
            : parameters.Where(p => HasAttribute(p, attributeNames)).ToList();
    }

    /// <summary>
    /// 生成方法参数列表字符串
    /// </summary>
    protected string GenerateParameterList(IReadOnlyList<ParameterInfo> parameters)
    {
        if (parameters == null || !parameters.Any())
            return string.Empty;

        var parameterStrings = parameters.Select(parameter =>
        {
            var parameterStr = $"{parameter.Type} {parameter.Name}";

            // 处理可选参数
            if (parameter.HasDefaultValue && !string.IsNullOrEmpty(parameter.DefaultValueLiteral))
            {
                parameterStr += $" = {parameter.DefaultValueLiteral}";
            }

            return parameterStr;
        });

        return string.Join(", ", parameterStrings);
    }

    /// <summary>
    /// 生成正确的参数调用列表，确保token参数替换掉原来标记了[Token]特性的参数位置
    /// </summary>
    protected IReadOnlyList<string> GenerateCorrectParameterCallList(IReadOnlyList<ParameterInfo> originalParameters, IReadOnlyList<ParameterInfo> filteredParameters, string tokenParameterName)
    {
        var callParameters = new List<string>();

        foreach (var originalParam in originalParameters)
        {
            // 检查当前参数是否是Token参数
            if (HasAttribute(originalParam, HttpClientGeneratorConstants.TokenAttributeNames))
            {
                // 如果是Token参数，用token参数替换
                callParameters.Add(tokenParameterName);
            }
            else
            {
                // 如果不是Token参数，检查是否在过滤后的参数列表中
                var matchingFilteredParam = filteredParameters.FirstOrDefault(p => p.Name == originalParam.Name);
                if (matchingFilteredParam != null)
                {
                    callParameters.Add(matchingFilteredParam.Name);
                }
            }
        }

        return callParameters;
    }
    #endregion

    #region Semantic Model Cache
    /// <summary>
    /// 获取或创建语义模型，使用共享缓存提高性能
    /// </summary>
    internal static SemanticModel GetOrCreateSemanticModel(Compilation compilation, SyntaxTree syntaxTree)
    {
        return SemanticModelCache.GetOrCreate(compilation, syntaxTree);
    }
    #endregion
}
