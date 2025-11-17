namespace Mud.Common.CodeGenerator;

/// <summary>
/// HTTP 声明式token参数特性
/// </summary>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class TokenAttribute : Attribute
{
    /// <summary>
    /// <inheritdoc cref="TokenAttribute" />
    /// </summary>
    public TokenAttribute() : this(FeishuTokenType.TenantAccessToken)
    {
    }

    /// <summary>
    /// <inheritdoc cref="TokenAttribute" />
    /// </summary>
    /// <param name="tokenType">飞书Token类型。</param>
    public TokenAttribute(FeishuTokenType tokenType)
    {
        TokenType = tokenType;
    }

    /// <summary>
    /// 飞书Token类型。
    /// </summary>
    public FeishuTokenType TokenType { get; set; } = FeishuTokenType.TenantAccessToken;
}

/// <summary>
/// 飞书Token类型。
/// </summary>
public enum FeishuTokenType
{
    /// <summary>
    /// 使用应用Token调用函数。
    /// </summary>
    TenantAccessToken = 0,
    /// <summary>
    /// 使用用户Token调用函数。
    /// </summary>
    UserAccessToken = 1,
    /// <summary>
    /// 由用户决定使用何种Token调用函数。
    /// </summary>
    Both = 2,
}