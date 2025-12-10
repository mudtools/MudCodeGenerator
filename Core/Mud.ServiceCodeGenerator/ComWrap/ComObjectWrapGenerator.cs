// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.CodeGenerator 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

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
    /// <summary>
    /// 获取COM对象包装特性名称数组
    /// </summary>
    /// <returns>特性名称数组</returns>
    protected override string[] ComWrapAttributeNames() => ComWrapGeneratorConstants.ComObjectWrapAttributeNames;

    /// <summary>
    /// 生成COM对象包装实现类
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
        var className = GetImplementationClassName(interfaceName);

        // 添加Imps命名空间
        var impNamespace = $"{namespaceName}.Imps";

        var sb = new StringBuilder();
        GenerateFileHeader(sb);

        sb.AppendLine();
        sb.AppendLine($"namespace {impNamespace}");
        sb.AppendLine("{");
        sb.AppendLine($"    /// <summary>");
        sb.AppendLine($"    /// {interfaceName} 的COM对象包装实现类");
        sb.AppendLine($"    /// </summary>");
        sb.AppendLine($"    {CompilerGeneratedAttribute}");
        sb.AppendLine($"    {GeneratedCodeAttribute}");
        sb.AppendLine($"    internal partial class {className} : {interfaceName}");
        sb.AppendLine("    {");

        // 生成字段
        GenerateFields(sb, interfaceDeclaration);

        // 生成构造函数
        GenerateConstructor(sb, className, interfaceDeclaration);

        // 生成属性
        GenerateProperties(sb, interfaceSymbol, interfaceDeclaration);

        // 生成方法
        GenerateMethods(sb, interfaceSymbol, interfaceDeclaration);

        // 生成IDisposable实现
        GenerateIDisposableImplementation(sb, interfaceDeclaration);

        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    /// <summary>
    /// 生成属性实现
    /// </summary>
    /// <param name="sb">字符串构建器</param>
    /// <param name="interfaceSymbol">接口符号</param>
    /// <param name="interfaceDeclaration">接口声明语法</param>
    private void GenerateProperties(StringBuilder sb, INamedTypeSymbol interfaceSymbol, InterfaceDeclarationSyntax interfaceDeclaration)
    {
        sb.AppendLine("        #region 属性");
        sb.AppendLine();

        foreach (var member in interfaceSymbol.GetMembers().OfType<IPropertySymbol>())
        {
            if (!ShouldIgnoreMember(member))
            {
                GenerateProperty(sb, member, interfaceDeclaration);
            }
        }

        sb.AppendLine("        #endregion");
        sb.AppendLine();
    }

    #region Generate Implementation Members

    /// <summary>
    /// 生成私有字段
    /// </summary>
    /// <param name="sb">字符串构建器</param>
    /// <param name="interfaceDeclaration">接口声明语法</param>
    private void GenerateFields(StringBuilder sb, InterfaceDeclarationSyntax interfaceDeclaration)
    {
        var comNamespace = GetComNamespace(interfaceDeclaration);
        var comClassName = GetComClassName(interfaceDeclaration);

        sb.AppendLine($"        private {comNamespace}.{comClassName}? {PrivateFieldNamingHelper.GeneratePrivateFieldName(comClassName)};");
        sb.AppendLine("        private bool _disposedValue;");
        sb.AppendLine($"        public {comNamespace}.{comClassName}? InternalComObject => {PrivateFieldNamingHelper.GeneratePrivateFieldName(comClassName)};");
        sb.AppendLine();
    }

    /// <summary>
    /// 生成IDisposable接口实现
    /// </summary>
    /// <param name="sb">字符串构建器</param>
    private void GenerateIDisposableImplementation(StringBuilder sb, InterfaceDeclarationSyntax interfaceDeclaration)
    {
        var comClassName = GetComClassName(interfaceDeclaration);
        sb.AppendLine("        #region IDisposable 实现");
        sb.AppendLine($"        {GeneratedCodeAttribute}");
        sb.AppendLine("        protected virtual void Dispose(bool disposing)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (_disposedValue) return;");
        sb.AppendLine();
        sb.AppendLine($"            if (disposing && {PrivateFieldNamingHelper.GeneratePrivateFieldName(comClassName)} != null)");
        sb.AppendLine("            {");
        sb.AppendLine($"                Marshal.ReleaseComObject({PrivateFieldNamingHelper.GeneratePrivateFieldName(comClassName)});");
        sb.AppendLine($"                {PrivateFieldNamingHelper.GeneratePrivateFieldName(comClassName)} = null;");
        sb.AppendLine("            }");
        sb.AppendLine();
        sb.AppendLine("            _disposedValue = true;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine($"        {GeneratedCodeAttribute}");
        sb.AppendLine("        public void Dispose()");
        sb.AppendLine("        {");
        sb.AppendLine("            Dispose(true);");
        sb.AppendLine("            GC.SuppressFinalize(this);");
        sb.AppendLine("        }");
        sb.AppendLine("        #endregion");
    }
    #endregion
}