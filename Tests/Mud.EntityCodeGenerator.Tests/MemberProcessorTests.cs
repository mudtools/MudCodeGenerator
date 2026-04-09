// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.CodeGenerator 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Reflection;
using Mud.EntityCodeGenerator.Helper;

namespace Mud.EntityCodeGenerator.Tests;

/// <summary>
/// MemberProcessor 成员处理器单元测试
/// </summary>
public class MemberProcessorTests
{
    private readonly MethodInfo _processMembersMethod;

    public MemberProcessorTests()
    {
        _processMembersMethod = typeof(MemberProcessor).GetMethod("ProcessMembers");
    }

    [Fact]
    public void ProcessMembers_WithValidClass_ShouldProcessProperties()
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
        var classDeclaration = GetClassDeclaration(compilation, "TestEntity");
        
        var processedProperties = new List<string>();
        Action<PropertyDeclarationSyntax, string, string, string> processor = (prop, orgName, name, type) =>
        {
            processedProperties.Add(name);
        };

        Func<PropertyDeclarationSyntax, (string orgPropertyName, string propertyName)> getPropertyNames = prop =>
        {
            var name = prop.Identifier.Text;
            return (name, name);
        };

        Func<PropertyDeclarationSyntax, string> getPropertyType = prop => prop.Type.ToString();

        var genericMethod = _processMembersMethod.MakeGenericMethod(typeof(PropertyDeclarationSyntax));
        genericMethod.Invoke(null, new object[]
        {
            classDeclaration,
            compilation,
            processor,
            null,
            null,
            null,
            getPropertyNames,
            getPropertyType
        });

        processedProperties.Should().HaveCount(2);
        processedProperties.Should().Contain("Name");
        processedProperties.Should().Contain("Age");
    }

    [Fact]
    public void ProcessMembers_WithNullClass_ShouldNotThrow()
    {
        var compilation = CreateCompilation("");
        var processedProperties = new List<string>();
        Action<PropertyDeclarationSyntax, string, string, string> processor = (prop, orgName, name, type) =>
        {
            processedProperties.Add(name);
        };

        var genericMethod = _processMembersMethod.MakeGenericMethod(typeof(PropertyDeclarationSyntax));
        var action = () => genericMethod.Invoke(null, new object?[]
        {
            null,
            compilation,
            processor,
            null,
            null,
            null,
            null,
            null
        });

        action.Should().NotThrow();
    }

    [Fact]
    public void ProcessMembers_WithNullProcessor_ShouldNotThrow()
    {
        var sourceCode = @"
namespace TestNamespace
{
    public class TestEntity
    {
        public string Name { get; set; }
    }
}";
        
        var compilation = CreateCompilation(sourceCode);
        var classDeclaration = GetClassDeclaration(compilation, "TestEntity");

        var genericMethod = _processMembersMethod.MakeGenericMethod(typeof(PropertyDeclarationSyntax));
        var action = () => genericMethod.Invoke(null, new object?[]
        {
            classDeclaration,
            compilation,
            null,
            null,
            null,
            null,
            null,
            null
        });

        action.Should().NotThrow();
    }

    [Fact]
    public void ProcessMembers_WithFields_ShouldProcessFields()
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
        
        var processedFields = new List<string>();
        Action<FieldDeclarationSyntax, string, string, string> processor = (field, orgName, name, type) =>
        {
            processedFields.Add(name);
        };

        Func<FieldDeclarationSyntax, (string orgPropertyName, string propertyName)> getPropertyNames = field =>
        {
            var name = field.Declaration.Variables.First().Identifier.Text;
            return (name, name);
        };

        Func<FieldDeclarationSyntax, string> getPropertyType = field => field.Declaration.Type.ToString();

        var genericMethod = _processMembersMethod.MakeGenericMethod(typeof(FieldDeclarationSyntax));
        genericMethod.Invoke(null, new object[]
        {
            classDeclaration,
            compilation,
            processor,
            null,
            null,
            null,
            getPropertyNames,
            getPropertyType
        });

        processedFields.Should().HaveCount(2);
    }

    [Fact]
    public void ProcessMembers_WithIgnoreGenerator_ShouldSkipProperty()
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
        
        var processedProperties = new List<string>();
        Action<PropertyDeclarationSyntax, string, string, string> processor = (prop, orgName, name, type) =>
        {
            processedProperties.Add(name);
        };

        Func<PropertyDeclarationSyntax, (string orgPropertyName, string propertyName)> getPropertyNames = prop =>
        {
            var name = prop.Identifier.Text;
            return (name, name);
        };

        Func<PropertyDeclarationSyntax, string> getPropertyType = prop => prop.Type.ToString();
        
        Func<PropertyDeclarationSyntax, bool> isIgnoreGenerator = prop => prop.Identifier.Text == "Age";

        var genericMethod = _processMembersMethod.MakeGenericMethod(typeof(PropertyDeclarationSyntax));
        genericMethod.Invoke(null, new object[]
        {
            classDeclaration,
            compilation,
            processor,
            null,
            isIgnoreGenerator,
            null,
            getPropertyNames,
            getPropertyType
        });

        processedProperties.Should().HaveCount(1);
        processedProperties.Should().Contain("Name");
    }

    [Fact]
    public void ProcessMembers_WithPrimaryKeyOnly_ShouldProcessOnlyPrimaryKey()
    {
        var sourceCode = @"
namespace TestNamespace
{
    public class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}";
        
        var compilation = CreateCompilation(sourceCode);
        var classDeclaration = GetClassDeclaration(compilation, "TestEntity");
        
        var processedProperties = new List<string>();
        Action<PropertyDeclarationSyntax, string, string, string> processor = (prop, orgName, name, type) =>
        {
            processedProperties.Add(name);
        };

        Func<PropertyDeclarationSyntax, (string orgPropertyName, string propertyName)> getPropertyNames = prop =>
        {
            var name = prop.Identifier.Text;
            return (name, name);
        };

        Func<PropertyDeclarationSyntax, string> getPropertyType = prop => prop.Type.ToString();
        
        Func<PropertyDeclarationSyntax, bool> isPrimary = prop => prop.Identifier.Text == "Id";

        var genericMethod = _processMembersMethod.MakeGenericMethod(typeof(PropertyDeclarationSyntax));
        genericMethod.Invoke(null, new object[]
        {
            classDeclaration,
            compilation,
            processor,
            true,
            null,
            isPrimary,
            getPropertyNames,
            getPropertyType
        });

        processedProperties.Should().HaveCount(1);
        processedProperties.Should().Contain("Id");
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
