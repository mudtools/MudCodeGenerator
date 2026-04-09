// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.CodeGenerator 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.EntityCodeGenerator.Tests;

/// <summary>
/// DTO 生成器基础功能单元测试
/// </summary>
public class DtoGeneratorTests
{
    [Fact]
    public void CreateCompilation_WithValidCode_ShouldSucceed()
    {
        var sourceCode = @"
using System;

namespace TestNamespace
{
    public class TestEntity
    {
        public string Name { get; set; }
        public int Age { get; set; }
    }
}";

        var compilation = CreateCompilation(sourceCode);

        compilation.Should().NotBeNull();
        compilation.SyntaxTrees.Should().HaveCount(1);
    }

    [Fact]
    public void GetClassDeclaration_WithValidClassName_ShouldReturnClass()
    {
        var sourceCode = @"
namespace TestNamespace
{
    public class TestEntity { }
}";

        var compilation = CreateCompilation(sourceCode);
        var classDeclaration = GetClassDeclaration(compilation, "TestEntity");

        classDeclaration.Should().NotBeNull();
        classDeclaration.Identifier.Text.Should().Be("TestEntity");
    }

    [Fact]
    public void GetPropertyDeclaration_WithValidClass_ShouldReturnProperties()
    {
        var sourceCode = @"
namespace TestNamespace
{
    public class TestEntity
    {
        public string Name { get; set; }
        public int Age { get; set; }
    }
}";

        var compilation = CreateCompilation(sourceCode);
        var classDeclaration = GetClassDeclaration(compilation, "TestEntity");
        var properties = classDeclaration.Members.OfType<PropertyDeclarationSyntax>().ToList();

        properties.Should().HaveCount(2);
        properties[0].Identifier.Text.Should().Be("Name");
        properties[1].Identifier.Text.Should().Be("Age");
    }

    [Fact]
    public void GetFieldDeclaration_WithValidClass_ShouldReturnFields()
    {
        var sourceCode = @"
namespace TestNamespace
{
    public class TestEntity
    {
        private string _name;
        private int _age;
    }
}";

        var compilation = CreateCompilation(sourceCode);
        var classDeclaration = GetClassDeclaration(compilation, "TestEntity");
        var fields = classDeclaration.Members.OfType<FieldDeclarationSyntax>().ToList();

        fields.Should().HaveCount(2);
    }

    [Fact]
    public void ClassDeclaration_WithAttributes_ShouldParseCorrectly()
    {
        var sourceCode = @"
using System.ComponentModel.DataAnnotations;

namespace TestNamespace
{
    public class TestEntity
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; }
    }
}";

        var compilation = CreateCompilation(sourceCode);
        var classDeclaration = GetClassDeclaration(compilation, "TestEntity");
        var properties = classDeclaration.Members.OfType<PropertyDeclarationSyntax>().ToList();

        properties[0].AttributeLists.Should().HaveCount(1);
        properties[1].AttributeLists.Should().HaveCount(2);
    }

    [Fact]
    public void ClassDeclaration_WithBaseClass_ShouldParseCorrectly()
    {
        var sourceCode = @"
namespace TestNamespace
{
    public class BaseEntity { }
    
    public class TestEntity : BaseEntity
    {
        public string Name { get; set; }
    }
}";

        var compilation = CreateCompilation(sourceCode);
        var classDeclaration = GetClassDeclaration(compilation, "TestEntity");

        classDeclaration.BaseList.Should().NotBeNull();
        classDeclaration.BaseList!.Types.Should().HaveCount(1);
    }

    [Fact]
    public void ClassDeclaration_WithNamespace_ShouldParseCorrectly()
    {
        var sourceCode = @"
namespace TestNamespace.SubNamespace
{
    public class TestEntity { }
}";

        var compilation = CreateCompilation(sourceCode);
        var classDeclaration = GetClassDeclaration(compilation, "TestEntity");
        var namespaceDecl = classDeclaration.Parent as NamespaceDeclarationSyntax;

        namespaceDecl.Should().NotBeNull();
        namespaceDecl!.Name.ToString().Should().Be("TestNamespace.SubNamespace");
    }

    private static Compilation CreateCompilation(string sourceCode)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.ComponentModel.DataAnnotations.RequiredAttribute).Assembly.Location)
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
}
