using Microsoft.CodeAnalysis.Diagnostics;

namespace Mud.CodeGenerator;

internal sealed class ProjectConfigHelper
{
    /// <summary>
    /// 从项目配置中读取指定的配置信息。
    /// </summary>
    /// <param name="options">分析器配置选项。</param>
    /// <param name="optionItem">选项键。</param>
    /// <param name="defaultValue">默认值，当配置中未指定时使用。</param>
    /// <returns>配置值，如果未找到配置且未提供默认值则返回 null。</returns>
    public static string? ReadConfigValue(AnalyzerConfigOptions? options, string optionItem, string? defaultValue = null)
    {
        if (options == null || string.IsNullOrWhiteSpace(optionItem))
            return defaultValue;

        return options.TryGetValue(optionItem, out string? value) ? value : defaultValue;
    }

    /// <summary>
    /// 从项目配置中读取指定的配置信息并执行回调操作。
    /// </summary>
    /// <param name="options">分析器配置选项。</param>
    /// <param name="optionItem">选项键。</param>
    /// <param name="action">处理选项值的操作。</param>
    /// <param name="defaultValue">默认值，当配置中未指定时使用。</param>
    public static void ReadProjectOptions(AnalyzerConfigOptions? options, string optionItem, Action<string>? action, string? defaultValue = null)
    {
        if (options == null || string.IsNullOrWhiteSpace(optionItem) || action == null)
            return;

        string? value = ReadConfigValue(options, optionItem, defaultValue);

        if (value != null)
        {
            action(value);
        }
    }

    /// <summary>
    /// 从项目配置中读取指定的配置信息并尝试转换为布尔值。
    /// </summary>
    /// <param name="options">分析器配置选项。</param>
    /// <param name="optionItem">选项键。</param>
    /// <param name="defaultValue">默认值，当配置中未指定时使用。</param>
    /// <returns>配置的布尔值。</returns>
    public static bool ReadConfigValueAsBool(AnalyzerConfigOptions? options, string optionItem, bool defaultValue = false)
    {
        string? stringValue = ReadConfigValue(options, optionItem);

        if (stringValue == null)
            return defaultValue;

        return bool.TryParse(stringValue, out bool result) ? result : defaultValue;
    }

    /// <summary>
    /// 从项目配置中读取指定的配置信息并尝试转换为整数值。
    /// </summary>
    /// <param name="options">分析器配置选项。</param>
    /// <param name="optionItem">选项键。</param>
    /// <param name="defaultValue">默认值，当配置中未指定时使用。</param>
    /// <returns>配置的整数值。</returns>
    public static int ReadConfigValueAsInt(AnalyzerConfigOptions? options, string optionItem, int defaultValue = 0)
    {
        string? stringValue = ReadConfigValue(options, optionItem);

        if (stringValue == null)
            return defaultValue;

        return int.TryParse(stringValue, out int result) ? result : defaultValue;
    }
}
