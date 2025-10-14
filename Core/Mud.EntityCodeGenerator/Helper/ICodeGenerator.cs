using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Mud.EntityCodeGenerator.Helper;

/// <summary>
/// 代码生成器接口
/// </summary>
public interface ICodeGenerator
{
    /// <summary>
    /// 生成代码
    /// </summary>
    /// <param name="context">源码生成上下文</param>
    /// <param name="compilation">编译对象</param>
    /// <param name="classDeclaration">类声明语法</param>
    void GenerateCode(SourceProductionContext context, Compilation compilation, ClassDeclarationSyntax classDeclaration);

    /// <summary>
    /// 获取生成器名称
    /// </summary>
    string GeneratorName { get; }

    /// <summary>
    /// 检查是否应该生成代码
    /// </summary>
    /// <param name="classDeclaration">类声明语法</param>
    /// <returns>是否应该生成</returns>
    bool ShouldGenerate(ClassDeclarationSyntax classDeclaration);
}

/// <summary>
/// DTO代码生成器接口
/// </summary>
public interface IDtoGenerator : ICodeGenerator
{
    /// <summary>
    /// 获取生成的类名
    /// </summary>
    /// <param name="classDeclaration">原始类声明</param>
    /// <returns>生成的类名</returns>
    string GetGeneratedClassName(ClassDeclarationSyntax classDeclaration);

    /// <summary>
    /// 获取生成的命名空间
    /// </summary>
    /// <param name="classDeclaration">原始类声明</param>
    /// <returns>生成的命名空间</returns>
    string GetGeneratedNamespace(ClassDeclarationSyntax classDeclaration);

    /// <summary>
    /// 获取属性配置
    /// </summary>
    /// <returns>属性配置数组</returns>
    string[] GetPropertyAttributes();
}