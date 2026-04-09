// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.CodeGenerator 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Reflection;

namespace Mud.ServiceCodeGenerator.Tests;

/// <summary>
/// CodeInjectGeneratorConstants 常量测试
/// </summary>
public class CodeInjectGeneratorConstantsTests
{
    private readonly Type _constantsType;

    public CodeInjectGeneratorConstantsTests()
    {
        _constantsType = TestHelper.GetType("Mud.ServiceCodeGenerator.CodeInject.CodeInjectGeneratorConstants");
    }

    [Fact]
    public void ConstructorInjectAttribute_ShouldHaveCorrectValue()
    {
        var field = _constantsType.GetField("ConstructorInjectAttribute", BindingFlags.Public | BindingFlags.Static);
        field.Should().NotBeNull();
        field!.GetValue(null).Should().Be("ConstructorInjectAttribute");
    }

    [Fact]
    public void LoggerInjectAttribute_ShouldHaveCorrectValue()
    {
        var field = _constantsType.GetField("LoggerInjectAttribute", BindingFlags.Public | BindingFlags.Static);
        field.Should().NotBeNull();
        field!.GetValue(null).Should().Be("LoggerInjectAttribute");
    }

    [Fact]
    public void OptionsInjectAttribute_ShouldHaveCorrectValue()
    {
        var field = _constantsType.GetField("OptionsInjectAttribute", BindingFlags.Public | BindingFlags.Static);
        field.Should().NotBeNull();
        field!.GetValue(null).Should().Be("OptionsInjectAttribute");
    }

    [Fact]
    public void CacheManagerInjectAttribute_ShouldHaveCorrectValue()
    {
        var field = _constantsType.GetField("CacheManagerInjectAttribute", BindingFlags.Public | BindingFlags.Static);
        field.Should().NotBeNull();
        field!.GetValue(null).Should().Be("CacheInjectAttribute");
    }

    [Fact]
    public void UserManagerInjectAttribute_ShouldHaveCorrectValue()
    {
        var field = _constantsType.GetField("UserManagerInjectAttribute", BindingFlags.Public | BindingFlags.Static);
        field.Should().NotBeNull();
        field!.GetValue(null).Should().Be("UserInjectAttribute");
    }

    [Fact]
    public void CustomInjectAttribute_ShouldHaveCorrectValue()
    {
        var field = _constantsType.GetField("CustomInjectAttribute", BindingFlags.Public | BindingFlags.Static);
        field.Should().NotBeNull();
        field!.GetValue(null).Should().Be("CustomInjectAttribute");
    }

    [Fact]
    public void DefaultCacheManagerType_ShouldHaveCorrectValue()
    {
        var field = _constantsType.GetField("DefaultCacheManagerType", BindingFlags.Public | BindingFlags.Static);
        field.Should().NotBeNull();
        field!.GetValue(null).Should().Be("ICacheManager");
    }

    [Fact]
    public void DefaultUserManagerType_ShouldHaveCorrectValue()
    {
        var field = _constantsType.GetField("DefaultUserManagerType", BindingFlags.Public | BindingFlags.Static);
        field.Should().NotBeNull();
        field!.GetValue(null).Should().Be("IUserManager");
    }

    [Fact]
    public void DefaultLoggerVariable_ShouldHaveCorrectValue()
    {
        var field = _constantsType.GetField("DefaultLoggerVariable", BindingFlags.Public | BindingFlags.Static);
        field.Should().NotBeNull();
        field!.GetValue(null).Should().Be("_logger");
    }

    [Fact]
    public void MatchesAttribute_WithFullName_ShouldReturnTrue()
    {
        var method = TestHelper.GetMethod(_constantsType, "MatchesAttribute");
        
        var result = (bool)method.Invoke(null, new object[] { "CustomInjectAttribute", "CustomInject", "CustomInjectAttribute", "CustomInject<" })!;
        
        result.Should().BeTrue();
    }

    [Fact]
    public void MatchesAttribute_WithShortName_ShouldReturnTrue()
    {
        var method = TestHelper.GetMethod(_constantsType, "MatchesAttribute");
        
        var result = (bool)method.Invoke(null, new object[] { "CustomInject", "CustomInject", "CustomInjectAttribute", "CustomInject<" })!;
        
        result.Should().BeTrue();
    }

    [Fact]
    public void MatchesAttribute_WithGenericName_ShouldReturnTrue()
    {
        var method = TestHelper.GetMethod(_constantsType, "MatchesAttribute");
        
        var result = (bool)method.Invoke(null, new object[] { "CustomInject<IMenuRepository>", "CustomInject", "CustomInjectAttribute", "CustomInject<" })!;
        
        result.Should().BeTrue();
    }

    [Fact]
    public void MatchesAttribute_WithNonMatchingName_ShouldReturnFalse()
    {
        var method = TestHelper.GetMethod(_constantsType, "MatchesAttribute");
        
        var result = (bool)method.Invoke(null, new object[] { "OtherAttribute", "CustomInject", "CustomInjectAttribute", "CustomInject<" })!;
        
        result.Should().BeFalse();
    }
}
