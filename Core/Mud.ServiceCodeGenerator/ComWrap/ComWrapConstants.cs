// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.CodeGenerator 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.ServiceCodeGenerator.ComWrapSourceGenerator;

/// <summary>
/// COM对象包装生成器常量配置
/// </summary>
internal static class ComWrapConstants
{
    /// <summary>
    /// ComObjectWrap特性名称数组
    /// </summary>
    public static readonly string[] ComObjectWrapAttributeNames = ["ComObjectWrapAttribute", "ComObjectWrap"];

    /// <summary>
    /// ComCollectionWrap特性名称数组
    /// </summary>
    public static readonly string[] ComCollectionWrapAttributeNames = ["ComCollectionWrapAttribute", "ComCollectionWrap"];


    /// <summary>
    /// ComPropertyWrap特性名称数组
    /// </summary>
    public static readonly string[] ComPropertyWrapAttributeNames = ["ComPropertyWrapAttribute", "ComPropertyWrap"];

    /// <summary>
    /// ItemIndex特性名称数组
    /// </summary>
    public static readonly string[] ItemIndexAttributeNames = ["ItemIndexAttribute", "ItemIndex"];

    /// <summary>
    /// 指示当前COM组件不能进行枚举取值
    /// </summary>
    public static readonly string[] NoneEnumerableAttributes = ["NoneEnumerableAttribute", "NoneEnumerable"];

    /// <summary>
    /// 转换为整数特性名称数组
    /// </summary>
    public static readonly string[] ConvertIntAttributeNames = ["ConvertIntAttribute", "ConvertInt"];

    /// <summary>
    /// 忽略生成器特性名称
    /// </summary>
    public const string IgnoreGeneratorAttribute = "IgnoreGeneratorAttribute";

    /// <summary>
    /// 用于标注方法的返回参数是否需要转换
    /// </summary>
    public static readonly string[] ReturnValueConvertAttributes = ["ReturnValueConvertAttribute", "ReturnValueConvert"];

    /// <summary>
    /// 用于标识COM组件所在的命名空间。
    /// </summary>
    public static readonly string[] ComNamespaceAttributes = ["ComNamespaceAttribute", "ComNamespace"];

    /// <summary>
    /// 获取COM命名空间属性名称
    /// </summary>
    public const string ComNamespaceProperty = "ComNamespace";

    /// <summary>
    /// 是否需要转换属性名称
    /// </summary>
    public const string NeedConvertProperty = "NeedConvert";

    /// <summary>
    /// 是否需要释放属性名称
    /// </summary>
    public const string NeedDisposeProperty = "NeedDispose";

    /// <summary>
    /// 不生成构造函数属性名称
    /// </summary>
    public const string NoneConstructorProperty = "NoneConstructor";

    /// <summary>
    /// 不生成资源释放函数属性名称
    /// </summary>
    public const string NoneDisposedProperty = "NoneDisposed";

    /// <summary>
    /// 默认COM命名空间
    /// </summary>
    public const string DefaultComNamespace = "UNKNOWN_NAMESPACE";

    /// <summary>
    /// 默认COM类名
    /// </summary>
    public const string DefaultComClassName = "UNKNOWN_CLASS";
}