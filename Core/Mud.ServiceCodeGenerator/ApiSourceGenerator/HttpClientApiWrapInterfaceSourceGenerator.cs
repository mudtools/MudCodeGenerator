using System.Text;

namespace Mud.ServiceCodeGenerator;

/// <summary>
/// 用于生成包装接口代码的代码生成器。
/// </summary>
[Generator]
public class HttpClientApiInterfaceWrapSourceGenerator : HttpClientApiWrapSourceGenerator
{
    protected override void GenerateWrapCode(Compilation compilation, InterfaceDeclarationSyntax interfaceDecl, INamedTypeSymbol interfaceSymbol, AttributeData wrapAttribute, SourceProductionContext context)
    {
        if (interfaceSymbol == null) return;

        // 生成包装接口代码
        var wrapInterfaceCode = GenerateWrapInterface(compilation, interfaceDecl, interfaceSymbol, wrapAttribute);
        if (!string.IsNullOrEmpty(wrapInterfaceCode))
        {
            var wrapFileName = $"{interfaceSymbol.Name}.Wrap.g.cs";
            context.AddSource(wrapFileName, wrapInterfaceCode);
        }
    }

    /// <summary>
    /// 生成包装接口代码
    /// </summary>
    protected string GenerateWrapInterface(Compilation compilation, InterfaceDeclarationSyntax interfaceDecl, INamedTypeSymbol interfaceSymbol, AttributeData wrapAttribute)
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
    /// 生成包装方法声明（接口方法）
    /// </summary>
    protected override string GenerateWrapMethod(MethodAnalysisResult methodInfo, MethodDeclarationSyntax methodSyntax, string interfaceName, string tokenManageInterfaceName)
    {
        if (methodInfo == null || methodSyntax == null)
            return string.Empty;

        // 检查是否有TokenType.Both的Token参数
        var bothTokenParameter = methodInfo.Parameters.FirstOrDefault(p => 
            HasAttribute(p, GeneratorConstants.TokenAttributeNames) && 
            p.TokenType.Equals("Both", StringComparison.OrdinalIgnoreCase));

        if (bothTokenParameter != null)
        {
            // 为Both类型生成两个方法
            var tenantMethod = GenerateBothWrapMethod(methodInfo, methodSyntax, "_Tenant_", bothTokenParameter);
            var userMethod = GenerateBothWrapMethod(methodInfo, methodSyntax, "_User_", bothTokenParameter);
            
            return $"{tenantMethod}\n\n{userMethod}";
        }
        else
        {
            // 原有逻辑
            var sb = new StringBuilder();

            // 添加方法注释
            var methodDoc = GetMethodXmlDocumentation(methodSyntax, methodInfo);
            if (!string.IsNullOrEmpty(methodDoc))
            {
                sb.AppendLine(methodDoc);
            }

            // 方法签名 - 使用包含命名空间的返回类型
            sb.Append($"    {methodInfo.ReturnType} {methodInfo.MethodName}(");

            // 过滤掉标记了[Token]特性的参数，保留其他所有参数
            var filteredParameters = FilterParametersByAttribute(methodInfo.Parameters, GeneratorConstants.TokenAttributeNames, exclude: true);

            // 生成参数列表
            var parameterList = GenerateParameterList(filteredParameters);
            sb.Append(parameterList);

            sb.Append(");");

            return sb.ToString();
        }
    }

    /// <summary>
    /// 为TokenType.Both生成特定的方法声明
    /// </summary>
    private string GenerateBothWrapMethod(MethodAnalysisResult methodInfo, MethodDeclarationSyntax methodSyntax, string prefix, ParameterInfo bothTokenParameter)
    {
        var sb = new StringBuilder();

        // 添加方法注释
        var methodDoc = GetMethodXmlDocumentation(methodSyntax, methodInfo);
        if (!string.IsNullOrEmpty(methodDoc))
        {
            sb.AppendLine(methodDoc);
        }

        // 方法签名 - 使用包含命名空间的返回类型，添加前缀到方法名
        var methodName = methodInfo.MethodName;
        if (methodName.EndsWith("Async"))
        {
            methodName = methodName.Insert(methodName.Length - 5, prefix);
        }
        else
        {
            methodName = prefix.Trim('_') + methodName;
        }
        
        sb.Append($"    {methodInfo.ReturnType} {methodName}(");

        // 过滤掉标记了[Token]特性的参数，保留其他所有参数
        var filteredParameters = FilterParametersByAttribute(methodInfo.Parameters, GeneratorConstants.TokenAttributeNames, exclude: true);

        // 生成参数列表
        var parameterList = GenerateParameterList(filteredParameters);
        sb.Append(parameterList);

        sb.Append(");");

        return sb.ToString();
    }
}