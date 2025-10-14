using System;
using System.Collections.Generic;
using System.Linq;

namespace Mud.EntityCodeGenerator.Helper;

/// <summary>
/// 代码生成器配置管理器
/// </summary>
public class GeneratorConfiguration
{
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
    /// 从分析器配置选项读取配置
    /// </summary>
    /// <param name="options">分析器配置选项</param>
    public void ReadFromOptions(Microsoft.CodeAnalysis.Diagnostics.AnalyzerConfigOptions options)
    {
        if (options == null) return;

        ProjectConfigHelper.ReadProjectOptions(options, "build_property.EntityAttachAttributes", 
            val => EntityAttachAttributes = val.Split(','), "");

        ProjectConfigHelper.ReadProjectOptions(options, "build_property.VoAttributes", 
            val => VoAttributes = val.SplitString(',', s => s.RemoveSuffix("Attribute", false)), "");
        
        ProjectConfigHelper.ReadProjectOptions(options, "build_property.BoAttributes", 
            val => BoAttributes = val.SplitString(',', s => s.RemoveSuffix("Attribute", false)), "");

        ProjectConfigHelper.ReadProjectOptions(options, "build_property.EntityPrefix", 
            val => EntityPrefix = val, "");
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