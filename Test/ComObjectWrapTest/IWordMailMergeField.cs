namespace ComObjectWrapTest;


[ComObjectWrap(ComNamespace = "MsWord", ComClassName = "MailMergeField")]
public interface IWordMailMergeField : IDisposable
{
    [ComPropertyWrap(PropertyType = PropertyType.ObjectType)]
    IWordApplication? Application { get; }

    object? Parent { get; }

    bool Locked { get; set; }

    [ComPropertyWrap(DefaultValue = nameof(WdFieldType.wdFieldEmpty))]
    WdFieldType Type { get; }

    IWordRange? Code { get; }

    void Delete();
}
