namespace ComObjectWrapTest
{
    [ComObjectWrap(ComNamespace = "MsWord", ComClassName = "Document")]
    public interface ITestDocument : IDisposable
    {
        /// <summary>
        /// 关闭文档
        /// </summary>
        /// <param name="saveChanges">指定保存更改的方式</param>
        /// <param name="routeDocument">是否路由文档</param>
        void Close(WdSaveOptions? saveChanges = WdSaveOptions.wdPromptToSaveChanges, bool? routeDocument = false);
    }

    /// <summary>
    /// 指定关闭文档时如何保存更改
    /// </summary>
    public enum WdSaveOptions
    {
        /// <summary>
        /// 不保存更改
        /// </summary>
        wdDoNotSaveChanges = 0,

        /// <summary>
        /// 提示保存更改
        /// </summary>
        wdPromptToSaveChanges = -2,

        /// <summary>
        /// 自动保存更改
        /// </summary>
        wdSaveChanges = -1
    }
}