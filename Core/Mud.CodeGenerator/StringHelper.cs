namespace Mud.CodeGenerator;


/// <summary>
/// 字符串扩展方法，提供更灵活的分割功能
/// </summary>
internal static class StringExtensions
{
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
}

