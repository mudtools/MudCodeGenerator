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
        // 获取此生成器的特性名称
        var attributeNames = ComWrapAttributeNames();

        // 使用增量语法提供器，只处理带有相关特性的接口声明
        var interfaceDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: (node, _) => node is InterfaceDeclarationSyntax interfaceDecl &&
                    HasComWrapAttributes(interfaceDecl, attributeNames),
                transform: (ctx, _) => GetSemanticTargetForGeneration(ctx))
            .Where(m => m != null);

        // 将编译和接口声明组合
        var compilationAndInterfaces = context.CompilationProvider.Combine(interfaceDeclarations.Collect());

        // 注册源代码生成
        context.RegisterSourceOutput(compilationAndInterfaces,
            (spc, source) => ExecuteGenerator(source.Left, source.Right!, spc));
    }

    /// <summary>
    /// 检查接口声明是否有 COM 包装特性
    /// </summary>
    private static bool HasComWrapAttributes(InterfaceDeclarationSyntax syntax, string[] attributeNames)
    {
        return syntax.AttributeLists.SelectMany(al => al.Attributes)
            .Any(attr =>
            {
                var attrName = attr.Name.ToString();
                return attributeNames.Contains(attrName);
            });
    }

    /// <summary>
    /// 检查接口声明是否有 COM 包装特性
    /// </summary>
    protected virtual bool HasComWrapAttributes(InterfaceDeclarationSyntax syntax)
    {
        return syntax.AttributeLists.SelectMany(al => al.Attributes)
            .Any(attr =>
            {
                var attrName = attr.Name.ToString();
                return ComWrapAttributeNames().Contains(attrName);
            });
    }

    /// <summary>
    /// 获取语义目标用于代码生成
    /// </summary>
    private static InterfaceDeclarationSyntax? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
    {
        // 获取接口声明的语义符号，确保其有效
        var interfaceDecl = (InterfaceDeclarationSyntax)context.Node;
        var semanticModel = context.SemanticModel;
        var interfaceSymbol = semanticModel.GetDeclaredSymbol(interfaceDecl);

        // 接口都是抽象的，但我们需要处理所有带有特性的接口
        // 只检查符号是否有效
        if (interfaceSymbol == null)
            return null;

        return interfaceDecl;
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
        var processedSymbols = new HashSet<string>();

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

                // 使用符号的完整限定名称防止为同一个 partial 接口重复生成
                var symbolKey = interfaceSymbol.GetFullyQualifiedName();
                if (!processedSymbols.Add(symbolKey))
                {
                    continue;
                }

                var source = GenerateImplementationClass(interfaceDeclaration, interfaceSymbol);

                if (!string.IsNullOrEmpty(source))
                {
                    var interfaceName = interfaceSymbol.Name;
                    var hintName = GenerateHintName(interfaceName);

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
                var errorMessage = $"{ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}";
                context.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.ComWrapGenerationError,
                    interfaceDeclaration.GetLocation(),
                    interfaceDeclaration.Identifier.Text,
                    errorMessage));
            }
        }
    }

    /// <summary>
    /// 生成包含命名空间的安全文件名
    /// </summary>
    /// <param name="interfaceName">接口名称</param>
    /// <returns>生成的文件名</returns>
    private static string GenerateHintName(string interfaceName)
    {
        var safeInterfaceName = interfaceName.TrimStart('I');
        return $"{safeInterfaceName}Impl.g.cs";
    }

    /// <summary>
    /// 生成COM对象包装实现类
    /// </summary>
    /// <param name="interfaceDeclaration">接口声明语法</param>
    /// <param name="interfaceSymbol">接口符号</param>
    /// <returns>生成的源代码</returns>
    protected string GenerateImplementationClass(InterfaceDeclarationSyntax interfaceDeclaration, INamedTypeSymbol interfaceSymbol)
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
        sb.AppendLine($"    internal sealed partial class {className} : {interfaceName}");
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

        // 模板方法：生成额外的实现内容（子类可重写）
        GenerateExtraImplementations(sb, interfaceSymbol, interfaceDeclaration);

        // 生成IDisposable实现
        GenerateIDisposableImplementation(sb, interfaceDeclaration, interfaceSymbol);

        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    /// <summary>
    /// 生成额外的实现内容（钩子方法，子类可重写）
    /// </summary>
    /// <param name="sb">字符串构建器</param>
    /// <param name="interfaceSymbol">接口符号</param>
    /// <param name="interfaceDeclaration">接口声明语法</param>
    protected virtual void GenerateExtraImplementations(StringBuilder sb, INamedTypeSymbol interfaceSymbol, InterfaceDeclarationSyntax interfaceDeclaration)
    {
        // 默认不生成额外内容
    }

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
        ArgumentNullExceptionExtensions.ThrowIfNull(sb, nameof(sb));
        ArgumentNullExceptionExtensions.ThrowIfNull(interfaceDeclaration, nameof(interfaceDeclaration));
        ArgumentNullExceptionExtensions.ThrowIfNull(interfaceSymbol, nameof(interfaceSymbol));

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
        ArgumentNullExceptionExtensions.ThrowIfNull(sb, nameof(sb));
        ArgumentNullExceptionExtensions.ThrowIfNull(className, nameof(className));
        ArgumentNullExceptionExtensions.ThrowIfNull(interfaceSymbol, nameof(interfaceSymbol));
        ArgumentNullExceptionExtensions.ThrowIfNull(interfaceDeclaration, nameof(interfaceDeclaration));

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
        ArgumentNullExceptionExtensions.ThrowIfNull(sb, nameof(sb));
        ArgumentNullExceptionExtensions.ThrowIfNull(className, nameof(className));
        ArgumentNullExceptionExtensions.ThrowIfNull(interfaceSymbol, nameof(interfaceSymbol));
        ArgumentNullExceptionExtensions.ThrowIfNull(interfaceDeclaration, nameof(interfaceDeclaration));

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
        ArgumentNullExceptionExtensions.ThrowIfNull(sb, nameof(sb));
        ArgumentNullExceptionExtensions.ThrowIfNull(interfaceSymbol, nameof(interfaceSymbol));
        ArgumentNullExceptionExtensions.ThrowIfNull(interfaceDeclaration, nameof(interfaceDeclaration));

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
    private void GenerateOutParameterVariable(StringBuilder sb, ParameterProcessingContext context)
    {
        ArgumentNullExceptionExtensions.ThrowIfNull(sb, nameof(sb));
        ArgumentNullExceptionExtensions.ThrowIfNull(context, nameof(context));

        if (context.IsEnumType)
        {
            if (context.ConvertToInteger)
                sb.AppendLine($"            int {context.Parameter.Name}Obj = 0;");
            else
            {
                // 对于枚举类型的out参数，使用COM命名空间的类型声明并赋默认值
                var comEnumType = ConvertEnumTypeToComNamespace(context.ParameterType, context.ComNamespace);
                var enumDefault = ConvertEnumToComNamespace(context.EnumValueName, context.ComNamespace);
                if (string.IsNullOrEmpty(enumDefault))
                {
                    // 如果没有默认值，使用类型的第一个枚举值
                    sb.AppendLine($"            {comEnumType} {context.Parameter.Name}Obj = default({comEnumType});");
                }
                else
                {
                    sb.AppendLine($"            {comEnumType} {context.Parameter.Name}Obj = {enumDefault};");
                }
            }
        }
        else if (context.IsObjectType)
        {
            sb.AppendLine($"            {context.ComNamespace}.{context.Parameter.Type.Name} {context.Parameter.Name}Obj = null;");
        }
        else
        {
            // 普通out参数，根据类型声明变量并赋初始值
            GenerateBasicOutParameterVariable(sb, context.Parameter, context.ParameterType);
        }
    }

    /// <summary>
    /// 生成基本类型的out参数变量声明
    /// </summary>
    private void GenerateBasicOutParameterVariable(StringBuilder sb, IParameterSymbol param, string pType)
    {
        // out参数需要赋初始值以避免编译器警告
        switch (pType)
        {
            case "string":
                sb.AppendLine($"            string {param.Name}Obj = string.Empty;");
                break;
            case "int":
                sb.AppendLine($"            int {param.Name}Obj = 0;");
                break;
            case "bool":
                sb.AppendLine($"            bool {param.Name}Obj = false;");
                break;
            case "double":
                sb.AppendLine($"            double {param.Name}Obj = 0.0;");
                break;
            case "float":
                sb.AppendLine($"            float {param.Name}Obj = 0.0f;");
                break;
            case "long":
                sb.AppendLine($"            long {param.Name}Obj = 0L;");
                break;
            case "object":
                sb.AppendLine($"            object {param.Name}Obj = null;");
                break;
            default:
                sb.AppendLine($"            {pType} {param.Name}Obj = default({pType});");
                break;
        }
    }

    /// <summary>
    /// 生成参数对象处理逻辑
    /// </summary>
    private void GenerateParameterObject(StringBuilder sb, ParameterProcessingContext context)
    {
        ArgumentNullExceptionExtensions.ThrowIfNull(sb, nameof(sb));
        ArgumentNullExceptionExtensions.ThrowIfNull(context, nameof(context));

        if (context.ParameterType.EndsWith("?", StringComparison.Ordinal))
        {
            GenerateNullableParameterObject(sb, context);
        }
        else if (context.HasConvertTriState && context.ParameterType == "bool")
        {
            // 带有ConvertTriState特性的bool参数
            sb.AppendLine($"            var {context.Parameter.Name}Obj = {context.Parameter.Name}.ConvertTriState();");
        }
        else
        {
            GenerateNonNullableParameterObject(sb, context);
        }
    }

    /// <summary>
    /// 生成可空参数对象处理逻辑
    /// </summary>
    private void GenerateNullableParameterObject(StringBuilder sb, ParameterProcessingContext context)
    {
        ArgumentNullExceptionExtensions.ThrowIfNull(sb, nameof(sb));
        ArgumentNullExceptionExtensions.ThrowIfNull(context, nameof(context));

        if (context.IsEnumType)
        {
            if (context.ConvertToInteger)
                sb.AppendLine($"            var {context.Parameter.Name}Obj = (int){context.Parameter.Name} ?? 0;");
            else
            {
                // 对于枚举类型，EnumValueName 包含完整的枚举值路径（如 "ComObjectWrapTest.WdSaveOptions.wdPromptToSaveChanges"）
                // 我们需要将其转换为 COM 命名空间的路径
                var enumDefault = ConvertEnumToComNamespace(context.EnumValueName, context.ComNamespace);
                sb.AppendLine($"            var {context.Parameter.Name}Obj = {context.Parameter.Name}?.EnumConvert({enumDefault}) ?? global::System.Type.Missing;");
            }
        }
        else if (context.IsObjectType)
        {
            sb.AppendLine($"            var {context.Parameter.Name}Obj = {context.Parameter.Name} != null ? (({context.ConstructType}){context.Parameter.Name}).InternalComObject : global::System.Type.Missing;");
        }
        else
        {
            // 普通可空参数
            if (context.ConvertToInteger)
                sb.AppendLine($"            var {context.Parameter.Name}Obj = {context.Parameter.Name} != null ? {context.Parameter.Name}.ConvertToInt() : 0;");
            else
                sb.AppendLine($"            var {context.Parameter.Name}Obj = {context.Parameter.Name} != null ? (object){context.Parameter.Name} : global::System.Type.Missing;");
        }
    }

    /// <summary>
    /// 生成非可空参数对象处理逻辑
    /// </summary>
    private void GenerateNonNullableParameterObject(StringBuilder sb, ParameterProcessingContext context)
    {
        ArgumentNullExceptionExtensions.ThrowIfNull(sb, nameof(sb));
        ArgumentNullExceptionExtensions.ThrowIfNull(context, nameof(context));

        if (context.IsEnumType)
        {
            // 枚举参数
            if (context.ConvertToInteger)
                sb.AppendLine($"            var {context.Parameter.Name}Obj = (int){context.Parameter.Name};");
            else
            {
                // 对于枚举类型，EnumValueName 包含完整的枚举值路径（如 "ComObjectWrapTest.WdSaveOptions.wdPromptToSaveChanges"）
                // 我们需要将其转换为 COM 命名空间的路径
                var enumDefault = ConvertEnumToComNamespace(context.EnumValueName, context.ComNamespace);

                // 临时调试：写入文件以查看值
                try
                {
                    var debugFile = Path.Combine(Path.GetTempPath(), $"enum_debug_{context.Parameter.Name}.txt");
                    File.WriteAllText(debugFile, $"EnumValueName: {context.EnumValueName}\nComNamespace: {context.ComNamespace}\nConverted: {enumDefault}");
                }
                catch { }

                sb.AppendLine($"            var {context.Parameter.Name}Obj = {context.Parameter.Name}.EnumConvert({enumDefault});");
            }
        }
        else if (context.IsObjectType)
        {
            // COM对象参数
            sb.AppendLine($"            var {context.Parameter.Name}Obj = (({context.ConstructType}){context.Parameter.Name}).InternalComObject;");
        }
        else if (context.ParameterType == "object")
        {
            // object类型参数
            if (context.ConvertToInteger)
                sb.AppendLine($"            var {context.Parameter.Name}Obj = {context.Parameter.Name}.ConvertToInt();");
            else
                sb.AppendLine($"            var {context.Parameter.Name}Obj = {context.Parameter.Name} ?? global::System.Type.Missing;");
        }
    }

    /// <summary>
    /// 将枚举值路径转换为 COM 命名空间的路径
    /// </summary>
    /// <param name="enumValuePath">枚举值路径，如 "ComObjectWrapTest.WdSaveOptions.wdPromptToSaveChanges"</param>
    /// <param name="comNamespace">COM 命名空间，如 "MsWord"</param>
    /// <returns>COM 命名空间的枚举路径，如 "MsWord.WdSaveOptions.wdPromptToSaveChanges"</returns>
    private static string ConvertEnumToComNamespace(string enumValuePath, string comNamespace)
    {
        if (string.IsNullOrEmpty(enumValuePath))
            return string.Empty;

        // 如果没有提供 COM 命名空间，返回原始值
        if (string.IsNullOrEmpty(comNamespace))
            return enumValuePath;

        // 枚举值路径格式：NamespaceA.NamespaceB.EnumName.MemberName
        // 我们需要将前面的命名空间替换为 comNamespace，保留 EnumName.MemberName
        var lastDotIndex = enumValuePath.LastIndexOf('.');
        if (lastDotIndex <= 0)
            return enumValuePath;

        var secondLastDotIndex = enumValuePath.LastIndexOf('.', lastDotIndex - 1);
        if (secondLastDotIndex <= 0)
            return enumValuePath;

        // 提取 EnumName.MemberName
        var enumNameAndMember = enumValuePath.Substring(secondLastDotIndex + 1);

        return $"{comNamespace}.{enumNameAndMember}";
    }

    /// <summary>
    /// 将枚举类型路径转换为 COM 命名空间的类型路径
    /// </summary>
    /// <param name="enumTypePath">枚举类型路径，如 "MudTools.OfficeInterop.Vbe.vbext_ProcKind"</param>
    /// <param name="comNamespace">COM 命名空间，如 "MsVb"</param>
    /// <returns>COM 命名空间的枚举类型路径，如 "MsVb.vbext_ProcKind"</returns>
    private static string ConvertEnumTypeToComNamespace(string enumTypePath, string comNamespace)
    {
        if (string.IsNullOrEmpty(enumTypePath))
            return string.Empty;

        // 如果没有提供 COM 命名空间，返回原始值
        if (string.IsNullOrEmpty(comNamespace))
            return enumTypePath;

        // 枚举类型路径格式：NamespaceA.NamespaceB.EnumName
        // 我们需要将前面的命名空间替换为 comNamespace，保留 EnumName
        var lastDotIndex = enumTypePath.LastIndexOf('.');
        if (lastDotIndex <= 0)
            return enumTypePath;

        // 提取 EnumName
        var enumName = enumTypePath.Substring(lastDotIndex + 1);

        return $"{comNamespace}.{enumName}";
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
        ArgumentNullExceptionExtensions.ThrowIfNull(sb, nameof(sb));
        ArgumentNullExceptionExtensions.ThrowIfNull(interfaceDeclaration, nameof(interfaceDeclaration));
        ArgumentNullExceptionExtensions.ThrowIfNull(interfaceSymbol, nameof(interfaceSymbol));

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
            var context = ParameterProcessingContext.CreateForMethodParameter(
                param, interfaceSymbol, interfaceDeclaration);

            // 处理out参数
            if (context.IsOut)
            {
                GenerateOutParameterVariable(sb, context);
                continue;
            }

            // 生成参数对象处理逻辑
            GenerateParameterObject(sb, context);
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
        var callParameters = GenerateCallParameters(methodSymbol, interfaceDeclaration, interfaceSymbol);

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
    private string GenerateCallParameters(IMethodSymbol methodSymbol, InterfaceDeclarationSyntax interfaceDeclaration, INamedTypeSymbol interfaceSymbol)
    {
        var parameters = new List<string>();

        foreach (var param in methodSymbol.Parameters)
        {
            var context = ParameterProcessingContext.CreateForMethodParameter(
                param, interfaceSymbol, interfaceDeclaration);

            if (context.IsOut)
            {
                // out参数需要添加out关键字
                parameters.Add($"out {context.Parameter.Name}Obj");
            }
            else if (context.ParameterType.EndsWith("?", StringComparison.Ordinal) ||
                     context.IsEnumType || context.IsObjectType ||
                     context.HasConvertTriState || context.ParameterType == "object")
            {
                parameters.Add($"{context.Parameter.Name}Obj");
            }
            else
            {
                parameters.Add(context.Parameter.Name);
            }
        }

        return string.Join(", ", parameters);
    }

    /// <summary>
    /// 生成out参数的返回值赋值
    /// </summary>
    private void GenerateOutParameterAssignment(StringBuilder sb, IMethodSymbol methodSymbol, InterfaceDeclarationSyntax interfaceDeclaration, INamedTypeSymbol interfaceSymbol)
    {
        ArgumentNullExceptionExtensions.ThrowIfNull(sb, nameof(sb));
        ArgumentNullExceptionExtensions.ThrowIfNull(methodSymbol, nameof(methodSymbol));
        ArgumentNullExceptionExtensions.ThrowIfNull(interfaceDeclaration, nameof(interfaceDeclaration));
        ArgumentNullExceptionExtensions.ThrowIfNull(interfaceSymbol, nameof(interfaceSymbol));

        foreach (var param in methodSymbol.Parameters)
        {
            if (param.RefKind != RefKind.Out)
                continue;

            var context = ParameterProcessingContext.CreateForMethodParameter(
                param, interfaceSymbol, interfaceDeclaration);

            if (context.IsEnumType)
            {
                if (context.ConvertToInteger)
                {
                    // 枚举转整数的out参数
                    sb.AppendLine($"                {context.Parameter.Name} = ({context.ParameterType}){context.Parameter.Name}Obj;");
                }
                else
                {
                    // 普通枚举out参数 - 使用泛型EnumConvert方法
                    var comEnumType = ConvertEnumTypeToComNamespace(context.ParameterType, context.ComNamespace);
                    sb.AppendLine($"                {context.Parameter.Name} = {context.Parameter.Name}Obj.EnumConvert<{comEnumType},{context.ParameterType}>();");
                }
            }
            else if (context.IsObjectType)
            {
                // COM对象out参数
                sb.AppendLine($"                {context.Parameter.Name} = new {context.ConstructType}({context.Parameter.Name}Obj);");
            }
            else
            {
                // 普通out参数
                sb.AppendLine($"                {context.Parameter.Name} = {context.Parameter.Name}Obj;");
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




    /// <summary>
    /// 生成异常处理逻辑
    /// </summary>
    private void GenerateExceptionHandling(StringBuilder sb, string methodName)
    {
        var operationDescription = GeneratorMessages.GetOperationDescription(methodName);

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
    /// 获取COM类名的Ordinal类型（移除接口前缀）
    /// </summary>
    protected string GetOrdinalComType(string ordinalComType)
    {
        return NamingHelper.GetOrdinalComType(ordinalComType);
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
