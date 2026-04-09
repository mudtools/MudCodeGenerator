// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.CodeGenerator 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.ServiceCodeGenerator.Tests;

/// <summary>
/// ServiceCodeBaseGenerator 服务代码生成器基类测试
/// </summary>
public class ServiceCodeBaseGeneratorTests
{
    [Fact]
    public void ServiceCodeBaseGenerator_ShouldBeAbstract()
    {
        var generatorType = TestHelper.GetType("Mud.ServiceCodeGenerator.ServiceCodeBaseGenerator");
        
        generatorType.IsAbstract.Should().BeTrue();
    }

    [Fact]
    public void ServiceCodeBaseGenerator_ShouldHaveServiceGeneratorAttributeName()
    {
        var generatorType = TestHelper.GetType("Mud.ServiceCodeGenerator.ServiceCodeBaseGenerator");
        
        var field = generatorType.GetField("ServiceGeneratorAttributeName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        field.Should().NotBeNull();
        field!.GetValue(null).Should().Be("ServiceGeneratorAttribute");
    }

    [Fact]
    public void ServiceCodeBaseGenerator_ShouldHaveInitializeMethod()
    {
        var generatorType = TestHelper.GetType("Mud.ServiceCodeGenerator.ServiceCodeBaseGenerator");
        
        var initializeMethod = generatorType.GetMethod("Initialize");
        initializeMethod.Should().NotBeNull();
    }

    [Fact]
    public void ServiceCodeBaseGenerator_ShouldHaveGenerateCodeMethod()
    {
        var generatorType = TestHelper.GetType("Mud.ServiceCodeGenerator.ServiceCodeBaseGenerator");
        
        var generateCodeMethod = generatorType.GetMethod("GenerateCode", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        generateCodeMethod.Should().NotBeNull();
        generateCodeMethod!.IsAbstract.Should().BeTrue();
    }

    [Fact]
    public void ServiceCodeBaseGenerator_ShouldHaveIsIgnoreGeneratorMethod()
    {
        var generatorType = TestHelper.GetType("Mud.ServiceCodeGenerator.ServiceCodeBaseGenerator");
        
        var isIgnoreGeneratorMethod = generatorType.GetMethod("IsIgnoreGenerator", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        isIgnoreGeneratorMethod.Should().NotBeNull();
    }

    [Fact]
    public void ServiceCodeBaseGenerator_ShouldHaveGetOrderByAttributeMethod()
    {
        var generatorType = TestHelper.GetType("Mud.ServiceCodeGenerator.ServiceCodeBaseGenerator");
        
        var getOrderByAttributeMethod = generatorType.GetMethod("GetOrderByAttribute", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        getOrderByAttributeMethod.Should().NotBeNull();
    }

    [Fact]
    public void ServiceCodeBaseGenerator_ShouldHaveOrderByNestedClass()
    {
        var generatorType = TestHelper.GetType("Mud.ServiceCodeGenerator.ServiceCodeBaseGenerator");
        
        var orderByType = generatorType.GetNestedType("OrderBy", System.Reflection.BindingFlags.NonPublic);
        orderByType.Should().NotBeNull();
    }

    [Fact]
    public void OrderBy_ShouldHaveCorrectProperties()
    {
        var generatorType = TestHelper.GetType("Mud.ServiceCodeGenerator.ServiceCodeBaseGenerator");
        var orderByType = generatorType.GetNestedType("OrderBy", System.Reflection.BindingFlags.NonPublic);
        
        orderByType.Should().NotBeNull();
        
        var propertyNameProperty = orderByType!.GetProperty("PropertyName");
        var isAscProperty = orderByType.GetProperty("IsAsc");
        var orderNumProperty = orderByType.GetProperty("OrderNum");
        
        propertyNameProperty.Should().NotBeNull();
        isAscProperty.Should().NotBeNull();
        orderNumProperty.Should().NotBeNull();
    }
}
