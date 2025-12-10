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
        int? RowIndex { get; }

        /// <summary>
        /// 获取单元格左边界位置。
        /// </summary>
        [ComPropertyWrap(DefaultValue = "-1")]
        float LeftPadding { get; set; }

        /// <summary>
        /// 获取或设置单元格垂直对齐方式。
        /// </summary>
        WdCellVerticalAlignment VerticalAlignment { get; set; }

        /// <summary>
        /// 拆分单元格。
        /// </summary>
        /// <param name="numRows">行数。</param>
        /// <param name="numColumns">列数。</param>
        void Split(int? numRows, int? numColumns);
    }
}
