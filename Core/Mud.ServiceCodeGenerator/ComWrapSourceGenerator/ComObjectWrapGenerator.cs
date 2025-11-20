using System.Collections.Immutable;
using System.Text;

namespace Mud.ServiceCodeGenerator.ComWrapSourceGenerator;

[Generator]
public class ComObjectWrapGenerator : TransitiveCodeGenerator
{
    public override void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var interfaceDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: (s, _) => IsSyntaxTargetForGeneration(s),
                transform: (ctx, _) => GetSemanticTargetForGeneration(ctx))
            .Where(m => m is not null);

        var compilationAndInterfaces = context.CompilationProvider.Combine(interfaceDeclarations.Collect());

        context.RegisterSourceOutput(compilationAndInterfaces,
             (spc, source) => Execute(source.Left, source.Right!, spc));
    }

    private bool IsSyntaxTargetForGeneration(SyntaxNode node)
    {
        return node is InterfaceDeclarationSyntax { AttributeLists.Count: > 0 };
    }

    private InterfaceDeclarationSyntax? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
    {
        var interfaceDeclarationSyntax = (InterfaceDeclarationSyntax)context.Node;

        foreach (var attributeList in interfaceDeclarationSyntax.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                var attributeSymbol = context.SemanticModel.GetSymbolInfo(attribute).Symbol?.ContainingType;
                if (attributeSymbol?.Name == "ComObjectWrapAttribute")
                {
                    return interfaceDeclarationSyntax;
                }
            }
        }

        return null;
    }

    private void Execute(Compilation compilation, ImmutableArray<InterfaceDeclarationSyntax> interfaces, SourceProductionContext context)
    {
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

            var generator = new ComObjectWrapGenerator();
            var source = generator.GenerateImplementationClass(interfaceDeclaration, interfaceSymbol, context);

            if (!string.IsNullOrEmpty(source))
            {
                var hintName = $"{interfaceSymbol.Name}Impl.g.cs";
                context.AddSource(hintName, source);
            }
        }
    }

    private string GenerateImplementationClass(InterfaceDeclarationSyntax interfaceDeclaration, INamedTypeSymbol interfaceSymbol, SourceProductionContext context)
    {
        var namespaceName = GetNamespaceName(interfaceDeclaration);
        var interfaceName = interfaceSymbol.Name;

        // 移除接口名前缀"I"
        var className = interfaceName.StartsWith("I") ? interfaceName.Substring(1) : interfaceName;

        // 添加Imps命名空间
        var impNamespace = $"{namespaceName}.Imps";

        var sb = new StringBuilder();
        GenerateFileHeader(sb);

        // 添加额外的命名空间引用
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Runtime.InteropServices;");
        sb.AppendLine("using Mud.Common.CodeGenerator.Extensions;");

        sb.AppendLine();
        sb.AppendLine($"namespace {impNamespace}");
        sb.AppendLine("{");
        sb.AppendLine($"    internal class {className} : {interfaceName}");
        sb.AppendLine("    {");

        // 生成字段
        GenerateFields(sb, interfaceSymbol, interfaceDeclaration);

        // 生成构造函数
        GenerateConstructor(sb, className, interfaceSymbol, interfaceDeclaration);

        // 生成属性
        GenerateProperties(sb, interfaceSymbol, interfaceDeclaration);

        // 生成方法
        GenerateMethods(sb, interfaceSymbol, interfaceDeclaration);

        // 生成IDisposable实现
        GenerateIDisposableImplementation(sb);

        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private void GenerateFields(StringBuilder sb, INamedTypeSymbol interfaceSymbol, InterfaceDeclarationSyntax interfaceDeclaration)
    {
        var comNamespace = GetComNamespace(interfaceDeclaration);
        sb.AppendLine($"        private {comNamespace}.MailMergeField? _mailMergeField;");
        sb.AppendLine("        private bool _disposedValue;");
        sb.AppendLine();
    }

    private void GenerateConstructor(StringBuilder sb, string className, INamedTypeSymbol interfaceSymbol, InterfaceDeclarationSyntax interfaceDeclaration)
    {
        var comNamespace = GetComNamespace(interfaceDeclaration);
        sb.AppendLine($"        internal {className}({comNamespace}.MailMergeField mailMergeField)");
        sb.AppendLine("        {");
        sb.AppendLine("            _mailMergeField = mailMergeField ?? throw new ArgumentNullException(nameof(mailMergeField));");
        sb.AppendLine("            _disposedValue = false;");
        sb.AppendLine("        }");
        sb.AppendLine();
    }

    private void GenerateProperties(StringBuilder sb, INamedTypeSymbol interfaceSymbol, InterfaceDeclarationSyntax interfaceDeclaration)
    {
        sb.AppendLine("        #region 属性");
        sb.AppendLine();

        foreach (var member in interfaceSymbol.GetMembers())
        {
            if (member is IPropertySymbol propertySymbol && !ShouldIgnoreMember(member))
            {
                GenerateProperty(sb, propertySymbol, interfaceDeclaration);
            }
        }

        sb.AppendLine("        #endregion");
        sb.AppendLine();
    }

    private void GenerateProperty(StringBuilder sb, IPropertySymbol propertySymbol, InterfaceDeclarationSyntax interfaceDeclaration)
    {
        var propertyName = propertySymbol.Name;
        var propertyType = propertySymbol.Type.ToDisplayString();
        var isReadOnly = propertySymbol.SetMethod == null;
        var isEnumType = IsEnumProperty(interfaceDeclaration, propertySymbol);
        var defaultValue = GetDefaultValue(interfaceDeclaration, propertySymbol);
        var comNamespace = GetComNamespace(interfaceDeclaration);

        if (isEnumType)
        {
            GenerateEnumProperty(sb, propertySymbol, interfaceDeclaration, defaultValue);
        }
        else if (propertySymbol.SetMethod == null && propertySymbol.GetMethod != null)
        {
            // 只读属性
            if (propertyType == "IWordApplication?")
            {
                sb.AppendLine($"        public {propertyType} {propertyName} => _mailMergeField != null ? new WordApplication(_mailMergeField.{propertyName}) : null;");
            }
            else if (propertyType == "IWordRange?")
            {
                sb.AppendLine($"        public {propertyType} {propertyName} => _mailMergeField?.{propertyName} != null ? new WordRange(_mailMergeField.{propertyName}) : null;");
            }
            else
            {
                sb.AppendLine($"        public {propertyType} {propertyName} => _mailMergeField?.{propertyName};");
            }
            sb.AppendLine();
        }
        else if (propertySymbol.SetMethod != null && propertySymbol.GetMethod != null)
        {
            // 读写属性
            if (propertyType == "bool")
            {
                sb.AppendLine($"        public {propertyType} {propertyName}");
                sb.AppendLine("        {");
                sb.AppendLine($"            get => _mailMergeField?.{propertyName} ?? false;");
                sb.AppendLine("            set");
                sb.AppendLine("            {");
                sb.AppendLine("                if (_mailMergeField != null)");
                sb.AppendLine($"                    _mailMergeField.{propertyName} = value;");
                sb.AppendLine("            }");
                sb.AppendLine("        }");
            }
            else
            {
                sb.AppendLine($"        public {propertyType} {propertyName}");
                sb.AppendLine("        {");
                sb.AppendLine($"            get => _mailMergeField?.{propertyName};");
                sb.AppendLine("            set");
                sb.AppendLine("            {");
                sb.AppendLine("                if (_mailMergeField != null)");
                sb.AppendLine($"                    _mailMergeField.{propertyName} = value;");
                sb.AppendLine("            }");
                sb.AppendLine("        }");
            }
            sb.AppendLine();
        }
    }

    private void GenerateEnumProperty(StringBuilder sb, IPropertySymbol propertySymbol, InterfaceDeclarationSyntax interfaceDeclaration, string defaultValue)
    {
        var propertyName = propertySymbol.Name;
        var propertyType = propertySymbol.Type.ToDisplayString();
        var comNamespace = GetComNamespace(interfaceDeclaration);

        if (propertySymbol.SetMethod == null)
        {
            // 只读枚举属性
            sb.AppendLine($"        public {propertyType} {propertyName} => _mailMergeField?.{propertyName}.EnumConvert({defaultValue}) ?? {defaultValue};");
            sb.AppendLine();
        }
        else
        {
            // 读写枚举属性
            sb.AppendLine($"        public {propertyType} {propertyName}");
            sb.AppendLine("        {");
            sb.AppendLine($"            get => _mailMergeField?.{propertyName}.EnumConvert({defaultValue}) ?? {defaultValue};");
            sb.AppendLine("            set");
            sb.AppendLine("            {");
            sb.AppendLine("                if (_mailMergeField != null)");
            sb.AppendLine($"                    _mailMergeField.{propertyName} = value.EnumConvert({comNamespace}.WdFieldType.wdFieldEmpty);");
            sb.AppendLine("            }");
            sb.AppendLine("        }");
            sb.AppendLine();
        }
    }

    private void GenerateMethods(StringBuilder sb, INamedTypeSymbol interfaceSymbol, InterfaceDeclarationSyntax interfaceDeclaration)
    {
        sb.AppendLine("        #region 方法实现");
        sb.AppendLine();

        foreach (var member in interfaceSymbol.GetMembers())
        {
            if (member is IMethodSymbol methodSymbol &&
                methodSymbol.MethodKind == MethodKind.Ordinary &&
                !ShouldIgnoreMember(member))
            {
                GenerateMethod(sb, methodSymbol, interfaceDeclaration);
            }
        }

        sb.AppendLine("        #endregion");
        sb.AppendLine();
    }

    private void GenerateMethod(StringBuilder sb, IMethodSymbol methodSymbol, InterfaceDeclarationSyntax interfaceDeclaration)
    {
        var methodName = methodSymbol.Name;
        var returnType = methodSymbol.ReturnType.ToDisplayString();
        var parameters = string.Join(", ", methodSymbol.Parameters.Select(p => $"{p.Type.ToDisplayString()} {p.Name}"));

        sb.AppendLine($"        public {returnType} {methodName}({parameters})");
        sb.AppendLine("        {");

        // 对于带参数的方法，添加参数检查
        if (methodSymbol.Parameters.Length > 0)
        {
            sb.AppendLine("            if (_mailMergeField == null)");
            sb.AppendLine("                throw new ObjectDisposedException(nameof(_mailMergeField));");
            sb.AppendLine();
        }

        sb.AppendLine("            try");
        sb.AppendLine("            {");

        if (returnType == "void")
        {
            sb.AppendLine($"                _mailMergeField?.{methodName}({string.Join(", ", methodSymbol.Parameters.Select(p => p.Name))});");
        }
        else if (returnType == "IWordRange?")
        {
            sb.AppendLine($"                var comObj = _mailMergeField.{methodName}({string.Join(", ", methodSymbol.Parameters.Select(p => p.Name))});");
            sb.AppendLine("                if (comObj == null)");
            sb.AppendLine("                    return null;");
            sb.AppendLine("                return new WordRange(comObj);");
        }
        else
        {
            sb.AppendLine($"                return _mailMergeField.{methodName}({string.Join(", ", methodSymbol.Parameters.Select(p => p.Name))});");
        }

        sb.AppendLine("            }");
        sb.AppendLine("            catch (Exception ex)");
        sb.AppendLine("            {");
        sb.AppendLine($"                throw new InvalidOperationException(\"执行MailMergeField对象的{methodName}方法失败。\", ex);");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine();
    }

    private void GenerateIDisposableImplementation(StringBuilder sb)
    {
        sb.AppendLine("        #region IDisposable 实现");
        sb.AppendLine("        protected virtual void Dispose(bool disposing)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (_disposedValue) return;");
        sb.AppendLine();
        sb.AppendLine("            if (disposing && _mailMergeField != null)");
        sb.AppendLine("            {");
        sb.AppendLine("                Marshal.ReleaseComObject(_mailMergeField);");
        sb.AppendLine("                _mailMergeField = null;");
        sb.AppendLine("            }");
        sb.AppendLine();
        sb.AppendLine("            _disposedValue = true;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        public void Dispose()");
        sb.AppendLine("        {");
        sb.AppendLine("            Dispose(true);");
        sb.AppendLine("            GC.SuppressFinalize(this);");
        sb.AppendLine("        }");
        sb.AppendLine("        #endregion");
    }

    private bool ShouldIgnoreMember(ISymbol member)
    {
        return member.GetAttributes().Any(attr => attr.AttributeClass?.Name == "IgnoreGeneratorAttribute");
    }

    private bool IsEnumProperty(InterfaceDeclarationSyntax interfaceDeclaration, IPropertySymbol propertySymbol)
    {
        var propertyDeclaration = interfaceDeclaration.DescendantNodes()
            .OfType<PropertyDeclarationSyntax>()
            .FirstOrDefault(p => p.Identifier.Text == propertySymbol.Name);

        if (propertyDeclaration != null)
        {
            return propertyDeclaration.AttributeLists
                .SelectMany(al => al.Attributes)
                .Any(attr => attr.Name.ToString() == "ComPropertyWrap" &&
                            attr.ArgumentList?.Arguments.Any(arg =>
                                arg.Expression.ToString().Contains("PropertyType.EnumType") ||
                                arg.NameEquals?.Name.Identifier.Text == "PropertyType" &&
                                arg.Expression.ToString() == "PropertyType.EnumType") == true);
        }

        return false;
    }

    private string GetDefaultValue(InterfaceDeclarationSyntax interfaceDeclaration, IPropertySymbol propertySymbol)
    {
        var propertyDeclaration = interfaceDeclaration.DescendantNodes()
            .OfType<PropertyDeclarationSyntax>()
            .FirstOrDefault(p => p.Identifier.Text == propertySymbol.Name);

        if (propertyDeclaration != null)
        {
            var defaultValueArgument = propertyDeclaration.AttributeLists
                .SelectMany(al => al.Attributes)
                .Where(attr => attr.Name.ToString() == "ComPropertyWrap")
                .SelectMany(attr => attr.ArgumentList?.Arguments ?? Enumerable.Empty<AttributeArgumentSyntax>())
                .FirstOrDefault(arg =>
                    arg.NameEquals?.Name.Identifier.Text == "DefaultValue" ||
                    arg.Expression.ToString().Contains("DefaultValue"));

            if (defaultValueArgument != null)
            {
                // 移除引号
                var defaultValue = defaultValueArgument.Expression.ToString();
                return defaultValue.Trim('"');
            }
        }

        return "default";
    }

    private string GetComNamespace(InterfaceDeclarationSyntax interfaceDeclaration)
    {
        var comObjectWrapAttribute = interfaceDeclaration.AttributeLists
            .SelectMany(al => al.Attributes)
            .FirstOrDefault(attr => attr.Name.ToString() == "ComObjectWrap");

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

        return "UNKNOWN_NAMESPACE";
    }
}