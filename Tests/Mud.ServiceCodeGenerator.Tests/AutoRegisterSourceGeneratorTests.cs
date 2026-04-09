// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.CodeGenerator 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Reflection;

namespace Mud.ServiceCodeGenerator.Tests;

/// <summary>
/// AutoRegisterSourceGenerator 自动注册源代码生成器测试
/// </summary>
public class AutoRegisterSourceGeneratorTests
{
    private readonly Type _generatorType;

    public AutoRegisterSourceGeneratorTests()
    {
        _generatorType = TestHelper.GetType("Mud.ServiceCodeGenerator.CodeInject.AutoRegisterSourceGenerator");
    }

    [Fact]
    public void AutoRegisterSourceGenerator_ShouldHaveGeneratorAttribute()
    {
        var attribute = _generatorType.GetCustomAttribute<Microsoft.CodeAnalysis.GeneratorAttribute>();
        attribute.Should().NotBeNull();
    }

    [Fact]
    public void AutoRegisterSourceGenerator_ShouldBePartialClass()
    {
        _generatorType.IsClass.Should().BeTrue();
        _generatorType.IsAbstract.Should().BeFalse();
    }

    [Fact]
    public void AutoRegisterMetadata_ShouldExist()
    {
        var metadataType = _generatorType.GetNestedType("AutoRegisterMetadata", BindingFlags.NonPublic);
        metadataType.Should().NotBeNull();
    }

    [Fact]
    public void AutoRegisterMetadata_ShouldHaveCorrectProperties()
    {
        var metadataType = _generatorType.GetNestedType("AutoRegisterMetadata", BindingFlags.NonPublic);
        metadataType.Should().NotBeNull();
        
        var implTypeProperty = metadataType!.GetProperty("ImplType");
        var baseTypeProperty = metadataType.GetProperty("BaseType");
        var lifeTimeProperty = metadataType.GetProperty("LifeTime");
        var keyProperty = metadataType.GetProperty("Key");
        
        implTypeProperty.Should().NotBeNull();
        baseTypeProperty.Should().NotBeNull();
        lifeTimeProperty.Should().NotBeNull();
        keyProperty.Should().NotBeNull();
    }

    [Fact]
    public void AutoRegisterMetadata_Constructor_ShouldSetProperties()
    {
        var metadataType = _generatorType.GetNestedType("AutoRegisterMetadata", BindingFlags.NonPublic);
        metadataType.Should().NotBeNull();
        
        var constructor = metadataType!.GetConstructors().First();
        var metadata = constructor.Invoke(new object[] { "TestImpl", "ITestBase", "AddScoped" });
        
        var implTypeProperty = metadataType.GetProperty("ImplType");
        var baseTypeProperty = metadataType.GetProperty("BaseType");
        var lifeTimeProperty = metadataType.GetProperty("LifeTime");
        
        implTypeProperty!.GetValue(metadata).Should().Be("TestImpl");
        baseTypeProperty!.GetValue(metadata).Should().Be("ITestBase");
        lifeTimeProperty!.GetValue(metadata).Should().Be("AddScoped");
    }

    [Fact]
    public void InjectAttributeType_Enum_ShouldHaveCorrectValues()
    {
        var enumType = _generatorType.GetNestedType("InjectAttributeType", BindingFlags.NonPublic);
        enumType.Should().NotBeNull();
        enumType!.IsEnum.Should().BeTrue();
        
        var names = Enum.GetNames(enumType);
        names.Should().Contain("Regular");
        names.Should().Contain("Generic");
        names.Should().Contain("Keyed");
        names.Should().Contain("KeyedGeneric");
        names.Should().Contain("Unknown");
    }

    [Fact]
    public void AttributeNames_ShouldHaveCorrectValues()
    {
        var attributeNamesType = _generatorType.GetNestedType("AttributeNames", BindingFlags.NonPublic | BindingFlags.Static);
        attributeNamesType.Should().NotBeNull();
        
        var autoRegisterField = attributeNamesType!.GetField("AutoRegister", BindingFlags.Public | BindingFlags.Static);
        var autoRegisterKeyedField = attributeNamesType.GetField("AutoRegisterKeyed", BindingFlags.Public | BindingFlags.Static);
        
        autoRegisterField.Should().NotBeNull();
        autoRegisterField!.GetValue(null).Should().Be("AutoRegisterAttribute");
        
        autoRegisterKeyedField.Should().NotBeNull();
        autoRegisterKeyedField!.GetValue(null).Should().Be("AutoRegisterKeyedAttribute");
    }
}
