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

                var source = GenerateImplementationClass(interfaceDeclaration, interfaceSymbol);

                if (!string.IsNullOrEmpty(source))
                {
                    // 使用完全限定名生成唯一的 hintName
                    var fullyQualifiedName = interfaceSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    // 移除全局命名空间前缀并替换 . 为 _ 以符合文件名规范
                    var safeName = fullyQualifiedName
                        .TrimStart('G', '.', 'I')
                        .Replace(".", "_")
                        .Replace("global::", "");
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
            if (ShouldIgnoreMember(member))
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
}
