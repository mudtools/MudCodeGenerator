using System.Collections.ObjectModel;

namespace Mud.CodeGenerator;

internal static class SyntaxHelper
{
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
}