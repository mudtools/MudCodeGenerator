// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.CodeGenerator 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Reflection;

namespace Mud.EntityCodeGenerator.Tests;

/// <summary>
/// 语法树辅助工具单元测试
/// </summary>
public class SyntaxHelperTests
{
    private readonly Type _syntaxHelperType;
    private readonly MethodInfo _getNamespaceNameMethod;
    private readonly MethodInfo _getClassNameMethod;
    private readonly MethodInfo _isValidPrivateFieldMethod;

    public SyntaxHelperTests()
    {
        _syntaxHelperType = TestHelper.GetType("Mud.CodeGenerator.SyntaxHelper");
        _getNamespaceNameMethod = TestHelper.GetMethod(_syntaxHelperType, "GetNamespaceName");
        _getClassNameMethod = TestHelper.GetMethod(_syntaxHelperType, "GetClassName");
        _isValidPrivateFieldMethod = TestHelper.GetMethod(_syntaxHelperType, "IsValidPrivateField");
    }

    [Fact]
    public void GetNamespaceName_WithValidClass_ShouldReturnNamespace()
    {
        var sourceCode = @"
namespace TestNamespace.SubNamespace
{
    public class TestClass { }
}";
        
        var compilation = CreateCompilation(sourceCode);
        var classDeclaration = GetClassDeclaration(compilation, "TestClass");

        var result = _getNamespaceNameMethod.Invoke(null, new object[] { classDeclaration, "" }) as string;

        result.Should().Be("TestNamespace.SubNamespace");
    }

    [Fact]
    public void GetNamespaceName_WithNoNamespace_ShouldReturnEmpty()
    {
        var sourceCode = @"
public class TestClass { }
";
        
        var compilation = CreateCompilation(sourceCode);
        var classDeclaration = GetClassDeclaration(compilation, "TestClass");

        var result = _getNamespaceNameMethod.Invoke(null, new object[] { classDeclaration, "" }) as string;

        result.Should().BeEmpty();
    }

    [Fact]
    public void GetNamespaceName_WithExtNamespace_ShouldAppendNamespace()
    {
        var sourceCode = @"
namespace TestNamespace
{
    public class TestClass { }
}";
        
        var compilation = CreateCompilation(sourceCode);
        var classDeclaration = GetClassDeclaration(compilation, "TestClass");

        var result = _getNamespaceNameMethod.Invoke(null, new object[] { classDeclaration, "ExtNamespace" }) as string;

        result.Should().Be("TestNamespace.ExtNamespace");
    }

    [Fact]
    public void GetClassName_WithValidClass_ShouldReturnClassName()
    {
        var sourceCode = @"
public class TestClass { }
";
        
        var compilation = CreateCompilation(sourceCode);
        var classDeclaration = GetClassDeclaration(compilation, "TestClass");

        var result = _getClassNameMethod.Invoke(null, new object[] { classDeclaration }) as string;

        result.Should().Be("TestClass");
    }

    [Fact]
    public void GetClassName_WithNullClass_ShouldReturnEmpty()
    {
        var result = _getClassNameMethod.Invoke(null, new object?[] { null }) as string;

        result.Should().BeEmpty();
    }

    [Fact]
    public void IsValidPrivateField_WithPrivateField_ShouldReturnTrue()
    {
        var sourceCode = @"
public class TestClass 
{
    private string _privateField;
}
";
        
        var compilation = CreateCompilation(sourceCode);
        var fieldDeclaration = GetFieldDeclaration(compilation, "_privateField");

        var result = (bool)_isValidPrivateFieldMethod.Invoke(null, new object[] { fieldDeclaration })!;

        result.Should().BeTrue();
    }

    [Fact]
    public void IsValidPrivateField_WithPublicField_ShouldReturnTrue()
    {
        var sourceCode = @"
public class TestClass 
{
    public string PublicField;
}
";
        
        var compilation = CreateCompilation(sourceCode);
        var fieldDeclaration = GetFieldDeclaration(compilation, "PublicField");

        var result = (bool)_isValidPrivateFieldMethod.Invoke(null, new object[] { fieldDeclaration })!;

        result.Should().BeTrue();
    }

    [Fact]
    public void IsValidPrivateField_WithConstField_ShouldReturnFalse()
    {
        var sourceCode = @"
public class TestClass 
{
    private const string ConstField = ""test"";
}
";
        
        var compilation = CreateCompilation(sourceCode);
        var fieldDeclaration = GetFieldDeclaration(compilation, "ConstField");

        var result = (bool)_isValidPrivateFieldMethod.Invoke(null, new object[] { fieldDeclaration })!;

        result.Should().BeFalse();
    }

    [Fact]
    public void IsValidPrivateField_WithStaticField_ShouldReturnFalse()
    {
        var sourceCode = @"
public class TestClass 
{
    private static string StaticField;
}
";
        
        var compilation = CreateCompilation(sourceCode);
        var fieldDeclaration = GetFieldDeclaration(compilation, "StaticField");

        var result = (bool)_isValidPrivateFieldMethod.Invoke(null, new object[] { fieldDeclaration })!;

        result.Should().BeFalse();
    }

    [Fact]
    public void IsValidPrivateField_WithReadOnlyField_ShouldReturnFalse()
    {
        var sourceCode = @"
public class TestClass 
{
    private readonly string ReadOnlyField;
}
";
        
        var compilation = CreateCompilation(sourceCode);
        var fieldDeclaration = GetFieldDeclaration(compilation, "ReadOnlyField");

        var result = (bool)_isValidPrivateFieldMethod.Invoke(null, new object[] { fieldDeclaration })!;

        result.Should().BeFalse();
    }

    private static Compilation CreateCompilation(string sourceCode)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Console).Assembly.Location)
        };

        return CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    private static ClassDeclarationSyntax GetClassDeclaration(Compilation compilation, string className)
    {
        var syntaxTree = compilation.SyntaxTrees.First();
        var root = syntaxTree.GetRoot();
        return root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .First(c => c.Identifier.Text == className);
    }

    private static FieldDeclarationSyntax GetFieldDeclaration(Compilation compilation, string fieldName)
    {
        var syntaxTree = compilation.SyntaxTrees.First();
        var root = syntaxTree.GetRoot();
        return root.DescendantNodes()
            .OfType<FieldDeclarationSyntax>()
            .First(f => f.Declaration.Variables.Any(v => v.Identifier.Text == fieldName));
    }
}
