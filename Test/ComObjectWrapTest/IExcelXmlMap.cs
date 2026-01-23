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

        /// <summary>
        /// Test method with out parameter of interface type - using a method that exists
        /// </summary>
        /// <param name="importMap">The imported XML map</param>
        void TestOutParameter(out IExcelXmlMap importMap);
    }
}

public enum XlXmlExportResult
{
    /// <summary>
    /// XML�����ɹ�
    /// </summary>
    xlXmlExportSuccess,

    /// <summary>
    /// XML������֤ʧ��
    /// </summary>
    xlXmlExportValidationFailed
}

public enum XlXmlImportResult
{
    /// <summary>
    /// XML导入成功
    /// </summary>
    xlXmlImportSuccess,

    /// <summary>
    /// XML导入验证失败
    /// </summary>
    xlXmlImportValidationFailed
}