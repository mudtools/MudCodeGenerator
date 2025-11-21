namespace Mud.Common.CodeGenerator;

/// <summary>
/// HTTP客户端API包装特性，用于标记需要生成包装类的接口
/// </summary>
/// <remarks>
/// 此特性应用于接口上，指示代码生成器为该接口创建HTTP客户端包装类。
/// 支持指定令牌管理接口和自定义包装接口名称。
/// </remarks>
[AttributeUsage(AttributeTargets.Interface, AllowMultiple = false)]
public class HttpClientApiWrapAttribute : Attribute
{
    /// <summary>
    /// 初始化 <see cref="HttpClientApiWrapAttribute"/> 类的新实例
    /// </summary>
    public HttpClientApiWrapAttribute()
    {
    }

    /// <summary>
    /// 使用指定的令牌管理接口初始化 <see cref="HttpClientApiWrapAttribute"/> 类的新实例
    /// </summary>
    /// <param name="tokenManage">令牌管理接口名称</param>
    public HttpClientApiWrapAttribute(string tokenManage)
    {
        TokenManage = tokenManage;
    }

    /// <summary>
    /// 使用指定的令牌管理接口和包装接口名称初始化 <see cref="HttpClientApiWrapAttribute"/> 类的新实例
    /// </summary>
    /// <param name="tokenManage">令牌管理接口名称</param>
    /// <param name="wrapInterface">包装接口名称</param>
    public HttpClientApiWrapAttribute(string tokenManage, string wrapInterface)
    {
        WrapInterface = wrapInterface;
        TokenManage = tokenManage;
    }

    /// <summary>
    /// 获取或设置包装接口名称
    /// </summary>
    /// <value>包装接口名称，如果未指定则使用默认规则生成</value>
    /// <remarks>
    /// 当未设置此属性时，将根据接口名称自动生成包装接口名称。
    /// 通常在原接口名称后添加"Wrap"后缀。
    /// </remarks>
    public string WrapInterface { get; set; }

#if NET8_0_OR_GREATER
    /// <summary>
    /// 获取或设置令牌管理接口名称
    /// </summary>
    /// <value>令牌管理接口名称，用于API调用的身份验证</value>
    /// <remarks>
    /// 在.NET 8.0或更高版本中，此属性为必需属性，必须在构造函数中指定。
    /// </remarks>
    public required string TokenManage { get; set; }
#else
    /// <summary>
    /// 获取或设置令牌管理接口名称
    /// </summary>
    /// <value>令牌管理接口名称，用于API调用的身份验证</value>
    /// <remarks>
    /// 在.NET 8.0以下版本中，此属性为可选属性，但建议在使用时进行设置。
    /// </remarks>
    public string TokenManage { get; set; }
#endif
}
