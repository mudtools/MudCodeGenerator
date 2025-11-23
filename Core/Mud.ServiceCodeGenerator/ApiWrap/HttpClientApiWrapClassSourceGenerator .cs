// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.CodeGenerator 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text;

namespace Mud.ServiceCodeGenerator.ApiSourceGenerator;

/// <summary>
/// 用于生成包装实现类代码的代码生成器。
/// </summary>
[Generator]
public class HttpClientApiWrapClassSourceGenerator : HttpClientApiWrapSourceGenerator
{
    protected override void GenerateWrapCode(Compilation compilation, InterfaceDeclarationSyntax interfaceDecl, INamedTypeSymbol interfaceSymbol, AttributeData wrapAttribute, SourceProductionContext context)
    {
        if (interfaceDecl == null || interfaceSymbol == null )
            return;
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
        var tokenManageInterfaceName = GetTokenManageInterfaceName(wrapAttribute);

        // 生成类开始部分
        GenerateClassOrInterfaceStart(sb, wrapClassName, wrapInterfaceName);

        // 生成字段声明
        GenerateFieldDeclarations(sb, interfaceSymbol.Name, tokenManageInterfaceName, wrapClassName);

        // 生成构造函数
        GenerateConstructor(sb, wrapClassName, interfaceSymbol.Name, tokenManageInterfaceName);

        // 生成包装方法实现
        GenerateWrapMethods(compilation, interfaceDecl, interfaceSymbol, sb, tokenManageInterfaceName, interfaceSymbol.Name);

        sb.AppendLine("}");

        return sb.ToString();
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

        // 检查是否忽略生成包装接口
        if (methodInfo.IgnoreWrapInterface)
            return string.Empty;

        // 检查是否有TokenType.Both的Token参数
        var bothTokenParameter = methodInfo.Parameters.FirstOrDefault(p =>
            HasAttribute(p, GeneratorConstants.TokenAttributeNames) &&
            p.TokenType.Equals("Both", StringComparison.OrdinalIgnoreCase));

        if (bothTokenParameter != null)
        {
            // 为Both类型生成两个方法实现
            var tenantMethod = GenerateBothWrapMethodImplementation(methodInfo, methodSyntax, interfaceName, tokenManageInterfaceName, "_Tenant_", "GetTenantAccessTokenAsync");
            var userMethod = GenerateBothWrapMethodImplementation(methodInfo, methodSyntax, interfaceName, tokenManageInterfaceName, "_User_", "GetUserAccessTokenAsync");

            return $"{tenantMethod}\n\n{userMethod}";
        }
        else
        {
            return GenerateSingleWrapMethodImplementation(methodInfo, methodSyntax, interfaceName, tokenManageInterfaceName);
        }
    }

    /// <summary>
    /// 生成单个包装方法实现
    /// </summary>
    private string GenerateSingleWrapMethodImplementation(MethodAnalysisResult methodInfo, MethodDeclarationSyntax methodSyntax, string interfaceName, string tokenManageInterfaceName)
    {
        var sb = new StringBuilder();

        // 添加方法注释
        var methodDoc = GetMethodXmlDocumentation(methodSyntax, methodInfo);
        if (!string.IsNullOrEmpty(methodDoc))
        {
            sb.AppendLine(methodDoc);
        }

        // 过滤掉标记了[Token]特性的参数，保留其他所有参数
        var filteredParameters = FilterParametersByAttribute(methodInfo.Parameters, GeneratorConstants.TokenAttributeNames, exclude: true);

        // 生成方法签名 - 实现类需要async关键字
        var methodSignature = GenerateMethodSignature(methodInfo, methodInfo.MethodName, filteredParameters);
        sb.AppendLine(methodSignature);

        // 生成方法体
        sb.AppendLine("    {");
        var methodBody = GenerateMethodBody(methodInfo, interfaceName, tokenManageInterfaceName, methodInfo.Parameters, filteredParameters);
        sb.Append(methodBody);

        return sb.ToString();
    }

    /// <summary>
    /// 为TokenType.Both生成特定的方法实现
    /// </summary>
    private string GenerateBothWrapMethodImplementation(MethodAnalysisResult methodInfo, MethodDeclarationSyntax methodSyntax, string interfaceName, string tokenManageInterfaceName, string prefix, string tokenMethodName)
    {
        var sb = new StringBuilder();

        // 添加方法注释
        var methodDoc = GetMethodXmlDocumentation(methodSyntax, methodInfo);
        if (!string.IsNullOrEmpty(methodDoc))
        {
            sb.AppendLine(methodDoc);
        }

        // 生成方法名
        var methodName = GenerateBothMethodName(methodInfo.MethodName, prefix);

        // 过滤掉标记了[Token]特性的参数，保留其他所有参数
        var filteredParameters = FilterParametersByAttribute(methodInfo.Parameters, GeneratorConstants.TokenAttributeNames, exclude: true);

        // 生成方法签名 - 实现类需要async关键字
        var methodSignature = GenerateMethodSignature(methodInfo, methodName, filteredParameters);
        sb.AppendLine(methodSignature);

        // 生成方法体
        sb.AppendLine("    {");

        // 特殊处理Both类型的方法体 - 使用指定的token方法名
        sb.AppendLine("        try");
        sb.AppendLine("        {");

        // 生成Token获取逻辑 - 使用指定的token方法名
        GenerateTokenAcquisition(sb, tokenManageInterfaceName, tokenMethodName, methodInfo.Parameters);

        // 生成API调用逻辑
        GenerateApiCall(sb, methodInfo, interfaceName, methodInfo.MethodName, methodInfo.Parameters, filteredParameters);

        // 生成异常处理逻辑
        GenerateExceptionHandling(sb, methodInfo.MethodName);

        return sb.ToString();
    }
}