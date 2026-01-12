// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.CodeGenerator 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

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
        var elementType = TypeSymbolHelper.GetTypeFullName(indexerSymbol.Type);
        var comClassName = GetComClassName(interfaceSymbol, interfaceDeclaration);
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
        GenerateIndexerGetBody(sb, indexerSymbol, interfaceDeclaration, interfaceSymbol, isItemIndex,
            elementImplType, privateFieldName, new[] { parameter }, new[] { parameterName });

        if (indexerSymbol.SetMethod != null)
        {
            GenerateIndexerSetBody(sb, indexerSymbol, interfaceDeclaration, interfaceSymbol, isItemIndex,
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
        InterfaceDeclarationSyntax interfaceDeclaration, INamedTypeSymbol interfaceSymbol, bool isItemIndex, string elementImplType,
        string privateFieldName, IParameterSymbol[] parameters, string[] parameterNames)
    {
        sb.AppendLine("            get");
        sb.AppendLine("            {");

        // 生成参数验证和预处理
        var processedParameters = GenerateParameterValidationAndProcessing(sb, interfaceDeclaration, interfaceSymbol,
            parameters, parameterNames, privateFieldName, isGetMethod: true);

        // 生成获取逻辑
        if (parameters.Length == 1)
        {
            GenerateSingleParameterGetLogic(sb, indexerSymbol, interfaceDeclaration, interfaceSymbol, isItemIndex,
                elementImplType, privateFieldName, processedParameters[0]);
        }
        else if (parameters.Length == 2)
        {
            CommonTwoParameterGetLogic(sb, indexerSymbol, interfaceDeclaration, interfaceSymbol, isItemIndex,
                elementImplType, privateFieldName, processedParameters[0], processedParameters[1]);
        }

        sb.AppendLine("            }");
    }

    /// <summary>
    /// 生成索引器set方法体
    /// </summary>
    private void GenerateIndexerSetBody(StringBuilder sb, IPropertySymbol indexerSymbol,
        InterfaceDeclarationSyntax interfaceDeclaration, INamedTypeSymbol interfaceSymbol, bool isItemIndex, string elementImplType,
        string privateFieldName, IParameterSymbol[] parameters, string[] parameterNames)
    {
        sb.AppendLine("            set");
        sb.AppendLine("            {");

        // 生成参数验证和预处理
        var processedParameters = GenerateParameterValidationAndProcessing(sb, interfaceDeclaration, interfaceSymbol,
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
        GenerateIndexerGetBody(sb, indexerSymbol, interfaceDeclaration, interfaceSymbol, isItemIndex,
            elementImplType, privateFieldName, new[] { param1, param2 }, new[] { param1Name, param2Name });

        if (indexerSymbol.SetMethod != null)
        {
            GenerateIndexerSetBody(sb, indexerSymbol, interfaceDeclaration, interfaceSymbol, isItemIndex,
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
        InterfaceDeclarationSyntax interfaceDeclaration, INamedTypeSymbol interfaceSymbol, IParameterSymbol[] parameters,
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
            GenerateDisposedCheckWithIndent(sb, privateFieldName, "                ");

            // 参数类型特定验证
            GenerateParameterTypeValidation(sb, paramType, paramName, paramEnumType, i + 1, param.Type.NullableAnnotation == NullableAnnotation.Annotated);

            // 处理参数转换
            processedParameters[i] = ProcessParameter(sb, interfaceDeclaration, interfaceSymbol, param, paramName, paramEnumType);
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
        INamedTypeSymbol interfaceSymbol,
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
                var sourceEnumType = TypeSymbolHelper.GetTypeFullName(param.Type);
                var comNamespace = GetComNamespace(interfaceSymbol, interfaceDeclaration);
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
        InterfaceDeclarationSyntax interfaceDeclaration, INamedTypeSymbol interfaceSymbol, bool isItemIndex, string elementImplType,
        string privateFieldName, string processedParameter)
    {
        var operationType = processedParameter.Contains("index") ? "根据索引" : "根据字段名称";
        CommonGetLogic(sb, indexerSymbol, interfaceDeclaration, interfaceSymbol, isItemIndex, elementImplType,
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
        INamedTypeSymbol interfaceSymbol,
        bool isItemIndex,
        string elementImplType,
        string privateFieldName,
        string processedParam1,
        string processedParam2)
    {
        var comNamespace = GetComNamespace(interfaceSymbol, interfaceDeclaration);
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
                var ordinalComType = GetImplementationOrdinalType(TypeSymbolHelper.GetTypeFullName(indexerSymbol.Type));
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
        INamedTypeSymbol interfaceSymbol,
        bool isItemIndex,
        string elementImplType,
        string privateFieldName,
        string parameterName,
        string operationType)
    {
        var comNamespace = GetComNamespace(interfaceSymbol, interfaceDeclaration);
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
            string returnType = TypeSymbolHelper.GetTypeFullName(indexerSymbol.Type);
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

    #region Common Code Generation Helpers

    /// <summary>
    /// 生成对象已释放检查代码
    /// </summary>
    protected void GenerateDisposedCheck(StringBuilder sb, string privateFieldName)
    {
        GenerateDisposedCheckWithIndent(sb, privateFieldName, "            ");
    }

    /// <summary>
    /// 生成对象已释放检查代码（带缩进）
    /// </summary>
    protected void GenerateDisposedCheckWithIndent(StringBuilder sb, string privateFieldName, string indent = "            ")
    {
        if (sb == null) return;
        sb.AppendLine($"{indent}if ({privateFieldName} == null)");
        sb.AppendLine($"{indent}    throw new ObjectDisposedException(nameof({privateFieldName}));");
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
            if (!AttributeDataHelper.IgnoreGenerator(member))
            {
                if (member.IsIndexer)
                {
                    // 处理索引器
                    GenerateIndexerImplementation(sb, member, interfaceSymbol, interfaceDeclaration);
                }
                else
                {
                    // 处理普通属性
                    GenerateProperty(sb, member, interfaceDeclaration, interfaceSymbol);
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
    private void GenerateProperty(StringBuilder sb, IPropertySymbol propertySymbol, InterfaceDeclarationSyntax interfaceDeclaration, INamedTypeSymbol interfaceSymbol)
    {
        if (sb == null || propertySymbol == null || interfaceDeclaration == null)
            return;

        var isEnumType = TypeSymbolHelper.IsEnumType(propertySymbol.Type);
        var isObjectType = TypeSymbolHelper.IsComplexObjectType(propertySymbol.Type);

        var comClassName = GetComClassName(interfaceSymbol, interfaceDeclaration);
        var needConvert = IsNeedConvert(propertySymbol);

        if (isEnumType)
        {
            GenerateEnumProperty(sb, propertySymbol, interfaceDeclaration, interfaceSymbol);
        }
        else if (isObjectType)
        {
            GenerateComObjectProperty(sb, propertySymbol, interfaceDeclaration, interfaceSymbol, needConvert, comClassName);
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
            GenerateDisposedCheckWithIndent(sb, fieldName, "               ");

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
            GenerateDisposedCheckWithIndent(sb, fieldName, "               ");

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
                INamedTypeSymbol interfaceSymbol,
                bool needConvert,
                string comClassName)
    {
        var comNamespace = GetComNamespace(interfaceSymbol, interfaceDeclaration);
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
            GenerateDisposedCheck(sb, privateFieldName);
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
            GenerateDisposedCheck(sb, privateFieldName);
            sb.AppendLine($"                if (value == null)");
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
    private void GenerateEnumProperty(StringBuilder sb, IPropertySymbol propertySymbol, InterfaceDeclarationSyntax interfaceDeclaration, INamedTypeSymbol interfaceSymbol)
    {
        var orgPropertyName = propertySymbol.Name;
        var propertyName = GetPropertyName(propertySymbol);
        var propertyType = TypeSymbolHelper.GetTypeFullName(propertySymbol.Type);
        var isMethod = IsMethod(propertySymbol);
        var comNamespace = GetComNamespace(interfaceSymbol, interfaceDeclaration);
        var comClassName = GetComClassName(interfaceSymbol, interfaceDeclaration);
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
            GenerateDisposedCheck(sb, privateFieldName);
            sb.AppendLine($"                return {GetPropertyGetString(privateFieldName, propertyName, isMethod)}.EnumConvert({defaultValue});");
            sb.AppendLine("             }");
        }

        if (propertySymbol.SetMethod != null)
        {
            var isConvertIntIndex = AttributeDataHelper.HasAttribute(propertySymbol, ComWrapConstants.ConvertIntAttributeNames);
            sb.AppendLine("            set");
            sb.AppendLine("            {");
            GenerateDisposedCheck(sb, privateFieldName);

            if (isConvertIntIndex)
                sb.AppendLine($"                {GetPropertySetString(privateFieldName, propertyName, isMethod, "value.ConvertToInt()")};");
            else
                sb.AppendLine($"                {GetPropertySetString(privateFieldName, propertyName, isMethod, $"value.EnumConvert({comNamespace}.{enumValueName})")};");
            sb.AppendLine("            }");
        }
        sb.AppendLine("        }");
        sb.AppendLine();
    }


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
    private bool IsNeeDispose(IPropertySymbol propertySymbol)
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
    private bool IsMethod(IPropertySymbol propertySymbol)
    {
        var propertyData = AttributeDataHelper.GetAttributeDataFromSymbol(propertySymbol, ComWrapConstants.ComPropertyWrapAttributeNames);
        if (propertyData == null)
            return false;
        var isMethod = AttributeDataHelper.GetBoolValueFromAttribute(propertyData, ComWrapConstants.IsMethodProperty, false);
        return isMethod;
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
            // 使用 AttributeSyntaxHelper 简化 DefaultValue 提取
            string? wrapAttribute = null;
            foreach (var attrName in ComWrapConstants.ComPropertyWrapAttributeNames)
            {
                var attributes = AttributeSyntaxHelper.GetAttributeSyntaxes(propertyDeclaration, attrName);
                if (attributes.Any())
                {
                    wrapAttribute = attrName;
                    break;
                }
            }

            if (wrapAttribute != null)
            {
                var attributes = AttributeSyntaxHelper.GetAttributeSyntaxes(propertyDeclaration, wrapAttribute);
                if (attributes.Any())
                {
                    var defaultValue = attributes[0].GetPropertyValue("DefaultValue", null)?.ToString();
                    if (defaultValue != null)
                    {
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
                    return TypeSymbolHelper.GetEnumValueLiteral(underlyingType, firstEnumValue.ConstantValue);
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
                return TypeSymbolHelper.GetEnumValueLiteral(typeSymbol, firstEnumValue.ConstantValue);
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
        return TypeSymbolHelper.GetBasicTypeConvertCode(typeSymbol, fieldName);
    }

    private string GenerateNullableConvertCode(ITypeSymbol actualType, string fieldName)
    {
        // 对于可空类型，我们生成带null检查的转换代码
        var conversionCode = TypeSymbolHelper.GetBasicTypeConvertCode(actualType, $"{fieldName}!");

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
    /// 从ComObjectWrap特性中获取COM命名空间（使用语义符号）
    /// </summary>
    /// <param name="interfaceSymbol">接口符号</param>
    /// <param name="interfaceDeclaration">接口声明语法（可选，用于降级处理）</param>
    /// <returns>COM命名空间</returns>
    protected string GetComNamespace(INamedTypeSymbol interfaceSymbol, InterfaceDeclarationSyntax interfaceDeclaration = null)
    {
        if (interfaceSymbol == null)
            return ComWrapConstants.DefaultComNamespace;

        List<string> attributes = [.. ComWrapConstants.ComObjectWrapAttributeNames];
        attributes.AddRange(ComWrapConstants.ComCollectionWrapAttributeNames);

        var attribute = AttributeDataHelper.GetAttributeDataFromSymbol(interfaceSymbol, [.. attributes]);

        if (attribute != null)
        {
            var namespaceValue = AttributeDataHelper.GetStringValueFromAttribute(
                attribute,
                ComWrapConstants.ComNamespaceProperty,
                ComWrapConstants.DefaultComNamespace);

            if (!string.IsNullOrEmpty(namespaceValue) && namespaceValue != ComWrapConstants.DefaultComNamespace)
                return namespaceValue;
        }

        // 降级到语法树处理
        if (interfaceDeclaration != null)
            return GetComNamespace(interfaceDeclaration);

        return ComWrapConstants.DefaultComNamespace;
    }

    /// <summary>
    /// 从ComObjectWrap特性中获取COM命名空间（仅使用语法树）
    /// </summary>
    /// <param name="interfaceDeclaration">接口声明语法</param>
    /// <returns>COM命名空间</returns>
    private string GetComNamespace(InterfaceDeclarationSyntax interfaceDeclaration)
    {
        if (interfaceDeclaration == null)
            return ComWrapConstants.DefaultComNamespace;

        var comObjectWrapAttribute = interfaceDeclaration.AttributeLists
            .SelectMany(al => al.Attributes)
            .FirstOrDefault(attr =>
            {
                var attrName = attr.Name.ToString();
                // 检查短名称或完全限定名称
                return ComWrapAttributeNames().Contains(attrName) ||
                       attrName.EndsWith("ComObjectWrap", StringComparison.OrdinalIgnoreCase) ||
                       attrName.EndsWith("ComCollectionWrap", StringComparison.OrdinalIgnoreCase);
            });

        if (comObjectWrapAttribute != null)
        {
            var namespaceArgument = comObjectWrapAttribute.ArgumentList?.Arguments
                .FirstOrDefault(arg =>
                    arg.NameEquals?.Name.Identifier.Text == ComWrapConstants.ComNamespaceProperty);

            if (namespaceArgument != null)
            {
                // 处理字符串字面量
                var namespaceValue = namespaceArgument.Expression.ToString();
                return namespaceValue.Trim('"');
            }
        }

        return ComWrapConstants.DefaultComNamespace;
    }

    /// <summary>
    /// 从ComObjectWrap特性中获取COM类名（使用语义符号）
    /// </summary>
    /// <param name="interfaceSymbol">接口符号</param>
    /// <param name="interfaceDeclaration">接口声明语法（可选，用于降级处理）</param>
    /// <returns>COM类名</returns>
    /// <remarks>
    /// 支持字符串字面量和nameof()表达式两种形式
    /// </remarks>
    protected string GetComClassName(INamedTypeSymbol interfaceSymbol, InterfaceDeclarationSyntax interfaceDeclaration = null)
    {
        if (interfaceSymbol == null)
            return ComWrapConstants.DefaultComClassName;

        List<string> attributes = [.. ComWrapConstants.ComObjectWrapAttributeNames];
        attributes.AddRange(ComWrapConstants.ComCollectionWrapAttributeNames);

        var attribute = AttributeDataHelper.GetAttributeDataFromSymbol(interfaceSymbol, [.. attributes]);

        if (attribute != null)
        {
            var classNameValue = AttributeDataHelper.GetStringValueFromAttribute(
                attribute,
                "ComClassName",
                string.Empty);

            if (!string.IsNullOrEmpty(classNameValue))
            {
                // 处理 nameof() 表达式
                if (classNameValue.StartsWith("nameof(", StringComparison.OrdinalIgnoreCase) && classNameValue.EndsWith(")", StringComparison.OrdinalIgnoreCase))
                {
                    var nameofContent = classNameValue.Substring(7, classNameValue.Length - 8);
                    return nameofContent.Trim();
                }

                // 处理字符串字面量，移除引号
                return classNameValue.Trim('"');
            }
        }

        // 降级到语法树处理
        if (interfaceDeclaration != null)
            return GetComClassName(interfaceDeclaration);

        return ComWrapConstants.DefaultComClassName;
    }

    /// <summary>
    /// 从ComObjectWrap特性中获取COM类名（仅使用语法树）
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

        var comClassName = GetComClassName(interfaceSymbol, interfaceDeclaration);
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
            sb.AppendLine($"                    // 释放 COM 对象");
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
        sb.AppendLine("            Dispose(false);");
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

            if (AttributeDataHelper.IgnoreGenerator(member))
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
