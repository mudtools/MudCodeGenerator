namespace Mud.CodeGenerator;

internal static class SyntaxNodeHelper
{
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