// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.CodeGenerator 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.EntityCodeGenerator.Helper;

/// <summary>
/// 代码生成器配置管理器
/// </summary>
public class GeneratorConfiguration
{
    /// <summary>
    /// 生成的实体DTO类中需要添加的引用的命名空间。
    /// </summary>
    public string[] UsingNameSpaces { get; private set; } = [];

    /// <summary>
    /// 在实体类上绑定的特性。
    /// </summary>
    public string[] EntityAttachAttributes { get; private set; } = [];

    /// <summary>
    /// VO类属性配置。
    /// </summary>
    public string[] VoAttributes { get; private set; } = [];

    /// <summary>
    /// BO类属性配置。
    /// </summary>
    public string[] BoAttributes { get; private set; } = [];

    /// <summary>
    /// 实体前缀配置
    /// </summary>
    public string EntityPrefix { get; private set; } = string.Empty;

    /// <summary>
    /// VO类后缀配置
    /// </summary>
    public string VoSuffix { get; private set; } = "ListOutput";

    /// <summary>
    /// InfoOutput类后缀配置
    /// </summary>
    public string InfoOutputSuffix { get; private set; } = "InfoOutput";

    /// <summary>
    /// BO类后缀配置
    /// </summary>
    public string BoSuffix { get; private set; } = "Bo";

    /// <summary>
    /// QueryInput类后缀配置
    /// </summary>
    public string QueryInputSuffix { get; private set; } = "QueryInput";

    /// <summary>
    /// CrInput类后缀配置
    /// </summary>
    public string CrInputSuffix { get; private set; } = "CrInput";

    /// <summary>
    /// UpInput类后缀配置
    /// </summary>
    public string UpInputSuffix { get; private set; } = "UpInput";

    /// <summary>
    /// 从分析器配置选项读取配置
    /// </summary>
    /// <param name="options">分析器配置选项</param>
    public void ReadFromOptions(Microsoft.CodeAnalysis.Diagnostics.AnalyzerConfigOptions options)
    {
        if (options == null) return;

        ProjectConfigHelper.ReadProjectOptions(options, "build_property.UsingNameSpaces",
           val => UsingNameSpaces = val.Split(','), "");

        ProjectConfigHelper.ReadProjectOptions(options, "build_property.EntityAttachAttributes",
            val => EntityAttachAttributes = val.Split(','), "");

        ProjectConfigHelper.ReadProjectOptions(options, "build_property.VoAttributes",
            val => VoAttributes = val.SplitString(',', s => s.RemoveSuffix("Attribute", false)), "");

        ProjectConfigHelper.ReadProjectOptions(options, "build_property.BoAttributes",
            val => BoAttributes = val.SplitString(',', s => s.RemoveSuffix("Attribute", false)), "");

        ProjectConfigHelper.ReadProjectOptions(options, "build_property.EntityPrefix",
            val => EntityPrefix = val, "");

        ProjectConfigHelper.ReadProjectOptions(options, "build_property.VoSuffix",
            val => VoSuffix = val, "ListOutput");

        ProjectConfigHelper.ReadProjectOptions(options, "build_property.InfoOutputSuffix",
            val => InfoOutputSuffix = val, "InfoOutput");

        ProjectConfigHelper.ReadProjectOptions(options, "build_property.BoSuffix",
            val => BoSuffix = val, "Bo");

        ProjectConfigHelper.ReadProjectOptions(options, "build_property.QueryInputSuffix",
            val => QueryInputSuffix = val, "QueryInput");

        ProjectConfigHelper.ReadProjectOptions(options, "build_property.CrInputSuffix",
            val => CrInputSuffix = val, "CrInput");

        ProjectConfigHelper.ReadProjectOptions(options, "build_property.UpInputSuffix",
            val => UpInputSuffix = val, "UpInput");
    }

    /// <summary>
    /// 获取属性配置
    /// </summary>
    /// <param name="generatorType">生成器类型</param>
    /// <returns>属性配置数组</returns>
    public string[] GetPropertyAttributes(string generatorType)
    {
        return generatorType?.ToLowerInvariant() switch
        {
            "vo" => VoAttributes,
            "bo" => BoAttributes,
            _ => []
        };
    }

    /// <summary>
    /// 合并默认属性和配置属性
    /// </summary>
    /// <param name="defaultAttributes">默认属性</param>
    /// <param name="generatorType">生成器类型</param>
    /// <returns>合并后的属性数组</returns>
    public string[] MergePropertyAttributes(string[] defaultAttributes, string generatorType)
    {
        if (defaultAttributes == null)
            defaultAttributes = [];

        var configAttributes = GetPropertyAttributes(generatorType);

        if (configAttributes != null && configAttributes.Length > 0)
        {
            // 合并默认属性和配置属性，并去重
            return defaultAttributes.Concat(configAttributes)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        return defaultAttributes;
    }

    /// <summary>
    /// 检查配置是否有效
    /// </summary>
    public bool IsValid()
    {
        return EntityAttachAttributes != null &&
               VoAttributes != null &&
               BoAttributes != null;
    }
}