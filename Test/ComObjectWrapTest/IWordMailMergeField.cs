using Mud.Common.CodeGenerator;

namespace ComObjectWrapTest;


[ComObjectWrap(ComNamespace = "MsWord", ComClassName = "Field")]
public partial interface IWordField : IDisposable
{
    [ComPropertyWrap(PropertyType = PropertyType.ObjectType)]
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

    [ComPropertyWrap(PropertyType = PropertyType.EnumType, DefaultValue = "WdFieldType.wdFieldEmpty")]
    WdFieldType Type { get; }

    [ComPropertyWrap(PropertyType = PropertyType.EnumType, DefaultValue = "WdFieldType.wdFieldEmpty")]
    WdFieldType Test { get; }

    [ComPropertyWrap(PropertyType = PropertyType.ObjectType)]
    IWordRange? Code { get; }

    void Delete();

}
