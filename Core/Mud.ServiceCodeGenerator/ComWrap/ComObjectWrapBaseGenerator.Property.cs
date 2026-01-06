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

partial class ComObjectWrapBaseGenerator
{
    #region Indexer Implementation
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
    #endregion

    #region Indexer Helper Methods
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
    #endregion

    #region Two Parameter Indexer
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
    #endregion

    #region Parameter Processing and Validation
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
            var paramEnumType = TypeSymbolHelper.IsEnumType(param.Type);

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
            case "int" or "long" or "float" or "short" or "double":
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
        var isEnumType = TypeSymbolHelper.IsEnumType(indexerSymbol.Type);
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
        var isEnumType = TypeSymbolHelper.IsEnumType(indexerSymbol.Type);

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
        else if (TypeSymbolHelper.IsComplexObjectType(indexerSymbol.Type))
        {
            var constructType = GetImplementationType(indexerSymbol.Type.Name);
            setValue = $"(({constructType}){setValue}).InternalComObject";
        }
        else if (!TypeSymbolHelper.IsBasicType(elementImplType))
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
        var isEnumType = TypeSymbolHelper.IsEnumType(indexerSymbol.Type);
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

        if (TypeSymbolHelper.IsBasicType(elementImplType) || isEnumType)
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
    #endregion

    #region Indexer Logic Implementation
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
        var isEnumType = TypeSymbolHelper.IsEnumType(indexerSymbol.Type);
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

        if (TypeSymbolHelper.IsBasicType(elementImplType) || isEnumType)
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
    #endregion

    #region Property Generation

    /// <summary>
    /// 生成集合属性和索引器实现
    /// </summary>
    /// <param name="sb">字符串构建器</param>
    /// <param name="interfaceSymbol">接口符号</param>
    /// <param name="interfaceDeclaration">接口声明语法</param>
    protected void GenerateProperties(StringBuilder sb, INamedTypeSymbol interfaceSymbol, InterfaceDeclarationSyntax interfaceDeclaration)
    {
        if (interfaceSymbol == null || interfaceDeclaration == null || sb == null)
            return;

        sb.AppendLine("        #region 属性");
        sb.AppendLine();

        foreach (var member in TypeSymbolHelper.GetAllProperties(interfaceSymbol))
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
    /// 生成单个属性实现
    /// </summary>
    /// <param name="sb">字符串构建器</param>
    /// <param name="propertySymbol">属性符号</param>
    /// <param name="interfaceDeclaration">接口声明语法</param>
    private void GenerateProperty(StringBuilder sb, IPropertySymbol propertySymbol, InterfaceDeclarationSyntax interfaceDeclaration)
    {
        if (sb == null || propertySymbol == null || interfaceDeclaration == null)
            return;

        var isEnumType = TypeSymbolHelper.IsEnumType(propertySymbol.Type);
        var isObjectType = TypeSymbolHelper.IsComplexObjectType(propertySymbol.Type);

        var comClassName = GetComClassName(interfaceDeclaration);
        var needConvert = IsNeedConvert(propertySymbol);

        if (isEnumType)
        {
            GenerateEnumProperty(sb, propertySymbol, interfaceDeclaration);
        }
        else if (isObjectType)
        {
            GenerateComObjectProperty(sb, propertySymbol, interfaceDeclaration, needConvert, comClassName);
        }
        else
        {
            GenerateObjectProperty(sb, propertySymbol, interfaceDeclaration, needConvert, comClassName);
        }
    }

    private string GetPropertyGetString(string fieldName, string propertyName, bool isMethod)
    {
        if (isMethod)
            return $"{fieldName}.get_{propertyName}()";
        else
            return $"{fieldName}.{propertyName}";
    }

    private string GetPropertySetString(string fieldName, string propertyName, bool isMethod, string setValue)
    {
        if (isMethod)
            return $"{fieldName}.set_{propertyName}({setValue})";
        else
            return $"{fieldName}.{propertyName} = {setValue}";
    }

    /// <summary>
    /// 生成普通对象属性实现
    /// </summary>
    /// <param name="sb">字符串构建器</param>
    /// <param name="propertySymbol">属性符号</param>
    /// <param name="interfaceDeclaration">接口声明语法</param>
    /// <param name="comClassName">COM类名</param>
    private void GenerateObjectProperty(StringBuilder sb, IPropertySymbol propertySymbol, InterfaceDeclarationSyntax interfaceDeclaration, bool needConvert, string comClassName)
    {
        var fieldName = PrivateFieldNamingHelper.GeneratePrivateFieldName(comClassName);
        var propertyName = GetPropertyName(propertySymbol);
        var isMethod = IsMethod(propertySymbol);
        var orgPropertyName = propertySymbol.Name;
        var propertyType = TypeSymbolHelper.GetTypeFullName(propertySymbol.Type);

        sb.AppendLine($"        ///  <inheritdoc/>");
        sb.AppendLine($"        {GeneratedCodeAttribute}");
        sb.AppendLine($"        public {propertyType} {orgPropertyName}");
        sb.AppendLine("        {");
        if (propertySymbol.GetMethod != null)
        {
            sb.AppendLine($"            get");
            sb.AppendLine("            {");
            sb.AppendLine($"                if({fieldName} == null)");
            sb.AppendLine($"                    throw new ObjectDisposedException(nameof({fieldName}));");

            if (needConvert)
            {
                var convertMethod = GetConvertCode(propertySymbol, $"{GetPropertyGetString(fieldName, propertyName, isMethod)}");
                sb.AppendLine($"                return {convertMethod};");
            }
            else
            {
                sb.AppendLine($"                return {GetPropertyGetString(fieldName, propertyName, isMethod)};");
            }
            sb.AppendLine("             }");
        }

        if (propertySymbol.SetMethod != null)
        {
            sb.AppendLine("            set");
            sb.AppendLine("            {");
            sb.AppendLine($"                if ({fieldName} == null)");
            sb.AppendLine($"                    throw new ObjectDisposedException(nameof({fieldName}));");

            if (propertyType.EndsWith("?", StringComparison.Ordinal))
            {
                sb.AppendLine($"                if (value == null)");
                sb.AppendLine($"                    throw new ArgumentNullException(nameof(value));");


                string setValue = "value.Value";
                if (ShouldUseDirectValueForNullable(propertyType))
                    setValue = "value";
                else if (needConvert && propertyType.StartsWith("bool", StringComparison.OrdinalIgnoreCase))
                {
                    setValue = $"value.Value.ConvertTriState()";
                }
                sb.AppendLine($"                {GetPropertySetString(fieldName, propertyName, isMethod, setValue)};");
            }
            else
            {
                string setValue = "value";
                if (needConvert)
                {
                    if (propertyType.StartsWith("bool", StringComparison.OrdinalIgnoreCase))
                        setValue = $"value.ConvertTriState()";
                    else if (IsSystemDrawingColor(propertySymbol.Type))
                        setValue = $"ColorHelper.ConvertToComColor(value)";

                }
                sb.AppendLine($"                {GetPropertySetString(fieldName, propertyName, isMethod, setValue)};");
            }
            sb.AppendLine("            }");
        }
        sb.AppendLine("        }");
        sb.AppendLine();
    }

    private bool ShouldUseDirectValueForNullable(string propertyType)
    {
        if (propertyType.StartsWith("object", StringComparison.OrdinalIgnoreCase)
            || propertyType.StartsWith("string", StringComparison.OrdinalIgnoreCase)
            || propertyType.StartsWith("System.Object", StringComparison.OrdinalIgnoreCase)
            || propertyType.StartsWith("System.String", StringComparison.OrdinalIgnoreCase))
            return true;
        return false;
    }

    /// <summary>
    /// 生成COM对象属性实现
    /// </summary>
    /// <param name="sb">字符串构建器</param>
    /// <param name="propertySymbol">属性符号</param>
    /// <param name="interfaceDeclaration">接口声明语法</param>
    /// <param name="comClassName">COM类名</param>
    private void GenerateComObjectProperty(StringBuilder sb,
                IPropertySymbol propertySymbol,
                InterfaceDeclarationSyntax interfaceDeclaration,
                bool needConvert,
                string comClassName)
    {
        var comNamespace = GetComNamespace(interfaceDeclaration);
        var orgPropertyName = propertySymbol.Name;
        var propertyName = GetPropertyName(propertySymbol);
        var isMethod = IsMethod(propertySymbol);
        var propertyType = TypeSymbolHelper.GetTypeFullName(propertySymbol.Type);
        var objectType = StringExtensions.RemoveInterfacePrefix(propertyType);
        var constructType = GetImplementationType(objectType);

        var impType = propertySymbol.Type.Name.Trim('?');
        var fieldName = PrivateFieldNamingHelper.GeneratePrivateFieldName(impType, FieldNamingStyle.UnderscoreCamel) + "_" + propertySymbol.Name;
        var privateFieldName = PrivateFieldNamingHelper.GeneratePrivateFieldName(comClassName);

        sb.AppendLine($"        ///  <inheritdoc/>");
        sb.AppendLine($"        {GeneratedCodeAttribute}");
        sb.AppendLine($"        public {propertyType} {orgPropertyName}");
        sb.AppendLine("        {");
        if (propertySymbol.GetMethod != null)
        {
            sb.AppendLine("             get");
            sb.AppendLine("             {");
            sb.AppendLine($"                if({privateFieldName} == null)");
            sb.AppendLine($"                    throw new ObjectDisposedException(nameof({privateFieldName}));");
            sb.AppendLine($"                var comObj = {GetPropertyGetString(privateFieldName, propertyName, isMethod)};");
            sb.AppendLine("                if (comObj == null)");
            sb.AppendLine("                    return null;");
            sb.AppendLine($"                if ({fieldName} != null)");
            sb.AppendLine($"                   return {fieldName};");

            if (!needConvert)
                sb.AppendLine($"                {fieldName} = new {constructType}(comObj);");
            else
            {
                var ordinalComType = GetImplementationOrdinalType(propertyType);
                var comType = GetOrdinalComType(ordinalComType);
                sb.AppendLine($"                if(comObj is {comNamespace}.{comType} rComObj)");
                sb.AppendLine($"                    {fieldName} = new {constructType}(rComObj);");
            }
            sb.AppendLine($"                return {fieldName};");
            sb.AppendLine("             }");
        }

        if (propertySymbol.SetMethod != null)
        {
            sb.AppendLine("             set");
            sb.AppendLine("             {");
            sb.AppendLine($"                if ({privateFieldName} == null )");
            sb.AppendLine($"                    throw new ObjectDisposedException(nameof({privateFieldName}));");
            sb.AppendLine($"                if (value != null)");
            sb.AppendLine($"                    throw new ArgumentNullException(nameof(value));");

            sb.AppendLine($"                if(value is {constructType} internalComObject)");
            sb.AppendLine("                {");
            sb.AppendLine($"                    var comObj = internalComObject.InternalComObject;");
            sb.AppendLine($"                    {GetPropertySetString(privateFieldName, propertyName, isMethod, "comObj")};");
            sb.AppendLine("                }");

            sb.AppendLine("             }");
        }
        sb.AppendLine("        }");
        sb.AppendLine();
    }

    /// <summary>
    /// 生成枚举属性实现
    /// </summary>
    /// <param name="sb">字符串构建器</param>
    /// <param name="propertySymbol">属性符号</param>
    /// <param name="interfaceDeclaration">接口声明语法</param>
    /// <param name="defaultValue">默认值</param>
    private void GenerateEnumProperty(StringBuilder sb, IPropertySymbol propertySymbol, InterfaceDeclarationSyntax interfaceDeclaration)
    {
        var orgPropertyName = propertySymbol.Name;
        var propertyName = GetPropertyName(propertySymbol);
        var propertyType = TypeSymbolHelper.GetTypeFullName(propertySymbol.Type);
        var isMethod = IsMethod(propertySymbol);
        var comNamespace = GetComNamespace(interfaceDeclaration);
        var comClassName = GetComClassName(interfaceDeclaration);
        var privateFieldName = PrivateFieldNamingHelper.GeneratePrivateFieldName(comClassName);

        var defaultValue = GetDefaultValue(interfaceDeclaration, propertySymbol, propertySymbol.Type);
        var enumValueName = GetEnumValueWithoutNamespace(defaultValue);

        var propertyComNamespace = GetPropertyComNamespace(propertySymbol);
        if (!string.IsNullOrEmpty(propertyComNamespace))
            comNamespace = propertyComNamespace;

        sb.AppendLine($"        ///  <inheritdoc/>");
        sb.AppendLine($"        {GeneratedCodeAttribute}");
        sb.AppendLine($"        public {propertyType} {orgPropertyName}");
        sb.AppendLine("        {");
        if (propertySymbol.GetMethod != null)
        {
            sb.AppendLine("            get");
            sb.AppendLine("            {");
            sb.AppendLine($"                if({privateFieldName} == null)");
            sb.AppendLine($"                    throw new ObjectDisposedException(nameof({privateFieldName}));");
            sb.AppendLine($"                return {GetPropertyGetString(privateFieldName, propertyName, isMethod)}.EnumConvert({defaultValue});");
            sb.AppendLine("             }");
        }

        if (propertySymbol.SetMethod != null)
        {
            var isConvertIntIndex = AttributeDataHelper.HasAttribute(propertySymbol, ComWrapConstants.ConvertIntAttributeNames);
            sb.AppendLine("            set");
            sb.AppendLine("            {");
            sb.AppendLine($"                if({privateFieldName} == null)");
            sb.AppendLine($"                    throw new ObjectDisposedException(nameof({privateFieldName}));");

            if (isConvertIntIndex)
                sb.AppendLine($"                {GetPropertySetString(privateFieldName, propertyName, isMethod, "value.ConvertToInt()")};");
            else
                sb.AppendLine($"                {GetPropertySetString(privateFieldName, propertyName, isMethod, $"value.EnumConvert({comNamespace}.{enumValueName})")};");
            sb.AppendLine("            }");
        }
        sb.AppendLine("        }");
        sb.AppendLine();
    }

    #region Method Generation
    /// <summary>
    /// 生成方法实现
    /// </summary>
    /// <param name="sb">字符串构建器</param>
    /// <param name="interfaceSymbol">接口符号</param>
    protected void GenerateMethods(StringBuilder sb, INamedTypeSymbol interfaceSymbol, InterfaceDeclarationSyntax interfaceDeclaration)
    {
        if (interfaceDeclaration == null || sb == null || interfaceSymbol == null)
            return;

        sb.AppendLine("        #region 方法实现");
        sb.AppendLine();

        foreach (var member in TypeSymbolHelper.GetAllMethods(interfaceSymbol, excludedInterfaces: ["IOfficeObject", "IDisposable", "System.IDisposable", "System.Collections.Generic.IEnumerable"]))
        {
            if (member.MethodKind == MethodKind.Ordinary && !ShouldIgnoreMember(member))
            {
                GenerateMethod(sb, member, interfaceDeclaration);
            }
        }

        sb.AppendLine("        #endregion");
        sb.AppendLine();
    }
    #endregion

    /// <summary>
    /// 生成单个方法实现
    /// </summary>
    /// <param name="sb">字符串构建器</param>
    /// <param name="methodSymbol">方法符号</param>
    /// <param name="interfaceDeclaration">接口声明语法</param>
    private void GenerateMethod(
        StringBuilder sb,
        IMethodSymbol methodSymbol,
        InterfaceDeclarationSyntax interfaceDeclaration)
    {
        // 生成方法签名
        GenerateMethodSignature(sb, methodSymbol);

        sb.AppendLine("        {");

        // 生成方法体
        GenerateMethodBody(sb, methodSymbol, interfaceDeclaration);

        sb.AppendLine("        }");
        sb.AppendLine();
    }

    /// <summary>
    /// 生成方法签名
    /// </summary>
    private void GenerateMethodSignature(
        StringBuilder sb,
        IMethodSymbol methodSymbol)
    {
        var methodName = methodSymbol.Name;
        var returnType = TypeSymbolHelper.GetTypeFullName(methodSymbol.ReturnType);
        var parameters = new List<string>();

        foreach (var param in methodSymbol.Parameters)
        {
            var paramType = TypeSymbolHelper.GetTypeFullName(param.Type);
            var paramName = param.Name;
            var refKind = param.RefKind;

            // 检查是否有 [ConvertTriState] 特性
            var hasConvertTriState = HasConvertTriStateAttribute(param);
            if (hasConvertTriState && paramType == "bool")
            {
                // 在方法签名中使用原始类型，在方法体中处理转换
            }

            // 处理ref和out参数
            string refModifier = "";
            if (refKind == RefKind.Out)
                refModifier = "out ";
            else if (refKind == RefKind.Ref)
                refModifier = "ref ";

            // 处理可选参数的默认值（out和ref参数不能有默认值）
            if (param.HasExplicitDefaultValue && refKind == RefKind.None)
            {
                var defaultValue = GetParameterDefaultValue(param);
                parameters.Add($"{refModifier}{paramType} {paramName} = {defaultValue}");
            }
            else
            {
                parameters.Add($"{refModifier}{paramType} {paramName}");
            }
        }

        var parametersStr = string.Join(", ", parameters);

        sb.AppendLine($"        ///  <inheritdoc/>");
        sb.AppendLine($"        {GeneratedCodeAttribute}");
        sb.AppendLine($"        public {returnType} {methodName}({parametersStr})");
    }

    /// <summary>
    /// 生成方法体
    /// </summary>
    private void GenerateMethodBody(
        StringBuilder sb,
        IMethodSymbol methodSymbol,
        InterfaceDeclarationSyntax interfaceDeclaration)
    {
        var comClassName = GetComClassName(interfaceDeclaration);
        var privateFieldName = PrivateFieldNamingHelper.GeneratePrivateFieldName(comClassName);

        var hasParameters = methodSymbol.Parameters.Length > 0;

        sb.AppendLine($"            if({privateFieldName} == null)");
        sb.AppendLine($"                throw new ObjectDisposedException(nameof({privateFieldName}));");

        // 参数预处理（如有必要）
        if (hasParameters)
        {
            GenerateParameterPreprocessing(sb, methodSymbol, interfaceDeclaration);
        }

        // 异常处理和方法调用
        GenerateMethodCallWithExceptionHandling(sb, methodSymbol, interfaceDeclaration, hasParameters);
    }

    /// <summary>
    /// 生成参数预处理逻辑
    /// </summary>
    private void GenerateParameterPreprocessing(
        StringBuilder sb,
        IMethodSymbol methodSymbol,
        InterfaceDeclarationSyntax interfaceDeclaration)
    {
        foreach (var param in methodSymbol.Parameters)
        {
            var pType = TypeSymbolHelper.GetTypeFullName(param.Type);
            bool isEnumType = TypeSymbolHelper.IsEnumType(param.Type);
            bool isObjectType = TypeSymbolHelper.IsComplexObjectType(param.Type);
            bool hasConvertTriState = HasConvertTriStateAttribute(param);
            bool convertToInteger = AttributeDataHelper.HasAttribute(param, ComWrapConstants.ConvertIntAttributeNames);
            bool isOut = param.RefKind == RefKind.Out;

            var defaultValue = GetDefaultValue(interfaceDeclaration, param, param.Type);
            var comNamespace = GetComNamespace(interfaceDeclaration);

            var paramcomNamespace = AttributeDataHelper.GetStringValueFromSymbol(param, ComWrapConstants.ComNamespaceAttributes, "Name", "");
            if (!string.IsNullOrEmpty(paramcomNamespace))
                comNamespace = paramcomNamespace;

            var enumValueName = GetEnumValueWithoutNamespace(defaultValue);
            // 对于枚举类型，直接使用类型名，不需要转换为实现类型
            var constructType = isEnumType ? TypeSymbolHelper.GetTypeFullName(param.Type) : GetImplementationType(TypeSymbolHelper.GetTypeFullName(param.Type));

            // 处理out参数
            if (isOut)
            {
                GenerateOutParameterVariable(sb, param, isEnumType, isObjectType, convertToInteger, pType, comNamespace, enumValueName, constructType);
                continue;
            }

            // 生成参数对象处理逻辑
            GenerateParameterObject(sb, param, pType, isEnumType, isObjectType, hasConvertTriState, convertToInteger, comNamespace, enumValueName, constructType);
        }
        sb.AppendLine();
    }

    /// <summary>
    /// 生成带有异常处理的方法调用
    /// </summary>
    private void GenerateMethodCallWithExceptionHandling(
        StringBuilder sb,
        IMethodSymbol methodSymbol,
        InterfaceDeclarationSyntax interfaceDeclaration,
        bool hasParameters)
    {
        var methodName = AttributeDataHelper.GetStringValueFromSymbol(methodSymbol, ComWrapConstants.MethodNameAttributes, "Name", "");
        if (string.IsNullOrEmpty(methodName))
            methodName = methodSymbol.Name;
        var isObjectType = TypeSymbolHelper.IsComplexObjectType(methodSymbol.ReturnType);
        var comClassName = GetComClassName(interfaceDeclaration);
        var comNamespace = GetComNamespace(interfaceDeclaration);
        var privateFieldName = PrivateFieldNamingHelper.GeneratePrivateFieldName(comClassName);

        var isIndexMethod = AttributeDataHelper.HasAttribute(methodSymbol, ComWrapConstants.MethodIndexAttributes);
        var needConvert = AttributeDataHelper.HasAttribute(methodSymbol, ComWrapConstants.ValueConvertAttributes);
        string returnType = TypeSymbolHelper.GetTypeFullName(methodSymbol.ReturnType);
        var isEnunType = TypeSymbolHelper.IsEnumType(methodSymbol.ReturnType);
        var defaultValue = GetDefaultValue(interfaceDeclaration, methodSymbol, methodSymbol.ReturnType);
        sb.AppendLine("            try");
        sb.AppendLine("            {");

        // 生成方法调用参数
        var callParameters = GenerateCallParameters(methodSymbol);

        if (returnType == "void")
        {
            if (isIndexMethod)
                sb.AppendLine($"                {privateFieldName}?.{methodName}[{callParameters}];");
            else
                sb.AppendLine($"                {privateFieldName}?.{methodName}({callParameters});");

            // 处理out参数的返回值赋值
            GenerateOutParameterAssignment(sb, methodSymbol, interfaceDeclaration);
        }
        else if (isObjectType)
        {
            var objectType = StringExtensions.RemoveInterfacePrefix(returnType);
            var constructType = GetImplementationType(objectType);
            if (isIndexMethod)
                sb.AppendLine($"                var comObj = {privateFieldName}?.{methodName}[{callParameters}];");
            else
                sb.AppendLine($"                var comObj = {privateFieldName}?.{methodName}({callParameters});");
            sb.AppendLine("                if (comObj == null)");
            if (returnType.EndsWith("?", StringComparison.Ordinal))
                sb.AppendLine("                    return null;");
            else
                sb.AppendLine("                    return null;");

            if (needConvert)
            {
                var ordinalComType = GetImplementationOrdinalType(returnType);
                var comType = GetOrdinalComType(ordinalComType);
                sb.AppendLine($"                if(comObj is {comNamespace}.{comType} rComObj)");
                sb.AppendLine($"                     return new {constructType}(rComObj);");
                sb.AppendLine($"                else");
                sb.AppendLine("                     return null;");
            }
            else
            {
                sb.AppendLine($"                return new {constructType}(comObj);");
            }
        }
        else
        {
            if (isIndexMethod)
                sb.AppendLine($"                var returnValue = {privateFieldName}?.{methodName}[{callParameters}];");
            else
                sb.AppendLine($"                var returnValue = {privateFieldName}?.{methodName}({callParameters});");
            // 处理out参数的返回值赋值
            GenerateOutParameterAssignment(sb, methodSymbol, interfaceDeclaration);

            if (isEnunType)
            {
                sb.AppendLine($"                return returnValue.EnumConvert({defaultValue});");
            }
            else
            {
                if (needConvert)
                {
                    string convertCode = GetConvertCodeForType(methodSymbol.ReturnType, "returnValue");
                    sb.AppendLine($"                return {convertCode};");
                }
                else
                {
                    sb.AppendLine("                return returnValue;");
                }
            }
        }

        sb.AppendLine("            }");

        // 异常处理
        GenerateExceptionHandling(sb, methodName);
    }

    protected string GetOrdinalComType(string ordinalComType)
    {
        if (string.IsNullOrEmpty(ordinalComType))
            return ordinalComType;
        foreach (var preFix in KnownPrefixes)
        {
            if (ordinalComType.StartsWith(preFix, StringComparison.Ordinal))
            {
                ordinalComType = ordinalComType.Substring(preFix.Length).TrimEnd('?');
                break;
            }
        }
        return ordinalComType;
    }

    /// <summary>
    /// 生成方法调用参数
    /// </summary>
    private string GenerateCallParameters(IMethodSymbol methodSymbol)
    {
        var parameters = new List<string>();

        foreach (var param in methodSymbol.Parameters)
        {
            var pType = TypeSymbolHelper.GetTypeFullName(param.Type);
            bool isEnumType = TypeSymbolHelper.IsEnumType(param.Type);
            bool isObjectType = TypeSymbolHelper.IsComplexObjectType(param.Type);
            bool hasConvertTriState = HasConvertTriStateAttribute(param);
            bool isOut = param.RefKind == RefKind.Out;

            if (isOut)
            {
                // out参数需要添加out关键字
                parameters.Add($"out {param.Name}Obj");
            }
            else if (pType.EndsWith("?", StringComparison.Ordinal) || isEnumType || isObjectType || hasConvertTriState || pType == "object")
            {
                parameters.Add($"{param.Name}Obj");
            }
            else
            {
                parameters.Add(param.Name);
            }
        }

        return string.Join(", ", parameters);
    }

    /// <summary>
    /// 生成out参数的返回值赋值
    /// </summary>
    private void GenerateOutParameterAssignment(StringBuilder sb, IMethodSymbol methodSymbol, InterfaceDeclarationSyntax interfaceDeclaration)
    {
        var comNamespace = GetComNamespace(interfaceDeclaration);

        foreach (var param in methodSymbol.Parameters)
        {
            if (param.RefKind != RefKind.Out)
                continue;

            var pType = TypeSymbolHelper.GetTypeFullName(param.Type);
            bool isEnumType = TypeSymbolHelper.IsEnumType(param.Type);
            bool isObjectType = TypeSymbolHelper.IsComplexObjectType(param.Type);
            bool convertToInteger = AttributeDataHelper.HasAttribute(param, ComWrapConstants.ConvertIntAttributeNames);

            if (isEnumType)
            {
                if (convertToInteger)
                {
                    // 枚举转整数的out参数
                    sb.AppendLine($"                {param.Name} = ({pType}){param.Name}Obj;");
                }
                else
                {
                    // 普通枚举out参数
                    var defaultValue = GetDefaultValue(interfaceDeclaration, param, param.Type);
                    var enumValueName = GetEnumValueWithoutNamespace(defaultValue);
                    sb.AppendLine($"                {param.Name} = {param.Name}Obj.EnumConvert({defaultValue});");
                }
            }
            else if (isObjectType)
            {
                // COM对象out参数
                var constructType = GetImplementationType(param.Type.Name);
                sb.AppendLine($"                {param.Name} = new {constructType}({param.Name}Obj);");
            }
            else
            {
                // 普通out参数
                sb.AppendLine($"                {param.Name} = {param.Name}Obj;");
            }
        }
    }

    /// <summary>
    /// 生成异常处理逻辑
    /// </summary>
    private void GenerateExceptionHandling(StringBuilder sb, string methodName)
    {
        var operationDescription = GetOperationDescription(methodName);

        sb.AppendLine("            catch (COMException cx)");
        sb.AppendLine("            {");
        sb.AppendLine($"                throw new InvalidOperationException(\"{operationDescription}失败。\", cx);");
        sb.AppendLine("            }");
        sb.AppendLine("            catch (Exception ex)");
        sb.AppendLine("            {");
        sb.AppendLine($"                throw new ExcelOperationException(\"{operationDescription}失败\", ex);");
        sb.AppendLine("            }");
    }

    /// <summary>
    /// 获取操作描述
    /// </summary>
    private string GetOperationDescription(string methodName)
    {
        // 根据方法名生成更友好的操作描述
        return methodName switch
        {
            "Add" => "添加对象操作",
            "Remove" => "移除对象操作",
            "Delete" => "删除对象操作",
            "Update" => "更新对象操作",
            "Insert" => "插入对象操作",
            "Copy" => "复制对象操作",
            "Paste" => "粘贴对象操作",
            "Select" => "选择对象",
            _ => $"执行{methodName}操作"
        };
    }

    #region Parameter Processing
    /// <summary>
    /// 检查参数是否有 [ConvertTriState] 特性
    /// </summary>
    private bool HasConvertTriStateAttribute(IParameterSymbol parameter)
    {
        return parameter.GetAttributes().Any(attr =>
            attr.AttributeClass?.Name == "ConvertTriStateAttribute");
    }

    /// <summary>
    /// 获取参数的默认值
    /// </summary>
    private string GetParameterDefaultValue(IParameterSymbol parameter)
    {
        if (!parameter.HasExplicitDefaultValue)
            return string.Empty;

        var value = parameter.ExplicitDefaultValue;
        if (value == null)
            return "null";

        var type = TypeSymbolHelper.GetTypeFullName(parameter.Type);
        var typeSymbol = parameter.Type;

        // 处理枚举类型
        if (typeSymbol.TypeKind == TypeKind.Enum)
        {
            return GetEnumDefaultValue(typeSymbol, value);
        }

        // 处理可空枚举类型
        if (type.EndsWith("?", StringComparison.Ordinal) && TypeSymbolHelper.IsEnumType(parameter.Type))
        {
            if (value != null)
            {
                // 获取非可空枚举类型符号
                if (parameter.Type is INamedTypeSymbol namedType &&
                    namedType.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T &&
                    namedType.TypeArguments.Length > 0)
                {
                    var enumType = namedType.TypeArguments[0];
                    return GetEnumDefaultValue(enumType, value);
                }
            }
            else
            {
                return "null";
            }
        }

        return type switch
        {
            "bool" => value.ToString().ToLower(),
            "int" or "float" or "double" or "decimal" => value.ToString(),
            "string" => $"\"{value}\"",
            _ when type.EndsWith("?", StringComparison.Ordinal) => HandleNullableDefaultValue(type, value),
            _ => value.ToString()
        };
    }

    /// <summary>
    /// 处理可空类型的默认值
    /// </summary>
    private static string HandleNullableDefaultValue(string type, object value)
    {
        if (value == null)
            return "null";

        var nonNullType = type.TrimEnd('?');
        return nonNullType switch
        {
            "bool" => value.ToString().ToLower(),
            "int" or "float" or "double" or "decimal" => value.ToString(),
            "string" => $"\"{value}\"",
            _ => value.ToString()
        };
    }

    /// <summary>
    /// 获取枚举类型的默认值表示
    /// </summary>
    private string GetEnumDefaultValue(ITypeSymbol enumType, object value)
    {
        var enumTypeName = enumType.ToDisplayString();

        // 尝试根据值找到对应的枚举成员
        foreach (var member in enumType.GetMembers().OfType<IFieldSymbol>())
        {
            if (member.HasConstantValue && Equals(member.ConstantValue, value))
            {
                return $"{enumTypeName}.{member.Name}";
            }
        }

        // 如果找不到对应的枚举成员，使用强制转换
        return $"({enumTypeName}){value}";
    }
    #endregion

    #region Helper Methods
    /// <summary>
    /// 是否需要需要生成构造函数。
    /// </summary>
    /// <param name="interfaceSymbol"></param>
    /// <returns></returns>
    protected bool NoneConstructor(INamedTypeSymbol interfaceSymbol)
    {
        List<string> attributes = [.. ComWrapConstants.ComObjectWrapAttributeNames];
        attributes.AddRange(ComWrapConstants.ComCollectionWrapAttributeNames);
        var propertyWrapAttr = AttributeDataHelper.GetAttributeDataFromSymbol(interfaceSymbol, [.. attributes]);
        if (propertyWrapAttr == null)
            return false;

        var bValue = AttributeDataHelper.GetBoolValueFromAttribute(propertyWrapAttr, ComWrapConstants.NoneConstructorProperty, false);
        return bValue;
    }

    /// <summary>
    /// 是否需要需要生成释放资源函数。
    /// </summary>
    /// <param name="interfaceSymbol"></param>
    /// <returns></returns>
    protected bool NoneDisposed(INamedTypeSymbol interfaceSymbol)
    {
        List<string> attributes = [.. ComWrapConstants.ComObjectWrapAttributeNames];
        attributes.AddRange(ComWrapConstants.ComCollectionWrapAttributeNames);
        var propertyWrapAttr = AttributeDataHelper.GetAttributeDataFromSymbol(interfaceSymbol, [.. attributes]);
        if (propertyWrapAttr == null)
            return false;

        var bValue = AttributeDataHelper.GetBoolValueFromAttribute(propertyWrapAttr, ComWrapConstants.NoneDisposedProperty, false);
        return bValue;
    }

    /// <summary>
    /// 获取属性所在的COM命名空间。
    /// </summary>
    /// <param name="propertySymbol"></param>
    /// <returns></returns>
    protected string GetPropertyComNamespace(IPropertySymbol propertySymbol)
    {
        var propertyWrapAttr = AttributeDataHelper.GetAttributeDataFromSymbol(propertySymbol, ComWrapConstants.ComPropertyWrapAttributeNames);
        if (propertyWrapAttr == null)
            return string.Empty;

        var nameSpace = AttributeDataHelper.GetStringValueFromAttribute(propertyWrapAttr, ComWrapConstants.ComNamespaceProperty, string.Empty);
        return nameSpace;
    }

    protected string GetPropertyName(IPropertySymbol propertySymbol)
    {
        if (propertySymbol == null)
            return string.Empty;
        var propertyWrapAttr = AttributeDataHelper.GetAttributeDataFromSymbol(propertySymbol, ComWrapConstants.ComPropertyWrapAttributeNames);
        if (propertyWrapAttr == null)
            return propertySymbol.Name;

        var propertyName = AttributeDataHelper.GetStringValueFromAttribute(propertyWrapAttr, ComWrapConstants.PropertyNameProperty, string.Empty);
        if (string.IsNullOrEmpty(propertyName))
            return propertySymbol.Name;
        return propertyName;
    }

    /// <summary>
    /// 是否需要转换。
    /// </summary>
    /// <param name="propertySymbol"></param>
    /// <returns></returns>
    protected bool IsNeedConvert(IPropertySymbol propertySymbol)
    {
        var propertyWrapAttr = AttributeDataHelper.GetAttributeDataFromSymbol(propertySymbol, ComWrapConstants.ComPropertyWrapAttributeNames);
        if (propertyWrapAttr == null)
            return false;

        var needConvert = AttributeDataHelper.GetBoolValueFromAttribute(propertyWrapAttr, ComWrapConstants.NeedConvertProperty, false);
        return needConvert;
    }

    /// <summary>
    /// 是否需要释放资源
    /// </summary>
    /// <param name="propertySymbol"></param>
    /// <returns></returns>
    protected bool IsNeeDispose(IPropertySymbol propertySymbol)
    {
        var propertyData = AttributeDataHelper.GetAttributeDataFromSymbol(propertySymbol, ComWrapConstants.ComPropertyWrapAttributeNames);
        if (propertyData == null)
            return true;
        var needDispose = AttributeDataHelper.GetBoolValueFromAttribute(propertyData, ComWrapConstants.NeedDisposeProperty, true);
        return needDispose;
    }

    /// <summary>
    /// 属性是否采用get、set方法进行访问。
    /// </summary>
    /// <param name="propertySymbol"></param>
    /// <returns></returns>
    protected bool IsMethod(IPropertySymbol propertySymbol)
    {
        var propertyData = AttributeDataHelper.GetAttributeDataFromSymbol(propertySymbol, ComWrapConstants.ComPropertyWrapAttributeNames);
        if (propertyData == null)
            return false;
        var isMethod = AttributeDataHelper.GetBoolValueFromAttribute(propertyData, ComWrapConstants.IsMethodProperty, false);
        return isMethod;
    }



    /// <summary>
    /// 检查成员是否应该被忽略
    /// </summary>
    /// <param name="member">成员符号</param>
    /// <returns>如果应该忽略返回true，否则返回false</returns>
    protected bool ShouldIgnoreMember(ISymbol member)
    {
        if (member == null)
            return false;
        return member.GetAttributes().Any(attr => attr.AttributeClass?.Name == ComWrapConstants.IgnoreGeneratorAttribute);
    }

    /// <summary>
    /// 获取属性的默认值
    /// </summary>
    /// <param name="interfaceDeclaration">接口声明语法</param>
    /// <param name="propertySymbol">属性符号</param>
    /// <param name="typeSymbol">类型符号</param>
    /// <returns>默认值字符串</returns>
    protected string GetDefaultValue(InterfaceDeclarationSyntax interfaceDeclaration, ISymbol propertySymbol, ITypeSymbol typeSymbol)
    {
        if (typeSymbol == null || interfaceDeclaration == null)
            return "default";

        // 首先检查特性中显式指定的默认值
        var propertyDeclaration = interfaceDeclaration.DescendantNodes()
            .OfType<PropertyDeclarationSyntax>()
            .FirstOrDefault(p => p.Identifier.Text == propertySymbol.Name);

        if (propertyDeclaration != null)
        {
            var defaultValueArgument = propertyDeclaration.AttributeLists
                .SelectMany(al => al.Attributes)
                .Where(attr => ComWrapConstants.ComPropertyWrapAttributeNames.Contains(attr.Name.ToString()))
                .SelectMany(attr => attr.ArgumentList?.Arguments ?? Enumerable.Empty<AttributeArgumentSyntax>())
                .FirstOrDefault(arg =>
                    arg.NameEquals?.Name.Identifier.Text == "DefaultValue" ||
                    arg.Expression.ToString().Contains("DefaultValue"));

            if (defaultValueArgument != null)
            {
                // 移除引号
                var defaultValue = defaultValueArgument.Expression.ToString();
                // 处理 nameof() 表达式
                if (defaultValue.StartsWith("nameof(", StringComparison.OrdinalIgnoreCase) && defaultValue.EndsWith(")", StringComparison.OrdinalIgnoreCase))
                {
                    // 提取 nameof() 中的参数，例如从 "nameof(Field)" 中提取 "Field"
                    var nameofContent = defaultValue.Substring(7, defaultValue.Length - 8);
                    return nameofContent.Trim();
                }

                // 处理字符串字面量，移除引号
                return defaultValue.Trim('"');
            }
        }

        // 如果没有显式指定，基于类型推断合理的默认值
        return InferDefaultValue(typeSymbol);
    }

    /// <summary>
    /// 基于类型推断合理的默认值
    /// </summary>
    /// <param name="typeSymbol">类型符号</param>
    /// <returns>推断的默认值字符串</returns>
    private string InferDefaultValue(ITypeSymbol typeSymbol)
    {
        // 处理可空类型
        if (typeSymbol is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullableType)
        {
            var underlyingType = nullableType.TypeArguments[0];

            // 如果是可空枚举类型，返回 null 或带有 ? 的枚举值
            if (underlyingType.TypeKind == TypeKind.Enum)
            {
                var firstEnumValue = underlyingType.GetMembers()
                    .OfType<IFieldSymbol>()
                    .FirstOrDefault(f => f.HasConstantValue);
                if (firstEnumValue != null)
                {
                    var enumTypeName = underlyingType.ToDisplayString();
                    return $"{enumTypeName}.{firstEnumValue.Name}";
                }
            }

            // 其他可空类型返回 null
            return "null";
        }

        // 枚举类型：使用第一个枚举值
        if (typeSymbol.TypeKind == TypeKind.Enum)
        {
            var firstEnumValue = typeSymbol.GetMembers()
                .OfType<IFieldSymbol>()
                .FirstOrDefault(f => f.HasConstantValue);
            if (firstEnumValue != null)
            {
                var enumTypeName = typeSymbol.ToDisplayString();
                return $"{enumTypeName}.{firstEnumValue.Name}";
            }
            return "default";
        }

        // 基础类型：提供合理的默认值
        var specialType = typeSymbol.SpecialType;
        return specialType switch
        {
            SpecialType.System_Boolean => "false",
            SpecialType.System_Char => "'\\0'",
            SpecialType.System_SByte => "0",
            SpecialType.System_Byte => "0",
            SpecialType.System_Int16 => "0",
            SpecialType.System_UInt16 => "0",
            SpecialType.System_Int32 => "0",
            SpecialType.System_UInt32 => "0",
            SpecialType.System_Int64 => "0L",
            SpecialType.System_UInt64 => "0UL",
            SpecialType.System_Single => "0.0f",
            SpecialType.System_Double => "0.0",
            SpecialType.System_Decimal => "0.0m",
            SpecialType.System_String => "string.Empty",
            SpecialType.System_DateTime => "DateTime.MinValue",
            _ => "default"
        };
    }

    #region GetConvertCode
    protected string GetConvertCode(IPropertySymbol typeSymbol, string fieldName)
    {
        // 检查是否为可空类型
        if (typeSymbol.Type is INamedTypeSymbol namedType &&
            namedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
        {
            // 获取可空类型的基础类型
            var underlyingType = namedType.TypeArguments[0];

            // 为可空类型生成带有空值检查的转换代码
            return GetConvertCodeForType(underlyingType, fieldName);
        }

        // 对于非可空类型，直接获取转换代码
        return GetConvertCodeForType(typeSymbol.Type, fieldName);
    }

    protected string GetConvertCodeForType(ITypeSymbol typeSymbol, string fieldName)
    {
        if (typeSymbol == null) return string.Empty;

        // 获取实际类型（如果是可空类型，则获取其内部类型）
        var actualType = typeSymbol;
        bool isNullable = false;

        // 检查是否为可空值类型 (Nullable<T>)
        if (typeSymbol is INamedTypeSymbol namedType &&
            namedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
        {
            actualType = namedType.TypeArguments[0];
            isNullable = true;
        }

        // 检查是否为可空引用类型（C# 8.0+的可空引用类型）
        if (typeSymbol.NullableAnnotation == NullableAnnotation.Annotated &&
            typeSymbol.IsReferenceType)
        {
            isNullable = true;
        }

        // 对于可空类型，我们需要进行null检查
        if (isNullable)
        {
            return GenerateNullableConvertCode(actualType, fieldName);
        }

        // 对于非空类型，使用原有的转换逻辑
        return GenerateNonNullableConvertCode(typeSymbol, fieldName);
    }

    private string GenerateNullableConvertCode(ITypeSymbol actualType, string fieldName)
    {
        // 对于可空类型，我们生成带null检查的转换代码
        var conversionCode = GenerateNonNullableConvertCode(actualType, $"{fieldName}!");

        // 生成带null检查的转换代码
        // 使用条件运算符：如果fieldName不为null，则进行转换，否则返回null/default
        if (actualType.IsValueType)
        {
            // 对于值类型，返回可空类型
            return $"{fieldName} != null ? {conversionCode} : null";
        }
        else
        {
            // 对于引用类型，直接返回null
            return $"{fieldName} != null ? {conversionCode} : null";
        }
    }

    private string GenerateNonNullableConvertCode(ITypeSymbol typeSymbol, string fieldName)
    {
        return typeSymbol switch
        {
            ITypeSymbol ts when IsSystemDrawingColor(ts) => $"ColorHelper.ConvertToColor({fieldName})",
            { SpecialType: SpecialType.System_Boolean } => $"{fieldName}.ConvertToBool()",
            { SpecialType: SpecialType.System_SByte } => $"{fieldName}.Convert.ToByte()",
            { SpecialType: SpecialType.System_Byte } => $"{fieldName}.Convert.ToByte()",
            { SpecialType: SpecialType.System_Int16 } => $"{fieldName}.ConvertToFloat()",
            { SpecialType: SpecialType.System_UInt16 } => $"{fieldName}.ConvertToFloat()",
            { SpecialType: SpecialType.System_Int32 } => $"{fieldName}.ConvertToInt()",
            { SpecialType: SpecialType.System_UInt32 } => $"{fieldName}.ConvertToInt()",
            { SpecialType: SpecialType.System_Int64 } => $"{fieldName}.ConvertToLong()",
            { SpecialType: SpecialType.System_UInt64 } => $"{fieldName}.ConvertToLong()",
            { SpecialType: SpecialType.System_Single } => $"{fieldName}.ConvertToFloat()",
            { SpecialType: SpecialType.System_Double } => $"{fieldName}.ConvertToDouble()",
            { SpecialType: SpecialType.System_Decimal } => $"{fieldName}.ConvertToDecimal()",
            { SpecialType: SpecialType.System_String } => $"{fieldName}.ToString()",
            { SpecialType: SpecialType.System_DateTime } => $"{fieldName}.ConvertToDateTime()",
            _ => $"{fieldName}.ToString()"
        };
    }

    private string GetTypeName(ITypeSymbol typeSymbol)
    {
        return typeSymbol.SpecialType switch
        {
            SpecialType.System_Boolean => "bool",
            SpecialType.System_SByte => "sbyte",
            SpecialType.System_Byte => "byte",
            SpecialType.System_Int16 => "short",
            SpecialType.System_UInt16 => "ushort",
            SpecialType.System_Int32 => "int",
            SpecialType.System_UInt32 => "uint",
            SpecialType.System_Int64 => "long",
            SpecialType.System_UInt64 => "ulong",
            SpecialType.System_Single => "float",
            SpecialType.System_Double => "double",
            SpecialType.System_Decimal => "decimal",
            SpecialType.System_String => "string",
            SpecialType.System_DateTime => "DateTime",
            _ => typeSymbol.Name
        };
    }

    private bool IsSystemDrawingColor(ITypeSymbol typeSymbol)
    {
        if (typeSymbol == null)
            return false;

        // 更健壮的检查方式，包括命名空间和类型名称
        return typeSymbol.ContainingNamespace?.ToDisplayString() == "System.Drawing" &&
               typeSymbol.Name == "Color";
    }
    #endregion

    /// <summary>
    /// 从ComObjectWrap特性中获取COM命名空间
    /// </summary>
    /// <param name="interfaceDeclaration">接口声明语法</param>
    /// <returns>COM命名空间</returns>
    protected string GetComNamespace(InterfaceDeclarationSyntax interfaceDeclaration)
    {
        if (interfaceDeclaration == null)
            return ComWrapConstants.DefaultComNamespace;

        var comObjectWrapAttribute = interfaceDeclaration.AttributeLists
            .SelectMany(al => al.Attributes)
            .FirstOrDefault(attr => ComWrapAttributeNames().Contains(attr.Name.ToString()));

        if (comObjectWrapAttribute != null)
        {
            var namespaceArgument = comObjectWrapAttribute.ArgumentList?.Arguments
                .FirstOrDefault(arg =>
                    arg.NameEquals?.Name.Identifier.Text == ComWrapConstants.ComNamespaceProperty ||
                    arg.Expression.ToString().Contains(ComWrapConstants.ComNamespaceProperty));

            if (namespaceArgument != null)
            {
                // 移除引号
                var namespaceValue = namespaceArgument.Expression.ToString();
                return namespaceValue.Trim('"');
            }
        }

        return ComWrapConstants.DefaultComNamespace;
    }

    /// <summary>
    /// 从ComObjectWrap特性中获取COM类名
    /// </summary>
    /// <param name="interfaceDeclaration">接口声明语法</param>
    /// <returns>COM类名</returns>
    /// <remarks>
    /// 支持字符串字面量和nameof()表达式两种形式
    /// </remarks>
    protected string GetComClassName(InterfaceDeclarationSyntax interfaceDeclaration)
    {
        if (interfaceDeclaration == null)
            return ComWrapConstants.DefaultComClassName;

        var comObjectWrapAttribute = interfaceDeclaration.AttributeLists
            .SelectMany(al => al.Attributes)
            .FirstOrDefault(attr => ComWrapAttributeNames().Contains(attr.Name.ToString()));

        if (comObjectWrapAttribute != null)
        {
            var classNameArgument = comObjectWrapAttribute.ArgumentList?.Arguments
                .FirstOrDefault(arg =>
                    arg.NameEquals?.Name.Identifier.Text == "ComClassName" ||
                    arg.Expression.ToString().Contains("ComClassName"));

            if (classNameArgument != null)
            {
                var classNameValue = classNameArgument.Expression.ToString();

                if (classNameValue.StartsWith("nameof(", StringComparison.OrdinalIgnoreCase) && classNameValue.EndsWith(")", StringComparison.OrdinalIgnoreCase))
                {
                    var nameofContent = classNameValue.Substring(7, classNameValue.Length - 8);
                    return nameofContent.Trim();
                }

                // 处理字符串字面量，移除引号
                return classNameValue.Trim('"');
            }
        }

        return GetDefaultComClassName(interfaceDeclaration);
    }

    private string GetDefaultComClassName(InterfaceDeclarationSyntax interfaceDeclaration)
    {
        var interfaceName = interfaceDeclaration.Identifier.Text;

        // 尝试匹配已知前缀
        foreach (var prefix in KnownPrefixes)
        {
            if (interfaceName.StartsWith(prefix, StringComparison.Ordinal))
            {
                return interfaceName.Substring(prefix.Length);
            }
        }

        // 默认情况：去掉前导 "I"（如果符合命名规范），否则加 "Com"
        return interfaceName.StartsWith("I", StringComparison.Ordinal)
               && interfaceName.Length > 1
               && char.IsUpper(interfaceName[1])
            ? interfaceName.Substring(1)
            : interfaceName + "Com";
    }

    /// <summary>
    /// 生成IDisposable接口实现
    /// </summary>
    /// <param name="sb">字符串构建器</param>
    /// <param name="interfaceDeclaration">接口声明语法</param>
    /// <param name="interfaceSymbol">接口符号</param>
    /// <param name="includeAdditionalDisposables">是否包含额外的可释放对象处理</param>
    protected void GenerateIDisposableImplementation(StringBuilder sb, InterfaceDeclarationSyntax interfaceDeclaration, INamedTypeSymbol interfaceSymbol)
    {
        if (interfaceDeclaration == null || interfaceSymbol == null || sb == null)
            return;

        var comClassName = GetComClassName(interfaceDeclaration);
        var impClassName = TypeSymbolHelper.GetImplementationClassName(interfaceSymbol.Name);
        var privateFieldName = PrivateFieldNamingHelper.GeneratePrivateFieldName(comClassName);


        sb.AppendLine("        #region IDisposable 实现");
        if (!NoneDisposed(interfaceSymbol))
        {
            sb.AppendLine($"        {GeneratedCodeAttribute}");
            sb.AppendLine("        protected virtual void Dispose(bool disposing)");
            sb.AppendLine("        {");
            sb.AppendLine("            if (_disposedValue) return;");
            sb.AppendLine();
            sb.AppendLine($"            if (disposing)");
            sb.AppendLine("            {");
            sb.AppendLine($"                if({privateFieldName} != null)");
            sb.AppendLine("                {");
            sb.AppendLine($"                    Marshal.ReleaseComObject({privateFieldName});");
            sb.AppendLine($"                    {privateFieldName} = null;");
            sb.AppendLine("                }");
            sb.AppendLine("                _disposableList.Dispose();");
            GeneratePrivateFieldDisposable(sb, interfaceSymbol, interfaceDeclaration);
            sb.AppendLine("            }");
            sb.AppendLine();

            sb.AppendLine("            _disposedValue = true;");
            sb.AppendLine("        }");
        }
        sb.AppendLine();
        sb.AppendLine($"        ///  <inheritdoc/>");
        sb.AppendLine($"        {GeneratedCodeAttribute}");
        sb.AppendLine("        public void Dispose()");
        sb.AppendLine("        {");
        sb.AppendLine("            Dispose(true);");
        sb.AppendLine("            GC.SuppressFinalize(this);");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine($"        ~{impClassName}()");
        sb.AppendLine("        {");
        sb.AppendLine("            Dispose(true);");
        sb.AppendLine("        }");
        sb.AppendLine("        #endregion");
        sb.AppendLine();
    }


    private void GeneratePrivateFieldDisposable(StringBuilder sb, INamedTypeSymbol interfaceSymbol, InterfaceDeclarationSyntax interfaceDeclaration)
    {
        if (interfaceSymbol == null || interfaceDeclaration == null || sb == null)
            return;


        foreach (var member in TypeSymbolHelper.GetAllProperties(interfaceSymbol))
        {
            if (member.IsIndexer)
                continue;

            var needDispose = IsNeeDispose(member);
            if (!needDispose)
                continue;
            var propertyName = member.Name;
            var isObjectType = TypeSymbolHelper.IsComplexObjectType(member.Type);
            if (!isObjectType)
                continue;

            var impType = member.Type.Name.Trim('?');

            var fieldName = PrivateFieldNamingHelper.GeneratePrivateFieldName(impType, FieldNamingStyle.UnderscoreCamel);
            sb.AppendLine($"                {fieldName}_{propertyName}?.Dispose();");
            sb.AppendLine($"                {fieldName}_{propertyName} = null;");
        }
    }
    #endregion


    /// <summary>
    /// 从枚举值字符串中提取不带命名空间的枚举值名称
    /// </summary>
    /// <param name="enumValue">可能包含命名空间的枚举值字符串</param>
    /// <returns>去掉命名空间的枚举值名称</returns>
    protected static string GetEnumValueWithoutNamespace(string enumValue)
    {
        if (string.IsNullOrEmpty(enumValue))
            return enumValue;

        var strs = enumValue.Split(['.'], StringSplitOptions.RemoveEmptyEntries);
        if (strs.Length > 2)
        {
            return strs[strs.Length - 2] + "." + strs[strs.Length - 1];
        }
        return enumValue;
    }

    protected string GetImplementationType(string elementType)
    {
        if (TypeSymbolHelper.IsBasicType(elementType))
            return elementType;

        // 移除可空标记
        var elementImplType = elementType.TrimEnd('?');

        // 分割命名空间
        var types = elementImplType.Split(['.'], StringSplitOptions.RemoveEmptyEntries);

        if (types.Length == 0)
            return elementType;

        // 处理最后一个类型名（接口名）
        string lastName = types[types.Length - 1];

        // 移除接口前缀 "I"，但确保不是全大写的情况（如"ID"）
        if (lastName.Length > 1 && lastName.StartsWith("I", StringComparison.Ordinal) && char.IsUpper(lastName[1]))
        {
            lastName = lastName.Substring(1);
        }

        // 重建类型名
        var result = new StringBuilder();

        // 添加命名空间部分（除了最后一个）
        for (int i = 0; i < types.Length - 1; i++)
        {
            result.Append(types[i]);
            result.Append('.');
        }

        // 添加 "Imps." 和实现类名
        result.Append("Imps.");
        result.Append(lastName);

        return result.ToString();
    }

    protected string GetImplementationOrdinalType(string elementType)
    {
        if (TypeSymbolHelper.IsBasicType(elementType) || string.IsNullOrEmpty(elementType))
            return elementType;
        var types = elementType.Split(['.'], StringSplitOptions.RemoveEmptyEntries);
        if (types.Length > 1)
            return types[types.Length - 1];
        return types[0];
    }
    #endregion
}
