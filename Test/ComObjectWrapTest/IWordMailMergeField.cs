using MsWord;
using Mud.Common.CodeGenerator;

namespace ComObjectWrapTest;


[ComObjectWrap(ComNamespace = "MsWord", ComClassName = nameof(Field))]
public interface IWordField : IDisposable
{
    IWordApplication? Application { get; }

    object? Parent { get; }

    bool locked { get; set; }
}


[ComObjectWrap(ComNamespace = "MsWord", ComClassName = "MailMergeField")]
public interface IWordMailMergeField : IDisposable
{
    IWordApplication? Application { get; }

    object? Parent { get; }

    bool locked { get; set; }

    [ComPropertyWrap(PropertyType = PropertyType.EnumType, DefaultValue = "WdFieldType.wdFieldEmpty")]
    WdFieldType Type { get; }

    [ComPropertyWrap(PropertyType = PropertyType.EnumType, DefaultValue = "WdFieldType.wdFieldEmpty")]
    WdFieldType Test { get; }

    IWordRange? Code { get; }

    void Delete();

    void Delete(int index);

    IWordRange Copy(int index);
}
