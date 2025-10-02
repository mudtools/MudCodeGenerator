using System.Collections.ObjectModel;
using System.Text;

namespace Mud.CodeGenerator;

internal static class SyntaxHelper
{
    /// <summary>
    /// 获取属性或字段的类型，并确保返回可空类型。
    /// </summary>
    public static string GetPropertyType<T>(T declarationSyntax) where T : MemberDeclarationSyntax
    {
        var propertyType = "";

        if (declarationSyntax is PropertyDeclarationSyntax propertySyntax)
        {
            propertyType = propertySyntax.Type.ToString();
        }
        else if (declarationSyntax is FieldDeclarationSyntax fieldSyntax)
        {
            propertyType = fieldSyntax.Declaration.Type.ToString();
        }
        else
        {
            propertyType = "";
        }

        return propertyType;

        //// 如果已经是空字符串，直接返回
        //if (string.IsNullOrEmpty(propertyType))
        //    return propertyType;

        //// 改进的可空类型检测
        //if (IsNullableType(propertyType))
        //{
        //    return propertyType;
        //}
        //else
        //{
        //    return propertyType;
        //}
    }

    /// <summary>
    /// 判断类型字符串是否表示可空类型
    /// </summary>
    private static bool IsNullableType(string typeName)
    {
        if (string.IsNullOrEmpty(typeName))
            return false;

        // 去除前后空格
        typeName = typeName.Trim();

        // 检查后缀问号（可空值类型）
        if (typeName.EndsWith("?", StringComparison.CurrentCulture))
            return true;

        // 检查 Nullable<T> 泛型形式
        if (typeName.StartsWith("Nullable<", StringComparison.OrdinalIgnoreCase) ||
            typeName.StartsWith("System.Nullable<", StringComparison.OrdinalIgnoreCase))
            return true;

        // 检查常见的引用类型（默认就是可空的）
        var referenceTypes = new[] { "string", "object", "System.String", "System.Object" };
        if (referenceTypes.Contains(typeName, StringComparer.OrdinalIgnoreCase))
            return true;

        return false;
    }

    /// <summary>
    /// 获取类声明上的特性对象。
    /// </summary>
    /// <typeparam name="T">需创建的特性类型。</typeparam>
    /// <param name="classDeclaration">类声明<see cref="ClassDeclarationSyntax"/>对象。</param>
    /// <param name="attributeName">注解名。</param>
    /// <param name="paramName">参数名。</param>
    /// <param name="defaultVal">参数默认值。</param>
    /// <returns>返回创建的特性对象。</returns>
    public static T GetClassAttributeValues<T>(ClassDeclarationSyntax classDeclaration, string attributeName, string paramName, T defaultVal)
        where T : notnull
    {
        var attributes = GetAttributeSyntaxes(classDeclaration, attributeName);
        return AttributeSyntaxHelper.GetAttributeValue(attributes, paramName, defaultVal);
    }


    /// <summary>
    /// 获取类上的注解。
    /// </summary>
    /// <param name="classDeclaration">类声明<see cref="ClassDeclarationSyntax"/>对象。</param>
    /// <param name="attributeName">注解名。</param>
    /// <returns>特性语法集合。</returns>
    public static ReadOnlyCollection<AttributeSyntax> GetAttributeSyntaxes(ClassDeclarationSyntax classDeclaration, string attributeName)
    {
        if (string.IsNullOrEmpty(attributeName))
            return new ReadOnlyCollection<AttributeSyntax>([]);

        if (classDeclaration == null)
            return new ReadOnlyCollection<AttributeSyntax>([]);

        var attriShortName = attributeName.Replace("Attribute", "");

        // 获取类上的特性
        var attributes = classDeclaration.AttributeLists
                        .SelectMany(al => al.Attributes)
                        .Where(a => a.Name.ToString() == attributeName || a.Name.ToString() == attriShortName)
                        .ToList();
        return new ReadOnlyCollection<AttributeSyntax>(attributes);
    }

    /// <summary>
    /// 获取类型名。
    /// </summary>
    /// <param name="typeSyntax">类型语法。</param>
    /// <returns>类型名称。</returns>
    public static string GetTypeSyntaxName(TypeSyntax typeSyntax)
    {
        if (typeSyntax is IdentifierNameSyntax identifierName)
        {
            return identifierName.Identifier.Text;
        }
        else if (typeSyntax is GenericNameSyntax genericName)
        {
            var typeName = genericName.Identifier.ValueText;
            var typeArguments = genericName.TypeArgumentList.Arguments;
            var argumentNames = string.Join(", ", typeArguments.Select(GetTypeSyntaxName));
            return $"{typeName}<{argumentNames}>";
        }
        else if (typeSyntax is QualifiedNameSyntax qualifiedName)
        {
            return $"{GetTypeSyntaxName(qualifiedName.Left)}.{GetTypeSyntaxName(qualifiedName.Right)}";
        }
        else if (typeSyntax is PredefinedTypeSyntax predefinedType)
        {
            return predefinedType.Keyword.Text;
        }
        else if (typeSyntax is ArrayTypeSyntax arrayType)
        {
            return $"{GetTypeSyntaxName(arrayType.ElementType)}[]";
        }
        else if (typeSyntax is PointerTypeSyntax pointerType)
        {
            return $"{GetTypeSyntaxName(pointerType.ElementType)}*";
        }
        else if (typeSyntax is NullableTypeSyntax nullableType)
        {
            return $"{GetTypeSyntaxName(nullableType.ElementType)}?";
        }
        else if (typeSyntax is TupleTypeSyntax tupleType)
        {
            return $"({string.Join(", ", tupleType.Elements.Select(e => GetTypeSyntaxName(e.Type)))})";
        }
        else if (typeSyntax is AliasQualifiedNameSyntax aliasQualifiedName)
        {
            return $"{aliasQualifiedName.Alias}.{GetTypeSyntaxName(aliasQualifiedName.Name)}";
        }
        else
        {
            return typeSyntax?.ToString() ?? string.Empty;
        }
    }

    /// <summary>
    /// 获取类的所有成员字段（含私有、保护、公开）。
    /// </summary>
    /// <param name="classDeclaration">类声明。</param>
    /// <returns>字段声明集合。</returns>
    public static ReadOnlyCollection<FieldDeclarationSyntax> GetClassMemberField(ClassDeclarationSyntax classDeclaration)
    {
        if (classDeclaration == null)
            return new ReadOnlyCollection<FieldDeclarationSyntax>([]);

        var fields = classDeclaration.Members
            .OfType<FieldDeclarationSyntax>()
            .Where(f => f.Modifiers.Any(m => m.IsKind(SyntaxKind.PrivateKeyword) ||
                                             m.IsKind(SyntaxKind.ProtectedKeyword) ||
                                             m.IsKind(SyntaxKind.PublicKeyword)))
            .ToList();

        return new ReadOnlyCollection<FieldDeclarationSyntax>(fields);
    }

    /// <summary>
    /// 获取指定的路径节点名。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="syntaxNode"></param>
    /// <param name="result"></param>
    /// <returns></returns>
    public static bool TryGetParentSyntax<T>(SyntaxNode syntaxNode, out T result)
        where T : SyntaxNode
    {
        result = null;
        if (syntaxNode == null)
        {
            return false;
        }

        syntaxNode = syntaxNode.Parent;
        if (syntaxNode == null)
        {
            return false;
        }

        if (syntaxNode.GetType() == typeof(T))
        {
            result = syntaxNode as T;
            return true;
        }
        return TryGetParentSyntax(syntaxNode, out result);
    }

    /// <summary>
    /// 获取原始的类名。
    /// </summary>
    /// <param name="classNode"><see cref="ClassDeclarationSyntax"/>对象。</param>
    /// <returns>类名。</returns>
    public static string GetClassName(ClassDeclarationSyntax classNode)
    {
        if (classNode == null)
            return "";
        return classNode.Identifier.Text;
    }


    /// <summary>
    /// 获取类的全路径名。
    /// </summary>
    /// <param name="varClassDec"></param>
    /// <returns></returns>
    public static string ClassFullName(this ClassDeclarationSyntax varClassDec)
    {
        SyntaxNode tempCurCls = varClassDec;
        var tempFullName = new Stack<string>();

        do
        {
            if (tempCurCls.IsKind(SyntaxKind.ClassDeclaration))
            {
                tempFullName.Push(((ClassDeclarationSyntax)tempCurCls).Identifier.ToString());
            }
            else if (tempCurCls.IsKind(SyntaxKind.NamespaceDeclaration))
            {
                tempFullName.Push(((NamespaceDeclarationSyntax)tempCurCls).Name.ToString());
            }
            else if (tempCurCls.IsKind(SyntaxKind.FileScopedNamespaceDeclaration))
            {
                tempFullName.Push(((FileScopedNamespaceDeclarationSyntax)tempCurCls).Name.ToString());
            }

            tempCurCls = tempCurCls.Parent;
        } while (tempCurCls != null);

        return string.Join(".", tempFullName);
    }


    /// <summary>
    /// 将字符串解释为<see cref="MethodDeclarationSyntax"/>对象。
    /// </summary>
    /// <param name="sb">字符串构建器。</param>
    /// <returns>方法声明语法。</returns>
    public static MethodDeclarationSyntax GetMethodDeclarationSyntax(StringBuilder sb)
    {
        if (sb == null || string.IsNullOrWhiteSpace(sb.ToString()))
            return null;

        try
        {
            var methodCode = sb.ToString().Trim();

            // 包装方法到完整的类中
            string completeCode = $@"
using System;
namespace TemporaryNamespace
{{
    public static class TemporaryClass
    {{
        {methodCode}
    }}
}}";

            var tree = CSharpSyntaxTree.ParseText(completeCode);
            var root = tree.GetRoot();

            // 检查解析错误
            var errors = tree.GetDiagnostics()
                .Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
                .ToList();

            return root.DescendantNodes().OfType<MethodDeclarationSyntax>().FirstOrDefault();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"解析异常: {ex.Message}");
            return null;
        }
    }
}