// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.CodeGenerator 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis.Diagnostics;

namespace Mud.EntityCodeGenerator.Helper;

/// <summary>
/// 配置管理器，统一管理代码生成器的配置
/// </summary>
public class ConfigurationManager
{
    private static ConfigurationManager _instance;
    private GeneratorConfiguration _configuration;

    /// <summary>
    /// 获取配置管理器实例
    /// </summary>
    public static ConfigurationManager Instance => _instance ??= new ConfigurationManager();

    /// <summary>
    /// 获取当前配置
    /// </summary>
    public GeneratorConfiguration Configuration => _configuration ??= new GeneratorConfiguration();

    private ConfigurationManager() { }

    /// <summary>
    /// 初始化配置
    /// </summary>
    /// <param name="options">分析器配置选项</param>
    public void Initialize(AnalyzerConfigOptions options)
    {
        Configuration.ReadFromOptions(options);
    }

    /// <summary>
    /// 获取类后缀配置
    /// </summary>
    /// <param name="generatorType">生成器类型</param>
    /// <returns>类后缀</returns>
    public string GetClassSuffix(string generatorType)
    {
        return generatorType?.ToLowerInvariant() switch
        {
            "vo" or "listoutput" => Configuration.VoSuffix,
            "infooutput" => Configuration.InfoOutputSuffix,
            "bo" => Configuration.BoSuffix,
            "queryinput" => Configuration.QueryInputSuffix,
            "crinput" => Configuration.CrInputSuffix,
            "upinput" => Configuration.UpInputSuffix,
            _ => ""
        };
    }

    /// <summary>
    /// 获取属性配置
    /// </summary>
    /// <param name="generatorType">生成器类型</param>
    /// <returns>属性配置数组</returns>
    public string[] GetPropertyAttributes(string generatorType)
    {
        return Configuration.GetPropertyAttributes(generatorType);
    }

    /// <summary>
    /// 合并默认属性和配置属性
    /// </summary>
    /// <param name="defaultAttributes">默认属性</param>
    /// <param name="generatorType">生成器类型</param>
    /// <returns>合并后的属性数组</returns>
    public string[] MergePropertyAttributes(string[] defaultAttributes, string generatorType)
    {
        return Configuration.MergePropertyAttributes(defaultAttributes, generatorType);
    }

    /// <summary>
    /// 检查配置是否有效
    /// </summary>
    public bool IsValid()
    {
        return Configuration.IsValid();
    }

    /// <summary>
    /// 重置配置（主要用于测试）
    /// </summary>
    public void Reset()
    {
        _configuration = new GeneratorConfiguration();
    }
}