namespace ComObjectWrapTest
{
    [ComCollectionWrap(ComNamespace = "MsCore", ComClassName = "FoundFiles")]
    public interface ITestSimpleEnumerable : IEnumerable<string>, IDisposable
    {
        /// <summary>
        /// The count of items in the collection.
        /// </summary>
        int Count { get; }
        /// <summary>
        /// The indexer to get item at the specified index.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        string? this[int index] { get; }
    }
}