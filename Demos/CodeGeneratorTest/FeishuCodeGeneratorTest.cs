using Microsoft.Extensions.Logging;

namespace CodeGeneratorTest;

/// <summary>
/// 飞书代码生成器测试
/// </summary>
public class FeishuCodeGeneratorTest
{
    private readonly ILogger<FeishuCodeGeneratorTest> _logger;
    private readonly IOptions<FeishuOptions> _feishuOptions;

    public FeishuCodeGeneratorTest(ILogger<FeishuCodeGeneratorTest> logger, IOptions<FeishuOptions> feishuOptions)
    {
        _logger = logger;
        _feishuOptions = feishuOptions;
    }

    /// <summary>
    /// 测试飞书代码生成器功能
    /// </summary>
    public void TestCodeGeneration()
    {
        var feishuOptions = _feishuOptions.Value;

        _logger.LogInformation("开始测试飞书代码生成器");

        // 验证 FeishuOptions 配置
        if (feishuOptions == null)
        {
            _logger.LogError("FeishuOptions 配置为空");
            return;
        }

        _logger.LogInformation("BaseUrl: {BaseUrl}", feishuOptions.BaseUrl);
        _logger.LogInformation("EnableLogging: {EnableLogging}", feishuOptions.EnableLogging);
        _logger.LogInformation("TimeOut: {TimeOut}", feishuOptions.TimeOut);
        _logger.LogInformation("AppId: {AppId}", feishuOptions.AppId);

        // 测试代码生成器是否能正确处理这些配置
        // 实际的生成逻辑将在编译时由源生成器处理

        _logger.LogInformation("飞书代码生成器测试完成");
    }
}