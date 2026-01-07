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
    #region Constants
    private static class CollectionConstants
    {
        public const string CountPropertyName = "Count";
        public const string IndexerFormat = "this[{0}]";
    }


    /// <summary>
    /// 元素类型信息封装
    /// </summary>
    private class ElementTypeInfo
    {
        public ITypeSymbol ElementType { get; }
        public string ElementTypeName { get; }
        public bool IsBasicType { get; }
        public bool IsEnumType { get; }
        public bool IsComplexType { get; }
        public string DefaultValue { get; }
        public string ImplType { get; }
        public string ComType { get; }

        public ElementTypeInfo(
            ITypeSymbol ElementType,
            string ElementTypeName,
            bool IsBasicType,
            bool IsEnumType,
            bool IsComplexType,
            string DefaultValue,
            string ImplType,
            string ComType)
        {
            this.ElementType = ElementType;
            this.ElementTypeName = ElementTypeName;
            this.IsBasicType = IsBasicType;
            this.IsEnumType = IsEnumType;
            this.IsComplexType = IsComplexType;
            this.DefaultValue = DefaultValue;
            this.ImplType = ImplType;
            this.ComType = ComType;
        }
    }
    #endregion

    #region Generator Override Methods
    /// <summary>
    /// 获取COM对象包装特性名称数组
    /// </summary>
    /// <returns>特性名称数组</returns>
    protected override string[] ComWrapAttributeNames() => ComWrapConstants.ComCollectionWrapAttributeNames;

    /// <summary>
    /// 重写 GenerateAdditionalImplementations，生成 IEnumerable 实现
    /// </summary>
    private void GenerateAdditionalImplementations(
        StringBuilder sb,
        INamedTypeSymbol interfaceSymbol,
        InterfaceDeclarationSyntax interfaceDeclaration)
    {
        if (sb == null) throw new ArgumentNullException(nameof(sb));
        if (interfaceSymbol == null) throw new ArgumentNullException(nameof(interfaceSymbol));
        if (interfaceDeclaration == null) throw new ArgumentNullException(nameof(interfaceDeclaration));

        GenerateEnumerableImplementation(sb, interfaceSymbol, interfaceDeclaration);
    }

    /// <summary>
    /// 重写 GenerateImplementationClass，使用模板方法模式
    /// </summary>
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
        GenerateFields(sb, interfaceDeclaration, interfaceSymbol);
        GeneratePrivateField(sb, interfaceSymbol, interfaceDeclaration);

        // 生成构造函数
        GenerateConstructor(sb, className, interfaceSymbol, interfaceDeclaration);

        // 生成公共接口方法。
        GenerateCommonInterfaceMethod(sb, className, interfaceSymbol, interfaceDeclaration);

        // 生成属性
        GenerateProperties(sb, interfaceSymbol, interfaceDeclaration);

        // 生成方法
        GenerateMethods(sb, interfaceSymbol, interfaceDeclaration);

        // 模板方法：生成额外的实现内容（IEnumerable）
        GenerateAdditionalImplementations(sb, interfaceSymbol, interfaceDeclaration);

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
    /// 创建元素类型信息
    /// </summary>
    private ElementTypeInfo CreateElementTypeInfo(
        ITypeSymbol elementType,
        InterfaceDeclarationSyntax interfaceDeclaration)
    {
        var elementTypeName = elementType.ToDisplayString();
        return new ElementTypeInfo(
            ElementType: elementType,
            ElementTypeName: elementTypeName,
            IsBasicType: TypeSymbolHelper.IsBasicType(elementTypeName),
            IsEnumType: TypeSymbolHelper.IsEnumType(elementType),
            IsComplexType: TypeSymbolHelper.IsComplexObjectType(elementType),
            DefaultValue: GetDefaultValue(interfaceDeclaration, elementType, elementType),
            ImplType: GetImplementationType(elementTypeName),
            ComType: GetOrdinalComType(elementType.Name));
    }
    #endregion

    #region Enumerable Implementation
    /// <summary>
    /// 生成 IEnumerable 实现
    /// </summary>
    private void GenerateEnumerableImplementation(
        StringBuilder sb,
        INamedTypeSymbol interfaceSymbol,
        InterfaceDeclarationSyntax interfaceDeclaration)
    {
        var elementType = GetCollectionElementType(interfaceSymbol);
        if (elementType == null)
        {
            GenerateEnumerableWarning(sb);
            return;
        }

        var elementInfo = CreateElementTypeInfo(
            elementType,
            interfaceDeclaration);

        var comNamespace = GetComNamespace(interfaceSymbol, interfaceDeclaration);
        var comClassName = GetComClassName(interfaceSymbol, interfaceDeclaration);
        var privateFieldName = PrivateFieldNamingHelper.GeneratePrivateFieldName(comClassName);
        var isNoneEnumerable = AttributeDataHelper.HasAttribute(interfaceSymbol, ComWrapConstants.NoneEnumerableAttributes);

        sb.AppendLine("        #region IEnumerable 实现");
        sb.AppendLine($"        ///  <inheritdoc/>");
        sb.AppendLine($"        {GeneratedCodeAttribute}");
        sb.AppendLine($"        public IEnumerator<{elementInfo.ElementTypeName}> GetEnumerator()");
        sb.AppendLine("        {");
        sb.AppendLine($"            if ({privateFieldName} == null) yield break;");
        sb.AppendLine();

        if (isNoneEnumerable)
        {
            GenerateIndexerBasedEnumerator(sb, elementInfo);
        }
        else
        {
            GenerateCollectionBasedEnumerator(sb, elementInfo, comNamespace, privateFieldName);
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

    /// <summary>
    /// 生成索引器模式的枚举器
    /// </summary>
    private void GenerateIndexerBasedEnumerator(
        StringBuilder sb,
        ElementTypeInfo elementInfo)
    {
        // 使用一个本地函数来处理异常，避免在 try-catch 中使用 yield
        sb.AppendLine($"                for (int i = 1; i <= this.{CollectionConstants.CountPropertyName}; i++)");
        sb.AppendLine("                {");
        sb.AppendLine($"                    {elementInfo.ElementTypeName} GetItemAt(int index)");
        sb.AppendLine("                    {");
        sb.AppendLine("                        try");
        sb.AppendLine("                        {");
        sb.AppendLine("                            var result = this[index];");
        sb.AppendLine("                            if (result != null)");
        sb.AppendLine($"                                return ({elementInfo.ElementTypeName})result;");
        sb.AppendLine("                            else");
        sb.AppendLine("                                return default;");
        sb.AppendLine("                        }");
        sb.AppendLine("                        catch (COMException ce)");
        sb.AppendLine("                        {");
        sb.AppendLine($"                            throw new ExcelOperationException(\"枚举 {elementInfo.ElementTypeName} 集合时发生COM异常: \" + ce.Message, ce);");
        sb.AppendLine("                        }");
        sb.AppendLine("                        catch (Exception ex)");
        sb.AppendLine("                        {");
        sb.AppendLine($"                            throw new ExcelOperationException(\"枚举 {elementInfo.ElementTypeName} 集合时发生异常\", ex);");
        sb.AppendLine("                        }");
        sb.AppendLine("                    }");
        sb.AppendLine();
        sb.AppendLine("                    yield return GetItemAt(i);");
        sb.AppendLine("                }");
    }

    /// <summary>
    /// 生成集合遍历模式的枚举器
    /// </summary>
    private void GenerateCollectionBasedEnumerator(
        StringBuilder sb,
        ElementTypeInfo elementInfo,
        string comNamespace,
        string privateFieldName)
    {
        sb.AppendLine($"            foreach (var item in {privateFieldName})");
        sb.AppendLine("            {");
        sb.AppendLine("                object? returnValue = null;");
        sb.AppendLine("                try");
        sb.AppendLine("                {");

        if (elementInfo.IsBasicType || elementInfo.IsEnumType)
        {
            // 对于基本类型和枚举类型，调用 ObjectConvertEnum 或转换方法
            GenerateBasicOrEnumElementConversion(sb, elementInfo, "item");
        }
        else
        {
            // 对于复杂对象类型，检查类型并创建包装对象
            GenerateComplexElementConversion(sb, elementInfo, comNamespace, "item");
        }

        sb.AppendLine("                }");
        sb.AppendLine("                catch (COMException ce)");
        sb.AppendLine("                {");
        sb.AppendLine($"                    throw new ExcelOperationException(\"转换 {elementInfo.ElementTypeName} 元素时发生COM异常: \" + ce.Message, ce);");
        sb.AppendLine("                }");
        sb.AppendLine("                catch (Exception ex)");
        sb.AppendLine("                {");
        sb.AppendLine($"                    throw new ExcelOperationException(\"转换 {elementInfo.ElementTypeName} 元素时发生异常\", ex);");
        sb.AppendLine("                }");

        // yield return 必须在 try-catch 块外面
        sb.AppendLine("                if (returnValue != null)");
        sb.AppendLine("                {");
        sb.AppendLine($"                    yield return ({elementInfo.ElementTypeName})returnValue;");
        sb.AppendLine("                }");
        sb.AppendLine("            }");
    }

    /// <summary>
    /// 生成基本类型或枚举类型的元素转换（用于通过 object? 中转的场景）
    /// </summary>
    private void GenerateBasicOrEnumElementConversion(
        StringBuilder sb,
        ElementTypeInfo elementInfo,
        string itemName)
    {
        if (elementInfo.IsEnumType)
        {
            // 枚举类型：直接调用 ObjectConvertEnum 扩展方法
            // 该方法会返回值类型，但由于 returnValue 是 object?，所以装箱是安全的
            sb.AppendLine($"                    returnValue = {itemName}.ObjectConvertEnum<{elementInfo.ElementTypeName}>();");
        }
        else
        {
            // 基本类型：使用扩展方法转换
            var convertCode = GenerateBasicTypeConvertCode(elementInfo.ElementType, itemName);
            sb.AppendLine($"                    returnValue = {convertCode};");
        }
    }

    /// <summary>
    /// 生成基本类型转换代码
    /// </summary>
    private string GenerateBasicTypeConvertCode(ITypeSymbol typeSymbol, string fieldName)
    {
        if (typeSymbol == null) return fieldName;

        return typeSymbol.SpecialType switch
        {
            SpecialType.System_Boolean => $"{fieldName}.ConvertToBool()",
            SpecialType.System_SByte => $"Convert.ToSByte({fieldName})",
            SpecialType.System_Byte => $"Convert.ToByte({fieldName})",
            SpecialType.System_Int16 => $"Convert.ToInt16({fieldName})",
            SpecialType.System_UInt16 => $"Convert.ToUInt16({fieldName})",
            SpecialType.System_Int32 => $"Convert.ToInt32({fieldName})",
            SpecialType.System_UInt32 => $"Convert.ToUInt32({fieldName})",
            SpecialType.System_Int64 => $"Convert.ToInt64({fieldName})",
            SpecialType.System_UInt64 => $"Convert.ToUInt64({fieldName})",
            SpecialType.System_Single => $"{fieldName}.ConvertToFloat()",
            SpecialType.System_Double => $"{fieldName}.ConvertToDouble()",
            SpecialType.System_Decimal => $"{fieldName}.ConvertToDecimal()",
            SpecialType.System_String => $"{fieldName}.ToString()",
            SpecialType.System_DateTime => $"{fieldName}.ConvertToDateTime()",
            _ => fieldName
        };
    }

    /// <summary>
    /// 生成复杂对象类型的元素转换
    /// </summary>
    private void GenerateComplexElementConversion(
        StringBuilder sb,
        ElementTypeInfo elementInfo,
        string comNamespace,
        string itemName)
    {
        sb.AppendLine($"                    if ({itemName} is {comNamespace}.{elementInfo.ComType} rComObj)");
        sb.AppendLine("                    {");
        sb.AppendLine($"                        returnValue = new {elementInfo.ImplType}(rComObj);");
        if (elementInfo.ImplType != null && elementInfo.ImplType.EndsWith("?", StringComparison.Ordinal))
        {
            sb.AppendLine("                        if (returnValue is IDisposable disposable)");
            sb.AppendLine("                            _disposableList.Add(disposable);");
        }
        sb.AppendLine("                    }");
    }

    /// <summary>
    /// 生成警告注释（当无法确定元素类型时）
    /// </summary>
    private void GenerateEnumerableWarning(StringBuilder sb)
    {
        sb.AppendLine("            // 警告: 无法确定集合元素类型，请确保接口实现 IEnumerable<T>");
        sb.AppendLine("            yield break;");
    }
    #endregion
}
