using System.Collections.Immutable;
using System.Text;

namespace Mud.ServiceCodeGenerator;


/// <summary>
/// HttpClientApiWrap 源代码生成器
/// 为标记有HttpClientApiWrap特性的接口生成二次包装实现类
/// </summary>
public abstract class HttpClientApiWrapSourceGenerator : WebApiSourceGenerator
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

                GenerateWrapCode(compilation, interfaceDecl, interfaceSymbol, wrapAttribute, context);
            }
            catch (Exception ex)
            {
                ReportDiagnosticError(context, interfaceDecl, ex);
            }
        }
    }

    protected abstract void GenerateWrapCode(Compilation compilation, InterfaceDeclarationSyntax interfaceDecl, INamedTypeSymbol interfaceSymbol, AttributeData wrapAttribute, SourceProductionContext context);

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
    /// 生成文件头部（命名空间和头部注释）
    /// </summary>
    protected void GenerateFileHeader(StringBuilder sb, InterfaceDeclarationSyntax interfaceDecl)
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
    protected void GenerateClassOrInterfaceStart(StringBuilder sb, string typeName, string interfaceName, bool isInterface = false)
    {
        var parentName = isInterface ? " " : $" : {interfaceName}";
        sb.AppendLine($"{CompilerGeneratedAttribute}");
        sb.AppendLine($"{GeneratedCodeAttribute}");
        sb.AppendLine($"{(isInterface ? "public partial interface" : "internal partial class")} {typeName}{parentName}");
        sb.AppendLine("{");
    }


    /// <summary>
    /// 生成包装方法（接口声明或实现）
    /// </summary>
    protected void GenerateWrapMethods(Compilation compilation, InterfaceDeclarationSyntax interfaceDecl, INamedTypeSymbol interfaceSymbol, StringBuilder sb, bool isInterface, string tokenManageInterfaceName = null, string interfaceName = null)
    {
        if (interfaceSymbol == null) return;

        var methods = interfaceSymbol.GetMembers()
                                     .OfType<IMethodSymbol>()
                                     .Where(m => IsValidMethod(m));

        foreach (var methodSymbol in methods)
        {
            var methodInfo = AnalyzeMethod(compilation, methodSymbol, interfaceDecl);
            if (!methodInfo.IsValid)
                continue;

            // 使用更精确的方法查找，确保正确处理重载方法
            var methodSyntax = FindMethodSyntax(compilation, methodSymbol, interfaceDecl);
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
        var callParameters = GenerateCorrectParameterCallList(methodInfo.Parameters, filteredParameters, "token");
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
    /// 生成正确的参数调用列表，确保token参数替换掉原来标记了[Token]特性的参数位置
    /// </summary>
    private List<string> GenerateCorrectParameterCallList(IReadOnlyList<ParameterInfo> originalParameters, IReadOnlyList<ParameterInfo> filteredParameters, string tokenParameterName)
    {
        var callParameters = new List<string>();

        foreach (var originalParam in originalParameters)
        {
            // 检查当前参数是否是Token参数
            if (HasAttribute(originalParam, GeneratorConstants.TokenAttributeNames))
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