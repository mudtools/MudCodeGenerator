
namespace ComObjectWrapTest
{
    /// <summary>
    /// Test interface for two-parameter indexers
    /// </summary>
    [ComCollectionWrap(ComNamespace = "MsExcel", ComClassName = "Axes"), ItemIndex]
    public interface ITestTwoParameterIndexer : IEnumerable<IExcelAxis>, IDisposable
    {
        /// <summary>
        /// Two int parameters indexer
        /// </summary>
        /// <param name="x">Row index</param>
        /// <param name="y">Column index</param>
        /// <returns>Cell value</returns>
        IExcelAxis this[XlAxisType x, XlAxisGroup y] { get; }
    }

    [ComObjectWrap(ComNamespace = "MsExcel")]
    public interface IExcelAxis : IDisposable
    {

    }

    /// <summary>
    /// 坐标轴类型枚举
    /// 用于指定图表中坐标轴的类型
    /// </summary>
    public enum XlAxisType
    {
        /// <summary>
        /// 分类轴
        /// 通常用于显示文本标签，如月份、产品名称等非数值数据
        /// </summary>
        xlCategory = 1,

        /// <summary>
        /// 系列轴（第三轴）
        /// 用于三维图表中的第三轴，通常在气泡图或三维图表中使用
        /// </summary>
        xlSeriesAxis = 3,

        /// <summary>
        /// 数值轴
        /// 用于显示数值数据，通常包含刻度和数值标签
        /// </summary>
        xlValue = 2
    }

    /// <summary>
    /// 坐标轴组枚举
    /// 用于指定图表中坐标轴的组别，主要用于支持次坐标轴
    /// </summary>
    public enum XlAxisGroup
    {
        /// <summary>
        /// 主坐标轴组
        /// 图表的主坐标轴，通常用于主要数据系列
        /// </summary>
        xlPrimary = 1,

        /// <summary>
        /// 次坐标轴组
        /// 图表的次坐标轴，通常用于与主数据系列不同量级或单位的数据系列
        /// </summary>
        xlSecondary
    }
}