
namespace ComObjectWrapTest
{
    /// <summary>
    /// Test interface for two-parameter indexers
    /// </summary>
    [ComCollectionWrap(ComNamespace = "TestCom", ComClassName = "TestMatrix")]
    public interface ITestTwoParameterIndexer
    {
        /// <summary>
        /// Two int parameters indexer
        /// </summary>
        /// <param name="x">Row index</param>
        /// <param name="y">Column index</param>
        /// <returns>Cell value</returns>
        string this[int x, int y] { get; set; }

        /// <summary>
        /// Two string parameters indexer
        /// </summary>
        /// <param name="name1">First name</param>
        /// <param name="name2">Second name</param>
        /// <returns>Combined value</returns>
        int this[string name1, string name2] { get; set; }
    }
}