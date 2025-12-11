namespace ComObjectWrapTest
{
    [ComCollectionWrap(ComNamespace = "MsCore", ComClassName = "FoundFiles")]
    public interface ITestSimpleEnumerable : IEnumerable<string>, IDisposable
    {
        int Count { get; }
        string? this[int index] { get; }
    }
}