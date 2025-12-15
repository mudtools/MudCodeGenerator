namespace ComObjectWrapTest
{
    /// <summary>
    /// Test interface for out parameter functionality
    /// </summary>
    [ComObjectWrap(ComNamespace = "MsExcel")]
    public interface IExcelXmlMap
    {
        /// <summary>
        /// Exports XML data
        /// </summary>
        /// <param name="data">The exported XML data</param>
        XlXmlExportResult ExportXml(out string data);

    }
}

public enum XlXmlExportResult
{
    /// <summary>
    /// XML导出成功
    /// </summary>
    xlXmlExportSuccess,

    /// <summary>
    /// XML导出验证失败
    /// </summary>
    xlXmlExportValidationFailed
}