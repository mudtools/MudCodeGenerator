// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.CodeGenerator 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Reflection;

namespace Mud.ServiceCodeGenerator.Tests;

/// <summary>
/// GeneratorMessages 生成器消息测试
/// </summary>
public class GeneratorMessagesTests
{
    private readonly Type _messagesType;

    public GeneratorMessagesTests()
    {
        _messagesType = TestHelper.GetType("Mud.ServiceCodeGenerator.ComWrap.GeneratorMessages");
    }

    [Fact]
    public void GeneratorMessages_ShouldBeStaticClass()
    {
        _messagesType.IsClass.Should().BeTrue();
        _messagesType.IsAbstract.Should().BeTrue();
        _messagesType.IsSealed.Should().BeTrue();
    }

    [Fact]
    public void EnumerateCollectionElementFailed_ShouldHaveCorrectValue()
    {
        var property = _messagesType.GetProperty("EnumerateCollectionElementFailed", BindingFlags.Public | BindingFlags.Static);
        property.Should().NotBeNull();
        property!.GetValue(null).Should().Be("枚举集合元素失败");
    }

    [Fact]
    public void ConvertCollectionElementFailed_ShouldHaveCorrectValue()
    {
        var property = _messagesType.GetProperty("ConvertCollectionElementFailed", BindingFlags.Public | BindingFlags.Static);
        property.Should().NotBeNull();
        property!.GetValue(null).Should().Be("转换集合元素失败");
    }

    [Fact]
    public void EnumerateCollectionElementFailedWithDetails_ShouldReturnCorrectMessage()
    {
        var method = TestHelper.GetMethod(_messagesType, "EnumerateCollectionElementFailedWithDetails");
        var result = method.Invoke(null, new object[] { "测试详情" });
        result.Should().Be("枚举集合元素失败: 测试详情");
    }

    [Fact]
    public void ConvertCollectionElementFailedWithDetails_ShouldReturnCorrectMessage()
    {
        var method = TestHelper.GetMethod(_messagesType, "ConvertCollectionElementFailedWithDetails");
        var result = method.Invoke(null, new object[] { "测试详情" });
        result.Should().Be("转换集合元素失败: 测试详情");
    }

    [Fact]
    public void OperationFailed_ShouldReturnCorrectMessage()
    {
        var method = TestHelper.GetMethod(_messagesType, "OperationFailed");
        var result = method.Invoke(null, new object[] { "测试操作" });
        result.Should().Be("执行测试操作操作失败");
    }

    [Fact]
    public void OperationFailedWithDetails_ShouldReturnCorrectMessage()
    {
        var method = TestHelper.GetMethod(_messagesType, "OperationFailedWithDetails");
        var result = method.Invoke(null, new object[] { "测试操作", "错误详情" });
        result.Should().Be("测试操作失败: 错误详情");
    }

    [Theory]
    [InlineData("Add", "添加对象操作")]
    [InlineData("Remove", "移除对象操作")]
    [InlineData("Delete", "删除对象操作")]
    [InlineData("Update", "更新对象操作")]
    [InlineData("Insert", "插入对象操作")]
    [InlineData("Copy", "复制对象操作")]
    [InlineData("Paste", "粘贴对象操作")]
    [InlineData("Select", "选择对象")]
    [InlineData("CustomMethod", "执行CustomMethod操作")]
    public void GetOperationDescription_ShouldReturnCorrectDescription(string methodName, string expected)
    {
        var method = TestHelper.GetMethod(_messagesType, "GetOperationDescription");
        var result = method.Invoke(null, new object[] { methodName });
        result.Should().Be(expected);
    }

    [Fact]
    public void GetObjectByIndexFailed_ShouldHaveCorrectValue()
    {
        var property = _messagesType.GetProperty("GetObjectByIndexFailed", BindingFlags.Public | BindingFlags.Static);
        property.Should().NotBeNull();
        property!.GetValue(null).Should().Be("根据索引获取对象失败");
    }

    [Fact]
    public void SetObjectByIndexFailed_ShouldHaveCorrectValue()
    {
        var property = _messagesType.GetProperty("SetObjectByIndexFailed", BindingFlags.Public | BindingFlags.Static);
        property.Should().NotBeNull();
        property!.GetValue(null).Should().Be("根据索引设置对象失败");
    }

    [Fact]
    public void MissingCountProperty_ShouldHaveCorrectValue()
    {
        var property = _messagesType.GetProperty("MissingCountProperty", BindingFlags.Public | BindingFlags.Static);
        property.Should().NotBeNull();
        property!.GetValue(null).Should().Be("集合接口缺少Count属性，无法生成基于索引的枚举器");
    }

    [Fact]
    public void IndexParameterMustBePositive_ShouldReturnCorrectMessage()
    {
        var method = TestHelper.GetMethod(_messagesType, "IndexParameterMustBePositive");
        var result = method.Invoke(null, new object[] { "第一个" });
        result.Should().Be("第一个索引参数不能少于1");
    }

    [Fact]
    public void ParameterCannotBeNull_ShouldReturnCorrectMessage()
    {
        var method = TestHelper.GetMethod(_messagesType, "ParameterCannotBeNull");
        var result = method.Invoke(null, new object[] { "testParam" });
        result.Should().Be("参数 testParam 不能为空");
    }
}
