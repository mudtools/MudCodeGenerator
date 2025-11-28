namespace CodeGeneratorTest.Options;

/// <summary>
/// 飞书配置类。
/// </summary>
public class FeishuOptions
{
    /// <summary>
    /// 飞书应用唯一标识，创建应用后获得。
    public string? AppId { get; set; }

    /// <summary>
    /// 应用秘钥，创建应用后获得。
    /// <para>示例值： "dskLLdkasdjlasdKK"</para>
    /// </summary>
    public string? AppSecret { get; set; }

    /// <summary>
    /// 飞书 API 基础地址。
    /// <para>默认值： "https://open.feishu.cn"</para>
    /// <para>用于自定义飞书服务的访问地址，通常在生产环境中使用默认值即可</para>
    /// </summary>
    public string? BaseUrl { get; set; }

     /// <summary>
    /// 是否启用日志记录，默认为true
    /// </summary>
    public bool EnableLogging { get; set; } = true;

    /// <summary>
    /// HTTP 请求超时时间（秒）。
    /// <para>默认值：30秒</para>
    /// <para>用于设置API调用的超时时间，网络环境较差时可适当增加此值</para>
    /// <para>建议值：10-120秒，根据网络环境调整</para>
    /// </summary>
    /// <remarks>
    /// 注意：此值目前使用字符串类型以便于配置文件读取，内部会自动转换为整数。
    /// </remarks>
    public string? TimeOut { get; set; }

    /// <summary>
    /// 失败重试次数。
    /// <para>默认值：3次</para>
    /// <para>当API调用失败时的自动重试次数，提高请求的成功率和稳定性</para>
    /// </summary>
    public int? RetryCount { get; set; }
}
