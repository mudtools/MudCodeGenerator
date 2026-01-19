namespace Mud.Common.CodeGenerator;


/// <summary>
/// 用于指定HTTP请求的内容类型
/// </summary>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method, AllowMultiple = false)]
public class HttpContentTypeAttribute : Attribute
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
