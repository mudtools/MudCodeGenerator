using System.Collections.Immutable;
using System.Text;

namespace Mud.ServiceCodeGenerator;

/// <summary>
/// 生成器常量配置
/// </summary>
internal static class GeneratorConstants
{
    // 特性名称
    public static readonly string[] HttpClientApiWrapAttributeNames = ["HttpClientApiWrapAttribute", "HttpClientApiWrap"];
    public static readonly string[] TokenAttributeNames = ["TokenAttribute", "Token"];

    // 默认值
    public const string DefaultTokenManageInterface = "ITokenManage";
    public const string DefaultWrapSuffix = "Wrap";

    // 诊断ID
    public const string GeneratorErrorDiagnosticId = "MCG001";
    public const string GeneratorWarningDiagnosticId = "MCG002";
}

/// <summary>
/// HttpClientApiWrap 源代码生成器
/// 为标记有HttpClientApiWrap特性的接口生成二次包装实现类
/// </summary>
[Generator]
public class HttpClientApiWrapSourceGenerator : WebApiSourceGenerator
{
    /// <inheritdoc/>
    protected override System.Collections.ObjectModel.Collection<string> GetFileUsingNameSpaces()
    {
        return
        [
            "System",
            "System.Text",
            "System.Text.Json",
            "System.Threading.Tasks",
            "System.Collections.Generic",
            "System.Linq",
            "Microsoft.Extensions.Logging",
            "Microsoft.Extensions.Options"
        ];
    }

    /// <summary>
    /// 初始化源代码生成器
    /// </summary>
    /// <param name="context">初始化上下文</param>
    public override void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // 使用自定义方法查找标记了[HttpClientApiWrap]的接口
        var interfaceDeclarations = GetClassDeclarationProvider<InterfaceDeclarationSyntax>(context, GeneratorConstants.HttpClientApiWrapAttributeNames);

        // 组合编译和接口声明
        var compilationAndInterfaces = context.CompilationProvider.Combine(interfaceDeclarations);

        // 注册源生成
        context.RegisterSourceOutput(compilationAndInterfaces,
             (spc, source) => Execute(source.Left, source.Right, spc));
    }


    /// <summary>
    /// 更新Execute方法以使用验证
    /// </summary>
    protected override void Execute(Compilation compilation, ImmutableArray<InterfaceDeclarationSyntax> interfaces, SourceProductionContext context)
    {
        if (compilation == null || interfaces.IsDefaultOrEmpty)
            return;

        foreach (var interfaceDecl in interfaces)
        {
            try
            {
                var semanticModel = compilation.GetSemanticModel(interfaceDecl.SyntaxTree);
                var interfaceSymbol = semanticModel.GetDeclaredSymbol(interfaceDecl);
                var wrapAttribute = GetHttpClientApiWrapAttribute(interfaceSymbol);

                if (!ValidateInterfaceConfiguration(interfaceSymbol, wrapAttribute, context, interfaceDecl))
                    continue;

                // 生成包装接口代码
                var wrapInterfaceCode = GenerateWrapInterface(compilation, interfaceDecl, interfaceSymbol, wrapAttribute);
                if (!string.IsNullOrEmpty(wrapInterfaceCode))
                {
                    var wrapFileName = $"{interfaceSymbol.Name}.Wrap.g.cs";
                    context.AddSource(wrapFileName, wrapInterfaceCode);
                }

                // 生成包装实现类代码
                var wrapImplementationCode = GenerateWrapImplementation(compilation, interfaceDecl, interfaceSymbol, wrapAttribute);
                if (!string.IsNullOrEmpty(wrapImplementationCode))
                {
                    var implFileName = $"{interfaceSymbol.Name}.WrapImpl.g.cs";
                    context.AddSource(implFileName, wrapImplementationCode);
                }
            }
            catch (Exception ex)
            {
                ReportDiagnosticError(context, interfaceDecl, ex);
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
            .FirstOrDefault(a => GeneratorConstants.HttpClientApiWrapAttributeNames.Contains(a.AttributeClass?.Name));
    }

    /// <summary>
    /// 获取Token管理接口名称
    /// </summary>
    private string GetTokenManageInterfaceName(INamedTypeSymbol interfaceSymbol, AttributeData wrapAttribute)
    {
        // 检查特性参数中是否有指定的Token管理接口名称
        var tokenManageArg = wrapAttribute.NamedArguments.FirstOrDefault(a => a.Key == "TokenManage");
        if (!string.IsNullOrEmpty(tokenManageArg.Value.Value?.ToString()))
        {
            return tokenManageArg.Value.Value.ToString();
        }

        // 默认使用 ITokenManage
        return GeneratorConstants.DefaultTokenManageInterface;
    }

    /// <summary>
    /// 获取包装类名称
    /// </summary>
    private string GetWrapClassName(string wrapInterfaceName)
    {
        if (wrapInterfaceName.StartsWith("I", StringComparison.Ordinal) && wrapInterfaceName.Length > 1)
        {
            return wrapInterfaceName.Substring(1);
        }
        return wrapInterfaceName + GeneratorConstants.DefaultWrapSuffix;
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

        // 默认在接口名称后添加"Wrap"
        return interfaceSymbol.Name + "Wrap";
    }

    /// <summary>
    /// 生成文件头部（命名空间和头部注释）
    /// </summary>
    private void GenerateFileHeader(StringBuilder sb, InterfaceDeclarationSyntax interfaceDecl)
    {
        GenerateFileHeader(sb);

        // 获取命名空间
        var namespaceName = GetNamespaceName(interfaceDecl);
        if (!string.IsNullOrEmpty(namespaceName))
        {
            sb.AppendLine();
            sb.AppendLine($"namespace {namespaceName};");
            sb.AppendLine();
        }
    }

    /// <summary>
    /// 生成接口或类的开始部分
    /// </summary>
    private void GenerateClassOrInterfaceStart(StringBuilder sb, string typeName, string interfaceName, bool isInterface = false)
    {
        var parentName = isInterface ? " " : $" : {interfaceName}";
        sb.AppendLine($"{CompilerGeneratedAttribute}");
        sb.AppendLine($"{GeneratedCodeAttribute}");
        sb.AppendLine($"{(isInterface ? "public partial interface" : "internal partial class")} {typeName}{parentName}");
        sb.AppendLine("{");
    }

    /// <summary>
    /// 生成构造函数代码
    /// </summary>
    private void GenerateConstructor(StringBuilder sb, string className, string interfaceName, string tokenManageInterfaceName)
    {
        var apiParameterName = char.ToLower(className[0]) + className.Substring(1) + "Api";
        var tokenParameterName = char.ToLower(tokenManageInterfaceName[0]) + tokenManageInterfaceName.Substring(1);

        sb.AppendLine($"    public {className}({interfaceName} {apiParameterName}, {tokenManageInterfaceName} {tokenParameterName}, ILogger<{className}> logger)");
        sb.AppendLine("    {");
        sb.AppendLine($"        {PrivateFieldNamingHelper.GeneratePrivateFieldName(interfaceName)} = {apiParameterName};");
        sb.AppendLine($"        {PrivateFieldNamingHelper.GeneratePrivateFieldName(tokenManageInterfaceName)} = {tokenParameterName};");
        sb.AppendLine("        _logger = logger;");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    /// <summary>
    /// 生成字段声明
    /// </summary>
    private void GenerateFieldDeclarations(StringBuilder sb, string interfaceName, string tokenManageInterfaceName, string className)
    {
        sb.AppendLine($"    private readonly {interfaceName} {PrivateFieldNamingHelper.GeneratePrivateFieldName(interfaceName)};");
        sb.AppendLine($"    private readonly {tokenManageInterfaceName} {PrivateFieldNamingHelper.GeneratePrivateFieldName(tokenManageInterfaceName)};");
        sb.AppendLine($"    private readonly ILogger<{className}> _logger;");
        sb.AppendLine();
    }

    /// <summary>
    /// 生成包装接口代码
    /// </summary>
    private string GenerateWrapInterface(Compilation compilation, InterfaceDeclarationSyntax interfaceDecl, INamedTypeSymbol interfaceSymbol, AttributeData wrapAttribute)
    {
        var sb = new StringBuilder();

        // 生成文件头部
        GenerateFileHeader(sb, interfaceDecl);

        // 获取包装接口名称
        var wrapInterfaceName = GetWrapInterfaceName(interfaceSymbol, wrapAttribute);

        // 添加接口注释
        var xmlDoc = GetXmlDocumentation(interfaceDecl);
        if (!string.IsNullOrEmpty(xmlDoc))
        {
            sb.Append(xmlDoc);
        }

        // 生成接口开始部分
        GenerateClassOrInterfaceStart(sb, wrapInterfaceName, "", isInterface: true);

        // 生成接口方法
        GenerateWrapMethods(compilation, interfaceDecl, interfaceSymbol, sb, isInterface: true);

        sb.AppendLine("}");

        return sb.ToString();
    }

    /// <summary>
    /// 生成包装实现类代码
    /// </summary>
    private string GenerateWrapImplementation(Compilation compilation, InterfaceDeclarationSyntax interfaceDecl, INamedTypeSymbol interfaceSymbol, AttributeData wrapAttribute)
    {
        var sb = new StringBuilder();

        // 生成文件头部
        GenerateFileHeader(sb, interfaceDecl);

        // 获取包装类名称和接口名称
        var wrapInterfaceName = GetWrapInterfaceName(interfaceSymbol, wrapAttribute);
        var wrapClassName = GetWrapClassName(wrapInterfaceName);
        var tokenManageInterfaceName = GetTokenManageInterfaceName(interfaceSymbol, wrapAttribute);

        // 生成类开始部分
        GenerateClassOrInterfaceStart(sb, wrapClassName, wrapInterfaceName);

        // 生成字段声明
        GenerateFieldDeclarations(sb, interfaceSymbol.Name, tokenManageInterfaceName, wrapClassName);

        // 生成构造函数
        GenerateConstructor(sb, wrapClassName, interfaceSymbol.Name, tokenManageInterfaceName);

        // 生成包装方法实现
        GenerateWrapMethods(compilation, interfaceDecl, interfaceSymbol, sb, isInterface: false, tokenManageInterfaceName, interfaceSymbol.Name);

        sb.AppendLine("}");

        return sb.ToString();
    }

    /// <summary>
    /// 生成包装方法（接口声明或实现）
    /// </summary>
    private void GenerateWrapMethods(Compilation compilation, InterfaceDeclarationSyntax interfaceDecl, INamedTypeSymbol interfaceSymbol, StringBuilder sb, bool isInterface, string tokenManageInterfaceName = null, string interfaceName = null)
    {
        var methods = interfaceSymbol.GetMembers()
                                     .OfType<IMethodSymbol>()
                                     .Where(m => IsValidMethod(m));

        foreach (var methodSymbol in methods)
        {
            var methodInfo = AnalyzeMethod(compilation, methodSymbol, interfaceDecl);
            if (!methodInfo.IsValid)
                continue;

            var methodSyntax = GetMethodDeclarationSyntax(methodSymbol, interfaceDecl);
            if (methodSyntax != null)
            {
                string wrapMethodCode;
                if (isInterface)
                {
                    wrapMethodCode = GenerateWrapMethodDeclaration(methodInfo, methodSyntax);
                }
                else
                {
                    wrapMethodCode = GenerateWrapMethodImplementation(methodInfo, methodSyntax, interfaceName, tokenManageInterfaceName);
                }

                if (!string.IsNullOrEmpty(wrapMethodCode))
                {
                    sb.AppendLine(wrapMethodCode);
                    sb.AppendLine();
                }
            }
        }
    }

    /// <summary>
    /// 生成包装方法声明（接口方法）
    /// </summary>
    private string GenerateWrapMethodDeclaration(MethodAnalysisResult methodInfo, MethodDeclarationSyntax methodSyntax)
    {
        var sb = new StringBuilder();

        // 添加方法注释
        var methodDoc = GetMethodXmlDocumentation(methodSyntax, methodInfo);
        if (!string.IsNullOrEmpty(methodDoc))
        {
            sb.AppendLine(methodDoc);
        }

        // 方法签名 - 使用原始方法的返回类型
        sb.Append($"    {methodSyntax.ReturnType} {methodInfo.MethodName}(");

        // 过滤掉标记了[Token]特性的参数，保留其他所有参数
        var filteredParameters = FilterParametersByAttribute(methodInfo.Parameters, GeneratorConstants.TokenAttributeNames, exclude: true);

        // 生成参数列表
        var parameterList = GenerateParameterList(filteredParameters);
        sb.Append(parameterList);

        sb.Append(");");

        return sb.ToString();
    }

    /// <summary>
    /// 生成包装方法实现（具体实现）
    /// </summary>
    private string GenerateWrapMethodImplementation(MethodAnalysisResult methodInfo, MethodDeclarationSyntax methodSyntax, string interfaceName, string tokenManageInterfaceName)
    {
        var sb = new StringBuilder();

        // 添加方法注释
        var methodDoc = GetMethodXmlDocumentation(methodSyntax, methodInfo);
        if (!string.IsNullOrEmpty(methodDoc))
        {
            sb.AppendLine(methodDoc);
        }

        // 方法签名 - 根据原始方法的返回类型决定是否添加async关键字
        var originalMethodReturnType = methodSyntax.ReturnType.ToString();
        var isAsyncMethod = originalMethodReturnType.StartsWith("Task", StringComparison.OrdinalIgnoreCase);

        if (isAsyncMethod)
        {
            sb.Append($"    public async {originalMethodReturnType} {methodInfo.MethodName}(");
        }
        else
        {
            sb.Append($"    public {originalMethodReturnType} {methodInfo.MethodName}(");
        }

        // 过滤掉标记了[Token]特性的参数，保留其他所有参数
        var filteredParameters = FilterParametersByAttribute(methodInfo.Parameters, GeneratorConstants.TokenAttributeNames, exclude: true);

        // 生成参数列表
        var parameterList = GenerateParameterList(filteredParameters);
        sb.AppendLine($"{parameterList})");

        sb.AppendLine("    {");

        // 生成方法体
        sb.AppendLine("        try");
        sb.AppendLine("        {");

        // 获取Token
        if (isAsyncMethod)
        {
            sb.AppendLine($"            var token = await {PrivateFieldNamingHelper.GeneratePrivateFieldName(tokenManageInterfaceName)}.GetTokenAsync();");
        }
        else
        {
            sb.AppendLine($"            var token = {PrivateFieldNamingHelper.GeneratePrivateFieldName(tokenManageInterfaceName)}.GetToken();");
        }
        sb.AppendLine();

        // Token空值检查
        sb.AppendLine("            if (string.IsNullOrEmpty(token))");
        sb.AppendLine("            {");
        sb.AppendLine("                _logger.LogWarning(\"获取到的Token为空！\");");
        sb.AppendLine("            }");
        sb.AppendLine();

        // 调用原始API方法
        if (isAsyncMethod)
        {
            sb.Append($"            return await {PrivateFieldNamingHelper.GeneratePrivateFieldName(interfaceName)}.{methodInfo.MethodName}(");
        }
        else
        {
            sb.Append($"            return {PrivateFieldNamingHelper.GeneratePrivateFieldName(interfaceName)}.{methodInfo.MethodName}(");
        }

        // 生成调用参数列表（包含Token和过滤后的参数）
        var callParameters = new List<string> { "token" };
        callParameters.AddRange(filteredParameters.Select(p => p.Name));
        sb.Append(string.Join(", ", callParameters));

        sb.AppendLine(");");
        sb.AppendLine("        }");
        sb.AppendLine("        catch (Exception x)");
        sb.AppendLine("        {");
        sb.AppendLine($"            _logger.LogError(x, \"执行{methodInfo.MethodName}操作失败：{{message}}\", x.Message);");
        sb.AppendLine("            throw;");
        sb.AppendLine("        }");
        sb.AppendLine("    }");

        return sb.ToString();
    }

    /// <summary>
    /// 获取方法的XML文档注释
    /// </summary>
    private string GetMethodXmlDocumentation(MethodDeclarationSyntax methodSyntax, MethodAnalysisResult methodInfo)
    {
        var xmlDoc = GetXmlDocumentation(methodSyntax);
        if (string.IsNullOrEmpty(xmlDoc))
            return string.Empty;

        // 获取标记了Token特性的参数名称
        var tokenParameterNames = FilterParametersByAttribute(methodInfo.Parameters, GeneratorConstants.TokenAttributeNames)
            .Select(p => p.Name)
            .ToList();

        // 处理XML文档注释，移除token参数的注释
        var docLines = xmlDoc.Split('\n');
        var result = new StringBuilder();

        int i = 0;
        foreach (var line in docLines)
        {
            var trimmedLine = line.TrimEnd();
            if (!string.IsNullOrWhiteSpace(trimmedLine))
            {
                // 检查是否是token参数的注释行
                var isTokenParameterComment = tokenParameterNames.Any(name =>
                    trimmedLine.Contains($"<param name=\"{name}\">") ||
                    trimmedLine.Contains($"<param name=\"{name}\""));

                if (!isTokenParameterComment)
                {
                    if (i == 0)
                        result.AppendLine($"    {trimmedLine}");
                    else
                        result.AppendLine($"{trimmedLine}");
                }

                i++;
            }
        }

        return result.ToString().TrimEnd();
    }

    /// <summary>
    /// 报告诊断错误
    /// </summary>
    private void ReportDiagnosticError(SourceProductionContext context, InterfaceDeclarationSyntax interfaceDecl, Exception ex)
    {
        context.ReportDiagnostic(Diagnostic.Create(
            new DiagnosticDescriptor(
                GeneratorConstants.GeneratorErrorDiagnosticId,
                "HttpClientApiWrap Source Generator Error",
                $"Error generating wrap interface for {interfaceDecl.Identifier}: {ex.Message}",
                "Generation",
                DiagnosticSeverity.Error,
                true),
            interfaceDecl.GetLocation()));
    }

    /// <summary>
    /// 报告诊断警告
    /// </summary>
    private void ReportDiagnosticWarning(SourceProductionContext context, InterfaceDeclarationSyntax interfaceDecl, string message)
    {
        context.ReportDiagnostic(Diagnostic.Create(
            new DiagnosticDescriptor(
                GeneratorConstants.GeneratorWarningDiagnosticId,
                "HttpClientApiWrap Source Generator Warning",
                message,
                "Generation",
                DiagnosticSeverity.Warning,
                true),
            interfaceDecl.GetLocation()));
    }

    /// <summary>
    /// 验证接口配置
    /// </summary>
    private bool ValidateInterfaceConfiguration(INamedTypeSymbol interfaceSymbol, AttributeData wrapAttribute, SourceProductionContext context, InterfaceDeclarationSyntax interfaceDecl)
    {
        if (interfaceSymbol == null)
        {
            ReportDiagnosticWarning(context, interfaceDecl, "Interface symbol is null, skipping generation.");
            return false;
        }

        if (!HasValidHttpMethods(interfaceSymbol))
        {
            ReportDiagnosticWarning(context, interfaceDecl, $"Interface {interfaceSymbol.Name} has no valid HTTP methods, skipping generation.");
            return false;
        }

        if (wrapAttribute == null)
        {
            ReportDiagnosticWarning(context, interfaceDecl, $"Interface {interfaceSymbol.Name} is missing HttpClientApiWrap attribute, skipping generation.");
            return false;
        }

        return true;
    }
}