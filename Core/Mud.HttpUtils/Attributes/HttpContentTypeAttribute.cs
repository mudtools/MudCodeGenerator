// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.CodeGenerator 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.HttpUtils.Attributes;


/// <summary>
/// 用于指定HTTP请求的内容类型
/// </summary>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method, AllowMultiple = false)]
public sealed class HttpContentTypeAttribute : Attribute
{
    /// <summary>
    /// HTTP请求的内容类型
    /// </summary>
    public string ContentType { get; set; } = "application/json";

    /// <summary>
    /// 使用默认的"application/json"内容类型初始化一个新的实例
    /// </summary>
    public HttpContentTypeAttribute()
    {

    }

    /// <summary>
    /// 使用指定的内容类型初始化一个新的实例
    /// </summary>
    /// <param name="contentType"></param>
    public HttpContentTypeAttribute(string contentType)
    {
        ContentType = contentType;
    }
}
