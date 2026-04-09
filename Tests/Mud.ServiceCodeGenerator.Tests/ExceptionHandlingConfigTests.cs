// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.CodeGenerator 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Reflection;

namespace Mud.ServiceCodeGenerator.Tests;

/// <summary>
/// ExceptionHandlingConfig 异常处理配置测试
/// </summary>
public class ExceptionHandlingConfigTests
{
    [Fact]
    public void ExceptionHandlingConfig_DefaultValues_ShouldBeCorrect()
    {
        var configType = TestHelper.GetType("Mud.ServiceCodeGenerator.ComWrap.ExceptionHandlingConfig");
        var config = Activator.CreateInstance(configType);
        
        var exceptionTypeNameProperty = configType.GetProperty("ExceptionTypeName");
        var includeOriginalMessageProperty = configType.GetProperty("IncludeOriginalMessage");
        var messagePrefixProperty = configType.GetProperty("MessagePrefix");
        
        exceptionTypeNameProperty!.GetValue(config).Should().Be("OfficeOperationException");
        includeOriginalMessageProperty!.GetValue(config).Should().Be(true);
        messagePrefixProperty!.GetValue(config).Should().Be("操作");
    }

    [Fact]
    public void ExceptionHandlingConfig_SetValues_ShouldWork()
    {
        var configType = TestHelper.GetType("Mud.ServiceCodeGenerator.ComWrap.ExceptionHandlingConfig");
        var config = Activator.CreateInstance(configType);
        
        var exceptionTypeNameProperty = configType.GetProperty("ExceptionTypeName");
        var includeOriginalMessageProperty = configType.GetProperty("IncludeOriginalMessage");
        var messagePrefixProperty = configType.GetProperty("MessagePrefix");
        
        exceptionTypeNameProperty!.SetValue(config, "CustomException");
        includeOriginalMessageProperty!.SetValue(config, false);
        messagePrefixProperty!.SetValue(config, "自定义");
        
        exceptionTypeNameProperty.GetValue(config).Should().Be("CustomException");
        includeOriginalMessageProperty.GetValue(config).Should().Be(false);
        messagePrefixProperty.GetValue(config).Should().Be("自定义");
    }

    [Fact]
    public void ExceptionHandlingConfigProvider_ShouldExist()
    {
        var providerType = TestHelper.GetType("Mud.ServiceCodeGenerator.ComWrap.ExceptionHandlingConfigProvider");
        providerType.Should().NotBeNull();
        providerType!.IsClass.Should().BeTrue();
        providerType.IsSealed.Should().BeTrue();
    }

    [Fact]
    public void ExceptionHandlingConfigProvider_ShouldHaveGetConfigMethod()
    {
        var providerType = TestHelper.GetType("Mud.ServiceCodeGenerator.ComWrap.ExceptionHandlingConfigProvider");
        var getConfigMethod = providerType.GetMethod("GetConfig", BindingFlags.Public | BindingFlags.Static);
        getConfigMethod.Should().NotBeNull();
    }
}
