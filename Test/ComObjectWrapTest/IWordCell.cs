namespace ComObjectWrapTest
{
    [ComObjectWrap(ComNamespace = "MsWord")]
    public interface IWordCell : IDisposable
    {
        /// <summary>
        /// 获取应用程序对象。
        /// </summary>
        IWordApplication Application { get; }

        /// <summary>
        /// 获取父对象。
        /// </summary>
        object Parent { get; }

        /// <summary>
        /// 获取单元格行索引。
        /// </summary>
        int RowIndex { get; }

        /// <summary>
        /// 获取或设置单元格垂直对齐方式。
        /// </summary>
        WdCellVerticalAlignment VerticalAlignment { get; set; }
    }
}
