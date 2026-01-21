// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.CodeGenerator 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.ServiceCodeGenerator.ComWrap;

/// <summary>
/// COM包装器代码生成器消息资源类
/// </summary>
internal static class GeneratorMessages
{
    /// <summary>
    /// 枚举集合元素失败
    /// </summary>
    public static string EnumerateCollectionElementFailed => "枚举集合元素失败";

    /// <summary>
    /// 枚举集合元素失败（带详细信息）
    /// </summary>
    /// <param name="details">错误详情</param>
    /// <returns>错误消息</returns>
    public static string EnumerateCollectionElementFailedWithDetails(string details) =>
        $"枚举集合元素失败: {details}";

    /// <summary>
    /// 转换集合元素失败
    /// </summary>
    public static string ConvertCollectionElementFailed => "转换集合元素失败";

    /// <summary>
    /// 转换集合元素失败（带详细信息）
    /// </summary>
    /// <param name="details">错误详情</param>
    /// <returns>错误消息</returns>
    public static string ConvertCollectionElementFailedWithDetails(string details) =>
        $"转换集合元素失败: {details}";

    /// <summary>
    /// 执行操作失败（通用）
    /// </summary>
    /// <param name="operationName">操作名称</param>
    /// <returns>错误消息</returns>
    public static string OperationFailed(string operationName) =>
        $"执行{operationName}操作失败";

    /// <summary>
    /// 执行操作失败（带详细信息）
    /// </summary>
    /// <param name="operationName">操作名称</param>
    /// <param name="details">错误详情</param>
    /// <returns>错误消息</returns>
    public static string OperationFailedWithDetails(string operationName, string details) =>
        $"{operationName}失败: {details}";

    /// <summary>
    /// 获取操作描述（用于生成友好的错误消息）
    /// </summary>
    /// <param name="methodName">方法名称</param>
    /// <returns>操作描述</returns>
    public static string GetOperationDescription(string methodName)
    {
        return methodName switch
        {
            "Add" => "添加对象操作",
            "Remove" => "移除对象操作",
            "Delete" => "删除对象操作",
            "Update" => "更新对象操作",
            "Insert" => "插入对象操作",
            "Copy" => "复制对象操作",
            "Paste" => "粘贴对象操作",
            "Select" => "选择对象",
            _ => $"执行{methodName}操作"
        };
    }

    /// <summary>
    /// 根据索引获取对象失败
    /// </summary>
    public static string GetObjectByIndexFailed => "根据索引获取对象失败";

    /// <summary>
    /// 根据索引设置对象失败
    /// </summary>
    public static string SetObjectByIndexFailed => "根据索引设置对象失败";

    /// <summary>
    /// 根据字段名称获取对象失败
    /// </summary>
    public static string GetObjectByFieldNameFailed => "根据字段名称获取对象失败";

    /// <summary>
    /// 根据字段名称设置对象失败
    /// </summary>
    public static string SetObjectByFieldNameFailed => "根据字段名称设置对象失败";

    /// <summary>
    /// 集合接口缺少Count属性
    /// </summary>
    public static string MissingCountProperty => "集合接口缺少Count属性，无法生成基于索引的枚举器";

    /// <summary>
    /// 无法确定集合元素类型
    /// </summary>
    public static string CannotDetermineCollectionElementType =>
        "警告: 无法确定集合元素类型，请确保接口实现 IEnumerable<T>";

    /// <summary>
    /// 索引参数不能少于1
    /// </summary>
    /// <param name="parameterDescription">参数描述（如"第一个"、"第二个"）</param>
    /// <returns>错误消息</returns>
    public static string IndexParameterMustBePositive(string parameterDescription) =>
        $"{parameterDescription}索引参数不能少于1";

    /// <summary>
    /// 参数不能为空
    /// </summary>
    /// <param name="parameterName">参数名称</param>
    /// <returns>错误消息</returns>
    public static string ParameterCannotBeNull(string parameterName) =>
        $"参数 {parameterName} 不能为空";
}
