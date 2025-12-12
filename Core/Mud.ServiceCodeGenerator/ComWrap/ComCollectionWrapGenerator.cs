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
    /// <summary>
    /// 获取COM对象包装特性名称数组
    /// </summary>
    /// <returns>特性名称数组</returns>
    protected override string[] ComWrapAttributeNames() => ComWrapGeneratorConstants.ComCollectionWrapAttributeNames;

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

        // 生成属性
        GenerateCollectionProperties(sb, interfaceSymbol, interfaceDeclaration);


        // 生成方法
        GenerateMethods(sb, interfaceSymbol, interfaceDeclaration);

        // 生成IEnumerable实现
        GenerateIEnumerableImplementation(sb, interfaceSymbol, interfaceDeclaration);

        // 生成IDisposable实现
        GenerateIDisposableImplementation(sb, interfaceDeclaration, interfaceSymbol);

        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

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
        sb.AppendLine("        private readonly DisposableList _disposableList = new();");

        sb.AppendLine();
        sb.AppendLine($"        /// <summary>");
        sb.AppendLine($"        /// 用于方便内部调用的 <see cref=\"{comNamespace}.{comClassName}\"/> COM对象。");
        sb.AppendLine($"        /// </summary>");
        sb.AppendLine($"        public {comNamespace}.{comClassName}? InternalComObject => {PrivateFieldNamingHelper.GeneratePrivateFieldName(comClassName)};");
        sb.AppendLine();
    }

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

    /// <summary>
    /// 生成集合属性和索引器实现
    /// </summary>
    /// <param name="sb">字符串构建器</param>
    /// <param name="interfaceSymbol">接口符号</param>
    /// <param name="interfaceDeclaration">接口声明语法</param>
    private void GenerateCollectionProperties(StringBuilder sb, INamedTypeSymbol interfaceSymbol, InterfaceDeclarationSyntax interfaceDeclaration)
    {
        sb.AppendLine("        #region 属性");
        sb.AppendLine();

        foreach (var member in interfaceSymbol.GetMembers().OfType<IPropertySymbol>())
        {
            if (!ShouldIgnoreMember(member))
            {
                if (member.IsIndexer)
                {
                    // 处理索引器
                    GenerateIndexerImplementation(sb, member, interfaceSymbol, interfaceDeclaration);
                }
                else
                {
                    // 处理普通属性
                    GenerateProperty(sb, member, interfaceDeclaration);
                }
            }
        }

        sb.AppendLine("        #endregion");
        sb.AppendLine();
    }

    /// <summary>
    /// 生成索引器实现
    /// </summary>
    /// <param name="sb">字符串构建器</param>
    /// <param name="indexerSymbol">索引器符号</param>
    /// <param name="interfaceDeclaration">接口声明语法</param>
    private void GenerateIndexerImplementation(StringBuilder sb, IPropertySymbol indexerSymbol, INamedTypeSymbol interfaceSymbol, InterfaceDeclarationSyntax interfaceDeclaration)
    {
        //System.Diagnostics.Debugger.Launch();
        var elementType = indexerSymbol.Type.ToDisplayString();
        var comClassName = GetComClassName(interfaceDeclaration);
        var elementImplType = GetImplementationType(elementType);
        var isEnumType = IsEnumType(indexerSymbol.Type);
        var defaultValue = GetDefaultValue(interfaceDeclaration, indexerSymbol, indexerSymbol.Type);
        var privateFieldName = PrivateFieldNamingHelper.GeneratePrivateFieldName(comClassName);
        var isItemIndex = AttributeDataHelper.HasAttribute(interfaceSymbol, ComWrapGeneratorConstants.ItemIndexAttributeNames);

        if (indexerSymbol.Parameters.Length == 1)
        {
            var parameter = indexerSymbol.Parameters[0];
            var parameterType = parameter.Type.ToString();
            var enumParameterType = IsEnumType(parameter.Type);
            var parameterName = "index"; // 统一使用 index 作为参数名
            sb.AppendLine($"        ///  <inheritdoc/>");
            sb.AppendLine($"        {GeneratedCodeAttribute}");
            sb.AppendLine($"        public {elementType} this[{parameterType} {parameterName}]");
            sb.AppendLine("        {");
            sb.AppendLine("            get");
            sb.AppendLine("            {");

            if (parameterType == "int")
            {
                GenerateIntIndex(
                    sb, isItemIndex,
                    isEnumType, defaultValue,
                    elementImplType, privateFieldName,
                    parameterName);
            }
            else if (parameterType == "string")
            {
                GenerateStringIndex(sb,
                    isItemIndex, isEnumType,
                    defaultValue, elementImplType,
                    privateFieldName, parameterName);
            }
            else if (enumParameterType)
            {
                var enumParamDefaultValue = GetDefaultValue(interfaceDeclaration, parameter, parameter.Type);
                var isConvertIntIndex = AttributeDataHelper.HasAttribute(parameter, ComWrapGeneratorConstants.ConvertIntAttributeNames);
                if (isConvertIntIndex)
                    parameterName = $"{parameterName}.ConvertToInt()";
                else
                    parameterName = $"{parameterName}.EnumConvert({enumParamDefaultValue})";

                GenerateEnumIndex(
                        sb, isItemIndex,
                        isEnumType, defaultValue,
                        elementImplType, privateFieldName,
                        parameterName);
            }
            else
            {
                // 默认实现
                sb.AppendLine($"                throw new NotSupportedException(\"不支持的索引器参数类型: {parameterType}\");");
            }

            sb.AppendLine("            }");
            sb.AppendLine("        }");
            sb.AppendLine();
        }
        else
        {
            sb.AppendLine($"                throw new NotSupportedException(\"不支持多参数的索引器参数类型 \");");
        }
    }

    private static void GenerateEnumIndex(
      StringBuilder sb,
      bool isItemIndex,
      bool isEnumType,
      string? defaultValue,
      string elementImplType,
      string privateFieldName,
      string parameterName)
    {
        sb.AppendLine($"                if ({privateFieldName} == null)");
        sb.AppendLine($"                     throw new ArgumentNullException(nameof({privateFieldName}), \"COM对象资源已释放，不能再次访问。\");");
        sb.AppendLine();
        GenerateCommonIndexLogic(sb, isItemIndex, isEnumType, defaultValue, elementImplType,
            privateFieldName, parameterName, "根据字段名称");
    }

    private static void GenerateStringIndex(
        StringBuilder sb,
        bool isItemIndex,
        bool isEnumType,
        string? defaultValue,
        string elementImplType,
        string privateFieldName,
        string parameterName)
    {
        sb.AppendLine($"                if ({privateFieldName} == null)");
        sb.AppendLine($"                     throw new ArgumentNullException(nameof({privateFieldName}), \"COM对象资源已释放，不能再次访问。\");");
        sb.AppendLine($"                if (string.IsNullOrEmpty({parameterName}))");
        sb.AppendLine($"                    throw new ArgumentNullException(nameof({parameterName}));");
        sb.AppendLine();

        GenerateCommonIndexLogic(sb, isItemIndex, isEnumType, defaultValue, elementImplType,
            privateFieldName, parameterName, "根据字段名称");
    }

    private static void GenerateIntIndex(
        StringBuilder sb,
        bool isItemIndex,
        bool isEnumType,
        string? defaultValue,
        string elementImplType,
        string privateFieldName,
        string parameterName)
    {
        sb.AppendLine($"                if ({privateFieldName} == null)");
        sb.AppendLine($"                     throw new ArgumentNullException(nameof({privateFieldName}), \"COM对象资源已释放，不能再次访问。\");");
        sb.AppendLine($"                if ({parameterName} < 1)");
        sb.AppendLine("                      throw new IndexOutOfRangeException(\"索引参数不能少于1\");");

        GenerateCommonIndexLogic(sb, isItemIndex, isEnumType, defaultValue, elementImplType,
            privateFieldName, parameterName, "根据索引");
    }

    private static void GenerateCommonIndexLogic(
        StringBuilder sb,
        bool isItemIndex,
        bool isEnumType,
        string? defaultValue,
        string elementImplType,
        string privateFieldName,
        string parameterName,
        string operationType)
    {
        sb.AppendLine("                try");
        sb.AppendLine("                {");
        if (isItemIndex)
        {
            sb.AppendLine($"                    var comElement = {privateFieldName}.Item({parameterName});");
        }
        else
        {
            sb.AppendLine($"                    var comElement = {privateFieldName}[{parameterName}];");
        }

        if (IsBasicType(elementImplType) || isEnumType)
        {
            if (isEnumType)
                sb.AppendLine($"                    return comElement.EnumConvert({defaultValue});");
            else
                sb.AppendLine($"                    return comElement;");
        }
        else
        {
            sb.AppendLine($"                    var result = comElement != null ? new {elementImplType}(comElement) : null;");
            sb.AppendLine("                    if (result != null)");
            sb.AppendLine("                        _disposableList.Add(result);");
            sb.AppendLine("                    return result;");
        }
        sb.AppendLine("                }");
        sb.AppendLine("                catch (COMException ce)");
        sb.AppendLine("                {");
        sb.AppendLine($"                    throw new ExcelOperationException(\"{operationType}检索 {elementImplType} 对象失败: \" + ce.Message, ce);");
        sb.AppendLine("                }");
    }

    private void GenerateIEnumerableImplementation(StringBuilder sb, INamedTypeSymbol interfaceSymbol, InterfaceDeclarationSyntax interfaceDeclaration)
    {
        var elementType = GetCollectionElementType(interfaceSymbol);
        if (elementType == null)
            return;
        var isEnumType = IsEnumType(elementType);
        var defaultValue = GetDefaultValue(interfaceDeclaration, interfaceSymbol, elementType);
        var elementImplType = GetImplementationType(elementType.ToDisplayString());
        var comClassName = GetComClassName(interfaceDeclaration);
        var privateFieldName = PrivateFieldNamingHelper.GeneratePrivateFieldName(comClassName);
        var isItemIndex = AttributeDataHelper.HasAttribute(interfaceSymbol, ComWrapGeneratorConstants.ItemIndexAttributeNames);

        sb.AppendLine("        #region IEnumerable 实现");
        sb.AppendLine($"        {GeneratedCodeAttribute}");
        sb.AppendLine($"        ///  <inheritdoc/>");
        sb.AppendLine($"        public IEnumerator<{elementType}> GetEnumerator()");
        sb.AppendLine("        {");
        sb.AppendLine($"            if ({privateFieldName} == null) yield break;");
        sb.AppendLine();
        sb.AppendLine("            for (int i = 1; i <= Count; i++)");
        sb.AppendLine("            {");
        if (isItemIndex)
        {
            sb.AppendLine($"               var comElement = {privateFieldName}.Item(i);");
        }
        else
        {
            sb.AppendLine($"               var comElement = {privateFieldName}[i];");
        }
        if (IsBasicType(elementImplType) || isEnumType)
        {
            if (isEnumType)
                sb.AppendLine($"               yield return comElement.EnumConvert({defaultValue});");
            else
                sb.AppendLine("               yield return comElement;");
        }
        else
        {
            sb.AppendLine("                {");
            sb.AppendLine($"                    var element = new {elementImplType}(comElement);");
            sb.AppendLine("                    _disposableList.Add(element);");
            sb.AppendLine("                    yield return element;");
            sb.AppendLine("                }");
        }
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()");
        sb.AppendLine("        {");
        sb.AppendLine("            return GetEnumerator();");
        sb.AppendLine("        }");
        sb.AppendLine("        #endregion");
        sb.AppendLine();
    }

    /// <summary>
    /// 生成额外的释放逻辑（重写基类方法）
    /// </summary>
    /// <param name="sb">字符串构建器</param>
    /// <param name="interfaceSymbol">接口符号</param>
    /// <param name="interfaceDeclaration">接口声明语法</param>
    protected override void GenerateAdditionalDisposalLogic(StringBuilder sb, INamedTypeSymbol interfaceSymbol, InterfaceDeclarationSyntax interfaceDeclaration)
    {
        sb.AppendLine("                _disposableList.Dispose();");
        GeneratePrivateFieldDisposable(sb, interfaceSymbol, interfaceDeclaration);
    }
}
