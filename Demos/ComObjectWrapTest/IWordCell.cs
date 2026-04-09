namespace ComObjectWrapTest
{
    /// <summary>
    /// COM接口，表示Word文档中的单元格。
    /// </summary>
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
        [ComPropertyWrap(DefaultValue = "-1", NeedConvert = true)]
        float LeftPadding { get; set; }

        [ComPropertyWrap(DefaultValue = "-1", NeedConvert = true, PropertyName = "LeftPadding")]
        float LeftPaddinggg { get; set; }

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
