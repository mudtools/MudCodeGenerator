// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.CodeGenerator 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.CodeGenerator.Helper;
using Mud.ServiceCodeGenerator.ComWrap;
using System.Text;

namespace Mud.ServiceCodeGenerator.ComWrapSourceGenerator;

/// <summary>
/// COM对象包装源代码生成器
/// </summary>
/// <remarks>
/// 为标记了ComObjectWrap特性的接口生成COM对象包装类，提供类型安全的COM对象访问
/// </remarks>
[Generator]
public class ComObjectWrapGenerator : ComObjectWrapBaseGenerator
{
    #region Generator Configuration

    /// <summary>
    /// 获取COM对象包装特性名称数组
    /// </summary>
    /// <returns>特性名称数组</returns>
    protected override string[] ComWrapAttributeNames() => ComWrapConstants.ComObjectWrapAttributeNames;

    #endregion

    #region Main Implementation Generation

    /// <summary>
    /// 生成COM对象包装实现类（使用模板方法模式）
    /// </summary>
    /// <param name="interfaceDeclaration">接口声明语法</param>
    /// <param name="interfaceSymbol">接口符号</param>
    /// <returns>生成的源代码</returns>
    protected override string GenerateImplementationClass(InterfaceDeclarationSyntax interfaceDeclaration, INamedTypeSymbol interfaceSymbol)
    {
        if (interfaceDeclaration == null) throw new ArgumentNullException(nameof(interfaceDeclaration));
        if (interfaceSymbol == null) throw new ArgumentNullException(nameof(interfaceSymbol));

        var namespaceName = SyntaxHelper.GetNamespaceName(interfaceDeclaration);
        var interfaceName = interfaceSymbol.Name;
        var className = TypeSymbolHelper.GetImplementationClassName(interfaceName);

        // 添加Imps命名空间
        var impNamespace = $"{namespaceName}.Imps";

        var sb = new StringBuilder();
        GenerateFileHeader(sb);

        sb.AppendLine();
        sb.AppendLine($"namespace {impNamespace}");
        sb.AppendLine("{");
        sb.AppendLine($"    /// <summary>");
        sb.AppendLine($"    /// COM封装接口 <see cref=\"{interfaceName}\"/> 的内容实现类。");
        sb.AppendLine($"    /// </summary>");
        sb.AppendLine($"    {CompilerGeneratedAttribute}");
        sb.AppendLine($"    {GeneratedCodeAttribute}");
        sb.AppendLine($"    internal partial class {className} : {interfaceName}");
        sb.AppendLine("    {");

        // 生成字段
        GenerateFields(sb, interfaceDeclaration);
        GeneratePrivateField(sb, interfaceSymbol, interfaceDeclaration);

        // 生成构造函数
        GenerateConstructor(sb, className, interfaceSymbol, interfaceDeclaration);

        // 生成公共接口方法。
        GenerateCommonInterfaceMethod(sb, className, interfaceSymbol, interfaceDeclaration);

        // 生成属性
        GenerateProperties(sb, interfaceSymbol, interfaceDeclaration);

        // 生成方法
        GenerateMethods(sb, interfaceSymbol, interfaceDeclaration);

        // 模板方法：生成额外的实现内容（由子类实现）
        GenerateAdditionalImplementations(sb, interfaceSymbol, interfaceDeclaration);

        // 生成IDisposable实现
        GenerateIDisposableImplementation(sb, interfaceDeclaration, interfaceSymbol);

        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    #endregion
}