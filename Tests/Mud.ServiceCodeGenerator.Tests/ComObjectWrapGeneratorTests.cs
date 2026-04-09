// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.CodeGenerator 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.ServiceCodeGenerator.Tests;

/// <summary>
/// ComObjectWrapGenerator COM对象包装生成器测试
/// </summary>
public class ComObjectWrapGeneratorTests
{
    [Fact]
    public void ComObjectWrapGenerator_ShouldHaveGeneratorAttribute()
    {
        var generatorType = TestHelper.GetType("Mud.ServiceCodeGenerator.ComWrap.ComObjectWrapGenerator");
        
        var attribute = generatorType.GetCustomAttribute<Microsoft.CodeAnalysis.GeneratorAttribute>();
        attribute.Should().NotBeNull();
    }

    [Fact]
    public void ComObjectWrapGenerator_ShouldBeClass()
    {
        var generatorType = TestHelper.GetType("Mud.ServiceCodeGenerator.ComWrap.ComObjectWrapGenerator");
        
        generatorType.IsClass.Should().BeTrue();
        generatorType.IsAbstract.Should().BeFalse();
    }

    [Fact]
    public void ComObjectWrapGenerator_ShouldInheritFromBaseGenerator()
    {
        var generatorType = TestHelper.GetType("Mud.ServiceCodeGenerator.ComWrap.ComObjectWrapGenerator");
        var baseType = TestHelper.GetType("Mud.ServiceCodeGenerator.ComWrap.ComObjectWrapBaseGenerator");
        
        generatorType.BaseType.Should().Be(baseType);
    }

    [Fact]
    public void ComObjectWrapBaseGenerator_ShouldBeAbstract()
    {
        var baseGeneratorType = TestHelper.GetType("Mud.ServiceCodeGenerator.ComWrap.ComObjectWrapBaseGenerator");
        
        baseGeneratorType.IsAbstract.Should().BeTrue();
    }

    [Fact]
    public void ComObjectWrapBaseGenerator_ShouldHaveInitializeMethod()
    {
        var baseGeneratorType = TestHelper.GetType("Mud.ServiceCodeGenerator.ComWrap.ComObjectWrapBaseGenerator");
        
        var initializeMethod = baseGeneratorType.GetMethod("Initialize");
        initializeMethod.Should().NotBeNull();
    }

    [Fact]
    public void ComObjectWrapBaseGenerator_ShouldHaveHasComWrapAttributesMethod()
    {
        var baseGeneratorType = TestHelper.GetType("Mud.ServiceCodeGenerator.ComWrap.ComObjectWrapBaseGenerator");
        
        var methods = baseGeneratorType.GetMethods(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .Where(m => m.Name == "HasComWrapAttributes")
            .ToList();
        
        methods.Should().NotBeEmpty();
    }
}
