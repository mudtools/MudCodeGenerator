// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.CodeGenerator 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text;

namespace Mud.EntityCodeGenerator.Helper;

/// <summary>
/// 代码生成辅助工具类，提供通用的代码生成功能
/// </summary>
public static class CodeGenerationHelper
{
    /// <summary>
    /// 生成方法声明
    /// </summary>
    /// <param name="methodName">方法名</param>
    /// <param name="returnType">返回类型</param>
    /// <param name="parameters">参数列表</param>
    /// <param name="isStatic">是否为静态方法</param>
    /// <param name="isExtension">是否为扩展方法</param>
    /// <param name="methodBody">方法体</param>
    /// <param name="summary">方法说明</param>
    /// <returns>方法声明语法</returns>
    public static MethodDeclarationSyntax GenerateMethod(
        string methodName,
        string returnType,
        List<(string type, string name, string description)> parameters,
        bool isStatic = false,
        bool isExtension = false,
        string methodBody = "",
        string summary = "")
    {
        var modifiers = new List<SyntaxToken> { SyntaxFactory.Token(SyntaxKind.PublicKeyword) };

        if (isStatic)
            modifiers.Add(SyntaxFactory.Token(SyntaxKind.StaticKeyword));

        var parameterList = SyntaxFactory.ParameterList();

        foreach (var (type, name, description) in parameters)
        {
            var parameter = SyntaxFactory.Parameter(SyntaxFactory.Identifier(name))
                .WithType(SyntaxFactory.ParseTypeName(type));

            if (isExtension && parameterList.Parameters.Count == 0)
            {
                parameter = parameter.WithModifiers(
                    SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.ThisKeyword)));
            }

            parameterList = parameterList.AddParameters(parameter);
        }

        var method = SyntaxFactory.MethodDeclaration(SyntaxFactory.ParseTypeName(returnType), methodName)
            .WithModifiers(SyntaxFactory.TokenList(modifiers))
            .WithParameterList(parameterList);

        if (!string.IsNullOrEmpty(methodBody))
        {
            var body = SyntaxFactory.ParseStatement(methodBody);
            method = method.WithBody(SyntaxFactory.Block(body));
        }

        if (!string.IsNullOrEmpty(summary))
        {
            var xmlComment = GenerateXmlComment(summary, parameters);
            method = method.WithLeadingTrivia(xmlComment);
        }

        return method;
    }

    /// <summary>
    /// 生成XML注释
    /// </summary>
    /// <param name="summary">方法说明</param>
    /// <param name="parameters">参数列表</param>
    /// <returns>XML注释语法</returns>
    private static SyntaxTriviaList GenerateXmlComment(
        string summary,
        List<(string type, string name, string description)> parameters)
    {
        var sb = new StringBuilder();
        sb.AppendLine("/// <summary>");
        sb.AppendLine($"/// {summary}");
        sb.AppendLine("/// </summary>");

        foreach (var (type, name, description) in parameters)
        {
            sb.AppendLine($"/// <param name=\"{name}\">{description}</param>");
        }

        sb.AppendLine("/// <returns>方法返回值</returns>");

        return SyntaxFactory.ParseLeadingTrivia(sb.ToString());
    }

    /// <summary>
    /// 生成类声明
    /// </summary>
    /// <param name="className">类名</param>
    /// <param name="namespaceName">命名空间</param>
    /// <param name="isStatic">是否为静态类</param>
    /// <param name="baseTypes">基类或接口列表</param>
    /// <returns>编译单元语法</returns>
    public static CompilationUnitSyntax GenerateClass(
        string className,
        string namespaceName,
        bool isStatic = false,
        List<string> baseTypes = null)
    {
        var modifiers = new List<SyntaxToken> { SyntaxFactory.Token(SyntaxKind.PublicKeyword) };

        if (isStatic)
            modifiers.Add(SyntaxFactory.Token(SyntaxKind.StaticKeyword));

        var classDeclaration = SyntaxFactory.ClassDeclaration(className)
            .WithModifiers(SyntaxFactory.TokenList(modifiers));

        if (baseTypes != null && baseTypes.Count > 0)
        {
            var baseList = SyntaxFactory.BaseList();
            foreach (var baseType in baseTypes)
            {
                baseList = baseList.AddTypes(SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName(baseType)));
            }
            classDeclaration = classDeclaration.WithBaseList(baseList);
        }

        var namespaceDeclaration = SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName(namespaceName))
            .AddMembers(classDeclaration);

        return SyntaxFactory.CompilationUnit()
            .AddUsings(
                SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System")),
                SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.Collections.Generic")),
                SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.Linq")))
            .AddMembers(namespaceDeclaration)
            .NormalizeWhitespace();
    }

    /// <summary>
    /// 生成属性声明
    /// </summary>
    /// <param name="propertyName">属性名</param>
    /// <param name="propertyType">属性类型</param>
    /// <param name="hasGetter">是否有getter</param>
    /// <param name="hasSetter">是否有setter</param>
    /// <param name="initialValue">初始值</param>
    /// <returns>属性声明语法</returns>
    public static PropertyDeclarationSyntax GenerateProperty(
        string propertyName,
        string propertyType,
        bool hasGetter = true,
        bool hasSetter = true,
        string initialValue = null)
    {
        var accessors = new List<AccessorDeclarationSyntax>();

        if (hasGetter)
        {
            accessors.Add(SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)));
        }

        if (hasSetter)
        {
            accessors.Add(SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)));
        }

        var property = SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName(propertyType), propertyName)
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
            .WithAccessorList(SyntaxFactory.AccessorList(SyntaxFactory.List(accessors)));

        if (!string.IsNullOrEmpty(initialValue))
        {
            property = property.WithInitializer(
                SyntaxFactory.EqualsValueClause(SyntaxFactory.ParseExpression(initialValue)))
                .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
        }

        return property;
    }

    /// <summary>
    /// 生成映射方法的标准模板
    /// </summary>
    /// <param name="sourceType">源类型</param>
    /// <param name="targetType">目标类型</param>
    /// <param name="mappingLines">映射行列表</param>
    /// <param name="isExtension">是否为扩展方法</param>
    /// <returns>方法代码字符串</returns>
    public static string GenerateMappingMethodTemplate(
        string sourceType,
        string targetType,
        List<string> mappingLines,
        bool isExtension = true)
    {
        var sb = new StringBuilder();

        var methodName = isExtension ? $"MapTo{targetType}" : $"MapFrom{sourceType}";
        var parameterName = isExtension ? "source" : "target";

        sb.AppendLine($"/// <summary>");
        sb.AppendLine($"/// 将 <see cref=\"{sourceType}\"/> 映射到 <see cref=\"{targetType}\"/> 实例。");
        sb.AppendLine($"/// </summary>");
        sb.AppendLine($"/// <param name=\"{parameterName}\">输入的 <see cref=\"{sourceType}\"/> 实例。</param>");
        sb.AppendLine($"/// <param name=\"action\">映射后对目标实例执行的操作。</param>");
        sb.AppendLine($"/// <returns>映射后的 <see cref=\"{targetType}\"/> 实例。</returns>");

        if (isExtension)
        {
            sb.AppendLine($"public static {targetType} {methodName}(this {sourceType} {parameterName}, Action<{targetType}>? action = null)");
        }
        else
        {
            sb.AppendLine($"public static {targetType} {methodName}(this {targetType} {parameterName}, {sourceType} source, Action<{targetType}>? action = null)");
        }

        sb.AppendLine("{");
        sb.AppendLine($"    if({parameterName} == null) return null;");
        sb.AppendLine($"    var result = new {targetType}();");
        sb.AppendLine();

        foreach (var mappingLine in mappingLines)
        {
            sb.AppendLine($"    {mappingLine}");
        }

        sb.AppendLine($"    if(action != null)");
        sb.AppendLine($"        action(result);");
        sb.AppendLine($"    return result;");
        sb.AppendLine("}");

        return sb.ToString();
    }
}