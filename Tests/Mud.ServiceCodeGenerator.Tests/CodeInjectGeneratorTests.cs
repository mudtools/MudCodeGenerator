// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.CodeGenerator 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Reflection;

namespace Mud.ServiceCodeGenerator.Tests;

/// <summary>
/// CodeInjectGenerator 代码注入生成器测试
/// </summary>
public class CodeInjectGeneratorTests
{
    private readonly Type _generatorType;

    public CodeInjectGeneratorTests()
    {
        _generatorType = TestHelper.GetType("Mud.ServiceCodeGenerator.CodeInject.CodeInjectGenerator");
    }

    [Fact]
    public void CodeInjectGenerator_ShouldHaveGeneratorAttribute()
    {
        var attribute = _generatorType.GetCustomAttribute<Microsoft.CodeAnalysis.GeneratorAttribute>();
        attribute.Should().NotBeNull();
    }

    [Fact]
    public void CodeInjectGenerator_ShouldBePartialClass()
    {
        _generatorType.IsClass.Should().BeTrue();
        _generatorType.IsAbstract.Should().BeFalse();
    }

    [Fact]
    public void InjectionContext_ShouldExist()
    {
        var contextType = _generatorType.GetNestedType("InjectionContext", BindingFlags.NonPublic);
        contextType.Should().NotBeNull();
    }

    [Fact]
    public void InjectionRequirements_ShouldExist()
    {
        var requirementsType = _generatorType.GetNestedType("InjectionRequirements", BindingFlags.NonPublic);
        requirementsType.Should().NotBeNull();
    }

    [Fact]
    public void IInjector_ShouldExist()
    {
        var injectorType = _generatorType.GetNestedType("IInjector", BindingFlags.NonPublic);
        injectorType.Should().NotBeNull();
        injectorType!.IsInterface.Should().BeTrue();
    }

    [Fact]
    public void ConstructorInjector_ShouldExist()
    {
        var injectorType = _generatorType.GetNestedType("ConstructorInjector", BindingFlags.NonPublic);
        injectorType.Should().NotBeNull();
    }

    [Fact]
    public void LoggerInjector_ShouldExist()
    {
        var injectorType = _generatorType.GetNestedType("LoggerInjector", BindingFlags.NonPublic);
        injectorType.Should().NotBeNull();
    }

    [Fact]
    public void CacheManagerInjector_ShouldExist()
    {
        var injectorType = _generatorType.GetNestedType("CacheManagerInjector", BindingFlags.NonPublic);
        injectorType.Should().NotBeNull();
    }

    [Fact]
    public void UserManagerInjector_ShouldExist()
    {
        var injectorType = _generatorType.GetNestedType("UserManagerInjector", BindingFlags.NonPublic);
        injectorType.Should().NotBeNull();
    }

    [Fact]
    public void OptionsInjector_ShouldExist()
    {
        var injectorType = _generatorType.GetNestedType("OptionsInjector", BindingFlags.NonPublic);
        injectorType.Should().NotBeNull();
    }

    [Fact]
    public void CustomInjector_ShouldExist()
    {
        var injectorType = _generatorType.GetNestedType("CustomInjector", BindingFlags.NonPublic);
        injectorType.Should().NotBeNull();
    }

    [Fact]
    public void InjectionContext_AddParameter_ShouldWork()
    {
        var contextType = _generatorType.GetNestedType("InjectionContext", BindingFlags.NonPublic);
        var classDeclaration = CreateTestClassDeclaration();
        
        var context = Activator.CreateInstance(contextType!, "TestClass", classDeclaration);
        
        var addParameterMethod = contextType!.GetMethod("AddParameter", BindingFlags.Public | BindingFlags.Instance);
        
        var parameter = SyntaxFactory.Parameter(
            SyntaxFactory.List<AttributeListSyntax>(),
            SyntaxFactory.TokenList(),
            SyntaxFactory.ParseTypeName("string"),
            SyntaxFactory.Identifier("testParam"),
            null);
        
        addParameterMethod!.Invoke(context, new object[] { parameter });
        
        var hasParameterMethod = contextType.GetMethod("HasParameter", BindingFlags.Public | BindingFlags.Instance);
        var hasParameter = (bool)hasParameterMethod!.Invoke(context, new object[] { "testParam" })!;
        
        hasParameter.Should().BeTrue();
    }

    [Fact]
    public void InjectionContext_HasParameter_WithNonExistentParameter_ShouldReturnFalse()
    {
        var contextType = _generatorType.GetNestedType("InjectionContext", BindingFlags.NonPublic);
        var classDeclaration = CreateTestClassDeclaration();
        
        var context = Activator.CreateInstance(contextType!, "TestClass", classDeclaration);
        
        var hasParameterMethod = contextType!.GetMethod("HasParameter", BindingFlags.Public | BindingFlags.Instance);
        var hasParameter = (bool)hasParameterMethod!.Invoke(context, new object[] { "nonExistentParam" })!;
        
        hasParameter.Should().BeFalse();
    }

    private static ClassDeclarationSyntax CreateTestClassDeclaration()
    {
        var sourceCode = @"
namespace TestNamespace
{
    public class TestClass
    {
        private string _field;
    }
}";
        
        var compilation = TestHelper.CreateCompilation(sourceCode);
        return TestHelper.GetClassDeclaration(compilation, "TestClass");
    }
}
