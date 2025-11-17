
using System.Text;

namespace Mud.ServiceCodeGenerator;
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
}