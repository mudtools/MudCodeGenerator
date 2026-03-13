// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.CodeGenerator 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Collections.Immutable;

namespace Mud.HttpUtils;

/// <summary>
/// FormContent 源代码生成器，用于为标记了 [FormContent] 特性的类生成 GetFormDataContentAsync 方法。
/// </summary>
[Generator(LanguageNames.CSharp)]
internal class FormContentGenerator : TransitiveCodeGenerator
{
    private const string FormContentAttributeName = "FormContentAttribute";
    private const string JsonPropertyNameAttributeName = "JsonPropertyNameAttribute";
    private const string FilePathAttributeName = "FilePathAttribute";

    /// <inheritdoc/>
    protected override Collection<string> GetFileUsingNameSpaces()
    {
        return
        [
            "System",
            "System.IO",
            "System.Net.Http",
            "System.Threading",
            "System.Threading.Tasks",
            "Mud.HttpUtils"
        ];
    }

    /// <inheritdoc/>
    public override void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // 查找标记了[FormContent]的类声明
        var formContentClasses = GetClassDeclarationProvider<ClassDeclarationSyntax>(context, [FormContentAttributeName]);

        // 获取编译信息和分析器配置选项
        var compilationWithOptions = context.CompilationProvider
            .Combine(context.AnalyzerConfigOptionsProvider);

        // 组合所有需要的数据：编译信息、类声明、配置选项
        var completeDataProvider = compilationWithOptions.Combine(formContentClasses);

        // 注册源代码生成器
        context.RegisterSourceOutput(completeDataProvider,
            (ctx, provider) => ExecuteGenerator(
                compilation: provider.Left.Left,
                formContentClasses: provider.Right.Where(c => c != null).ToImmutableArray()!,
                context: ctx));
    }

    /// <summary>
    /// 执行源代码生成逻辑
    /// </summary>
    /// <param name="compilation">编译信息</param>
    /// <param name="formContentClasses">FormContent 类声明数组</param>
    /// <param name="context">源代码生成上下文</param>
    private void ExecuteGenerator(
        Compilation compilation,
        ImmutableArray<ClassDeclarationSyntax> formContentClasses,
        SourceProductionContext context)
    {
        if (formContentClasses.IsDefaultOrEmpty)
            return;

        // 用于跟踪已处理的类，避免重复生成
        var processedClasses = new HashSet<string>();

        foreach (var classDecl in formContentClasses)
        {
            if (classDecl == null)
                continue;

            try
            {
                var semanticModel = compilation.GetSemanticModel(classDecl.SyntaxTree);
                var classSymbol = semanticModel.GetDeclaredSymbol(classDecl);

                if (classSymbol == null)
                    continue;

                // 创建类的唯一标识符（包含完整限定名）
                var classKey = classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                // 如果已经处理过这个类，则跳过
                if (!processedClasses.Add(classKey))
                    continue;

                // 获取所有标记了 [JsonPropertyName] 的属性
                var properties = GetJsonPropertyProperties(classSymbol);

                // 验证 [FilePath] 属性数量
                if (!ValidateFilePathAttribute(classSymbol, properties, context))
                {
                    // 验证失败，跳过代码生成
                    continue;
                }

                // 生成代码
                var generatedCode = GenerateFormContentCode(classDecl, classSymbol, properties);

                if (!string.IsNullOrEmpty(generatedCode))
                {
                    var fileName = $"{classSymbol.Name}.FormContent.g.cs";
                    context.AddSource(fileName, generatedCode);
                }
            }
            catch (Exception ex)
            {
                // 报告生成错误
                context.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.FormContentGenerationError,
                    classDecl.GetLocation(),
                    classDecl.Identifier.Text,
                    ex.Message));
            }
        }
    }

    /// <summary>
    /// 验证 [FilePath] 属性数量
    /// </summary>
    /// <param name="classSymbol">类符号</param>
    /// <param name="properties">属性信息列表</param>
    /// <param name="context">源代码生成上下文</param>
    /// <returns>验证是否通过</returns>
    private bool ValidateFilePathAttribute(
        INamedTypeSymbol classSymbol,
        List<PropertyInfo> properties,
        SourceProductionContext context)
    {
        var filePathCount = properties.Count(p => p.HasFilePath);

        if (filePathCount == 0)
        {
            // 没有找到 [FilePath] 属性
            context.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.FormContentNoFilePathAttribute,
                Location.None,
                classSymbol.Name));
            return false;
        }

        if (filePathCount > 1)
        {
            // 找到多个 [FilePath] 属性
            var filePathProperties = properties.Where(p => p.HasFilePath).Select(p => p.Name);
            context.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.FormContentMultipleFilePathAttributes,
                Location.None,
                classSymbol.Name,
                string.Join(", ", filePathProperties)));
            return false;
        }

        return true;
    }

    /// <summary>
    /// 生成 FormContent 代码
    /// </summary>
    /// <param name="classDecl">类声明语法</param>
    /// <param name="classSymbol">类符号</param>
    /// <param name="properties">属性信息列表</param>
    /// <returns>生成的代码</returns>
    private string GenerateFormContentCode(
        ClassDeclarationSyntax classDecl,
        INamedTypeSymbol classSymbol,
        List<PropertyInfo> properties)
    {
        var namespaceName = GetNamespace(classDecl);
        var className = classSymbol.Name;

        // 查找 byte[] 类型的属性名
        var byteArrayPropertyName = GetByteArrayPropertyName(classSymbol);

        var sb = new StringBuilder();

        // 生成文件头
        GenerateFileHeader(sb);

        // 生成命名空间和类
        sb.AppendLine($"namespace {namespaceName};");
        sb.AppendLine();
        sb.AppendLine($"public partial class {className}");
        sb.AppendLine("{");

        // 生成 GetFormDataContentAsync 方法
        GenerateGetFormDataContentAsyncMethod(sb, properties, byteArrayPropertyName);

        sb.AppendLine("}");

        return sb.ToString();
    }

    /// <summary>
    /// 获取类的命名空间
    /// </summary>
    /// <param name="classDecl">类声明语法</param>
    /// <returns>命名空间名称</returns>
    private string GetNamespace(ClassDeclarationSyntax classDecl)
    {
        if (classDecl.Parent is NamespaceDeclarationSyntax namespaceDecl)
        {
            return namespaceDecl.Name.ToString();
        }

        if (classDecl.Parent is FileScopedNamespaceDeclarationSyntax fileScopedNamespace)
        {
            return fileScopedNamespace.Name.ToString();
        }

        return "Global";
    }

    /// <summary>
    /// 获取所有标记了 [JsonPropertyName] 的属性信息
    /// </summary>
    /// <param name="classSymbol">类符号</param>
    /// <returns>属性信息列表</returns>
    private List<PropertyInfo> GetJsonPropertyProperties(INamedTypeSymbol classSymbol)
    {
        var properties = new List<PropertyInfo>();

        foreach (var member in classSymbol.GetMembers())
        {
            if (member is not IPropertySymbol propertySymbol)
                continue;

            // 查找 [JsonPropertyName] 特性
            var jsonPropertyAttr = propertySymbol.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.Name == JsonPropertyNameAttributeName ||
                                     a.AttributeClass?.Name == "JsonPropertyName");

            if (jsonPropertyAttr == null)
                continue;

            // 获取 JSON 属性名
            var jsonPropertyName = GetJsonPropertyName(jsonPropertyAttr);

            // 检查是否有 [FilePath] 特性
            var hasFilePathAttr = propertySymbol.GetAttributes()
                .Any(a => a.AttributeClass?.Name == FilePathAttributeName ||
                          a.AttributeClass?.Name == "FilePath");

            // 判断是否为可空类型
            var isNullable = propertySymbol.Type.NullableAnnotation == NullableAnnotation.Annotated;

            properties.Add(new PropertyInfo(
                propertySymbol.Name,
                jsonPropertyName,
                propertySymbol.Type,
                hasFilePathAttr,
                isNullable));
        }

        return properties;
    }

    /// <summary>
    /// 查找类中 byte[] 类型的属性名
    /// </summary>
    /// <param name="classSymbol">类符号</param>
    /// <returns>byte[] 属性名，如果没有则返回 null</returns>
    private string? GetByteArrayPropertyName(INamedTypeSymbol classSymbol)
    {
        foreach (var member in classSymbol.GetMembers())
        {
            if (member is not IPropertySymbol propertySymbol)
                continue;

            // 判断是否为 byte[] 类型
            if (propertySymbol.Type is IArrayTypeSymbol arrayType &&
                arrayType.ElementType.SpecialType == SpecialType.System_Byte)
            {
                return propertySymbol.Name;
            }
        }

        return null;
    }

    /// <summary>
    /// 从 JsonPropertyName 特性中获取属性名
    /// </summary>
    /// <param name="attribute">特性数据</param>
    /// <returns>JSON 属性名</returns>
    private string GetJsonPropertyName(AttributeData attribute)
    {
        // 尝试从构造函数参数获取
        if (attribute.ConstructorArguments.Length > 0)
        {
            return attribute.ConstructorArguments[0].Value?.ToString() ?? "";
        }

        // 尝试从命名参数获取
        var nameArg = attribute.NamedArguments
            .FirstOrDefault(a => a.Key.Equals("name", StringComparison.OrdinalIgnoreCase));

        return nameArg.Value.Value?.ToString() ?? "";
    }

    /// <summary>
    /// 生成 GetFormDataContentAsync 方法
    /// </summary>
    /// <param name="sb">字符串构建器</param>
    /// <param name="properties">属性信息列表</param>
    /// <param name="byteArrayPropertyName">byte[] 类型属性的名称，如果没有则为 null</param>
    private void GenerateGetFormDataContentAsyncMethod(StringBuilder sb, List<PropertyInfo> properties, string? byteArrayPropertyName)
    {
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// 构建表单数据内容");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    /// <param name=\"cancellationToken\">取消令牌</param>");
        sb.AppendLine("    /// <returns>表单数据内容</returns>");
        sb.AppendLine("    public async Task<MultipartFormDataContent> GetFormDataContentAsync(CancellationToken cancellationToken = default)");
        sb.AppendLine("    {");
        sb.AppendLine("        var formData = new MultipartFormDataContent();");
        sb.AppendLine();

        // 生成普通属性处理代码
        foreach (var prop in properties.Where(p => !p.HasFilePath))
        {
            GeneratePropertyAddCode(sb, prop);
        }

        // 生成文件属性处理代码
        foreach (var prop in properties.Where(p => p.HasFilePath))
        {
            GenerateFilePropertyAddCode(sb, prop, byteArrayPropertyName);
        }

        sb.AppendLine();

        // 如果有 byte[] 属性，使用 Task.FromResult 返回
        if (!string.IsNullOrEmpty(byteArrayPropertyName))
        {
            sb.AppendLine("        return await Task.FromResult(formData);");
        }
        else
        {
            sb.AppendLine("        return formData;");
        }

        sb.AppendLine("    }");
    }

    /// <summary>
    /// 生成普通属性的添加代码
    /// </summary>
    /// <param name="sb">字符串构建器</param>
    /// <param name="prop">属性信息</param>
    private void GeneratePropertyAddCode(StringBuilder sb, PropertyInfo prop)
    {
        var propertyName = prop.Name;
        var jsonName = prop.JsonName;
        var type = prop.Type;

        // 判断是否为字符串类型
        var isString = type.SpecialType == SpecialType.System_String;

        // 判断是否为可空字符串
        var isNullableString = isString && prop.IsNullable;

        // 判断是否为值类型
        var isValueType = type.IsValueType && !isString;

        if (isString)
        {
            // 字符串类型：添加非空判断
            sb.AppendLine($"        if (!string.IsNullOrEmpty({propertyName}))");
            sb.AppendLine($"            formData.Add(new StringContent({propertyName}), \"{jsonName}\");");
        }
        else if (isValueType)
        {
            // 值类型：直接添加
            sb.AppendLine($"        formData.Add(new StringContent({propertyName}.ToString()), \"{jsonName}\");");
        }
        else
        {
            // 引用类型：添加 null 检查
            sb.AppendLine($"        if ({propertyName} != null)");
            sb.AppendLine($"            formData.Add(new StringContent({propertyName}.ToString()), \"{jsonName}\");");
        }
    }

    /// <summary>
    /// 生成文件属性的添加代码
    /// </summary>
    /// <param name="sb">字符串构建器</param>
    /// <param name="prop">属性信息</param>
    /// <param name="byteArrayPropertyName">byte[] 类型属性的名称，如果没有则为 null</param>
    private void GenerateFilePropertyAddCode(StringBuilder sb, PropertyInfo prop, string? byteArrayPropertyName)
    {
        var propertyName = prop.Name;
        var jsonName = prop.JsonName;

        sb.AppendLine($"        if (!string.IsNullOrEmpty({propertyName}))");
        sb.AppendLine("        {");

        if (!string.IsNullOrEmpty(byteArrayPropertyName))
        {
            // 如果有 byte[] 属性，使用 CreateFileContent 方法
            sb.AppendLine($"            var fileContent = HttpClientUtils.CreateFileContent({propertyName}, {byteArrayPropertyName});");
        }
        else
        {
            // 否则使用异步方法读取文件
            sb.AppendLine($"            var fileContent = await HttpClientUtils.GetByteArrayContentAsync({propertyName});");
        }

        sb.AppendLine($"            formData.Add(fileContent, \"{jsonName}\", Path.GetFileName({propertyName}));");
        sb.AppendLine("        }");
    }

    /// <summary>
    /// 属性信息
    /// </summary>
    private sealed class PropertyInfo
    {
        public string Name { get; }
        public string JsonName { get; }
        public ITypeSymbol Type { get; }
        public bool HasFilePath { get; }
        public bool IsNullable { get; }

        public PropertyInfo(string name, string jsonName, ITypeSymbol type, bool hasFilePath, bool isNullable)
        {
            Name = name;
            JsonName = jsonName;
            Type = type;
            HasFilePath = hasFilePath;
            IsNullable = isNullable;
        }
    }
}
