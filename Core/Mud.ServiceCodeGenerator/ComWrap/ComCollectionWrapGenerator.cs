// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.CodeGenerator 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.CodeGenerator.Helper;
using Mud.ServiceCodeGenerator.ComWrapSourceGenerator;
using System.Text;

namespace Mud.ServiceCodeGenerator.ComWrap;

/// <summary>
/// COM集合对象包装源代码生成器
/// </summary>
/// <remarks>
/// 为标记了ComCollectionWrap特性的接口生成COM对象包装类，提供类型安全的COM对象访问
/// </remarks>
[Generator]
public class ComCollectionWrapGenerator : ComObjectWrapBaseGenerator
{
    #region Generator Override Methods
    /// <summary>
    /// 获取COM对象包装特性名称数组
    /// </summary>
    /// <returns>特性名称数组</returns>
    protected override string[] ComWrapAttributeNames() => ComWrapConstants.ComCollectionWrapAttributeNames;

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

        // 生成IEnumerable实现
        GenerateEnumerableImplementation(sb, interfaceSymbol, interfaceDeclaration);

        // 生成IDisposable实现
        GenerateIDisposableImplementation(sb, interfaceDeclaration, interfaceSymbol);

        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }
    #endregion

    #region Helper Methods
    /// <summary>
    /// 获取集合元素类型
    /// </summary>
    /// <param name="interfaceSymbol">接口符号</param>
    /// <returns>元素类型，如果无法确定则返回null</returns>
    private ITypeSymbol? GetCollectionElementType(INamedTypeSymbol interfaceSymbol)
    {
        // 查找 IEnumerable<T> 接口
        foreach (var interfaceImpl in interfaceSymbol.AllInterfaces)
        {
            if (interfaceImpl.Name == "IEnumerable" && interfaceImpl.IsGenericType)
            {
                var typeArgument = interfaceImpl.TypeArguments.FirstOrDefault();
                return typeArgument;
            }
        }
        return null;
    }
    #endregion

    #region Enumerable Implementation
    private void GenerateEnumerableImplementation(
        StringBuilder sb,
        INamedTypeSymbol interfaceSymbol,
        InterfaceDeclarationSyntax interfaceDeclaration)
    {
        var elementType = GetCollectionElementType(interfaceSymbol);
        if (elementType == null)
            return;

        var comNamespace = GetComNamespace(interfaceDeclaration);
        var comClassName = GetComClassName(interfaceDeclaration);
        var ordinalComType = GetOrdinalComType(elementType.Name);
        var privateFieldName = PrivateFieldNamingHelper.GeneratePrivateFieldName(comClassName);
        var isEnumType = TypeSymbolHelper.IsEnumType(elementType);
        var defaultValue = GetDefaultValue(interfaceDeclaration, elementType, elementType);
        var elementImplType = GetImplementationType(elementType.ToDisplayString());
        var isNoneEnumerableObj = AttributeDataHelper.HasAttribute(interfaceSymbol, ComWrapConstants.NoneEnumerableAttributes);

        sb.AppendLine("        #region IEnumerable 实现");
        sb.AppendLine($"        ///  <inheritdoc/>");
        sb.AppendLine($"        {GeneratedCodeAttribute}");
        sb.AppendLine($"        public IEnumerator<{elementType}> GetEnumerator()");
        sb.AppendLine("        {");
        sb.AppendLine($"            if ({privateFieldName} == null) yield break;");
        sb.AppendLine();

        if (isNoneEnumerableObj)
        {
            sb.AppendLine("            for (int i = 1; i <= Count; i++)");
            sb.AppendLine("            {");
            sb.AppendLine("                yield return this[i];");
            sb.AppendLine("            }");
        }
        else
        {
            sb.AppendLine($"            foreach (var item in {privateFieldName})");
            sb.AppendLine("            {");
            if (TypeSymbolHelper.IsBasicType(elementImplType) || isEnumType)
            {
                if (isEnumType)
                    sb.AppendLine($"                var returnValue = item.ObjectConvertEnum<{elementType}>();");
                else
                {
                    var convertCode = GetConvertCodeForType(elementType, "item");
                    sb.AppendLine($"                var returnValue = {convertCode};");
                }
            }
            else
            {
                sb.AppendLine($"                {elementImplType}? returnValue = null;");
                sb.AppendLine($"                if(item is {comNamespace}.{ordinalComType} rComObj)");
                sb.AppendLine($"                     returnValue = new {elementImplType}(rComObj);");
                sb.AppendLine("                if (returnValue != null)");
                sb.AppendLine("                     _disposableList.Add(returnValue);");
            }
            sb.AppendLine("                yield return returnValue;");
            sb.AppendLine("            }");
        }


        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine($"        {GeneratedCodeAttribute}");
        sb.AppendLine("        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()");
        sb.AppendLine("        {");
        sb.AppendLine("            return GetEnumerator();");
        sb.AppendLine("        }");
        sb.AppendLine("        #endregion");
        sb.AppendLine();
    }
    #endregion

}
