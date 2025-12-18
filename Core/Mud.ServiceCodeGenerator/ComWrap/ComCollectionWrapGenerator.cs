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
        GenerateEnumerableImplementation(sb, interfaceSymbol, interfaceDeclaration);

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
        var privateFieldName = PrivateFieldNamingHelper.GeneratePrivateFieldName(comClassName);


        sb.AppendLine($"        private {comNamespace}.{comClassName}? {privateFieldName};");
        sb.AppendLine("        private bool _disposedValue;");
        sb.AppendLine("        private readonly DisposableList _disposableList = new();");

        sb.AppendLine();
        sb.AppendLine($"        /// <summary>");
        sb.AppendLine($"        /// 用于方便内部调用的 <see cref=\"{comNamespace}.{comClassName}\"/> COM对象。");
        sb.AppendLine($"        /// </summary>");
        sb.AppendLine($"        public {comNamespace}.{comClassName}? InternalComObject => {privateFieldName};");
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
        var elementType = indexerSymbol.Type.ToDisplayString();
        var comClassName = GetComClassName(interfaceDeclaration);
        var elementImplType = GetImplementationType(elementType);
        var isItemIndex = AttributeDataHelper.HasAttribute(interfaceSymbol, ComWrapConstants.ItemIndexAttributeNames);
        var privateFieldName = PrivateFieldNamingHelper.GeneratePrivateFieldName(comClassName);

        if (indexerSymbol.Parameters.Length == 1)
        {
            GenerateSingleParameterIndexer(sb, indexerSymbol, interfaceDeclaration, interfaceSymbol,
                elementType, elementImplType, isItemIndex, privateFieldName);
        }
        else if (indexerSymbol.Parameters.Length == 2)
        {
            GenerateTwoParameterIndexer(sb, indexerSymbol, interfaceDeclaration, interfaceSymbol,
                elementType, elementImplType, isItemIndex, privateFieldName);
        }
        else
        {
            sb.AppendLine($"                throw new NotSupportedException(\"不支持超过2个参数的索引器参数类型 \");");
        }
    }

    /// <summary>
    /// 生成单参数索引器实现
    /// </summary>
    private void GenerateSingleParameterIndexer(StringBuilder sb, IPropertySymbol indexerSymbol,
        InterfaceDeclarationSyntax interfaceDeclaration, INamedTypeSymbol interfaceSymbol,
        string elementType, string elementImplType, bool isItemIndex, string privateFieldName)
    {
        var parameter = indexerSymbol.Parameters[0];
        var parameterType = parameter.Type.ToString();
        var parameterName = "index";

        GenerateIndexerSignature(sb, elementType, parameterType, parameterName);
        GenerateIndexerGetBody(sb, indexerSymbol, interfaceDeclaration, isItemIndex,
            elementImplType, privateFieldName, new[] { parameter }, new[] { parameterName });

        if (indexerSymbol.SetMethod != null)
        {
            GenerateIndexerSetBody(sb, indexerSymbol, interfaceDeclaration, isItemIndex,
                elementImplType, privateFieldName, new[] { parameter }, new[] { parameterName });
        }

        sb.AppendLine("        }");
        sb.AppendLine();
    }

    /// <summary>
    /// 生成双参数索引器实现
    /// </summary>
    private void GenerateTwoParameterIndexer(StringBuilder sb, IPropertySymbol indexerSymbol,
        InterfaceDeclarationSyntax interfaceDeclaration, INamedTypeSymbol interfaceSymbol,
        string elementType, string elementImplType, bool isItemIndex, string privateFieldName)
    {
        var param1 = indexerSymbol.Parameters[0];
        var param2 = indexerSymbol.Parameters[1];
        var param1Type = param1.Type.ToString();
        var param2Type = param2.Type.ToString();
        var param1Name = "index1";
        var param2Name = "index2";

        GenerateIndexerSignature(sb, elementType, new[] { param1Type, param2Type }, new[] { param1Name, param2Name });
        GenerateIndexerGetBody(sb, indexerSymbol, interfaceDeclaration, isItemIndex,
            elementImplType, privateFieldName, new[] { param1, param2 }, new[] { param1Name, param2Name });

        if (indexerSymbol.SetMethod != null)
        {
            GenerateIndexerSetBody(sb, indexerSymbol, interfaceDeclaration, isItemIndex,
                elementImplType, privateFieldName, new[] { param1, param2 }, new[] { param1Name, param2Name });
        }

        sb.AppendLine("        }");
        sb.AppendLine();
    }

    /// <summary>
    /// 生成索引器签名
    /// </summary>
    private void GenerateIndexerSignature(StringBuilder sb, string elementType, string parameterType, string parameterName)
    {
        sb.AppendLine($"        ///  <inheritdoc/>");
        sb.AppendLine($"        {GeneratedCodeAttribute}");
        sb.AppendLine($"        public {elementType} this[{parameterType} {parameterName}]");
        sb.AppendLine("        {");
    }

    /// <summary>
    /// 生成索引器签名（多参数重载）
    /// </summary>
    private void GenerateIndexerSignature(StringBuilder sb, string elementType, string[] parameterTypes, string[] parameterNames)
    {
        var parameters = parameterTypes.Zip(parameterNames, (type, name) => $"{type} {name}");
        var parametersStr = string.Join(", ", parameters);

        sb.AppendLine($"        ///  <inheritdoc/>");
        sb.AppendLine($"        {GeneratedCodeAttribute}");
        sb.AppendLine($"        public {elementType} this[{parametersStr}]");
        sb.AppendLine("        {");
    }

    /// <summary>
    /// 生成索引器get方法体
    /// </summary>
    private void GenerateIndexerGetBody(StringBuilder sb, IPropertySymbol indexerSymbol,
        InterfaceDeclarationSyntax interfaceDeclaration, bool isItemIndex, string elementImplType,
        string privateFieldName, IParameterSymbol[] parameters, string[] parameterNames)
    {
        sb.AppendLine("            get");
        sb.AppendLine("            {");

        // 生成参数验证和预处理
        var processedParameters = GenerateParameterValidationAndProcessing(sb, interfaceDeclaration,
            parameters, parameterNames, privateFieldName, isGetMethod: true);

        // 生成获取逻辑
        if (parameters.Length == 1)
        {
            GenerateSingleParameterGetLogic(sb, indexerSymbol, interfaceDeclaration, isItemIndex,
                elementImplType, privateFieldName, processedParameters[0]);
        }
        else if (parameters.Length == 2)
        {
            CommonTwoParameterGetLogic(sb, indexerSymbol, interfaceDeclaration, isItemIndex,
                elementImplType, privateFieldName, processedParameters[0], processedParameters[1]);
        }

        sb.AppendLine("            }");
    }

    /// <summary>
    /// 生成索引器set方法体
    /// </summary>
    private void GenerateIndexerSetBody(StringBuilder sb, IPropertySymbol indexerSymbol,
        InterfaceDeclarationSyntax interfaceDeclaration, bool isItemIndex, string elementImplType,
        string privateFieldName, IParameterSymbol[] parameters, string[] parameterNames)
    {
        sb.AppendLine("            set");
        sb.AppendLine("            {");

        // 生成参数验证和预处理
        var processedParameters = GenerateParameterValidationAndProcessing(sb, interfaceDeclaration,
            parameters, parameterNames, privateFieldName, isGetMethod: false);

        // 生成设置逻辑
        if (parameters.Length == 1)
        {
            GenerateSingleParameterSetLogic(sb, indexerSymbol, interfaceDeclaration, isItemIndex,
                elementImplType, privateFieldName, processedParameters[0]);
        }
        else if (parameters.Length == 2)
        {
            CommonMultiParameterSetLogic(sb, indexerSymbol, interfaceDeclaration, isItemIndex,
                elementImplType, privateFieldName, processedParameters);
        }

        sb.AppendLine("            }");
    }

    /// <summary>
    /// 生成参数验证和预处理逻辑
    /// </summary>
    private string[] GenerateParameterValidationAndProcessing(StringBuilder sb,
        InterfaceDeclarationSyntax interfaceDeclaration, IParameterSymbol[] parameters,
        string[] parameterNames, string privateFieldName, bool isGetMethod)
    {
        var processedParameters = new string[parameters.Length];

        for (int i = 0; i < parameters.Length; i++)
        {
            var param = parameters[i];
            var paramName = parameterNames[i];
            var paramType = param.Type.ToString();
            var paramEnumType = IsEnumType(param.Type);

            // 添加通用对象检查
            if (!isGetMethod)
            {
                sb.AppendLine($"                if ({privateFieldName} == null)");
                sb.AppendLine($"                    throw new ObjectDisposedException(nameof({privateFieldName}));");
            }
            else
            {
                sb.AppendLine($"                if ({privateFieldName} == null)");
                sb.AppendLine($"                    throw new ObjectDisposedException(nameof({privateFieldName}));");
            }

            // 参数类型特定验证
            GenerateParameterTypeValidation(sb, paramType, paramName, paramEnumType, i + 1, param.Type.NullableAnnotation == NullableAnnotation.Annotated);

            // 处理参数转换
            processedParameters[i] = ProcessParameter(sb, interfaceDeclaration, param, paramName, paramEnumType);
        }

        sb.AppendLine();
        return processedParameters;
    }

    /// <summary>
    /// 生成参数类型特定的验证逻辑
    /// </summary>
    private void GenerateParameterTypeValidation(StringBuilder sb, string paramType,
        string paramName, bool isEnumType, int parameterIndex, bool isNullable)
    {
        var paramDescription = parameterIndex == 1 ? "第一个" : "第二个";

        switch (paramType)
        {
            case "int":
                sb.AppendLine($"                if ({paramName} < 1)");
                sb.AppendLine($"                    throw new IndexOutOfRangeException(\"{paramDescription}索引参数不能少于1\");");
                break;
            case "string":
                sb.AppendLine($"                if (string.IsNullOrEmpty({paramName}))");
                sb.AppendLine($"                    throw new ArgumentNullException(nameof({paramName}));");
                break;
            default:
                if (isEnumType)
                {
                    // 对于非空值枚举，不需要检查 null
                    if (isNullable)
                    {
                        sb.AppendLine($"                if ({paramName} == null)");
                        sb.AppendLine($"                    throw new ArgumentNullException(nameof({paramName}));");
                    }
                }
                else
                {
                    sb.AppendLine($"                if ({paramName} == null)");
                    sb.AppendLine($"                    throw new ArgumentNullException(nameof({paramName}));");
                }
                break;
        }
    }

    /// <summary>
    /// 处理单个参数的转换
    /// </summary>
    private string ProcessParameter(
        StringBuilder sb,
        InterfaceDeclarationSyntax interfaceDeclaration,
        IParameterSymbol param,
        string paramName,
        bool isEnumType)
    {
        if (isEnumType)
        {
            var isConvertIntIndex = AttributeDataHelper.HasAttribute(param, ComWrapConstants.ConvertIntAttributeNames);

            if (isConvertIntIndex)
                return $"{paramName}.ConvertToInt()";
            else
            {
                var sourceEnumType = param.Type.ToDisplayString();
                var comNamespace = GetComNamespace(interfaceDeclaration);
                var targetEnumType = $"{comNamespace}.{param.Type.Name}";
                return $"{paramName}.EnumConvert<{sourceEnumType}, {targetEnumType}>()";
            }
        }

        return paramName;
    }

    /// <summary>
    /// 生成单参数获取逻辑
    /// </summary>
    private void GenerateSingleParameterGetLogic(StringBuilder sb, IPropertySymbol indexerSymbol,
        InterfaceDeclarationSyntax interfaceDeclaration, bool isItemIndex, string elementImplType,
        string privateFieldName, string processedParameter)
    {
        var operationType = processedParameter.Contains("index") ? "根据索引" : "根据字段名称";
        CommonGetLogic(sb, indexerSymbol, interfaceDeclaration, isItemIndex, elementImplType,
            privateFieldName, processedParameter, operationType);
    }

    /// <summary>
    /// 生成单参数设置逻辑
    /// </summary>
    private void GenerateSingleParameterSetLogic(StringBuilder sb, IPropertySymbol indexerSymbol,
        InterfaceDeclarationSyntax interfaceDeclaration, bool isItemIndex, string elementImplType,
        string privateFieldName, string processedParameter)
    {
        var operationType = processedParameter.Contains("index") ? "根据索引" : "根据字段名称";
        CommonSetLogic(sb, indexerSymbol, interfaceDeclaration, isItemIndex, elementImplType,
            privateFieldName, processedParameter, "value");
        AddSetExceptionHandling(sb, elementImplType, operationType);
    }

    private void CommonGetLogic(
        StringBuilder sb,
        IPropertySymbol indexerSymbol,
        InterfaceDeclarationSyntax interfaceDeclaration,
        bool isItemIndex,
        string elementImplType,
        string privateFieldName,
        string parameterName,
        string operationType)
    {
        var comNamespace = GetComNamespace(interfaceDeclaration);
        var isEnumType = IsEnumType(indexerSymbol.Type);
        var defaultValue = GetDefaultValue(interfaceDeclaration, indexerSymbol, indexerSymbol.Type);
        var needConvert = IsNeedConvert(indexerSymbol);
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
            string returnType = indexerSymbol.Type.ToDisplayString();
            var ordinalComType = GetImplementationOrdinalType(returnType);
            var comType = GetOrdinalComType(ordinalComType);
            returnType = returnType.EndsWith("?", StringComparison.Ordinal) ? returnType : returnType + "?";
            sb.AppendLine($"                    {returnType} result = null;");
            if (needConvert)
            {
                sb.AppendLine($"                    if(comElement is {comNamespace}.{comType} rComObj)");
                sb.AppendLine($"                       result = new {elementImplType}(rComObj);");
            }
            else
            {
                sb.AppendLine("                    if(comElement != null)");
                sb.AppendLine($"                       result = new {elementImplType}(comElement);");
            }
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



    /// <summary>
    /// 添加设置操作的异常处理
    /// </summary>
    private void AddSetExceptionHandling(StringBuilder sb, string elementImplType, string operationType)
    {
        sb.AppendLine("                }");
        sb.AppendLine("                catch (COMException ce)");
        sb.AppendLine("                {");
        sb.AppendLine($"                    throw new ExcelOperationException(\"{operationType}设置 {elementImplType} 对象失败: \" + ce.Message, ce);");
        sb.AppendLine("                }");
        sb.AppendLine("                catch (Exception ex)");
        sb.AppendLine("                {");
        sb.AppendLine($"                    throw new ExcelOperationException(\"{operationType}设置 {elementImplType} 对象失败\", ex);");
        sb.AppendLine("                }");
    }

    private void CommonSetLogic(
        StringBuilder sb,
        IPropertySymbol indexerSymbol,
        InterfaceDeclarationSyntax interfaceDeclaration,
        bool isItemIndex,
        string elementImplType,
        string privateFieldName,
        string parameterName,
        string valueExpression)
    {
        var isEnumType = IsEnumType(indexerSymbol.Type);
        sb.AppendLine("                try");
        sb.AppendLine("                {");
        // 处理value表达式，根据类型进行转换
        string setValue = valueExpression;
        if (elementImplType.EndsWith("?", StringComparison.Ordinal))
        {
            setValue = $"{valueExpression}.Value";
        }

        if (isEnumType)
        {
            setValue = $"{valueExpression}.EnumUnderlyingValue()";
        }

        // 生成COM对象赋值
        if (isItemIndex)
        {
            sb.AppendLine($"                    {privateFieldName}.Item({parameterName}) = {setValue};");
        }
        else
        {
            sb.AppendLine($"                    {privateFieldName}[{parameterName}] = {setValue};");
        }
    }

    /// <summary>
    /// 通用多参数设置逻辑
    /// </summary>
    private void CommonMultiParameterSetLogic(
        StringBuilder sb,
        IPropertySymbol indexerSymbol,
        InterfaceDeclarationSyntax interfaceDeclaration,
        bool isItemIndex,
        string elementImplType,
        string privateFieldName,
        string[] processedParameters)
    {
        var isEnumType = IsEnumType(indexerSymbol.Type);

        // 处理value表达式，根据类型进行转换
        string setValue = "value";
        if (elementImplType.EndsWith("?", StringComparison.Ordinal))
        {
            setValue = "value.Value";
        }

        if (isEnumType)
        {
            setValue = $"{setValue}.EnumUnderlyingValue()";
        }
        else if (IsComObjectType(indexerSymbol.Type))
        {
            var constructType = GetImplementationType(indexerSymbol.Type.Name);
            setValue = $"(({constructType}){setValue}).InternalComObject";
        }
        else if (!IsBasicType(elementImplType))
        {
            var constructType = GetImplementationType(indexerSymbol.Type.Name);
            setValue = $"(({constructType}){setValue}).InternalComObject";
        }

        var parametersStr = string.Join(", ", processedParameters);

        sb.AppendLine("                try");
        sb.AppendLine("                {");
        if (isItemIndex)
        {
            sb.AppendLine($"                    {privateFieldName}.Item({parametersStr}) = {setValue};");
        }
        else
        {
            sb.AppendLine($"                    {privateFieldName}[{parametersStr}] = {setValue};");
        }
        sb.AppendLine("                }");
        sb.AppendLine("                catch (COMException ce)");
        sb.AppendLine("                {");
        sb.AppendLine($"                    throw new ExcelOperationException(\"根据双索引设置 {elementImplType} 对象失败: \" + ce.Message, ce);");
        sb.AppendLine("                }");
        sb.AppendLine("                catch (Exception ex)");
        sb.AppendLine("                {");
        sb.AppendLine($"                    throw new ExcelOperationException(\"根据双索引设置 {elementImplType} 对象失败\", ex);");
        sb.AppendLine("                }");
    }

    /// <summary>
    /// 两个参数索引器的通用获取逻辑
    /// </summary>
    private void CommonTwoParameterGetLogic(
        StringBuilder sb,
        IPropertySymbol indexerSymbol,
        InterfaceDeclarationSyntax interfaceDeclaration,
        bool isItemIndex,
        string elementImplType,
        string privateFieldName,
        string processedParam1,
        string processedParam2)
    {
        var comNamespace = GetComNamespace(interfaceDeclaration);
        var isEnumType = IsEnumType(indexerSymbol.Type);
        var defaultValue = GetDefaultValue(interfaceDeclaration, indexerSymbol, indexerSymbol.Type);
        var needConvert = IsNeedConvert(indexerSymbol);

        sb.AppendLine("                try");
        sb.AppendLine("                {");
        if (isItemIndex)
        {
            sb.AppendLine($"                    var comElement = {privateFieldName}.Item({processedParam1}, {processedParam2});");
        }
        else
        {
            sb.AppendLine($"                    var comElement = {privateFieldName}[{processedParam1}, {processedParam2}];");
        }

        if (IsBasicType(elementImplType) || isEnumType)
        {
            if (isEnumType)
                sb.AppendLine($"                    return comElement.EnumConvert({defaultValue});");
            else if (needConvert)
            {
                var convertCode = GetConvertCode(indexerSymbol, "comElement");
                sb.AppendLine($"                    return {convertCode};");
            }
            else
                sb.AppendLine($"                    return ({elementImplType})comElement;");
        }
        else
        {
            if (needConvert)
            {
                var ordinalComType = GetImplementationOrdinalType(indexerSymbol.Type.ToDisplayString());
                var comType = GetOrdinalComType(ordinalComType);
                sb.AppendLine($"                    if(comElement is {comNamespace}.{comType} rComObj)");
                sb.AppendLine($"                        return new {elementImplType}(rComObj);");
                sb.AppendLine($"                    else");
                sb.AppendLine("                        return null;");
            }
            else
                sb.AppendLine($"                    return new {elementImplType}(comElement);");
        }

        sb.AppendLine("                }");
        sb.AppendLine("                catch (COMException ce)");
        sb.AppendLine("                {");
        sb.AppendLine($"                    throw new ExcelOperationException(\"根据双索引获取 {elementImplType} 对象失败: \" + ce.Message, ce);");
        sb.AppendLine("                }");
        sb.AppendLine("                catch (Exception ex)");
        sb.AppendLine("                {");
        sb.AppendLine($"                    throw new ExcelOperationException(\"根据双索引获取 {elementImplType} 对象失败\", ex);");
        sb.AppendLine("                }");
    }

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
        var isEnumType = IsEnumType(elementType);
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
            if (IsBasicType(elementImplType) || isEnumType)
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
