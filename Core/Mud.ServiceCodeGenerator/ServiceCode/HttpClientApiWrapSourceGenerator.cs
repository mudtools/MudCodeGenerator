using System.Collections.Immutable;
using System.Text;

namespace Mud.ServiceCodeGenerator;

/// <summary>
/// HttpClientApiWrap 源代码生成器
/// 为标记有HttpClientApiWrap特性的接口生成二次包装接口
/// </summary>
[Generator]
public class HttpClientApiWrapSourceGenerator : WebApiSourceGenerator
{
    /// <summary>
    /// HttpClientApiWrap特性名称数组
    /// </summary>
    private readonly string[] HttpClientApiWrapAttributeName = ["HttpClientApiWrapAttribute", "HttpClientApiWrap"];
    private readonly string[] TokenAttributeNames = new[] { "TokenAttribute", "Token" };

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
        if (interfaces.IsDefaultOrEmpty)
            return;

        foreach (var interfaceDecl in interfaces)
        {
            try
            {
                var semanticModel = compilation.GetSemanticModel(interfaceDecl.SyntaxTree);
                var interfaceSymbol = semanticModel.GetDeclaredSymbol(interfaceDecl) as INamedTypeSymbol;

                if (interfaceSymbol == null || !HasValidHttpMethods(interfaceSymbol, interfaceDecl))
                    continue;

                var wrapAttribute = GetHttpClientApiWrapAttribute(interfaceSymbol);
                if (wrapAttribute == null)
                    continue;

                // 生成包装接口代码
                var generatedCode = GenerateWrapInterface(interfaceDecl, interfaceSymbol, wrapAttribute);
                if (!string.IsNullOrEmpty(generatedCode))
                {
                    var fileName = $"{interfaceSymbol.Name}.Wrap.g.cs";
                    context.AddSource(fileName, generatedCode);
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
    /// 生成包装接口代码
    /// </summary>
    private string GenerateWrapInterface(InterfaceDeclarationSyntax interfaceDecl, INamedTypeSymbol interfaceSymbol, AttributeData wrapAttribute)
    {
        var sb = new StringBuilder();

        // 获取命名空间
        var namespaceName = GetNamespaceName(interfaceDecl);
        if (!string.IsNullOrEmpty(namespaceName))
        {
            sb.AppendLine($"namespace {namespaceName};");
        }

        // 获取包装接口名称
        var wrapInterfaceName = GetWrapInterfaceName(interfaceSymbol, wrapAttribute);

        // 添加接口注释
        var xmlDoc = GetXmlDocumentation(interfaceDecl);
        if (!string.IsNullOrEmpty(xmlDoc))
        {
            sb.Append(xmlDoc);
        }

        sb.AppendLine($"public interface {wrapInterfaceName}");
        sb.AppendLine("{");

        // 生成接口方法
        var methods = interfaceSymbol.GetMembers().OfType<IMethodSymbol>().Where(m => IsValidMethod(m));
        foreach (var method in methods)
        {
            var methodSyntax = GetMethodDeclarationSyntax(method, interfaceDecl);
            if (methodSyntax != null)
            {
                var wrapMethodCode = GenerateWrapMethod(method, methodSyntax);
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
    /// 获取包装接口名称
    /// </summary>
    private string GetWrapInterfaceName(INamedTypeSymbol interfaceSymbol, AttributeData wrapAttribute)
    {
        // 检查特性参数中是否有指定的包装接口名称
        var wrapInterfaceArg = wrapAttribute.NamedArguments.FirstOrDefault(a => a.Key == "WrapInterface");
        if (!string.IsNullOrEmpty(wrapInterfaceArg.Value.Value?.ToString()))
        {
            return wrapInterfaceArg.Value.Value.ToString();
        }

        // 根据接口名称生成默认包装接口名称
        var interfaceName = interfaceSymbol.Name;
        if (interfaceName.EndsWith("Api", StringComparison.OrdinalIgnoreCase))
        {
            return interfaceName.Substring(0, interfaceName.Length - 3);
        }
        else if (interfaceName.StartsWith("I", StringComparison.OrdinalIgnoreCase) && interfaceName.Length > 1)
        {
            return interfaceName.Substring(1);
        }

        return interfaceName + "Wrap";
    }

    /// <summary>
    /// 获取XML文档注释
    /// </summary>
    private string GetXmlDocumentation(InterfaceDeclarationSyntax interfaceDecl)
    {
        var leadingTrivia = interfaceDecl.GetLeadingTrivia();
        var xmlDocTrivia = leadingTrivia.FirstOrDefault(t => t.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.SingleLineDocumentationCommentTrivia) ||
                                                           t.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.MultiLineDocumentationCommentTrivia));

        if (xmlDocTrivia != default)
        {
            return xmlDocTrivia.ToFullString();
        }

        return string.Empty;
    }

    /// <summary>
    /// 检查方法是否有效
    /// </summary>
    private bool IsValidMethod(IMethodSymbol method)
    {
        return method.GetAttributes().Any(attr => SupportedHttpMethods.Contains(attr.AttributeClass?.Name));
    }

    /// <summary>
    /// 获取方法的声明语法
    /// </summary>
    private MethodDeclarationSyntax? GetMethodDeclarationSyntax(IMethodSymbol method, InterfaceDeclarationSyntax interfaceDecl)
    {
        return interfaceDecl.Members
            .OfType<MethodDeclarationSyntax>()
            .FirstOrDefault(m => m.Identifier.ValueText == method.Name);
    }

    /// <summary>
    /// 生成包装方法
    /// </summary>
    private string GenerateWrapMethod(IMethodSymbol method, MethodDeclarationSyntax methodSyntax)
    {
        var sb = new StringBuilder();

        // 添加方法注释
        var methodDoc = GetMethodXmlDocumentation(methodSyntax);
        if (!string.IsNullOrEmpty(methodDoc))
        {
            sb.AppendLine(methodDoc);
        }

        // 方法签名
        sb.Append($"    {method.ReturnType} {method.Name}(");

        // 过滤掉标记了[Token]特性的参数，保留其他所有参数
        var filteredParameters = method.Parameters.Where(p => !HasTokenAttribute(p)).ToList();

        for (int i = 0; i < filteredParameters.Count; i++)
        {
            var parameter = filteredParameters[i];
            var parameterSyntax = GetParameterSyntax(parameter, methodSyntax);

            if (parameterSyntax != null)
            {
                // 获取参数类型和名称，但移除特性标记
                var parameterStr = $"{parameter.Type} {parameter.Name}";

                // 处理可选参数
                if (parameter.HasExplicitDefaultValue)
                {
                    parameterStr += $" = {GetDefaultValueLiteral(parameter.Type, parameter.ExplicitDefaultValue)}";
                }

                sb.Append(parameterStr);

                if (i < filteredParameters.Count - 1)
                {
                    sb.Append(", ");
                }
            }
        }

        sb.Append(");");

        return sb.ToString();
    }

    /// <summary>
    /// 获取方法的XML文档注释
    /// </summary>
    private string GetMethodXmlDocumentation(MethodDeclarationSyntax methodSyntax)
    {
        var leadingTrivia = methodSyntax.GetLeadingTrivia();
        var xmlDocTrivia = leadingTrivia.FirstOrDefault(t => t.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.SingleLineDocumentationCommentTrivia) ||
                                                           t.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.MultiLineDocumentationCommentTrivia));

        if (xmlDocTrivia != default)
        {
            // 简化处理：每行都添加4个空格的缩进
            var docLines = xmlDocTrivia.ToFullString().Split('\n');
            var result = new StringBuilder();
            
            foreach (var line in docLines)
            {
                var trimmedLine = line.TrimEnd();
                if (!string.IsNullOrWhiteSpace(trimmedLine))
                {
                    result.AppendLine($"    {trimmedLine}");
                }
            }
            
            return result.ToString().TrimEnd();
        }

        return string.Empty;
    }

    /// <summary>
    /// 检查参数是否有Token特性
    /// </summary>
    private bool HasTokenAttribute(IParameterSymbol parameter)
    {
        return parameter.GetAttributes()
            .Any(attr =>TokenAttributeNames.Contains(attr.AttributeClass?.Name));
    }

    /// <summary>
    /// 获取参数语法节点
    /// </summary>
    private ParameterSyntax? GetParameterSyntax(IParameterSymbol parameter, MethodDeclarationSyntax methodSyntax)
    {
        return methodSyntax.ParameterList.Parameters
            .FirstOrDefault(p => p.Identifier.ValueText == parameter.Name);
    }

    /// <summary>
    /// 获取默认值的字面量表示
    /// </summary>
    private string GetDefaultValueLiteral(ITypeSymbol type, object? defaultValue)
    {
        if (defaultValue == null)
        {
            // 对于CancellationToken类型，使用default而不是null
            if (type.Name == "CancellationToken")
            {
                return "default";
            }
            return "null";
        }

        if (type.SpecialType == SpecialType.System_String)
        {
            return $"\"{defaultValue}\"";
        }

        if (type.SpecialType == SpecialType.System_Boolean)
        {
            return defaultValue.ToString()?.ToLowerInvariant();
        }

        if (type.SpecialType == SpecialType.System_Char)
        {
            return $"'{(char)defaultValue}'";
        }

        return defaultValue.ToString() ?? "default";
    }
}