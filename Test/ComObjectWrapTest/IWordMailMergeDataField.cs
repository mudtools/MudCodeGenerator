namespace ComObjectWrapTest;

[ComObjectWrap(ComNamespace = "MsWord")]
public interface IWordMailMergeDataField : IDisposable
{
    /// <summary>
    /// 获取此数据字段所属的 Word 应用程序对象。
    /// </summary>
    [ComPropertyWrap(NeedDispose = false)]
    IWordApplication? Application { get; }

    /// <summary>
    /// 获取此数据字段在数据源中的名称（即列名）。
    /// </summary>
    string? Name { get; }

    /// <summary>
    /// 获取此数据字段在当前活动记录中的值。
    /// </summary>
    string? Value { get; }
}
