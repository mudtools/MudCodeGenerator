// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.CodeGenerator 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.HttpUtils.Attributes;

/// <summary>
///     HTTP 声明式查询参数Token特性
/// </summary>
/// <remarks>
///     <para>用于将Token作为URL查询参数传递，常用于微信等API。</para>
///     <para>示例：https://api.weixin.qq.com/cgi-bin/user/tag/get?access_token=ACCESS_TOKEN</para>
///     <para>支持多次指定。</para>
/// </remarks>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Interface | AttributeTargets.Parameter, AllowMultiple = true)]
public sealed class QueryTokenAttribute : Attribute
{
    /// <summary>
    /// 查询参数键
    /// </summary>
    public string? Name { get; private set; }

    /// <summary>
    ///  <inheritdoc cref="QueryTokenAttribute" />
    /// </summary>
    /// <remarks>特性作用于参数时有效，默认使用"access_token"作为查询参数键。</remarks>
    public QueryTokenAttribute() : this("access_token")
    {
    }

    /// <summary>
    ///     <inheritdoc cref="QueryTokenAttribute" />
    /// </summary>
    /// <remarks>
    ///     <para>当特性作用于方法或接口时，则表示移除指定查询参数操作。</para>
    ///     <para>当特性作用于参数时，则表示添加查询参数Token，同时设置查询参数键为 <c>name</c> 的值。</para>
    /// </remarks>
    /// <param name="name">查询参数键（如：access_token）</param>
    public QueryTokenAttribute(string name)
    {
        Name = name;
    }
}
