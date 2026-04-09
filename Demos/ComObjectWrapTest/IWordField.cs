namespace ComObjectWrapTest
{

    [ComObjectWrap(ComNamespace = "MsWord", ComClassName = "Field")]
    public partial interface IWordField : IDisposable
    {
        IWordApplication? Application { get; }

        object? Parent { get; }

        bool? Locked { get; set; }
    }
}
