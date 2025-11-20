using ComObjectWrapTest;

namespace MsWord;

public class Field : IDisposable
{
    public void Dispose()
    {
        throw new NotImplementedException();
    }
}

public class MailMergeField : IDisposable
{
    public void Dispose()
    {
        throw new NotImplementedException();
    }

    public void Delete()
    {

    }

    public void Delete(int index)
    {

    }

    public object Copy(int index)
    {
        return null;
    }

    public WdFieldType Type { get; set; }

    public WdFieldType Test { get; set; }

    public object? Application { get; set; }
    public object? Parent { get; }
    public object? Code { get; }

    public bool Locked { get; set; }
}
