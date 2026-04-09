// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.CodeGenerator 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.EntityCodeGenerator.Helper;

namespace Mud.EntityCodeGenerator.Tests;

/// <summary>
/// GeneratorConfiguration 生成器配置单元测试
/// </summary>
public class GeneratorConfigurationTests
{
    [Fact]
    public void GeneratorConfiguration_DefaultValues_ShouldBeCorrect()
    {
        var config = new GeneratorConfiguration();

        config.UsingNameSpaces.Should().BeEmpty();
        config.EntityAttachAttributes.Should().BeEmpty();
        config.VoAttributes.Should().BeEmpty();
        config.BoAttributes.Should().BeEmpty();
        config.EntityPrefix.Should().BeEmpty();
        config.VoSuffix.Should().Be("ListOutput");
        config.InfoOutputSuffix.Should().Be("InfoOutput");
        config.BoSuffix.Should().Be("Bo");
        config.QueryInputSuffix.Should().Be("QueryInput");
        config.CrInputSuffix.Should().Be("CrInput");
        config.UpInputSuffix.Should().Be("UpInput");
    }

    [Fact]
    public void GetPropertyAttributes_WithVoType_ShouldReturnVoAttributes()
    {
        var config = new GeneratorConfiguration();

        var result = config.GetPropertyAttributes("vo");

        result.Should().NotBeNull();
        result.Should().BeSameAs(config.VoAttributes);
    }

    [Fact]
    public void GetPropertyAttributes_WithBoType_ShouldReturnBoAttributes()
    {
        var config = new GeneratorConfiguration();

        var result = config.GetPropertyAttributes("bo");

        result.Should().NotBeNull();
        result.Should().BeSameAs(config.BoAttributes);
    }

    [Fact]
    public void GetPropertyAttributes_WithUnknownType_ShouldReturnEmptyArray()
    {
        var config = new GeneratorConfiguration();

        var result = config.GetPropertyAttributes("unknown");

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public void MergePropertyAttributes_WithNullDefaultAttributes_ShouldReturnConfigAttributes()
    {
        var config = new GeneratorConfiguration();
        var configAttributes = new[] { "Attr1", "Attr2" };
        config.GetType().GetProperty("VoAttributes")?.SetValue(config, configAttributes);

        var result = config.MergePropertyAttributes(null, "vo");

        result.Should().NotBeNull();
        result.Should().Contain("Attr1");
        result.Should().Contain("Attr2");
    }

    [Fact]
    public void MergePropertyAttributes_WithEmptyConfigAttributes_ShouldReturnDefaultAttributes()
    {
        var config = new GeneratorConfiguration();
        var defaultAttributes = new[] { "Default1", "Default2" };

        var result = config.MergePropertyAttributes(defaultAttributes, "vo");

        result.Should().NotBeNull();
        result.Should().Contain("Default1");
        result.Should().Contain("Default2");
    }

    [Fact]
    public void MergePropertyAttributes_WithBothAttributes_ShouldMergeAndDeduplicate()
    {
        var config = new GeneratorConfiguration();
        var configAttributes = new[] { "Attr1", "Attr2" };
        config.GetType().GetProperty("VoAttributes")?.SetValue(config, configAttributes);
        var defaultAttributes = new[] { "Default1", "Attr1" };

        var result = config.MergePropertyAttributes(defaultAttributes, "vo");

        result.Should().NotBeNull();
        result.Should().Contain("Default1");
        result.Should().Contain("Attr1");
        result.Should().Contain("Attr2");
        result.Distinct().Count().Should().Be(result.Length);
    }

    [Fact]
    public void IsValid_WithValidConfiguration_ShouldReturnTrue()
    {
        var config = new GeneratorConfiguration();

        var result = config.IsValid();

        result.Should().BeTrue();
    }

    [Fact]
    public void GetPropertyAttributes_WithCaseInsensitiveType_ShouldWork()
    {
        var config = new GeneratorConfiguration();
        var voAttributes = new[] { "TestAttr" };
        config.GetType().GetProperty("VoAttributes")?.SetValue(config, voAttributes);

        var result = config.GetPropertyAttributes("VO");

        result.Should().Contain("TestAttr");
    }
}
