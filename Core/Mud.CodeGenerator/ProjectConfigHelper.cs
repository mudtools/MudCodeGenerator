using Microsoft.CodeAnalysis.Diagnostics;

namespace Mud.CodeGenerator
{
    internal sealed class ProjectConfigHelper
    {
        /// <summary>
        /// 从项目配置中读取指定的配置信息。
        /// </summary>
        /// <param name="options">分析器配置选项。</param>
        /// <param name="optionItem">选项键。</param>
        /// <param name="defaultValue">默认值，当配置中未指定时使用。</param>
        /// <returns>配置值</returns>
        public static string ReadConfigValue(AnalyzerConfigOptions options, string optionItem, string defaultValue)
        {
            if (options == null)
                return defaultValue;

            if (options.TryGetValue(optionItem, out var val))
            {
                return val;
            }

            return defaultValue;
        }
    }
}
