using System.Globalization;

namespace Mud.CodeGenerator;

internal static class StringExtensions
{
    /// <summary>
    /// 将首字母小写（根据配置）。
    /// </summary>
    /// <param name="input">输入字符串。</param>
    /// <returns>首字母小写的字符串。</returns>
    public static string ToLowerFirstLetter(string input)
    {
        if (string.IsNullOrEmpty(input) || input.Length <= 2)
        {
            return input?.ToLower(CultureInfo.CurrentCulture) ?? string.Empty;
        }
        return char.ToLower(input[0], CultureInfo.CurrentCulture) + input.Substring(1);
    }

    /// <summary>
    /// 将首字母大写（根据配置）。
    /// </summary>
    /// <param name="input">输入字符串。</param>
    /// <returns>首字母大写的字符串。</returns>
    public static string ToUpperFirstLetter(string input)
    {
        if (string.IsNullOrEmpty(input) || input.Length < 2)
        {
            return input?.ToUpper(CultureInfo.CurrentCulture) ?? string.Empty;
        }
        return char.ToUpper(input[0], CultureInfo.CurrentCulture) + input.Substring(1);
    }

    /// <summary>
    /// 使用指定分隔符分割字符串，并可对每个分割结果进行处理
    /// </summary>
    /// <param name="str">要分割的字符串</param>
    /// <param name="splitChar">分隔字符，默认为逗号</param>
    /// <param name="processFunc">对每个分割结果进行处理的函数</param>
    /// <returns>处理后的字符串数组</returns>
    public static string[] SplitString(
        this string str,
        char splitChar = ',',
        Func<string, string> processFunc = null)
    {
        // 处理空字符串或空白字符串
        if (string.IsNullOrWhiteSpace(str))
            return Array.Empty<string>();

        // 分割字符串并移除空项
        var result = str.Split(new[] { splitChar }, StringSplitOptions.RemoveEmptyEntries);

        // 如果提供了处理函数，则对每个分割结果进行处理
        if (processFunc != null)
        {
            result = result.Select(processFunc).ToArray();
        }
        return result;
    }

    /// <summary>
    /// 扩展方法：移除字符串末尾指定的后缀字符串（区分大小写）
    /// </summary>
    /// <param name="str">源字符串</param>
    /// <param name="suffix">要移除的后缀字符串</param>
    /// <returns>
    /// 如果字符串以指定后缀结尾，则返回移除后缀后的字符串；
    /// 否则返回原字符串；如果输入为null或空字符串，或后缀为null，则返回原字符串
    /// </returns>
    public static string RemoveSuffix(this string str, string suffix)
    {
        // 处理边界情况
        if (string.IsNullOrEmpty(str) || string.IsNullOrEmpty(suffix))
            return str;

        // 检查字符串是否以指定后缀结尾
        if (str.EndsWith(suffix, StringComparison.CurrentCulture))
        {
            // 移除后缀：截取从开头到 (总长度 - 后缀长度) 的部分
            return str.Substring(0, str.Length - suffix.Length);
        }

        return str;
    }

    /// <summary>
    /// 扩展方法：移除字符串末尾指定的后缀字符串（可选择是否区分大小写）
    /// </summary>
    /// <param name="str">源字符串</param>
    /// <param name="suffix">要移除的后缀字符串</param>
    /// <param name="ignoreCase">是否忽略大小写，默认为false（区分大小写）</param>
    /// <returns>
    /// 如果字符串以指定后缀结尾（根据ignoreCase参数决定是否区分大小写），
    /// 则返回移除后缀后的字符串；否则返回原字符串
    /// </returns>
    public static string RemoveSuffix(this string str, string suffix, bool ignoreCase)
    {
        // 处理边界情况
        if (string.IsNullOrEmpty(str) || string.IsNullOrEmpty(suffix))
            return str;

        // 根据ignoreCase参数选择合适的比较方式
        bool endsWith = ignoreCase
            ? str.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)
            : str.EndsWith(suffix, StringComparison.CurrentCulture);

        if (endsWith)
        {
            return str.Substring(0, str.Length - suffix.Length);
        }

        return str;
    }

    /// <summary>
    /// 扩展方法：移除字符串末尾指定的后缀字符串（使用指定的字符串比较选项）
    /// </summary>
    /// <param name="str">源字符串</param>
    /// <param name="suffix">要移除的后缀字符串</param>
    /// <param name="comparisonType">字符串比较选项</param>
    /// <returns>
    /// 如果字符串以指定后缀结尾（根据comparisonType参数决定比较方式），
    /// 则返回移除后缀后的字符串；否则返回原字符串
    /// </returns>
    public static string RemoveSuffix(this string str, string suffix, StringComparison comparisonType)
    {
        // 处理边界情况
        if (string.IsNullOrEmpty(str) || string.IsNullOrEmpty(suffix))
            return str;

        // 使用指定的比较选项检查是否以指定后缀结尾
        if (str.EndsWith(suffix, comparisonType))
        {
            return str.Substring(0, str.Length - suffix.Length);
        }

        return str;
    }
}
