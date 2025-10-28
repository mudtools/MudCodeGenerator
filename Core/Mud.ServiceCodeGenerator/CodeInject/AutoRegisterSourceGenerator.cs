using Microsoft.CodeAnalysis.Text;
using System.Text;
using System.Text.RegularExpressions;

namespace Mud.ServiceCodeGenerator;

/// <summary>
/// 自动注册服务代码生成器
/// </summary>
[Generator(LanguageNames.CSharp)]
public class AutoRegisterSourceGenerator : TransitiveCodeGenerator
{
    private static class AttributeNames
    {
        public const string AutoRegister = "AutoRegisterAttribute";
        public const string AutoRegisterGeneric = "AutoRegisterAttribute`1";
        public const string AutoRegisterKeyed = "AutoRegisterKeyedAttribute";
        public const string AutoRegisterKeyedGeneric = "AutoRegisterKeyedAttribute`1";
    }

    private const string AttributeValueMetadataNameInject = "AutoRegister";
    private const string AutoRegisterKeyedMetadataNameInject = "AutoRegisterKeyed";

    /// <inheritdoc/>
    public override void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var generationInfo = GetClassDeclarationProvider(context, [
            AttributeNames.AutoRegister,
            AttributeNames.AutoRegisterGeneric,
            AttributeNames.AutoRegisterKeyed,
            AttributeNames.AutoRegisterKeyedGeneric
        ]);

        var compilationProvider = context.CompilationProvider;
        var providers = generationInfo.Combine(compilationProvider);

        context.RegisterSourceOutput(providers, (sourceContext, provider) =>
        {
            var (classDeclarations, compilation) = provider;

            if (!classDeclarations.Any())
                return;

            //Debugger.Launch();
            var @namespace = compilation.AssemblyName ?? "Mud";
            var autoInjects = new List<AutoRegisterMetadata>();

            foreach (var classDeclaration in classDeclarations)
            {
                if (classDeclaration == null) continue;

                try
                {
                    // 使用项目中的公共类分析特性
                    var metadataList = ExtractAutoRegisterMetadata(classDeclaration, compilation);
                    if (metadataList != null)
                    {
                        autoInjects.AddRange(metadataList);

                        // 调试信息：显示提取到的元数据
                        foreach (var metadata in metadataList)
                        {
                            ErrorHandler.ReportInfo(sourceContext, DiagnosticDescriptors.AutoRegisterMetadataDetails,
                                SyntaxHelper.GetClassName(classDeclaration),
                                metadata.ImplType,
                                metadata.BaseType,
                                metadata.LifeTime);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // 记录生成错误但不要中断编译
                    ReportErrorDiagnostic(sourceContext, DiagnosticDescriptors.AutoRegisterGenerationError,
                        SyntaxHelper.GetClassName(classDeclaration), ex);
                }
            }

            if (autoInjects.Any())
            {
                GenSource(sourceContext, autoInjects, @namespace, compilation);
            }
            else
            {
                foreach (var classDeclaration in classDeclarations)
                {
                    if (classDeclaration == null) continue;

                    var className = SyntaxHelper.GetClassName(classDeclaration);
                    var attributes = classDeclaration.AttributeLists.SelectMany(al => al.Attributes);
                    var autoRegisterAttributes = attributes.Where(a => a.Name.ToString().Contains("AutoRegister"));
                }
            }
        });
    }

    /// <summary>
    /// 从类声明中提取AutoRegister元数据
    /// </summary>
    private static List<AutoRegisterMetadata>? ExtractAutoRegisterMetadata(ClassDeclarationSyntax classDeclaration, Compilation compilation)
    {
        var attributes = classDeclaration.AttributeLists.SelectMany(al => al.Attributes);
        var metadataList = new List<AutoRegisterMetadata>();

        foreach (var attribute in attributes)
        {
            var attributeName = attribute.Name.ToString();

            // 判断特性类型
            InjectAttributeType injectType = DetermineInjectType(attribute);
            if (injectType == InjectAttributeType.Unknown) continue;

            string attributeMetadataName = injectType switch
            {
                InjectAttributeType.Regular or InjectAttributeType.Generic => AttributeValueMetadataNameInject,
                InjectAttributeType.Keyed or InjectAttributeType.KeyedGeneric => AutoRegisterKeyedMetadataNameInject,
                _ => AttributeValueMetadataNameInject
            };

            string lifetimePrefix = (injectType == InjectAttributeType.Keyed || injectType == InjectAttributeType.KeyedGeneric) ? "AddKeyed" : "Add";

            string baseTypeName = ExtractBaseTypeName(attribute, injectType);

            if (string.IsNullOrEmpty(baseTypeName) && injectType != InjectAttributeType.Regular)
            {
                continue;
            }

            // 获取实现类型的完整名称
            var classSymbol = compilation.GetSemanticModel(classDeclaration.SyntaxTree)?.GetDeclaredSymbol(classDeclaration);
            var implTypeName = classSymbol?.ToDisplayString() ?? SyntaxHelper.GetClassName(classDeclaration);

            if (string.IsNullOrEmpty(baseTypeName) && injectType == InjectAttributeType.Regular)
                baseTypeName = implTypeName;

            // 对于基类型，我们需要通过语义模型来解析类型符号
            if (!string.IsNullOrEmpty(baseTypeName) && classSymbol != null)
            {
                // 尝试解析基类型名称
                var baseTypeSyntax = SyntaxFactory.ParseTypeName(baseTypeName);
                var baseTypeInfo = compilation.GetSemanticModel(classDeclaration.SyntaxTree)?.GetSpeculativeSymbolInfo(0, baseTypeSyntax, SpeculativeBindingOption.BindAsTypeOrNamespace);
                var baseSymbol = baseTypeInfo?.Symbol as ITypeSymbol;
                if (baseSymbol != null)
                {
                    baseTypeName = baseSymbol.ToDisplayString();
                }
                else
                {
                    // 如果语义模型解析失败，尝试在整个编译中查找类型
                    var allSymbols = compilation.GetSymbolsWithName(baseTypeName, SymbolFilter.Type);
                    var matchingSymbol = allSymbols.OfType<ITypeSymbol>().FirstOrDefault();
                    if (matchingSymbol != null)
                    {
                        baseTypeName = matchingSymbol.ToDisplayString();
                    }
                    else if (!baseTypeName.Contains("."))
                    {
                        // 如果仍然找不到，尝试常见的命名空间组合
                        var possibleNamespaces = new[] { "Mud.Interface", "Mud.Services", compilation.AssemblyName };
                        foreach (var ns in possibleNamespaces.Where(n => n != null))
                        {
                            var fullName = $"{ns}.{baseTypeName}";
                            var testSymbol = compilation.GetTypeByMetadataName(fullName);
                            if (testSymbol != null)
                            {
                                baseTypeName = fullName;
                                break;
                            }
                        }
                    }
                }
            }

            string? key = null;
            if (injectType == InjectAttributeType.Keyed || injectType == InjectAttributeType.KeyedGeneric)
                key = ExtractKeyFromAttribute(attribute);

            string lifeTime = DetermineLifetime(attribute, injectType, lifetimePrefix);
            var metadata = new AutoRegisterMetadata(implTypeName, baseTypeName, lifeTime);
            if (key != null) metadata.Key = key;

            metadataList.Add(metadata);
        }

        return metadataList.Any() ? metadataList : null;
    }

    /// <summary>
    /// 判断特性类型
    /// </summary>
    private static InjectAttributeType DetermineInjectType(string attributeName)
    {
        // 检查是否包含AutoRegister但不包含Keyed
        if (attributeName == "AutoRegister" || attributeName == "AutoRegisterAttribute" ||
            attributeName == "AutoRegisterAttribute`1")
        {
            return attributeName.Contains("`1") ? InjectAttributeType.Generic : InjectAttributeType.Regular;
        }
        // 检查是否包含AutoRegisterKeyed
        else if (attributeName == "AutoRegisterKeyed" || attributeName == "AutoRegisterKeyedAttribute" ||
                 attributeName == "AutoRegisterKeyedAttribute`1")
        {
            return attributeName.Contains("`1") ? InjectAttributeType.KeyedGeneric : InjectAttributeType.Keyed;
        }
        return InjectAttributeType.Unknown;
    }

    /// <summary>
    /// 判断特性类型（基于AttributeSyntax）
    /// </summary>
    private static InjectAttributeType DetermineInjectType(AttributeSyntax attributeSyntax)
    {
        var attributeName = attributeSyntax.Name.ToString();

        // 首先检查是否是泛型语法形式
        if (attributeSyntax.Name is GenericNameSyntax)
        {
            if (attributeName.Contains("AutoRegister") && !attributeName.Contains("Keyed"))
            {
                return InjectAttributeType.Generic;
            }
            else if (attributeName.Contains("AutoRegisterKeyed"))
            {
                return InjectAttributeType.KeyedGeneric;
            }
        }

        // 如果不是泛型语法形式，使用原来的逻辑
        return DetermineInjectType(attributeName);
    }

    /// <summary>
    /// 提取基类型名称
    /// </summary>
    private static string ExtractBaseTypeName(AttributeSyntax attributeSyntax, InjectAttributeType injectType)
    {
        // 处理常规和Keyed非泛型特性
        if (injectType == InjectAttributeType.Regular || injectType == InjectAttributeType.Keyed)
        {
            if (attributeSyntax.ArgumentList == null || attributeSyntax.ArgumentList.Arguments.Count == 0)
                return string.Empty;

            // 对于Keyed特性，第一个参数是key，需要从第二个参数开始查找typeof表达式
            int startIndex = injectType == InjectAttributeType.Keyed ? 1 : 0;

            for (int i = startIndex; i < attributeSyntax.ArgumentList.Arguments.Count; i++)
            {
                var argument = attributeSyntax.ArgumentList.Arguments[i];
                // 跳过命名参数
                if (argument.NameEquals != null) continue;

                if (argument.Expression is TypeOfExpressionSyntax typeOfExpression)
                {
                    // 直接返回完整的类型名称，而不是只取最后一部分
                    return typeOfExpression.Type.ToString();
                }
            }
            return string.Empty;
        }
        else
        {
            // 处理泛型形式: [AutoRegister<IFoo>] 或 [AutoRegisterKeyed<IFoo>("key")]
            var attributeString = attributeSyntax.ToString();

            // 首先尝试使用更精确的正则表达式来匹配泛型类型参数
            string pattern = @"AutoRegister(?:Keyed)?<([^>]+)>";
            var match = Regex.Match(attributeString, pattern);
            if (match.Success)
            {
                return match.Groups[1].Value;
            }

            // 如果正则表达式失败，尝试使用更简单的方法
            var startIndex = attributeString.IndexOf('<');
            var endIndex = attributeString.IndexOf('>');

            if (startIndex > 0 && endIndex > startIndex)
            {
                return attributeString.Substring(startIndex + 1, endIndex - startIndex - 1);
            }

            // 如果以上方法都失败，尝试使用语法分析
            if (attributeSyntax.Name is GenericNameSyntax genericName)
            {
                if (genericName.TypeArgumentList.Arguments.Count > 0)
                {
                    return genericName.TypeArgumentList.Arguments[0].ToString();
                }
            }

            return string.Empty;
        }
    }

    private static string? ExtractKeyFromAttribute(AttributeSyntax attributeSyntax)
    {
        if (attributeSyntax.ArgumentList?.Arguments.Count > 0)
        {
            // Key 一定是第一个参数
            string? key = attributeSyntax.ArgumentList.Arguments[0].Expression.ToString();
            string keyPattern1 = "\"(.*?)\"";
            if (Regex.IsMatch(key, keyPattern1)) return Regex.Match(key, keyPattern1).Groups[1].Value;
            string keyPattern2 = @"\((.*?)\)";
            if (Regex.IsMatch(key, keyPattern2)) { key = Regex.Match(key, keyPattern2).Groups[1].Value; return key.Split(['.']).Last(); }
            return key;
        }
        return null;
    }

    private static string DetermineLifetime(AttributeSyntax attributeSyntax, InjectAttributeType injectType, string prefix)
    {
        string defaultLifetime = $"{prefix}Scoped";
        if (attributeSyntax.ArgumentList == null) return defaultLifetime;

        // 首先检查命名参数
        foreach (var argument in attributeSyntax.ArgumentList.Arguments)
        {
            if (argument.NameEquals?.Name.Identifier.ValueText == "ServiceLifetime")
            {
                var expressionSyntax = argument.Expression;
                if (expressionSyntax.IsKind(SyntaxKind.SimpleMemberAccessExpression))
                {
                    var name = ((MemberAccessExpressionSyntax)expressionSyntax).Name.Identifier.ValueText;
                    return name switch
                    {
                        "Singleton" => $"{prefix}Singleton",
                        "Transient" => $"{prefix}Transient",
                        "Scoped" => $"{prefix}Scoped",
                        _ => defaultLifetime,
                    };
                }
            }
        }

        // 如果没有找到命名参数，检查位置参数
        int startIndex = injectType == InjectAttributeType.Keyed ? 1 : 0; // Keyed: index0 是 key
        for (var i = startIndex; i < attributeSyntax.ArgumentList.Arguments.Count; i++)
        {
            var argument = attributeSyntax.ArgumentList.Arguments[i];
            // 跳过命名参数（已经处理过）
            if (argument.NameEquals != null) continue;

            var expressionSyntax = argument.Expression;
            if (expressionSyntax.IsKind(SyntaxKind.SimpleMemberAccessExpression))
            {
                var name = ((MemberAccessExpressionSyntax)expressionSyntax).Name.Identifier.ValueText;
                return name switch
                {
                    "Singleton" => $"{prefix}Singleton",
                    "Transient" => $"{prefix}Transient",
                    "Scoped" => $"{prefix}Scoped",
                    _ => defaultLifetime,
                };
            }
        }
        return defaultLifetime;
    }

    private enum InjectAttributeType { Regular, Generic, Keyed, KeyedGeneric, Unknown }

    private static void GenSource(SourceProductionContext context, IEnumerable<AutoRegisterMetadata> metas, string? rootNamespace, Compilation compilation)
    {
        if (!metas.Any()) return;
        var languageVersion = (compilation as CSharpCompilation)?.LanguageVersion ?? LanguageVersion.Default;
        bool useFileScoped = languageVersion >= LanguageVersion.CSharp10;

        StringBuilder registrations = new();
        if (metas.Any())
        {
            foreach (var meta in metas.Distinct())
            {
                if (meta.Key != null)
                {
                    if (meta.ImplType != meta.BaseType)
                        registrations.AppendLine($"services.{meta.LifeTime}<{meta.BaseType}, {meta.ImplType}>(\"{meta.Key}\");");
                    else
                        registrations.AppendLine($"services.{meta.LifeTime}<{meta.ImplType}>(\"{meta.Key}\");");
                }
                else
                {
                    if (meta.ImplType != meta.BaseType)
                        registrations.AppendLine($"services.{meta.LifeTime}<{meta.BaseType}, {meta.ImplType}>();");
                    else
                        registrations.AppendLine($"services.{meta.LifeTime}<{meta.ImplType}>();");
                }
            }
        }
        else
        {
            // 如果没有元数据，生成空的注册语句
            registrations.AppendLine("// No AutoRegister services found");
        }

        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("// This file is generated by Mud.ServiceCodeGenerator.AutoRegisterSourceGenerator");
        sb.AppendLine();
        sb.AppendLine("#pragma warning disable");
        sb.AppendLine("using Microsoft.Extensions.DependencyInjection;");

        string indent = string.Empty;
        if (!string.IsNullOrEmpty(rootNamespace))
        {
            if (useFileScoped)
            {
                sb.AppendLine($"namespace {rootNamespace};");
            }
            else
            {
                sb.AppendLine($"namespace {rootNamespace}");
                sb.AppendLine("{");
                indent = "    ";
            }
        }

        sb.AppendLine($"{indent}[global::System.CodeDom.Compiler.GeneratedCode(\"Mud.ServiceCodeGenerator\", \"1.0.0.0\")]");
        sb.AppendLine($"{indent}public static partial class AutoRegisterExtension");
        sb.AppendLine($"{indent}{{");
        sb.AppendLine($"{indent}    /// <summary>");
        sb.AppendLine($"{indent}    /// 自动注册标注的服务");
        sb.AppendLine($"{indent}    /// </summary>");
        sb.AppendLine($"{indent}    public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddAutoRegister(this Microsoft.Extensions.DependencyInjection.IServiceCollection services)");
        sb.AppendLine($"{indent}    {{");
        foreach (var line in registrations.ToString().Split(['\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            sb.AppendLine($"{indent}        {line}");
        }
        sb.AppendLine($"{indent}        return services;");
        sb.AppendLine($"{indent}    }}");
        sb.AppendLine($"{indent}}}");
        if (!useFileScoped && !string.IsNullOrEmpty(rootNamespace)) sb.AppendLine("}");
        sb.AppendLine("#pragma warning restore");

        var source = sb.ToString().FormatCode();
        var projectName = !string.IsNullOrEmpty(rootNamespace) ? rootNamespace : "Mud";
        var fileName = $"{projectName}.ServiceCodeGeneratorInject.g.cs";
        context.AddSource(fileName, SourceText.From(source, Encoding.UTF8));
    }

    private class AutoRegisterMetadata(string implType, string baseType, string lifeTime)
    {
        public string ImplType { get; set; } = implType;

        public string BaseType { get; set; } = baseType;

        public string LifeTime { get; set; } = lifeTime;

        /// <summary>
        /// 针对Microsoft.Extensions.DependencyInjection 8.0以上的Keyed Service,默认为NULL
        /// </summary>
        public string? Key { get; set; }
    }
}