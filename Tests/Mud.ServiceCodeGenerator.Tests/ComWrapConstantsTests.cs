// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.CodeGenerator 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Reflection;

namespace Mud.ServiceCodeGenerator.Tests;

/// <summary>
/// ComWrapConstants 常量测试
/// </summary>
public class ComWrapConstantsTests
{
    private readonly Type _constantsType;

    public ComWrapConstantsTests()
    {
        _constantsType = TestHelper.GetType("Mud.ServiceCodeGenerator.ComWrap.ComWrapConstants");
    }

    [Fact]
    public void ComObjectWrapAttributeNames_ShouldContainCorrectValues()
    {
        var field = _constantsType.GetField("ComObjectWrapAttributeNames", BindingFlags.Public | BindingFlags.Static);
        field.Should().NotBeNull();
        
        var value = field!.GetValue(null) as string[];
        value.Should().NotBeNull();
        value.Should().Contain("ComObjectWrapAttribute");
        value.Should().Contain("ComObjectWrap");
    }

    [Fact]
    public void ComCollectionWrapAttributeNames_ShouldContainCorrectValues()
    {
        var field = _constantsType.GetField("ComCollectionWrapAttributeNames", BindingFlags.Public | BindingFlags.Static);
        field.Should().NotBeNull();
        
        var value = field!.GetValue(null) as string[];
        value.Should().NotBeNull();
        value.Should().Contain("ComCollectionWrapAttribute");
        value.Should().Contain("ComCollectionWrap");
    }

    [Fact]
    public void DefaultComNamespace_ShouldHaveCorrectValue()
    {
        var field = _constantsType.GetField("DefaultComNamespace", BindingFlags.Public | BindingFlags.Static);
        field.Should().NotBeNull();
        field!.GetValue(null).Should().Be("UNKNOWN_NAMESPACE");
    }

    [Fact]
    public void DefaultComClassName_ShouldHaveCorrectValue()
    {
        var field = _constantsType.GetField("DefaultComClassName", BindingFlags.Public | BindingFlags.Static);
        field.Should().NotBeNull();
        field!.GetValue(null).Should().Be("UNKNOWN_CLASS");
    }

    [Fact]
    public void DefaultExceptionTypeName_ShouldHaveCorrectValue()
    {
        var field = _constantsType.GetField("DefaultExceptionTypeName", BindingFlags.Public | BindingFlags.Static);
        field.Should().NotBeNull();
        field!.GetValue(null).Should().Be("OfficeOperationException");
    }

    [Fact]
    public void KnownPrefixes_ShouldContainExpectedPrefixes()
    {
        var field = _constantsType.GetField("KnownPrefixes", BindingFlags.Public | BindingFlags.Static);
        field.Should().NotBeNull();
        
        var value = field!.GetValue(null) as string[];
        value.Should().NotBeNull();
        value.Should().Contain("IWord");
        value.Should().Contain("IExcel");
        value.Should().Contain("IOffice");
    }

    [Fact]
    public void ComNamespaceProperty_ShouldHaveCorrectValue()
    {
        var field = _constantsType.GetField("ComNamespaceProperty", BindingFlags.Public | BindingFlags.Static);
        field.Should().NotBeNull();
        field!.GetValue(null).Should().Be("ComNamespace");
    }

    [Fact]
    public void NeedConvertProperty_ShouldHaveCorrectValue()
    {
        var field = _constantsType.GetField("NeedConvertProperty", BindingFlags.Public | BindingFlags.Static);
        field.Should().NotBeNull();
        field!.GetValue(null).Should().Be("NeedConvert");
    }

    [Fact]
    public void NoneConstructorProperty_ShouldHaveCorrectValue()
    {
        var field = _constantsType.GetField("NoneConstructorProperty", BindingFlags.Public | BindingFlags.Static);
        field.Should().NotBeNull();
        field!.GetValue(null).Should().Be("NoneConstructor");
    }

    [Fact]
    public void ExceptionMessageTemplate_ShouldHaveCorrectValue()
    {
        var field = _constantsType.GetField("ExceptionMessageTemplate", BindingFlags.Public | BindingFlags.Static);
        field.Should().NotBeNull();
        field!.GetValue(null).Should().Be("{0}失败: {1}");
    }
}
