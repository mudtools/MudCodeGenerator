// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.CodeGenerator 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Reflection;

namespace Mud.ServiceCodeGenerator.Tests;

/// <summary>
/// 测试辅助工具类，用于获取 Mud.ServiceCodeGenerator 程序集中的内部类型
/// </summary>
public static class TestHelper
{
    private static readonly Assembly GeneratorAssembly;

    static TestHelper()
    {
        var assemblyName = "Mud.ServiceCodeGenerator";
        
        GeneratorAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == assemblyName);

        if (GeneratorAssembly == null)
        {
            var assemblyPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                $"{assemblyName}.dll");
            
            if (File.Exists(assemblyPath))
            {
                GeneratorAssembly = Assembly.LoadFrom(assemblyPath);
            }
            else
            {
                throw new InvalidOperationException(
                    $"无法找到 {assemblyName} 程序集。搜索路径: {assemblyPath}");
            }
        }
    }

    /// <summary>
    /// 获取指定名称的类型
    /// </summary>
    /// <param name="typeName">类型全名</param>
    /// <returns>类型对象</returns>
    public static Type GetType(string typeName)
    {
        return GeneratorAssembly.GetType(typeName)
            ?? throw new InvalidOperationException($"无法在 Mud.ServiceCodeGenerator 程序集中找到类型: {typeName}");
    }

    /// <summary>
    /// 获取指定类型的方法
    /// </summary>
    /// <param name="type">类型对象</param>
    /// <param name="methodName">方法名</param>
    /// <param name="bindingFlags">绑定标志</param>
    /// <returns>方法信息</returns>
    public static MethodInfo GetMethod(Type type, string methodName, BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Static)
    {
        return type.GetMethod(methodName, bindingFlags)
            ?? throw new InvalidOperationException($"无法在类型 {type.Name} 中找到方法: {methodName}");
    }

    /// <summary>
    /// 获取指定类型和参数的方法
    /// </summary>
    /// <param name="type">类型对象</param>
    /// <param name="methodName">方法名</param>
    /// <param name="parameterTypes">参数类型数组</param>
    /// <returns>方法信息</returns>
    public static MethodInfo GetMethod(Type type, string methodName, Type[] parameterTypes)
    {
        return type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static, null, parameterTypes, null)
            ?? throw new InvalidOperationException($"无法在类型 {type.Name} 中找到方法: {methodName}，参数类型: {string.Join(", ", parameterTypes.Select(t => t.Name))}");
    }

    /// <summary>
    /// 创建编译对象
    /// </summary>
    /// <param name="sourceCode">源代码</param>
    /// <returns>编译对象</returns>
    public static Compilation CreateCompilation(string sourceCode)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Collections.Generic.List<>).Assembly.Location)
        };

        return CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    /// <summary>
    /// 获取类声明语法节点
    /// </summary>
    /// <param name="compilation">编译对象</param>
    /// <param name="className">类名</param>
    /// <returns>类声明语法节点</returns>
    public static ClassDeclarationSyntax GetClassDeclaration(Compilation compilation, string className)
    {
        var syntaxTree = compilation.SyntaxTrees.First();
        var root = syntaxTree.GetRoot();
        return root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .First(c => c.Identifier.Text == className);
    }

    /// <summary>
    /// 获取接口声明语法节点
    /// </summary>
    /// <param name="compilation">编译对象</param>
    /// <param name="interfaceName">接口名</param>
    /// <returns>接口声明语法节点</returns>
    public static InterfaceDeclarationSyntax GetInterfaceDeclaration(Compilation compilation, string interfaceName)
    {
        var syntaxTree = compilation.SyntaxTrees.First();
        var root = syntaxTree.GetRoot();
        return root.DescendantNodes()
            .OfType<InterfaceDeclarationSyntax>()
            .First(c => c.Identifier.Text == interfaceName);
    }
}
