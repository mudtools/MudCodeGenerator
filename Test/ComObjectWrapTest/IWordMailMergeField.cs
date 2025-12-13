namespace ComObjectWrapTest;


[ComObjectWrap(ComNamespace = "MsWord", ComClassName = "MailMergeField")]
public interface IWordMailMergeField : IDisposable
{
    [ComPropertyWrap(NeedDispose = false)]
    IWordApplication? Application { get; }

    object? Parent { get; }

    bool Locked { get; set; }

    [ComPropertyWrap(DefaultValue = nameof(WdFieldType.wdFieldEmpty))]
    WdFieldType Type { get; }

    IWordRange? Code { get; set; }

    //IWordRange? XX { get; }

    void Delete();

    //void Delete(IWordCell? wordCell);

    //void Delete(IWordField wordCell);

    //void Delete(int? index, WdFieldType wdFieldType);

    //[ReturnValueConvert]
    //IWordCell? FindCell(string? name, int? index, WdFieldType wdFieldType = WdFieldType.wdFieldAddin);

}
