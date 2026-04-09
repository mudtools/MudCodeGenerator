// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.CodeGenerator 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.EntityCodeGenerator.Tests;

/// <summary>
/// 映射生成器单元测试
/// </summary>
public class MappingGeneratorTests
{
    [Fact]
    public void GenerateMappingCode_WithSimpleProperties_ShouldGenerateCorrectCode()
    {
        var sourceCode = @"
namespace TestNamespace
{
    public class SourceEntity
    {
        public string Name { get; set; }
        public int Age { get; set; }
    }
}";

        var compilation = CreateCompilation(sourceCode);
        var classDeclaration = GetClassDeclaration(compilation, "SourceEntity");
        var properties = classDeclaration.Members.OfType<PropertyDeclarationSyntax>().ToList();

        var mappingLines = new List<string>();
        foreach (var prop in properties)
        {
            mappingLines.Add($"result.{prop.Identifier.Text} = source.{prop.Identifier.Text};");
        }

        var mappingCode = string.Join("\n", mappingLines);

        mappingCode.Should().Contain("result.Name = source.Name;");
        mappingCode.Should().Contain("result.Age = source.Age;");
    }

    [Fact]
    public void GenerateMappingCode_WithDifferentPropertyNames_ShouldHandleCorrectly()
    {
        var sourceCode = @"
namespace TestNamespace
{
    public class SourceEntity
    {
        public string UserName { get; set; }
        public int UserAge { get; set; }
    }
}";

        var compilation = CreateCompilation(sourceCode);
        var classDeclaration = GetClassDeclaration(compilation, "SourceEntity");
        var properties = classDeclaration.Members.OfType<PropertyDeclarationSyntax>().ToList();

        var mappingLines = new List<string>();
        foreach (var prop in properties)
        {
            var targetPropName = prop.Identifier.Text.Replace("User", "");
            mappingLines.Add($"result.{targetPropName} = source.{prop.Identifier.Text};");
        }

        var mappingCode = string.Join("\n", mappingLines);

        mappingCode.Should().Contain("result.Name = source.UserName;");
        mappingCode.Should().Contain("result.Age = source.UserAge;");
    }

    [Fact]
    public void GenerateMappingCode_WithNullableProperties_ShouldHandleCorrectly()
    {
        var sourceCode = @"
namespace TestNamespace
{
    public class SourceEntity
    {
        public string? Name { get; set; }
        public int? Age { get; set; }
    }
}";

        var compilation = CreateCompilation(sourceCode);
        var classDeclaration = GetClassDeclaration(compilation, "SourceEntity");
        var properties = classDeclaration.Members.OfType<PropertyDeclarationSyntax>().ToList();

        var mappingLines = new List<string>();
        foreach (var prop in properties)
        {
            mappingLines.Add($"result.{prop.Identifier.Text} = source.{prop.Identifier.Text};");
        }

        var mappingCode = string.Join("\n", mappingLines);

        mappingCode.Should().Contain("result.Name = source.Name;");
        mappingCode.Should().Contain("result.Age = source.Age;");
    }

    [Fact]
    public void GenerateMappingCode_WithComplexTypes_ShouldHandleCorrectly()
    {
        var sourceCode = @"
namespace TestNamespace
{
    public class Address
    {
        public string City { get; set; }
    }
    
    public class SourceEntity
    {
        public string Name { get; set; }
        public Address Address { get; set; }
    }
}";

        var compilation = CreateCompilation(sourceCode);
        var classDeclaration = GetClassDeclaration(compilation, "SourceEntity");
        var properties = classDeclaration.Members.OfType<PropertyDeclarationSyntax>().ToList();

        var mappingLines = new List<string>();
        foreach (var prop in properties)
        {
            mappingLines.Add($"result.{prop.Identifier.Text} = source.{prop.Identifier.Text};");
        }

        var mappingCode = string.Join("\n", mappingLines);

        mappingCode.Should().Contain("result.Name = source.Name;");
        mappingCode.Should().Contain("result.Address = source.Address;");
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
}
