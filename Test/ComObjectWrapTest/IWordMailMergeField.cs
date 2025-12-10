using Mud.Common.CodeGenerator;

namespace ComObjectWrapTest;


[ComObjectWrap(ComNamespace = "MsWord", ComClassName = "Field")]
public partial interface IWordField : IDisposable
{
    IWordApplication? Application { get; }

    object? Parent { get; }

    bool Locked { get; set; }
}


[ComObjectWrap(ComNamespace = "MsWord", ComClassName = "MailMergeField")]
public interface IWordMailMergeField : IDisposable
{
    [ComPropertyWrap(PropertyType = PropertyType.ObjectType)]
    IWordApplication? Application { get; }

    object? Parent { get; }

    bool Locked { get; set; }

    [ComPropertyWrap(DefaultValue = "WdFieldType.wdFieldEmpty")]
    WdFieldType Type { get; }

    IWordRange? Code { get; }

    void Delete();

}
