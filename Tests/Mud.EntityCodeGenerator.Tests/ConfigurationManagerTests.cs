// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.CodeGenerator 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.EntityCodeGenerator.Helper;

namespace Mud.EntityCodeGenerator.Tests;

/// <summary>
/// ConfigurationManager 配置管理器单元测试
/// </summary>
public class ConfigurationManagerTests
{
    public ConfigurationManagerTests()
    {
        ConfigurationManager.Instance.Reset();
    }

    [Fact]
    public void Instance_ShouldReturnSingletonInstance()
    {
        var instance1 = ConfigurationManager.Instance;
        var instance2 = ConfigurationManager.Instance;

        instance1.Should().BeSameAs(instance2);
    }

    [Fact]
    public void Configuration_ShouldReturnValidConfiguration()
    {
        var config = ConfigurationManager.Instance.Configuration;

        config.Should().NotBeNull();
    }

    [Fact]
    public void GetClassSuffix_WithVoType_ShouldReturnVoSuffix()
    {
        var result = ConfigurationManager.Instance.GetClassSuffix("vo");

        result.Should().Be("ListOutput");
    }

    [Fact]
    public void GetClassSuffix_WithBoType_ShouldReturnBoSuffix()
    {
        var result = ConfigurationManager.Instance.GetClassSuffix("bo");

        result.Should().Be("Bo");
    }

    [Fact]
    public void GetClassSuffix_WithQueryInputType_ShouldReturnQueryInputSuffix()
    {
        var result = ConfigurationManager.Instance.GetClassSuffix("queryinput");

        result.Should().Be("QueryInput");
    }

    [Fact]
    public void GetClassSuffix_WithCrInputType_ShouldReturnCrInputSuffix()
    {
        var result = ConfigurationManager.Instance.GetClassSuffix("crinput");

        result.Should().Be("CrInput");
    }

    [Fact]
    public void GetClassSuffix_WithUpInputType_ShouldReturnUpInputSuffix()
    {
        var result = ConfigurationManager.Instance.GetClassSuffix("upinput");

        result.Should().Be("UpInput");
    }

    [Fact]
    public void GetClassSuffix_WithInfoOutputType_ShouldReturnInfoOutputSuffix()
    {
        var result = ConfigurationManager.Instance.GetClassSuffix("infooutput");

        result.Should().Be("InfoOutput");
    }

    [Fact]
    public void GetClassSuffix_WithListOutputType_ShouldReturnVoSuffix()
    {
        var result = ConfigurationManager.Instance.GetClassSuffix("listoutput");

        result.Should().Be("ListOutput");
    }

    [Fact]
    public void GetClassSuffix_WithUnknownType_ShouldReturnEmpty()
    {
        var result = ConfigurationManager.Instance.GetClassSuffix("unknown");

        result.Should().BeEmpty();
    }

    [Fact]
    public void GetClassSuffix_WithNullType_ShouldReturnEmpty()
    {
        var result = ConfigurationManager.Instance.GetClassSuffix(null);

        result.Should().BeEmpty();
    }

    [Fact]
    public void GetPropertyAttributes_WithVoType_ShouldReturnVoAttributes()
    {
        var result = ConfigurationManager.Instance.GetPropertyAttributes("vo");

        result.Should().NotBeNull();
    }

    [Fact]
    public void GetPropertyAttributes_WithBoType_ShouldReturnBoAttributes()
    {
        var result = ConfigurationManager.Instance.GetPropertyAttributes("bo");

        result.Should().NotBeNull();
    }

    [Fact]
    public void MergePropertyAttributes_ShouldMergeCorrectly()
    {
        var defaultAttributes = new[] { "Required", "StringLength" };

        var result = ConfigurationManager.Instance.MergePropertyAttributes(defaultAttributes, "bo");

        result.Should().NotBeNull();
        result.Should().Contain("Required");
        result.Should().Contain("StringLength");
    }

    [Fact]
    public void IsValid_ShouldReturnTrueAfterInitialization()
    {
        var result = ConfigurationManager.Instance.IsValid();

        result.Should().BeTrue();
    }

    [Fact]
    public void Reset_ShouldCreateNewConfiguration()
    {
        var originalConfig = ConfigurationManager.Instance.Configuration;
        
        ConfigurationManager.Instance.Reset();
        
        var newConfig = ConfigurationManager.Instance.Configuration;
        newConfig.Should().NotBeNull();
    }
}
