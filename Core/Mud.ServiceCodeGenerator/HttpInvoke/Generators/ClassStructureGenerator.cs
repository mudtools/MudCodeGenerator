// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.CodeGenerator 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text;

namespace Mud.ServiceCodeGenerator.HttpInvoke.Generators;

/// <summary>
/// 类结构生成器，负责生成实现类的结构声明
/// </summary>
internal class ClassStructureGenerator
{
    private readonly HttpInvokeClassSourceGenerator _httpInvokeClassSourceGenerator;
    private readonly INamedTypeSymbol _interfaceSymbol;
    private readonly StringBuilder _codeBuilder;
    private readonly bool _isAbstract;
    private readonly string? _inheritedFrom;

    public ClassStructureGenerator(
        HttpInvokeClassSourceGenerator httpInvokeClassSourceGenerator,
        INamedTypeSymbol interfaceSymbol,
        StringBuilder codeBuilder,
        bool isAbstract,
        string? inheritedFrom)
    {
        _httpInvokeClassSourceGenerator = httpInvokeClassSourceGenerator;
        _interfaceSymbol = interfaceSymbol;
        _codeBuilder = codeBuilder;
        _isAbstract = isAbstract;
        _inheritedFrom = inheritedFrom;
    }

    /// <summary>
    /// 生成类结构
    /// </summary>
    public void Generate(string className, string namespaceName)
    {
        // 根据接口的可访问性确定类的可访问性
        var classAccessibility = _interfaceSymbol.DeclaredAccessibility switch
        {
            Accessibility.Public => "public",
            Accessibility.Internal => "internal",
            Accessibility.Protected => "protected",
            Accessibility.ProtectedOrInternal => "protected internal",
            Accessibility.ProtectedAndInternal => "private protected",
            _ => "internal"
        };

        // 构建类声明
        var classKeyword = _isAbstract ? "abstract partial class" : "partial class";
        var inheritanceList = BuildInheritanceList();

        _httpInvokeClassSourceGenerator.GenerateFileHeader(_codeBuilder);

        _codeBuilder.AppendLine();
        _codeBuilder.AppendLine($"namespace {namespaceName}");
        _codeBuilder.AppendLine("{");
        _codeBuilder.AppendLine("    /// <summary>");
        _codeBuilder.AppendLine($"    /// <inheritdoc cref=\"{_interfaceSymbol.Name}\"/>");
        _codeBuilder.AppendLine("    /// </summary>");
        _codeBuilder.AppendLine($"    {GeneratedCodeConsts.CompilerGeneratedAttribute}");
        _codeBuilder.AppendLine($"    {GeneratedCodeConsts.GeneratedCodeAttribute}");
        _codeBuilder.AppendLine($"    {classAccessibility} {classKeyword} {className} : {string.Join(", ", inheritanceList)}");
        _codeBuilder.AppendLine("    {");
    }

    /// <summary>
    /// 构建继承列表
    /// </summary>
    private List<string> BuildInheritanceList()
    {
        var inheritanceList = new List<string> { _interfaceSymbol.Name };

        if (!string.IsNullOrEmpty(_inheritedFrom))
        {
            inheritanceList.Insert(0, _inheritedFrom);
        }

        return inheritanceList;
    }
}
