using Microsoft.CodeAnalysis;

namespace Mud.ServiceCodeGenerator.ComWrap;

/// <summary>
/// 异常处理配置
/// </summary>
public class ExceptionHandlingConfig
{
    /// <summary>
    /// 异常类型名称
    /// </summary>
    public string ExceptionTypeName { get; set; } = ComWrapConstants.DefaultExceptionTypeName;

    /// <summary>
    /// 是否包含原始异常消息
    /// </summary>
    public bool IncludeOriginalMessage { get; set; } = true;

    /// <summary>
    /// 消息前缀
    /// </summary>
    public string MessagePrefix { get; set; } = "操作";
}

/// <summary>
/// 异常处理配置提供器
/// </summary>
public static class ExceptionHandlingConfigProvider
{
    /// <summary>
    /// 获取异常处理配置
    /// </summary>
    /// <param name="interfaceSymbol">接口符号</param>
    /// <returns>异常处理配置</returns>
    public static ExceptionHandlingConfig GetConfig(INamedTypeSymbol interfaceSymbol)
    {
        var config = new ExceptionHandlingConfig();

        // 从特性中读取配置（可选）
        var attribute = AttributeDataHelper.GetAttributeDataFromSymbol(
            interfaceSymbol,
            [.. ComWrapConstants.ComObjectWrapAttributeNames, .. ComWrapConstants.ComCollectionWrapAttributeNames]);

        if (attribute != null)
        {
            var exceptionTypeName = AttributeDataHelper.GetStringValueFromAttribute(
                attribute,
                "ExceptionTypeName",
                null);

            if (!string.IsNullOrEmpty(exceptionTypeName))
            {
                config.ExceptionTypeName = exceptionTypeName;
            }
        }

        return config;
    }
}