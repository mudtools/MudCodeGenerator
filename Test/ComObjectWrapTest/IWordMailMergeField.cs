namespace ComObjectWrapTest;


[ComObjectWrap(ComNamespace = "MsWord", ComClassName = "MailMergeField")]
public interface IWordMailMergeField : IDisposable
{
    [ComPropertyWrap(PropertyType = PropertyType.ObjectType, NeedDispose = false)]
    IWordApplication? Application { get; }

    object? Parent { get; }

    [ComPropertyWrap(NeedConvert = true)]
    bool Locked { get; set; }

    [ComPropertyWrap(DefaultValue = nameof(WdFieldType.wdFieldEmpty))]
    WdFieldType Type { get; }

    IWordRange? Code { get; }

    void Delete();

    void Delete(IWordCell? wordCell);

    void Delete(IWordField wordCell);

    void Delete(int? index, WdFieldType wdFieldType);

    IWordCell? FindCell(string? name, int? index, WdFieldType wdFieldType = WdFieldType.wdFieldAddin);

}
