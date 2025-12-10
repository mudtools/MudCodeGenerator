// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.CodeGenerator 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Collections.Immutable;
using System.Text;

namespace Mud.ServiceCodeGenerator.ComWrapSourceGenerator;

/// <summary>
/// COM对象包装源代码生成器
/// </summary>
/// <remarks>
/// 为标记了ComObjectWrap特性的接口生成COM对象包装类，提供类型安全的COM对象访问
/// </remarks>
[Generator]
public class ComObjectWrapGenerator : TransitiveCodeGenerator
{
    private static readonly string[] KnownPrefixes = { "IWord", "IExcel", "IOffice", "IPowerPoint", "IVbe" };

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
    protected virtual string[] ComWrapAttributeNames() => ComWrapGeneratorConstants.ComObjectWrapAttributeNames;

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
        var className = GetImplementationClassName(interfaceName);

        // 添加Imps命名空间
        var impNamespace = $"{namespaceName}.Imps";

        var sb = new StringBuilder();
        GenerateFileHeader(sb);

        sb.AppendLine();
        sb.AppendLine($"namespace {impNamespace}");
        sb.AppendLine("{");
        sb.AppendLine($"    /// <summary>");
        sb.AppendLine($"    /// {interfaceName} 的COM对象包装实现类");
        sb.AppendLine($"    /// </summary>");
        sb.AppendLine($"    {CompilerGeneratedAttribute}");
        sb.AppendLine($"    {GeneratedCodeAttribute}");
        sb.AppendLine($"    internal class {className} : {interfaceName}");
        sb.AppendLine("    {");

        // 生成字段
        GenerateFields(sb, interfaceDeclaration);

        // 生成构造函数
        GenerateConstructor(sb, className, interfaceDeclaration);

        // 生成属性
        GenerateProperties(sb, interfaceSymbol, interfaceDeclaration);

        // 生成方法
        GenerateMethods(sb, interfaceSymbol, interfaceDeclaration);

        // 生成IDisposable实现
        GenerateIDisposableImplementation(sb, interfaceDeclaration);

        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    #region Generate Implementation Members

    /// <summary>
    /// 生成私有字段
    /// </summary>
    /// <param name="sb">字符串构建器</param>
    /// <param name="interfaceDeclaration">接口声明语法</param>
    private void GenerateFields(StringBuilder sb, InterfaceDeclarationSyntax interfaceDeclaration)
    {
        var comNamespace = GetComNamespace(interfaceDeclaration);
        var comClassName = GetComClassName(interfaceDeclaration);

        sb.AppendLine($"        internal {comNamespace}.{comClassName}? {PrivateFieldNamingHelper.GeneratePrivateFieldName(comClassName)};");
        sb.AppendLine("        private bool _disposedValue;");
        sb.AppendLine();
    }

    /// <summary>
    /// 生成构造函数
    /// </summary>
    /// <param name="sb">字符串构建器</param>
    /// <param name="className">类名</param>
    /// <param name="interfaceDeclaration">接口声明语法</param>
    private void GenerateConstructor(StringBuilder sb, string className, InterfaceDeclarationSyntax interfaceDeclaration)
    {
        var comNamespace = GetComNamespace(interfaceDeclaration);
        var comClassName = GetComClassName(interfaceDeclaration);
        sb.AppendLine($"        internal {className}({comNamespace}.{comClassName} comObject)");
        sb.AppendLine("        {");
        sb.AppendLine($"            {PrivateFieldNamingHelper.GeneratePrivateFieldName(comClassName)} = comObject ?? throw new ArgumentNullException(nameof(comObject));");
        sb.AppendLine("            _disposedValue = false;");
        sb.AppendLine("        }");
        sb.AppendLine();
    }

    /// <summary>
    /// 生成属性实现
    /// </summary>
    /// <param name="sb">字符串构建器</param>
    /// <param name="interfaceSymbol">接口符号</param>
    /// <param name="interfaceDeclaration">接口声明语法</param>
    private void GenerateProperties(StringBuilder sb, INamedTypeSymbol interfaceSymbol, InterfaceDeclarationSyntax interfaceDeclaration)
    {
        sb.AppendLine("        #region 属性");
        sb.AppendLine();

        foreach (var member in interfaceSymbol.GetMembers().OfType<IPropertySymbol>())
        {
            if (!ShouldIgnoreMember(member))
            {
                GenerateProperty(sb, member, interfaceDeclaration);
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
        var propertyName = propertySymbol.Name;
        var propertyType = propertySymbol.Type.ToDisplayString();
        var isEnumType = IsEnumType(propertySymbol.Type);
        var isObjectType = IsComObjectType(propertySymbol.Type);
        var defaultValue = GetDefaultValue(interfaceDeclaration, propertySymbol, propertySymbol.Type);
        var comClassName = GetComClassName(interfaceDeclaration);

        if (isEnumType)
        {
            GenerateEnumProperty(sb, propertySymbol, interfaceDeclaration, defaultValue);
        }
        else if (isObjectType)
        {
            GenerateComObjectProperty(sb, propertySymbol, interfaceDeclaration, comClassName);
        }
        else if (propertyType == "bool")
        {
            GenerateBoolProperty(sb, propertySymbol, interfaceDeclaration, comClassName);
        }
        else
        {
            GenerateObjectProperty(sb, propertySymbol, interfaceDeclaration, comClassName);
        }
    }

    /// <summary>
    /// 生成普通对象属性实现
    /// </summary>
    /// <param name="sb">字符串构建器</param>
    /// <param name="propertySymbol">属性符号</param>
    /// <param name="interfaceDeclaration">接口声明语法</param>
    /// <param name="comClassName">COM类名</param>
    private void GenerateObjectProperty(StringBuilder sb, IPropertySymbol propertySymbol, InterfaceDeclarationSyntax interfaceDeclaration, string comClassName)
    {
        var propertyName = propertySymbol.Name;
        var propertyType = propertySymbol.Type.ToDisplayString();

        sb.AppendLine($"        {GeneratedCodeAttribute}");
        sb.AppendLine($"        public {propertyType} {propertyName}");
        sb.AppendLine("        {");
        sb.AppendLine($"            get => {PrivateFieldNamingHelper.GeneratePrivateFieldName(comClassName)}?.{propertyName};");

        if (propertySymbol.SetMethod != null)
        {
            sb.AppendLine("            set");
            sb.AppendLine("            {");
            if (propertyType.EndsWith("?", StringComparison.Ordinal))
            {
                sb.AppendLine($"                if ({PrivateFieldNamingHelper.GeneratePrivateFieldName(comClassName)} != null && value != null)");
                sb.AppendLine($"                    {PrivateFieldNamingHelper.GeneratePrivateFieldName(comClassName)}.{propertyName} = value.Value;");
            }
            else
            {
                sb.AppendLine($"                if ({PrivateFieldNamingHelper.GeneratePrivateFieldName(comClassName)} != null)");
                sb.AppendLine($"                    {PrivateFieldNamingHelper.GeneratePrivateFieldName(comClassName)}.{propertyName} = value;");
            }
            sb.AppendLine("            }");
        }
        sb.AppendLine("        }");
        sb.AppendLine();
    }

    /// <summary>
    /// 生成COM对象属性实现
    /// </summary>
    /// <param name="sb">字符串构建器</param>
    /// <param name="propertySymbol">属性符号</param>
    /// <param name="interfaceDeclaration">接口声明语法</param>
    /// <param name="comClassName">COM类名</param>
    private void GenerateComObjectProperty(StringBuilder sb, IPropertySymbol propertySymbol, InterfaceDeclarationSyntax interfaceDeclaration, string comClassName)
    {
        var propertyName = propertySymbol.Name;
        var propertyType = propertySymbol.Type.ToDisplayString();
        var objectType = StringExtensions.RemoveInterfacePrefix(propertyType);
        var constructType = objectType;
        if (constructType.EndsWith("?", StringComparison.OrdinalIgnoreCase))
            constructType = constructType.Substring(0, constructType.Length - 1);

        sb.AppendLine($"        {GeneratedCodeAttribute}");
        sb.AppendLine($"        public {propertyType} {propertyName}");
        sb.AppendLine("        {");
        sb.AppendLine("             get");
        sb.AppendLine("             {");
        sb.AppendLine($"                var comObj = {PrivateFieldNamingHelper.GeneratePrivateFieldName(comClassName)}?.{propertyName};");
        sb.AppendLine("                  if (comObj == null)");
        sb.AppendLine("                       return null;");
        sb.AppendLine($"                return new {constructType}(comObj);");
        sb.AppendLine("             }");

        if (propertySymbol.SetMethod != null)
        {
            sb.AppendLine("             set");
            sb.AppendLine("             {");
            sb.AppendLine($"                if ({PrivateFieldNamingHelper.GeneratePrivateFieldName(comClassName)} != null && value != null)");
            sb.AppendLine($"                {{");
            sb.AppendLine($"                    var comObj = value.{PrivateFieldNamingHelper.GeneratePrivateFieldName(constructType)};");
            sb.AppendLine($"                    {PrivateFieldNamingHelper.GeneratePrivateFieldName(comClassName)}.{propertyName} = comObj;");
            sb.AppendLine($"                }}");
            sb.AppendLine("             }");
        }
        sb.AppendLine("        }");
        sb.AppendLine();
    }

    /// <summary>
    /// 生成布尔属性实现
    /// </summary>
    /// <param name="sb">字符串构建器</param>
    /// <param name="propertySymbol">属性符号</param>
    /// <param name="interfaceDeclaration">接口声明语法</param>
    /// <param name="comClassName">COM类名</param>
    private void GenerateBoolProperty(StringBuilder sb, IPropertySymbol propertySymbol, InterfaceDeclarationSyntax interfaceDeclaration, string comClassName)
    {
        var propertyName = propertySymbol.Name;
        var propertyType = propertySymbol.Type.ToDisplayString();

        sb.AppendLine($"        {GeneratedCodeAttribute}");
        sb.AppendLine($"        public {propertyType} {propertyName}");
        sb.AppendLine("        {");
        sb.AppendLine($"            get => {PrivateFieldNamingHelper.GeneratePrivateFieldName(comClassName)}?.{propertyName} ?? false;");

        if (propertySymbol.SetMethod != null)
        {
            sb.AppendLine("            set");
            sb.AppendLine("            {");
            if (propertyType.EndsWith("?", StringComparison.Ordinal))
            {
                sb.AppendLine($"                if ({PrivateFieldNamingHelper.GeneratePrivateFieldName(comClassName)} != null && value != null)");
                sb.AppendLine($"                    {PrivateFieldNamingHelper.GeneratePrivateFieldName(comClassName)}.{propertyName} = value.Value;");
            }
            else
            {
                sb.AppendLine($"                if ({PrivateFieldNamingHelper.GeneratePrivateFieldName(comClassName)} != null)");
                sb.AppendLine($"                    {PrivateFieldNamingHelper.GeneratePrivateFieldName(comClassName)}.{propertyName} = value;");
            }
            sb.AppendLine("            }");
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
    private void GenerateEnumProperty(StringBuilder sb, IPropertySymbol propertySymbol, InterfaceDeclarationSyntax interfaceDeclaration, string defaultValue)
    {
        var propertyName = propertySymbol.Name;
        var propertyType = propertySymbol.Type.ToDisplayString();
        var comNamespace = GetComNamespace(interfaceDeclaration);
        var comClassName = GetComClassName(interfaceDeclaration);

        sb.AppendLine($"        {GeneratedCodeAttribute}");
        sb.AppendLine($"        public {propertyType} {propertyName}");
        sb.AppendLine("        {");
        sb.AppendLine("            get");
        sb.AppendLine("            {");
        sb.AppendLine($"                if({PrivateFieldNamingHelper.GeneratePrivateFieldName(comClassName)} != null)");
        sb.AppendLine($"                   return {PrivateFieldNamingHelper.GeneratePrivateFieldName(comClassName)}.{propertyName}.EnumConvert({defaultValue});");
        sb.AppendLine($"                return {defaultValue};");
        sb.AppendLine("             }");

        if (propertySymbol.SetMethod != null)
        {
            sb.AppendLine("            set");
            sb.AppendLine("            {");
            sb.AppendLine($"                if ({PrivateFieldNamingHelper.GeneratePrivateFieldName(comClassName)} != null)");
            sb.AppendLine($"                    {PrivateFieldNamingHelper.GeneratePrivateFieldName(comClassName)}.{propertyName} = value.EnumConvert({comNamespace}.{defaultValue});");
            sb.AppendLine("            }");
        }
        sb.AppendLine("        }");
        sb.AppendLine();
    }

    /// <summary>
    /// 生成方法实现
    /// </summary>
    /// <param name="sb">字符串构建器</param>
    /// <param name="interfaceSymbol">接口符号</param>
    private void GenerateMethods(StringBuilder sb, INamedTypeSymbol interfaceSymbol, InterfaceDeclarationSyntax interfaceDeclaration)
    {
        sb.AppendLine("        #region 方法实现");
        sb.AppendLine();

        foreach (var member in interfaceSymbol.GetMembers().OfType<IMethodSymbol>())
        {
            if (member.MethodKind == MethodKind.Ordinary && !ShouldIgnoreMember(member))
            {
                GenerateMethod(sb, member, interfaceDeclaration);
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
    private void GenerateMethod(StringBuilder sb, IMethodSymbol methodSymbol, InterfaceDeclarationSyntax interfaceDeclaration)
    {
        var methodName = methodSymbol.Name;
        var returnType = methodSymbol.ReturnType.ToDisplayString();
        var parameters = string.Join(", ", methodSymbol.Parameters.Select(p => $"{p.Type.ToDisplayString()} {p.Name}"));
        var comClassName = GetComClassName(interfaceDeclaration);

        sb.AppendLine($"        {GeneratedCodeAttribute}");
        sb.AppendLine($"        public {returnType} {methodName}({parameters})");
        sb.AppendLine("        {");

        // 对于带参数的方法，添加参数检查
        if (methodSymbol.Parameters.Length > 0)
        {
            sb.AppendLine($"            if ({PrivateFieldNamingHelper.GeneratePrivateFieldName(comClassName)} == null)");
            sb.AppendLine($"                throw new ObjectDisposedException(nameof({PrivateFieldNamingHelper.GeneratePrivateFieldName(comClassName)}));");
            sb.AppendLine();
        }

        sb.AppendLine("            try");
        sb.AppendLine("            {");

        if (returnType == "void")
        {
            sb.AppendLine($"                {PrivateFieldNamingHelper.GeneratePrivateFieldName(comClassName)}?.{methodName}({string.Join(", ", methodSymbol.Parameters.Select(p => p.Name))});");
        }
        else if (returnType == "IWordRange?")
        {
            sb.AppendLine($"                var comObj = {PrivateFieldNamingHelper.GeneratePrivateFieldName(comClassName)}.{methodName}({string.Join(", ", methodSymbol.Parameters.Select(p => p.Name))});");
            sb.AppendLine("                if (comObj == null)");
            sb.AppendLine("                    return null;");
            sb.AppendLine("                return new WordRange(comObj);");
        }
        else
        {
            sb.AppendLine($"                return {PrivateFieldNamingHelper.GeneratePrivateFieldName(comClassName)}.{methodName}({string.Join(", ", methodSymbol.Parameters.Select(p => p.Name))});");
        }

        sb.AppendLine("            }");
        sb.AppendLine("            catch (Exception ex)");
        sb.AppendLine("            {");
        sb.AppendLine($"                throw new InvalidOperationException(\"执行COM对象的{methodName}方法失败。\", ex);");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine();
    }

    /// <summary>
    /// 生成IDisposable接口实现
    /// </summary>
    /// <param name="sb">字符串构建器</param>
    private void GenerateIDisposableImplementation(StringBuilder sb, InterfaceDeclarationSyntax interfaceDeclaration)
    {
        var comClassName = GetComClassName(interfaceDeclaration);
        sb.AppendLine("        #region IDisposable 实现");
        sb.AppendLine($"        {GeneratedCodeAttribute}");
        sb.AppendLine("        protected virtual void Dispose(bool disposing)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (_disposedValue) return;");
        sb.AppendLine();
        sb.AppendLine($"            if (disposing && {PrivateFieldNamingHelper.GeneratePrivateFieldName(comClassName)} != null)");
        sb.AppendLine("            {");
        sb.AppendLine($"                Marshal.ReleaseComObject({PrivateFieldNamingHelper.GeneratePrivateFieldName(comClassName)});");
        sb.AppendLine($"                {PrivateFieldNamingHelper.GeneratePrivateFieldName(comClassName)} = null;");
        sb.AppendLine("            }");
        sb.AppendLine();
        sb.AppendLine("            _disposedValue = true;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine($"        {GeneratedCodeAttribute}");
        sb.AppendLine("        public void Dispose()");
        sb.AppendLine("        {");
        sb.AppendLine("            Dispose(true);");
        sb.AppendLine("            GC.SuppressFinalize(this);");
        sb.AppendLine("        }");
        sb.AppendLine("        #endregion");
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// 通过语义分析判断类型是否为枚举类型
    /// </summary>
    /// <param name="typeSymbol">类型符号</param>
    /// <returns>如果是枚举类型返回true，否则返回false</returns>
    private bool IsEnumType(ITypeSymbol typeSymbol)
    {
        // 优先检查特性标注（保持向后兼容）
        if (typeSymbol.GetAttributes().Any(attr =>
            attr.AttributeClass?.Name == "ComPropertyWrapAttribute" &&
            attr.NamedArguments.Any(na => na.Key == "PropertyType" &&
                na.Value.Value?.ToString() == "PropertyType.EnumType")))
        {
            return true;
        }

        // 通过语义分析判断
        return typeSymbol.TypeKind == TypeKind.Enum;
    }

    /// <summary>
    /// 通过语义分析判断类型是否为COM对象类型
    /// </summary>
    /// <param name="typeSymbol">类型符号</param>
    /// <returns>如果是COM对象类型返回true，否则返回false</returns>
    private bool IsComObjectType(ITypeSymbol typeSymbol)
    {
        // 优先检查特性标注（保持向后兼容）
        if (typeSymbol.GetAttributes().Any(attr =>
            attr.AttributeClass?.Name == "ComPropertyWrapAttribute" &&
            attr.NamedArguments.Any(na => na.Key == "PropertyType" &&
                na.Value.Value?.ToString() == "PropertyType.ObjectType")))
        {
            return true;
        }

        // 通过语义分析判断
        var typeDisplayString = typeSymbol.ToDisplayString();

        // 1. 检查命名空间中是否包含常见的COM相关命名空间
        if (typeSymbol.ContainingNamespace?.ToDisplayString().Contains("Interop") == true)
            return true;

        // 2. 检查类型名是否以I开头（接口类型通常是COM对象）
        if (typeDisplayString.StartsWith("I", StringComparison.Ordinal) &&
            typeSymbol.TypeKind == TypeKind.Interface)
            return true;

        //// 3. 检查是否为引用类型且不是基础类型
        //if (typeSymbol.IsReferenceType &&
        //    !IsBasicType(typeSymbol) &&
        //    typeSymbol.TypeKind != TypeKind.Enum)
        //    return true;

        // 4. 检查特定模式，如COM接口的常见命名模式
        var typeName = typeSymbol.Name;
        if (typeName.StartsWith("I", StringComparison.Ordinal) && typeName.Length > 1 && char.IsUpper(typeName[1]))
            return true;

        return false;
    }


    /// <summary>
    /// 检查成员是否应该被忽略
    /// </summary>
    /// <param name="member">成员符号</param>
    /// <returns>如果应该忽略返回true，否则返回false</returns>
    private bool ShouldIgnoreMember(ISymbol member)
    {
        return member.GetAttributes().Any(attr => attr.AttributeClass?.Name == ComWrapGeneratorConstants.IgnoreGeneratorAttribute);
    }

    /// <summary>
    /// 获取属性的默认值
    /// </summary>
    /// <param name="interfaceDeclaration">接口声明语法</param>
    /// <param name="propertySymbol">属性符号</param>
    /// <param name="typeSymbol">类型符号</param>
    /// <returns>默认值字符串</returns>
    private string GetDefaultValue(InterfaceDeclarationSyntax interfaceDeclaration, IPropertySymbol propertySymbol, ITypeSymbol typeSymbol)
    {
        // 首先检查特性中显式指定的默认值
        var propertyDeclaration = interfaceDeclaration.DescendantNodes()
            .OfType<PropertyDeclarationSyntax>()
            .FirstOrDefault(p => p.Identifier.Text == propertySymbol.Name);

        if (propertyDeclaration != null)
        {
            var defaultValueArgument = propertyDeclaration.AttributeLists
                .SelectMany(al => al.Attributes)
                .Where(attr => ComWrapGeneratorConstants.ComPropertyWrapAttributeNames.Contains(attr.Name.ToString()))
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

    /// <summary>
    /// 从ComObjectWrap特性中获取COM命名空间
    /// </summary>
    /// <param name="interfaceDeclaration">接口声明语法</param>
    /// <returns>COM命名空间</returns>
    private string GetComNamespace(InterfaceDeclarationSyntax interfaceDeclaration)
    {
        var comObjectWrapAttribute = interfaceDeclaration.AttributeLists
            .SelectMany(al => al.Attributes)
            .FirstOrDefault(attr => ComWrapGeneratorConstants.ComObjectWrapAttributeNames.Contains(attr.Name.ToString()));

        if (comObjectWrapAttribute != null)
        {
            var namespaceArgument = comObjectWrapAttribute.ArgumentList?.Arguments
                .FirstOrDefault(arg =>
                    arg.NameEquals?.Name.Identifier.Text == "ComNamespace" ||
                    arg.Expression.ToString().Contains("ComNamespace"));

            if (namespaceArgument != null)
            {
                // 移除引号
                var namespaceValue = namespaceArgument.Expression.ToString();
                return namespaceValue.Trim('"');
            }
        }

        return ComWrapGeneratorConstants.DefaultComNamespace;
    }

    /// <summary>
    /// 从ComObjectWrap特性中获取COM类名
    /// </summary>
    /// <param name="interfaceDeclaration">接口声明语法</param>
    /// <returns>COM类名</returns>
    /// <remarks>
    /// 支持字符串字面量和nameof()表达式两种形式
    /// </remarks>
    private string GetComClassName(InterfaceDeclarationSyntax interfaceDeclaration)
    {
        var comObjectWrapAttribute = interfaceDeclaration.AttributeLists
            .SelectMany(al => al.Attributes)
            .FirstOrDefault(attr => ComWrapGeneratorConstants.ComObjectWrapAttributeNames.Contains(attr.Name.ToString()));

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

    #endregion
}