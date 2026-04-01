// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.CodeGenerator 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.HttpUtils.Attributes;

/// <summary>
/// 指示当前参数需要进行加密处理的特性。当应用于HTTP请求参数时，生成器将自动对该参数进行加密，以确保数据在传输过程中得到保护。
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class EncryptAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the name of the property represented by this instance.
    /// </summary>
    public string PropertyName { get; set; } = "data";

    /// <summary>
    /// Gets or sets the serialization format used for data processing.
    /// </summary>
    public SerializeType SerializeType { get; set; } = SerializeType.Json;

    /// <summary>
    /// Initializes a new instance of the EncryptAttribute class.
    /// </summary>
    public EncryptAttribute()
    {
    }

    /// <summary>
    /// Initializes a new instance of the EncryptAttribute class for the specified property using JSON serialization.
    /// </summary>
    /// <param name="propertyName">The name of the property to which encryption should be applied. Cannot be null or empty.</param>
    public EncryptAttribute(string propertyName) : this(propertyName, SerializeType.Json)
    {

    }

    /// <summary>
    /// Initializes a new instance of the EncryptAttribute class with the specified property name and serialization type.
    /// </summary>
    /// <param name="propertyName">The name of the property to be encrypted. Cannot be null or empty.</param>
    /// <param name="serializeType">The serialization type to use when encrypting the property.</param>
    public EncryptAttribute(string propertyName, SerializeType serializeType)
    {
        PropertyName = propertyName;
        SerializeType = serializeType;
    }
}
