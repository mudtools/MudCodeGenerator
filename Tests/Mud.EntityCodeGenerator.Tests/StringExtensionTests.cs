// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.CodeGenerator 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Reflection;

namespace Mud.EntityCodeGenerator.Tests;

/// <summary>
/// 字符串扩展方法单元测试
/// </summary>
public class StringExtensionTests
{
    private readonly Type _stringExtensionsType;
    private readonly MethodInfo _removeSuffixMethod;
    private readonly MethodInfo _splitStringMethod;

    public StringExtensionTests()
    {
        _stringExtensionsType = TestHelper.GetType("Mud.CodeGenerator.StringExtensions");
        _removeSuffixMethod = TestHelper.GetMethod(_stringExtensionsType, "RemoveSuffix", new[] { typeof(string), typeof(string), typeof(bool) });
        _splitStringMethod = TestHelper.GetMethod(_stringExtensionsType, "SplitString");
    }

    [Theory]
    [InlineData("TestAttribute", "Test")]
    [InlineData("RequiredAttribute", "Required")]
    [InlineData("StringLengthAttribute", "StringLength")]
    [InlineData("MaxLengthAttribute", "MaxLength")]
    [InlineData("Test", "Test")]
    [InlineData("", "")]
    public void RemoveSuffix_WithAttributeSuffix_ShouldRemoveSuffix(string input, string expected)
    {
        var result = _removeSuffixMethod.Invoke(null, new object[] { input, "Attribute", false }) as string;

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("Testattribute", "Test", true)]
    [InlineData("Testattribute", "Testattribute", false)]
    public void RemoveSuffix_WithCaseSensitive_ShouldRespectCase(string input, string expected, bool ignoreCase)
    {
        var result = _removeSuffixMethod.Invoke(null, new object[] { input, "Attribute", ignoreCase }) as string;

        result.Should().Be(expected);
    }

    [Fact]
    public void RemoveSuffix_WithNullInput_ShouldReturnNull()
    {
        var result = _removeSuffixMethod.Invoke(null, new object?[] { null, "Attribute", false }) as string;

        result.Should().BeNull();
    }

    [Fact]
    public void RemoveSuffix_WithEmptyInput_ShouldReturnEmpty()
    {
        var result = _removeSuffixMethod.Invoke(null, new object[] { "", "Attribute", false }) as string;

        result.Should().BeEmpty();
    }

    [Fact]
    public void RemoveSuffix_WithNullSuffix_ShouldReturnOriginal()
    {
        var result = _removeSuffixMethod.Invoke(null, new object?[] { "Test", null, false }) as string;

        result.Should().Be("Test");
    }

    [Theory]
    [InlineData("A,B,C", new[] { "A", "B", "C" })]
    [InlineData("A, B, C", new[] { "A", "B", "C" })]
    [InlineData("A,B,", new[] { "A", "B" })]
    [InlineData("", new string[0])]
    public void SplitString_WithCommaSeparator_ShouldSplitCorrectly(string input, string[] expected)
    {
        Func<string, string> trimFunc = s => s.Trim();
        var result = _splitStringMethod.Invoke(null, new object[] { input, ',', trimFunc }) as string[];

        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void SplitString_WithNullInput_ShouldReturnEmptyArray()
    {
        Func<string, string> identityFunc = s => s;
        var result = _splitStringMethod.Invoke(null, new object?[] { null, ',', identityFunc }) as string[];

        result.Should().BeEmpty();
    }

    [Fact]
    public void SplitString_WithEmptyInput_ShouldReturnEmptyArray()
    {
        Func<string, string> identityFunc = s => s;
        var result = _splitStringMethod.Invoke(null, new object[] { "", ',', identityFunc }) as string[];

        result.Should().BeEmpty();
    }
}
