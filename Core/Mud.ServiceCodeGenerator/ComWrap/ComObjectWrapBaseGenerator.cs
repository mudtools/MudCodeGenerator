// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.CodeGenerator 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Collections.Immutable;
using System.Text;

namespace Mud.ServiceCodeGenerator.ComWrap;

public abstract partial class ComObjectWrapBaseGenerator : TransitiveCodeGenerator
{
    #region Constants and Fields
    private static readonly string[] KnownPrefixes = ["IWord", "IExcel", "IOffice", "IPowerPoint", "IVbe"];

    private static readonly string[] KnownImpPrefixes = ["Word", "Excel", "Office", "PowerPoint", "Vbe"];
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
            return;

        var generatedHintNames = new HashSet<string>();
        var processedSymbols = new HashSet<int>();

        foreach (var interfaceDeclaration in interfaces)
        {
            if (interfaceDeclaration is null)
                continue;

            try
            {
                var semanticModel = compilation.GetSemanticModel(interfaceDeclaration.SyntaxTree);
                var interfaceSymbol = semanticModel.GetDeclaredSymbol(interfaceDeclaration);

                if (interfaceSymbol is null)
                    continue;

                // 使用符号的唯一标识符防止为同一个 partial 接口重复生成
                var symbolHashCode = interfaceSymbol.GetHashCode();
                if (!processedSymbols.Add(symbolHashCode))
                {
                    continue;
                }

                var source = GenerateImplementationClass(interfaceDeclaration, interfaceSymbol);

                if (!string.IsNullOrEmpty(source))
                {
                    // 生成文件名: WordFieldImpl.g.cs
                    var interfaceName = interfaceSymbol.Name;
                    var safeName = interfaceName.TrimStart('I');
                    var hintName = $"{safeName}Impl.g.cs";

                    // 防止重复生成
                    if (!generatedHintNames.Add(hintName))
                    {
                        continue;
                    }

                    context.AddSource(hintName, source);
                }
            }
            catch (Exception ex)
            {
                // 报告代码生成失败
                context.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.ComWrapGenerationError,
                    interfaceDeclaration.GetLocation(),
                    interfaceDeclaration.Identifier.Text,
                    ex.Message));
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

    #region Field Generation
    /// <summary>
    /// 生成私有字段
    /// </summary>
    /// <param name="sb">字符串构建器</param>
    /// <param name="interfaceDeclaration">接口声明语法</param>
    /// <param name="interfaceSymbol">接口符号</param>
    protected void GenerateFields(StringBuilder sb, InterfaceDeclarationSyntax interfaceDeclaration, INamedTypeSymbol interfaceSymbol)
    {
        var comNamespace = GetComNamespace(interfaceSymbol, interfaceDeclaration);
        var comClassName = GetComClassName(interfaceSymbol, interfaceDeclaration);
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

        var comNamespace = GetComNamespace(interfaceSymbol, interfaceDeclaration);
        var comClassName = GetComClassName(interfaceSymbol, interfaceDeclaration);
        var interfaceName = interfaceSymbol.Name;

        sb.AppendLine();
        sb.AppendLine($"        /// <summary>");
        sb.AppendLine($"        ///  无参初始化默认实例");
        sb.AppendLine($"        /// </summary>");
        sb.AppendLine($"        public {className}()");
        sb.AppendLine("        {");
        sb.AppendLine("            _disposedValue = false;");
        sb.AppendLine("        }");
        sb.AppendLine();

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

    protected void GenerateCommonInterfaceMethod(StringBuilder sb, string className, INamedTypeSymbol interfaceSymbol, InterfaceDeclarationSyntax interfaceDeclaration)
    {
        if (interfaceDeclaration == null || interfaceSymbol == null || sb == null)
            return;

        var comNamespace = GetComNamespace(interfaceSymbol, interfaceDeclaration);
        var comClassName = GetComClassName(interfaceSymbol, interfaceDeclaration);
        var interfaceName = interfaceSymbol.Name;

        sb.AppendLine();
        sb.AppendLine($"        ///  <inheritdoc/>");
        sb.AppendLine($"        {GeneratedCodeAttribute}");
        sb.AppendLine($"        public {interfaceName}? LoadFromObject(object comObject)");
        sb.AppendLine("        {");
        sb.AppendLine("            if(comObject == null) return null;");
        sb.AppendLine($"            if(comObject is {comNamespace}.{comClassName} comInstance)");
        sb.AppendLine($"               return new {className}(comInstance);");
        sb.AppendLine("            return null;");
        sb.AppendLine("        }");
        sb.AppendLine();
    }

    protected void GeneratePrivateField(StringBuilder sb, INamedTypeSymbol interfaceSymbol, InterfaceDeclarationSyntax interfaceDeclaration)
    {
        if (interfaceSymbol == null || interfaceDeclaration == null || sb == null)
            return;


        foreach (var member in TypeSymbolHelper.GetAllProperties(interfaceSymbol))
        {
            if (member.IsIndexer)
                continue;
            if (AttributeDataHelper.IgnoreGenerator(member))
                continue;

            var propertyName = member.Name;
            var propertyType = TypeSymbolHelper.GetTypeFullName(member.Type);
            var isObjectType = TypeSymbolHelper.IsComplexObjectType(member.Type);
            if (!isObjectType)
                continue;

            var impType = member.Type.Name.Trim('?');

            var fieldName = PrivateFieldNamingHelper.GeneratePrivateFieldName(impType, FieldNamingStyle.UnderscoreCamel);
            sb.AppendLine($"        private {propertyType} {fieldName}_{propertyName};");
        }
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
        var comClassName = GetComClassNameByImpClass(constructType);
        if (isEnumType)
        {
            if (convertToInteger)
                sb.AppendLine($"            int {param.Name}Obj;");
            else
                sb.AppendLine($"            {comNamespace}.{comClassName} {param.Name}Obj = {comNamespace}.{enumValueName};");
        }
        else if (isObjectType)
        {
            sb.AppendLine($"            {comNamespace}.{comClassName} {param.Name}Obj = null;");
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
                sb.AppendLine($"            var {param.Name}Obj = {param.Name}?.EnumConvert({comNamespace}.{enumValueName}) ?? global::System.Type.Missing;");
        }
        else if (isObjectType)
        {
            sb.AppendLine($"            var {param.Name}Obj = {param.Name} != null ? (({constructType}){param.Name}).InternalComObject : global::System.Type.Missing;");
        }
        else
        {
            // 普通可空参数
            if (convertToInteger)
                sb.AppendLine($"            var {param.Name}Obj = {param.Name} != null ? {param.Name}.ConvertToInt() : 0;");
            else
                sb.AppendLine($"            var {param.Name}Obj = {param.Name} != null ? (object){param.Name} : global::System.Type.Missing;");
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
                sb.AppendLine($"            var {param.Name}Obj = {param.Name} ?? global::System.Type.Missing;");
        }
    }

    /// <summary>
    /// 从实现类的类型名字符串中提取有意义的类名
    /// </summary>
    /// <param name="typeName">类型名字符串，可以包含命名空间</param>
    /// <returns>提取出的类名</returns>
    private static string GetComClassNameByImpClass(string typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
            return typeName;

        // 1. 获取最后一个点后面的部分（去掉命名空间）
        string className = typeName;
        int lastDotIndex = typeName.LastIndexOf('.');
        if (lastDotIndex >= 0 && lastDotIndex < typeName.Length - 1)
        {
            className = typeName.Substring(lastDotIndex + 1);
        }

        // 2. 遍历预定义前缀，检查并移除
        foreach (string prefix in KnownImpPrefixes.OrderByDescending(p => p.Length))
        {
            if (className.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                // 检查移除前缀后的部分是否以大写字母开头或为空
                string remaining = className.Substring(prefix.Length);
                if (remaining.Length > 0 && char.IsUpper(remaining[0]))
                {
                    return remaining;
                }
                else if (remaining.Length == 0)
                {
                    // 如果整个类名就是前缀本身，直接返回
                    return className;
                }
                // 如果移除前缀后不以大写字母开头，可能不是正确的前缀，继续尝试其他前缀
            }
        }

        // 3. 如果没有找到预定义前缀，返回原始类名
        return className;
    }
    #endregion

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
            if (member.MethodKind == MethodKind.Ordinary && !AttributeDataHelper.IgnoreGenerator(member))
            {
                GenerateMethod(sb, member, interfaceDeclaration, interfaceSymbol);
            }
        }

        sb.AppendLine("        #endregion");
        sb.AppendLine();
    }


    /// <summary>
    /// 生成单个方法实现
    /// </summary>
    /// <param name="sb">字符串构建器</param>
    /// <param name="methodSymbol">方法符号</param>
    /// <param name="interfaceDeclaration">接口声明语法</param>
    private void GenerateMethod(
        StringBuilder sb,
        IMethodSymbol methodSymbol,
        InterfaceDeclarationSyntax interfaceDeclaration,
        INamedTypeSymbol interfaceSymbol)
    {
        // 生成方法签名
        GenerateMethodSignature(sb, methodSymbol);

        sb.AppendLine("        {");

        // 生成方法体
        GenerateMethodBody(sb, methodSymbol, interfaceDeclaration, interfaceSymbol);

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
        InterfaceDeclarationSyntax interfaceDeclaration,
        INamedTypeSymbol interfaceSymbol)
    {
        var comClassName = GetComClassName(interfaceSymbol, interfaceDeclaration);
        var privateFieldName = PrivateFieldNamingHelper.GeneratePrivateFieldName(comClassName);

        var hasParameters = methodSymbol.Parameters.Length > 0;

        GenerateDisposedCheck(sb, privateFieldName);

        // 参数预处理（如有必要）
        if (hasParameters)
        {
            GenerateParameterPreprocessing(sb, methodSymbol, interfaceDeclaration, interfaceSymbol);
        }

        // 异常处理和方法调用
        GenerateMethodCallWithExceptionHandling(sb, methodSymbol, interfaceDeclaration, interfaceSymbol, hasParameters);
    }

    /// <summary>
    /// 生成参数预处理逻辑
    /// </summary>
    private void GenerateParameterPreprocessing(
        StringBuilder sb,
        IMethodSymbol methodSymbol,
        InterfaceDeclarationSyntax interfaceDeclaration,
        INamedTypeSymbol interfaceSymbol)
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
            var comNamespace = GetComNamespace(interfaceSymbol, interfaceDeclaration);

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
        INamedTypeSymbol interfaceSymbol,
        bool hasParameters)
    {
        var methodName = AttributeDataHelper.GetStringValueFromSymbol(methodSymbol, ComWrapConstants.MethodNameAttributes, "Name", "");
        if (string.IsNullOrEmpty(methodName))
            methodName = methodSymbol.Name;
        var isObjectType = TypeSymbolHelper.IsComplexObjectType(methodSymbol.ReturnType);
        var comClassName = GetComClassName(interfaceSymbol, interfaceDeclaration);
        var comNamespace = GetComNamespace(interfaceSymbol, interfaceDeclaration);
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
            GenerateOutParameterAssignment(sb, methodSymbol, interfaceDeclaration, interfaceSymbol);
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
            GenerateOutParameterAssignment(sb, methodSymbol, interfaceDeclaration, interfaceSymbol);

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
    private void GenerateOutParameterAssignment(StringBuilder sb, IMethodSymbol methodSymbol, InterfaceDeclarationSyntax interfaceDeclaration, INamedTypeSymbol interfaceSymbol)
    {
        var comNamespace = GetComNamespace(interfaceSymbol, interfaceDeclaration);

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
    #endregion

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
            // 获取非可空枚举类型符号
            if (parameter.Type is INamedTypeSymbol namedType &&
                namedType.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T &&
                namedType.TypeArguments.Length > 0)
            {
                var enumType = namedType.TypeArguments[0];
                return GetEnumDefaultValue(enumType, value);
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
    /// 生成异常处理逻辑
    /// </summary>
    private void GenerateExceptionHandling(StringBuilder sb, string methodName)
    {
        var operationDescription = GetOperationDescription(methodName);

        sb.AppendLine("            catch (COMException cx)");
        sb.AppendLine("            {");
        sb.AppendLine($"                throw new ExcelOperationException(\"{operationDescription}失败: \" + cx.Message, cx);");
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
        return TypeSymbolHelper.GetEnumValueLiteral(enumType, value);
    }
    #endregion
}
