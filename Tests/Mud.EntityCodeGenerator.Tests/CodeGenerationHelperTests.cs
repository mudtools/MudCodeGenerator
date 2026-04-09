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
/// CodeGenerationHelper 代码生成辅助工具单元测试
/// </summary>
public class CodeGenerationHelperTests
{
    private readonly Type _codeGenerationHelperType;
    private readonly MethodInfo _generateMethodMethod;
    private readonly MethodInfo _generateClassMethod;
    private readonly MethodInfo _generatePropertyMethod;
    private readonly MethodInfo _generateMappingMethodTemplateMethod;

    public CodeGenerationHelperTests()
    {
        _codeGenerationHelperType = typeof(CodeGenerationHelper);
        _generateMethodMethod = TestHelper.GetMethod(_codeGenerationHelperType, "GenerateMethod");
        _generateClassMethod = TestHelper.GetMethod(_codeGenerationHelperType, "GenerateClass");
        _generatePropertyMethod = TestHelper.GetMethod(_codeGenerationHelperType, "GenerateProperty");
        _generateMappingMethodTemplateMethod = TestHelper.GetMethod(_codeGenerationHelperType, "GenerateMappingMethodTemplate");
    }

    [Fact]
    public void GenerateMethod_WithBasicParameters_ShouldReturnMethodDeclaration()
    {
        var parameters = new List<(string type, string name, string description)>
        {
            ("string", "name", "名称参数"),
            ("int", "age", "年龄参数")
        };

        var result = _generateMethodMethod.Invoke(null, new object[]
        {
            "TestMethod",
            "void",
            parameters,
            false,
            false,
            "",
            "测试方法"
        }) as MethodDeclarationSyntax;

        result.Should().NotBeNull();
        result!.Identifier.Text.Should().Be("TestMethod");
        result.ReturnType.ToString().Should().Be("void");
        result.ParameterList.Parameters.Should().HaveCount(2);
    }

    [Fact]
    public void GenerateMethod_WithStaticModifier_ShouldHaveStaticKeyword()
    {
        var parameters = new List<(string type, string name, string description)>();

        var result = _generateMethodMethod.Invoke(null, new object[]
        {
            "StaticMethod",
            "string",
            parameters,
            true,
            false,
            "return \"test\";",
            "静态方法"
        }) as MethodDeclarationSyntax;

        result.Should().NotBeNull();
        result!.Modifiers.Should().Contain(m => m.IsKind(SyntaxKind.StaticKeyword));
    }

    [Fact]
    public void GenerateMethod_WithExtensionMethod_ShouldHaveThisKeyword()
    {
        var parameters = new List<(string type, string name, string description)>
        {
            ("string", "source", "源字符串")
        };

        var result = _generateMethodMethod.Invoke(null, new object[]
        {
            "ExtensionMethod",
            "string",
            parameters,
            false,
            true,
            "return source;",
            "扩展方法"
        }) as MethodDeclarationSyntax;

        result.Should().NotBeNull();
        result!.ParameterList.Parameters[0].Modifiers.Should().Contain(m => m.IsKind(SyntaxKind.ThisKeyword));
    }

    [Fact]
    public void GenerateClass_WithBasicParameters_ShouldReturnCompilationUnit()
    {
        var result = _generateClassMethod.Invoke(null, new object[]
        {
            "TestClass",
            "TestNamespace",
            false,
            null
        }) as CompilationUnitSyntax;

        result.Should().NotBeNull();
        result!.Members.Should().HaveCount(1);
        
        var namespaceDecl = result.Members[0] as NamespaceDeclarationSyntax;
        namespaceDecl.Should().NotBeNull();
        namespaceDecl!.Name.ToString().Should().Be("TestNamespace");
        
        var classDecl = namespaceDecl.Members[0] as ClassDeclarationSyntax;
        classDecl.Should().NotBeNull();
        classDecl!.Identifier.Text.Should().Be("TestClass");
    }

    [Fact]
    public void GenerateClass_WithStaticModifier_ShouldHaveStaticKeyword()
    {
        var result = _generateClassMethod.Invoke(null, new object[]
        {
            "StaticClass",
            "TestNamespace",
            true,
            null
        }) as CompilationUnitSyntax;

        result.Should().NotBeNull();
        
        var namespaceDecl = result!.Members[0] as NamespaceDeclarationSyntax;
        var classDecl = namespaceDecl!.Members[0] as ClassDeclarationSyntax;
        classDecl!.Modifiers.Should().Contain(m => m.IsKind(SyntaxKind.StaticKeyword));
    }

    [Fact]
    public void GenerateClass_WithBaseTypes_ShouldHaveBaseList()
    {
        var baseTypes = new List<string> { "IDisposable", "ICloneable" };

        var result = _generateClassMethod.Invoke(null, new object[]
        {
            "DerivedClass",
            "TestNamespace",
            false,
            baseTypes
        }) as CompilationUnitSyntax;

        result.Should().NotBeNull();
        
        var namespaceDecl = result!.Members[0] as NamespaceDeclarationSyntax;
        var classDecl = namespaceDecl!.Members[0] as ClassDeclarationSyntax;
        classDecl!.BaseList.Should().NotBeNull();
        classDecl.BaseList!.Types.Should().HaveCount(2);
    }

    [Fact]
    public void GenerateProperty_WithGetterAndSetter_ShouldReturnPropertyDeclaration()
    {
        var result = _generatePropertyMethod.Invoke(null, new object[]
        {
            "TestProperty",
            "string",
            true,
            true,
            null
        }) as PropertyDeclarationSyntax;

        result.Should().NotBeNull();
        result!.Identifier.Text.Should().Be("TestProperty");
        result.Type.ToString().Should().Be("string");
        result.AccessorList!.Accessors.Should().HaveCount(2);
    }

    [Fact]
    public void GenerateProperty_WithOnlyGetter_ShouldHaveOnlyGetAccessor()
    {
        var result = _generatePropertyMethod.Invoke(null, new object[]
        {
            "ReadOnlyProperty",
            "int",
            true,
            false,
            null
        }) as PropertyDeclarationSyntax;

        result.Should().NotBeNull();
        result!.AccessorList!.Accessors.Should().HaveCount(1);
        result.AccessorList.Accessors[0].IsKind(SyntaxKind.GetAccessorDeclaration).Should().BeTrue();
    }

    [Fact]
    public void GenerateProperty_WithInitialValue_ShouldHaveInitializer()
    {
        var result = _generatePropertyMethod.Invoke(null, new object[]
        {
            "InitializedProperty",
            "string",
            true,
            true,
            "\"default\""
        }) as PropertyDeclarationSyntax;

        result.Should().NotBeNull();
        result!.Initializer.Should().NotBeNull();
    }

    [Fact]
    public void GenerateMappingMethodTemplate_WithExtensionMethod_ShouldGenerateCorrectTemplate()
    {
        var mappingLines = new List<string>
        {
            "result.Name = source.Name;",
            "result.Age = source.Age;"
        };

        var result = _generateMappingMethodTemplateMethod.Invoke(null, new object[]
        {
            "SourceType",
            "TargetType",
            mappingLines,
            true
        }) as string;

        result.Should().NotBeNull();
        result!.Should().Contain("MapToTargetType");
        result.Should().Contain("this SourceType source");
        result.Should().Contain("result.Name = source.Name;");
        result.Should().Contain("result.Age = source.Age;");
    }

    [Fact]
    public void GenerateMappingMethodTemplate_WithNonExtensionMethod_ShouldGenerateCorrectTemplate()
    {
        var mappingLines = new List<string>
        {
            "result.Name = source.Name;"
        };

        var result = _generateMappingMethodTemplateMethod.Invoke(null, new object[]
        {
            "SourceType",
            "TargetType",
            mappingLines,
            false
        }) as string;

        result.Should().NotBeNull();
        result!.Should().Contain("MapFromSourceType");
        result.Should().Contain("this TargetType target");
        result.Should().Contain("SourceType source");
    }
}
