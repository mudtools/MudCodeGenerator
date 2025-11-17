
using System.Text;

namespace Mud.ServiceCodeGenerator;

/// <summary>
/// 用于生成包装实现类代码的代码生成器。
/// </summary>
[Generator]
public class HttpClientApiWrapClassSourceGenerator : HttpClientApiWrapSourceGenerator
{
    protected override void GenerateWrapCode(Compilation compilation, InterfaceDeclarationSyntax interfaceDecl, INamedTypeSymbol interfaceSymbol, AttributeData wrapAttribute, SourceProductionContext context)
    {
        // 生成包装实现类代码
        var wrapImplementationCode = GenerateWrapImplementation(compilation, interfaceDecl, interfaceSymbol, wrapAttribute);
        if (!string.IsNullOrEmpty(wrapImplementationCode))
        {
            var implFileName = $"{interfaceSymbol.Name}.WrapImpl.g.cs";
            context.AddSource(implFileName, wrapImplementationCode);
        }
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
    /// 生成构造函数代码
    /// </summary>
    private void GenerateConstructor(StringBuilder sb, string className, string interfaceName, string tokenManageInterfaceName)
    {
        var apiParameterName = char.ToLower(className[0], System.Globalization.CultureInfo.InvariantCulture) + className.Substring(1) + "Api";
        var tokenParameterName = char.ToLower(tokenManageInterfaceName[0], System.Globalization.CultureInfo.InvariantCulture) + tokenManageInterfaceName.Substring(1);

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
    /// 生成包装方法实现（具体实现）
    /// </summary>
    protected override string GenerateWrapMethod(MethodAnalysisResult methodInfo, MethodDeclarationSyntax methodSyntax, string interfaceName, string tokenManageInterfaceName)
    {
        if (methodInfo == null || methodSyntax == null)
            return string.Empty;

        var sb = new StringBuilder();

        // 添加方法注释
        var methodDoc = GetMethodXmlDocumentation(methodSyntax, methodInfo);
        if (!string.IsNullOrEmpty(methodDoc))
        {
            sb.AppendLine(methodDoc);
        }

        // 方法签名 - 根据原始方法的返回类型决定是否添加async关键字
        if (methodInfo.IsAsyncMethod)
        {
            sb.Append($"    public async {methodInfo.ReturnType} {methodInfo.MethodName}(");
        }
        else
        {
            sb.Append($"    public {methodInfo.ReturnType} {methodInfo.MethodName}(");
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
        if (methodInfo.IsAsyncMethod)
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
        if (methodInfo.IsAsyncMethod)
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
}