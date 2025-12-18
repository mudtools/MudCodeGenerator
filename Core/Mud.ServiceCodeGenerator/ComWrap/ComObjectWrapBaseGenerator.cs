// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.CodeGenerator 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.CodeGenerator.Helper;
using Mud.ServiceCodeGenerator.ComWrapSourceGenerator;
using System.Collections.Immutable;
using System.Text;

namespace Mud.ServiceCodeGenerator.ComWrap;

public abstract class ComObjectWrapBaseGenerator : TransitiveCodeGenerator
{
    #region Constants and Fields
    private static readonly string[] KnownPrefixes = { "IWord", "IExcel", "IOffice", "IPowerPoint", "IVbe" };
    #endregion

    #region Generator Initialization and Execution
    /// <inheritdoc/>
    protected override System.Collections.ObjectModel.Collection<string> GetFileUsingNameSpaces()
    {
        return
        [
            "System",
            "System.Runtime.InteropServices",
        ];
    }

    /// <summary>
    /// 获取COM对象包装特性名称数组
    /// </summary>
    /// <returns>特性名称数组</returns>
    protected virtual string[] ComWrapAttributeNames() => ComWrapConstants.ComObjectWrapAttributeNames;

    /// <inheritdoc/>
    public override void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var interfaceDeclarations = GetClassDeclarationProvider<InterfaceDeclarationSyntax>(context, ComWrapAttributeNames());

        var compilationAndInterfaces = context.CompilationProvider.Combine(interfaceDeclarations);

        context.RegisterSourceOutput(compilationAndInterfaces,
             (spc, source) => ExecuteGenerator(source.Left, source.Right!, spc));
    }

    /// <summary>
    /// 执行源代码生成逻辑
    /// </summary>
    /// <param name="compilation">编译信息</param>
    /// <param name="interfaces">接口声明数组</param>
    /// <param name="context">源代码生成上下文</param>
    protected void ExecuteGenerator(Compilation compilation, ImmutableArray<InterfaceDeclarationSyntax> interfaces, SourceProductionContext context)
    {
        if (compilation == null) throw new ArgumentNullException(nameof(compilation));
        if (interfaces == null) throw new ArgumentNullException(nameof(interfaces));

        if (interfaces.IsDefaultOrEmpty)
            if (interfaces.IsDefaultOrEmpty)
                return;

        foreach (var interfaceDeclaration in interfaces)
        {
            if (interfaceDeclaration is null)
                continue;

            var semanticModel = compilation.GetSemanticModel(interfaceDeclaration.SyntaxTree);
            var interfaceSymbol = semanticModel.GetDeclaredSymbol(interfaceDeclaration);

            if (interfaceSymbol is null)
                continue;

            var source = GenerateImplementationClass(interfaceDeclaration, interfaceSymbol);

            if (!string.IsNullOrEmpty(source))
            {
                var hintName = $"{interfaceSymbol.Name}Impl.g.cs";
                context.AddSource(hintName, source);
            }
        }
    }

    /// <summary>
    /// 生成COM对象包装实现类
    /// </summary>
    /// <param name="interfaceDeclaration">接口声明语法</param>
    /// <param name="interfaceSymbol">接口符号</param>
    /// <returns>生成的源代码</returns>
    protected abstract string GenerateImplementationClass(InterfaceDeclarationSyntax interfaceDeclaration, INamedTypeSymbol interfaceSymbol);
    #endregion

    #region Constructor and Field Generation
    /// <summary>
    /// 生成构造函数 />
    /// </summary>
    /// <param name="sb">字符串构建器</param>
    /// <param name="className">类名</param>
    /// <param name="interfaceDeclaration">接口声明语法</param>
    protected void GenerateConstructor(StringBuilder sb, string className, INamedTypeSymbol interfaceSymbol, InterfaceDeclarationSyntax interfaceDeclaration)
    {
        if (interfaceDeclaration == null || interfaceSymbol == null || sb == null)
            return;

        if (NoneConstructor(interfaceSymbol))
            return;

        var comNamespace = GetComNamespace(interfaceDeclaration);
        var comClassName = GetComClassName(interfaceDeclaration);

        sb.AppendLine();
        sb.AppendLine($"        /// <summary>");
        sb.AppendLine($"        ///  使用 <see cref=\"{comNamespace}.{comClassName}\"/> COM对象初始化当前实例");
        sb.AppendLine($"        /// </summary>");
        sb.AppendLine($"        internal {className}({comNamespace}.{comClassName} comObject)");
        sb.AppendLine("        {");
        sb.AppendLine($"            {PrivateFieldNamingHelper.GeneratePrivateFieldName(comClassName)} = comObject ?? throw new ArgumentNullException(nameof(comObject));");
        sb.AppendLine("            _disposedValue = false;");
        sb.AppendLine("        }");
        sb.AppendLine();
    }

    protected void GeneratePrivateField(StringBuilder sb, INamedTypeSymbol interfaceSymbol, InterfaceDeclarationSyntax interfaceDeclaration)
    {
        if (interfaceSymbol == null || interfaceDeclaration == null)
            return;

        foreach (var member in interfaceSymbol.GetMembers().OfType<IPropertySymbol>())
        {
            if (member.IsIndexer)
                continue;

            var propertyName = member.Name;
            var propertyType = member.Type.ToDisplayString();
            var isObjectType = IsComObjectType(member.Type);
            if (!isObjectType)
                continue;

            var impType = member.Type.Name.Trim('?');

            var fieldName = PrivateFieldNamingHelper.GeneratePrivateFieldName(impType, FieldNamingStyle.UnderscoreCamel);
            sb.AppendLine($"        private {propertyType} {fieldName}_{propertyName};");
        }
    }

    protected void GeneratePrivateFieldDisposable(StringBuilder sb, INamedTypeSymbol interfaceSymbol, InterfaceDeclarationSyntax interfaceDeclaration)
    {
        if (interfaceSymbol == null || interfaceDeclaration == null || sb == null)
            return;

        foreach (var member in interfaceSymbol.GetMembers().OfType<IPropertySymbol>())
        {
            if (member.IsIndexer)
                continue;

            var needDispose = IsNeeDispose(member);
            if (!needDispose)
                continue;
            var propertyName = member.Name;
            var propertyType = member.Type.ToDisplayString();
            var isObjectType = IsComObjectType(member.Type);
            if (!isObjectType)
                continue;

            var impType = member.Type.Name.Trim('?');

            var fieldName = PrivateFieldNamingHelper.GeneratePrivateFieldName(impType, FieldNamingStyle.UnderscoreCamel);
            sb.AppendLine($"                {fieldName}_{propertyName}?.Dispose();");
            sb.AppendLine($"                {fieldName}_{propertyName} = null;");
        }
    }
    #endregion

    #region Property Generation
    /// <summary>
    /// 生成单个属性实现
    /// </summary>
    /// <param name="sb">字符串构建器</param>
    /// <param name="propertySymbol">属性符号</param>
    /// <param name="interfaceDeclaration">接口声明语法</param>
    protected void GenerateProperty(StringBuilder sb, IPropertySymbol propertySymbol, InterfaceDeclarationSyntax interfaceDeclaration)
    {
        if (sb == null || propertySymbol == null || interfaceDeclaration == null)
            return;

        var isEnumType = IsEnumType(propertySymbol.Type);
        var isObjectType = IsComObjectType(propertySymbol.Type);

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
        var propertyName = propertySymbol.Name;
        var propertyType = propertySymbol.Type.ToDisplayString();

        sb.AppendLine($"        ///  <inheritdoc/>");
        sb.AppendLine($"        {GeneratedCodeAttribute}");
        sb.AppendLine($"        public {propertyType} {propertyName}");
        sb.AppendLine("        {");
        sb.AppendLine($"            get");
        sb.AppendLine("            {");
        sb.AppendLine($"                if({fieldName} == null)");
        sb.AppendLine($"                    throw new ObjectDisposedException(nameof({fieldName}));");

        if (needConvert)
        {
            var convertMethod = GetConvertCode(propertySymbol, $"{fieldName}.{propertyName}");
            sb.AppendLine($"                return {convertMethod};");
        }
        else
        {
            sb.AppendLine($"                return {fieldName}.{propertyName};");
        }
        sb.AppendLine("             }");

        if (propertySymbol.SetMethod != null)
        {
            sb.AppendLine("            set");
            sb.AppendLine("            {");


            if (propertyType.EndsWith("?", StringComparison.Ordinal))
            {
                string setValue = "value.Value";
                if (ShouldUseDirectValueForNullable(propertyType))
                    setValue = "value";
                else if (needConvert && propertyType.StartsWith("bool", StringComparison.OrdinalIgnoreCase))
                {
                    setValue = $"value.Value.ConvertTriState()";
                }
                sb.AppendLine($"                if ({fieldName} != null && value != null)");
                sb.AppendLine($"                    {fieldName}.{propertyName} = {setValue};");
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
                sb.AppendLine($"                if ({fieldName} != null)");
                sb.AppendLine($"                    {fieldName}.{propertyName} = {setValue};");
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
        var propertyName = propertySymbol.Name;
        var propertyType = propertySymbol.Type.ToDisplayString();
        var objectType = StringExtensions.RemoveInterfacePrefix(propertyType);
        var constructType = GetImplementationType(objectType);

        var impType = propertySymbol.Type.Name.Trim('?');
        var fieldName = PrivateFieldNamingHelper.GeneratePrivateFieldName(impType, FieldNamingStyle.UnderscoreCamel) + "_" + propertySymbol.Name;
        var privateFieldName = PrivateFieldNamingHelper.GeneratePrivateFieldName(comClassName);

        sb.AppendLine($"        ///  <inheritdoc/>");
        sb.AppendLine($"        {GeneratedCodeAttribute}");
        sb.AppendLine($"        public {propertyType} {propertyName}");
        sb.AppendLine("        {");
        sb.AppendLine("             get");
        sb.AppendLine("             {");
        sb.AppendLine($"                if({privateFieldName} == null)");
        sb.AppendLine($"                    throw new ObjectDisposedException(nameof({privateFieldName}));");
        sb.AppendLine($"                var comObj = {privateFieldName}?.{propertyName};");
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

        if (propertySymbol.SetMethod != null)
        {
            sb.AppendLine("             set");
            sb.AppendLine("             {");
            sb.AppendLine($"                if ({privateFieldName} != null && value != null)");
            sb.AppendLine($"                {{");
            sb.AppendLine($"                    var comObj = (({constructType})value).InternalComObject;");
            sb.AppendLine($"                    {privateFieldName}.{propertyName} = comObj;");
            sb.AppendLine($"                }}");
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
        var propertyName = propertySymbol.Name;
        var propertyType = propertySymbol.Type.ToDisplayString();
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
        sb.AppendLine($"        public {propertyType} {propertyName}");
        sb.AppendLine("        {");
        sb.AppendLine("            get");
        sb.AppendLine("            {");
        sb.AppendLine($"                if({privateFieldName} == null)");
        sb.AppendLine($"                    throw new ObjectDisposedException(nameof({privateFieldName}));");
        sb.AppendLine($"                return {privateFieldName}.{propertyName}.EnumConvert({defaultValue});");
        sb.AppendLine("             }");

        if (propertySymbol.SetMethod != null)
        {
            sb.AppendLine("            set");
            sb.AppendLine("            {");
            sb.AppendLine($"                if ({privateFieldName} != null)");
            sb.AppendLine($"                    {privateFieldName}.{propertyName} = value.EnumConvert({comNamespace}.{enumValueName});");
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

        foreach (var member in InterfaceHelper.GetAllMethods(interfaceSymbol, excludedInterfaces: new[] { "IDisposable", "System.IDisposable", "System.Collections.Generic.IEnumerable" }))
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
        var returnType = methodSymbol.ReturnType.ToDisplayString();
        var parameters = new List<string>();

        foreach (var param in methodSymbol.Parameters)
        {
            var paramType = param.Type.ToDisplayString();
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
            var pType = param.Type.ToDisplayString();
            bool isEnumType = IsEnumType(param.Type);
            bool isObjectType = IsComObjectType(param.Type);
            bool hasConvertTriState = HasConvertTriStateAttribute(param);
            bool convertToInteger = AttributeDataHelper.HasAttribute(param, ComWrapConstants.ConvertIntAttributeNames);
            bool isOut = param.RefKind == RefKind.Out;

            var defaultValue = GetDefaultValue(interfaceDeclaration, param, param.Type);
            var comNamespace = GetComNamespace(interfaceDeclaration);

            var paramcomNamespace = AttributeDataHelper.GetStringValueFromSymbol(param, ComWrapConstants.ComNamespaceAttributes, "Name", "");
            if (!string.IsNullOrEmpty(paramcomNamespace))
                comNamespace = paramcomNamespace;

            var enumValueName = GetEnumValueWithoutNamespace(defaultValue);
            var constructType = GetImplementationType(param.Type.ToDisplayString());

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
        var methodName = methodSymbol.Name;
        var isObjectType = IsComObjectType(methodSymbol.ReturnType);
        var comClassName = GetComClassName(interfaceDeclaration);
        var comNamespace = GetComNamespace(interfaceDeclaration);
        var privateFieldName = PrivateFieldNamingHelper.GeneratePrivateFieldName(comClassName);

        var needConvert = AttributeDataHelper.HasAttribute(methodSymbol, ComWrapConstants.ReturnValueConvertAttributes);
        string returnType = methodSymbol.ReturnType.ToDisplayString();
        var isEnunType = IsEnumType(methodSymbol.ReturnType);
        var defaultValue = GetDefaultValue(interfaceDeclaration, methodSymbol, methodSymbol.ReturnType);
        sb.AppendLine("            try");
        sb.AppendLine("            {");

        // 生成方法调用参数
        var callParameters = GenerateCallParameters(methodSymbol);

        if (returnType == "void")
        {
            sb.AppendLine($"                {privateFieldName}?.{methodName}({callParameters});");

            // 处理out参数的返回值赋值
            GenerateOutParameterAssignment(sb, methodSymbol, interfaceDeclaration);
        }
        else if (isObjectType)
        {
            var objectType = StringExtensions.RemoveInterfacePrefix(returnType);
            var constructType = GetImplementationType(objectType);
            sb.AppendLine($"                var comObj = {privateFieldName}?.{methodName}({callParameters});");
            sb.AppendLine("                if (comObj == null)");
            if (returnType.EndsWith("?", StringComparison.Ordinal))
                sb.AppendLine("                    return null;");
            else
                sb.AppendLine("                    return null;");

            if (!needConvert)
                sb.AppendLine($"                return new {constructType}(comObj);");
            else
            {
                var ordinalComType = GetImplementationOrdinalType(returnType);
                var comType = GetOrdinalComType(ordinalComType);
                sb.AppendLine($"                if(comObj is {comNamespace}.{comType} rComObj)");
                sb.AppendLine($"                     return new {constructType}(rComObj);");
                sb.AppendLine($"                else");
                sb.AppendLine("                     return null;");
            }
        }
        else
        {
            sb.AppendLine($"                var returnValue = {privateFieldName}?.{methodName}({callParameters});");
            // 处理out参数的返回值赋值
            GenerateOutParameterAssignment(sb, methodSymbol, interfaceDeclaration);

            if (isEnunType)
            {
                sb.AppendLine($"                return returnValue.EnumConvert({defaultValue});");
            }
            else
            {
                sb.AppendLine("                return returnValue;");
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
            var pType = param.Type.ToDisplayString();
            bool isEnumType = IsEnumType(param.Type);
            bool isObjectType = IsComObjectType(param.Type);
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

            var pType = param.Type.ToDisplayString();
            bool isEnumType = IsEnumType(param.Type);
            bool isObjectType = IsComObjectType(param.Type);
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

        var type = parameter.Type.ToDisplayString();
        var typeSymbol = parameter.Type;

        // 处理枚举类型
        if (typeSymbol.TypeKind == TypeKind.Enum)
        {
            return GetEnumDefaultValue(typeSymbol, value);
        }

        // 处理可空枚举类型
        if (type.EndsWith("?", StringComparison.Ordinal))
        {
            var nonNullType = type.TrimEnd('?');
            if (IsEnumType(parameter.Type) && value != null)
            {
                // 获取非可空枚举类型符号
                var originalDefinition = typeSymbol.OriginalDefinition;
                if (originalDefinition is INamedTypeSymbol namedType && namedType.TypeArguments.Length > 0)
                {
                    var enumType = namedType.TypeArguments[0];
                    return GetEnumDefaultValue(enumType, value);
                }
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

        var needConvert = AttributeDataHelper.GetStringValueFromAttribute(propertyWrapAttr, ComWrapConstants.ComNamespaceProperty, string.Empty);
        return needConvert;
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
    /// 通过语义分析判断类型是否为枚举类型
    /// </summary>
    /// <param name="typeSymbol">类型符号</param>
    /// <returns>如果是枚举类型返回true，否则返回false</returns>
    protected bool IsEnumType(ITypeSymbol typeSymbol)
    {
        if (typeSymbol == null)
            return false;
        // 首先检查是否是直接的枚举类型
        if (typeSymbol.TypeKind == TypeKind.Enum)
            return true;

        // 如果是可空类型，获取其底层类型再检查
        if (typeSymbol is INamedTypeSymbol namedType && namedType.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T)
        {
            var underlyingType = namedType.TypeArguments.FirstOrDefault();
            return underlyingType?.TypeKind == TypeKind.Enum;
        }

        return false;
    }

    /// <summary>
    /// 通过语义分析判断类型是否为COM对象类型
    /// </summary>
    /// <param name="typeSymbol">类型符号</param>
    /// <returns>如果是COM对象类型返回true，否则返回false</returns>
    protected bool IsComObjectType(ITypeSymbol typeSymbol)
    {
        if (typeSymbol == null)
            return false;
        // 首先检查是否是直接的枚举类型
        if (typeSymbol.TypeKind == TypeKind.Interface)
            return true;
        // 如果是可空类型，获取其底层类型再检查
        if (typeSymbol is INamedTypeSymbol namedType && namedType.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T)
        {
            var underlyingType = namedType.TypeArguments.FirstOrDefault();
            return underlyingType?.TypeKind == TypeKind.Interface;
        }

        return false;
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
        var specialType = typeSymbol.SpecialType;

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
            _ => "ToString()"
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

    /// <summary>
    /// 根据接口名称获取实现类名称
    /// </summary>
    /// <param name="interfaceName">接口名称</param>
    /// <returns>实现类名称</returns>
    /// <remarks>
    /// 如果接口名称以"I"开头且第二个字符为大写，则移除"I"前缀；否则添加"Impl"后缀
    /// </remarks>
    protected string GetImplementationClassName(string interfaceName)
    {
        if (string.IsNullOrEmpty(interfaceName))
            return "NullOrEmptyInterfaceName";

        return interfaceName.StartsWith("I", StringComparison.Ordinal) && interfaceName.Length > 1 && char.IsUpper(interfaceName[1])
            ? interfaceName.Substring(1)
            : interfaceName + "Impl";
    }


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
    protected virtual void GenerateIDisposableImplementation(StringBuilder sb, InterfaceDeclarationSyntax interfaceDeclaration, INamedTypeSymbol interfaceSymbol)
    {
        if (interfaceDeclaration == null || interfaceSymbol == null || sb == null)
            return;

        var comClassName = GetComClassName(interfaceDeclaration);
        var impClassName = GetImplementationClassName(interfaceSymbol.Name);
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
            GenerateAdditionalDisposalLogic(sb, interfaceSymbol, interfaceDeclaration);
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
    #endregion

    /// <summary>
    /// 生成额外的释放逻辑（可被子类重写）
    /// </summary>
    /// <param name="sb">字符串构建器</param>
    /// <param name="interfaceSymbol">接口符号</param>
    /// <param name="interfaceDeclaration">接口声明语法</param>
    protected virtual void GenerateAdditionalDisposalLogic(StringBuilder sb, INamedTypeSymbol interfaceSymbol, InterfaceDeclarationSyntax interfaceDeclaration)
    {

    }

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
        if (IsBasicType(elementType))
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
        if (IsBasicType(elementType) || string.IsNullOrEmpty(elementType))
            return elementType;
        var types = elementType.Split(['.'], StringSplitOptions.RemoveEmptyEntries);
        if (types.Length > 1)
            return types[types.Length - 1];
        return types[0];
    }

    /// <summary>
    /// 检查是否为基本类型
    /// </summary>
    /// <param name="typeName">类型名称</param>
    /// <returns>如果是基本类型返回true，否则返回false</returns>
    protected static bool IsBasicType(string typeName)
    {
        return typeName switch
        {
            "string" or "string?" => true,
            "int" or "int?" => true,
            "short" or "short?" => true,
            "long" or "long?" => true,
            "float" or "float?" => true,
            "double" or "double?" => true,
            "decimal" or "decimal?" => true,
            "bool" or "bool?" => true,
            "byte" or "byte?" => true,
            "char" or "char?" => true,
            "uint" or "uint?" => true,
            "ushort" or "ushort?" => true,
            "ulong" or "ulong?" => true,
            "sbyte" or "sbyte?" => true,
            "object" or "object?" => true,
            _ => false
        };
    }
    #endregion

    #region Refactored Parameter Processing Methods
    /// <summary>
    /// 生成out参数变量声明
    /// </summary>
    private void GenerateOutParameterVariable(StringBuilder sb, IParameterSymbol param, bool isEnumType,
        bool isObjectType, bool convertToInteger, string pType, string comNamespace,
        string enumValueName, string constructType)
    {
        if (isEnumType)
        {
            if (convertToInteger)
                sb.AppendLine($"            int {param.Name}Obj;");
            else
                sb.AppendLine($"            {comNamespace}.{enumValueName} {param.Name}Obj;");
        }
        else if (isObjectType)
        {
            sb.AppendLine($"            {constructType} {param.Name}Obj;");
        }
        else
        {
            // 普通out参数，根据类型声明变量
            GenerateBasicOutParameterVariable(sb, param, pType);
        }
    }

    /// <summary>
    /// 生成基本类型的out参数变量声明
    /// </summary>
    private void GenerateBasicOutParameterVariable(StringBuilder sb, IParameterSymbol param, string pType)
    {
        switch (pType)
        {
            case "string":
                sb.AppendLine($"            string {param.Name}Obj = string.Empty;");
                break;
            case "int":
                sb.AppendLine($"            int {param.Name}Obj = 0;");
                break;
            default:
                sb.AppendLine($"            {pType} {param.Name}Obj = null;");
                break;
        }
    }

    /// <summary>
    /// 生成参数对象处理逻辑
    /// </summary>
    private void GenerateParameterObject(StringBuilder sb, IParameterSymbol param, string pType,
        bool isEnumType, bool isObjectType, bool hasConvertTriState, bool convertToInteger,
        string comNamespace, string enumValueName, string constructType)
    {
        if (pType.EndsWith("?", StringComparison.Ordinal))
        {
            GenerateNullableParameterObject(sb, param, pType, isEnumType, isObjectType,
                convertToInteger, comNamespace, enumValueName, constructType);
        }
        else if (hasConvertTriState && pType == "bool")
        {
            // 带有ConvertTriState特性的bool参数
            sb.AppendLine($"            var {param.Name}Obj = {param.Name}.ConvertTriState();");
        }
        else
        {
            GenerateNonNullableParameterObject(sb, param, pType, isEnumType, isObjectType,
                convertToInteger, comNamespace, enumValueName, constructType);
        }
    }

    /// <summary>
    /// 生成可空参数对象处理逻辑
    /// </summary>
    private void GenerateNullableParameterObject(StringBuilder sb, IParameterSymbol param, string pType,
        bool isEnumType, bool isObjectType, bool convertToInteger, string comNamespace,
        string enumValueName, string constructType)
    {
        if (isEnumType)
        {
            if (convertToInteger)
                sb.AppendLine($"            var {param.Name}Obj = (int){param.Name} ?? 0;");
            else
                sb.AppendLine($"            var {param.Name}Obj = {param.Name}?.EnumConvert({comNamespace}.{enumValueName}) ?? System.Type.Missing;");
        }
        else if (isObjectType)
        {
            sb.AppendLine($"            var {param.Name}Obj = {param.Name} != null ? (({constructType}){param.Name}).InternalComObject : System.Type.Missing;");
        }
        else
        {
            // 普通可空参数
            if (convertToInteger)
                sb.AppendLine($"            var {param.Name}Obj = {param.Name} != null ? {param.Name}.ConvertToInt() : 0;");
            else
                sb.AppendLine($"            var {param.Name}Obj = {param.Name} != null ? (object){param.Name} : System.Type.Missing;");
        }
    }

    /// <summary>
    /// 生成非可空参数对象处理逻辑
    /// </summary>
    private void GenerateNonNullableParameterObject(StringBuilder sb, IParameterSymbol param, string pType,
        bool isEnumType, bool isObjectType, bool convertToInteger, string comNamespace,
        string enumValueName, string constructType)
    {
        if (isEnumType)
        {
            // 枚举参数
            if (convertToInteger)
                sb.AppendLine($"            var {param.Name}Obj = (int){param.Name};");
            else
                sb.AppendLine($"            var {param.Name}Obj = {param.Name}.EnumConvert({comNamespace}.{enumValueName});");
        }
        else if (isObjectType)
        {
            // COM对象参数
            sb.AppendLine($"            var {param.Name}Obj = (({constructType}){param.Name}).InternalComObject;");
        }
        else if (pType == "object")
        {
            // object类型参数
            if (convertToInteger)
                sb.AppendLine($"            var {param.Name}Obj = {param.Name}.ConvertToInt();");
            else
                sb.AppendLine($"            var {param.Name}Obj = {param.Name} ?? System.Type.Missing;");
        }
    }
    #endregion

}
