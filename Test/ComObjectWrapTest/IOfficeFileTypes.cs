using Mud.Common.CodeGenerator.Com;

namespace ComObjectWrapTest
{
    /// <summary>
    /// 表示 Office 中文件类型集合的接口封装。
    /// 该接口提供对可搜索文件类型的管理功能。
    /// </summary>
    [ComCollectionWrap(ComNamespace = "MsCore")]
    public interface IOfficeFileTypes : IEnumerable<MsoFileType>, IDisposable
    {
        /// <summary>
        /// 获取文件类型集合中项的数量。
        /// </summary>
        int Count { get; }

        /// <summary>
        /// 通过索引获取文件类型（索引从 1 开始）。
        /// </summary>
        /// <param name="index">文件类型索引。</param>
        /// <returns>文件类型枚举值。</returns>
        MsoFileType this[int index] { get; }

        /// <summary>
        /// 添加文件类型到集合中。
        /// </summary>
        /// <param name="fileType">要添加的文件类型。</param>
        void Add(MsoFileType fileType);

        /// <summary>
        /// 从集合中移除指定的文件类型。
        /// </summary>
        /// <param name="fileType">要移除的文件类型。</param>
        void Remove([ConvertInt] MsoFileType fileType);
    }

    /// <summary>
    /// 指定文件类型，主要用于文件搜索和过滤操作
    /// </summary>
    public enum MsoFileType
    {
        /// <summary>
        /// 所有文件类型
        /// </summary>
        msoFileTypeAllFiles = 1,

        /// <summary>
        /// Office文件类型
        /// </summary>
        msoFileTypeOfficeFiles,

        /// <summary>
        /// Word文档类型
        /// </summary>
        msoFileTypeWordDocuments,

        /// <summary>
        /// Excel工作簿类型
        /// </summary>
        msoFileTypeExcelWorkbooks,

        /// <summary>
        /// PowerPoint演示文稿类型
        /// </summary>
        msoFileTypePowerPointPresentations,

        /// <summary>
        /// 装订器文件类型
        /// </summary>
        msoFileTypeBinders,

        /// <summary>
        /// 数据库文件类型
        /// </summary>
        msoFileTypeDatabases,

        /// <summary>
        /// 模板文件类型
        /// </summary>
        msoFileTypeTemplates,

        /// <summary>
        /// Outlook项目类型
        /// </summary>
        msoFileTypeOutlookItems,

        /// <summary>
        /// 邮件项目类型
        /// </summary>
        msoFileTypeMailItem,

        /// <summary>
        /// 日历项目类型
        /// </summary>
        msoFileTypeCalendarItem,

        /// <summary>
        /// 联系人项目类型
        /// </summary>
        msoFileTypeContactItem,

        /// <summary>
        /// 笔记项目类型
        /// </summary>
        msoFileTypeNoteItem,

        /// <summary>
        /// 日记项目类型
        /// </summary>
        msoFileTypeJournalItem,

        /// <summary>
        /// 任务项目类型
        /// </summary>
        msoFileTypeTaskItem,

        /// <summary>
        /// PhotoDraw文件类型
        /// </summary>
        msoFileTypePhotoDrawFiles,

        /// <summary>
        /// 数据连接文件类型
        /// </summary>
        msoFileTypeDataConnectionFiles,

        /// <summary>
        /// Publisher文件类型
        /// </summary>
        msoFileTypePublisherFiles,

        /// <summary>
        /// Project文件类型
        /// </summary>
        msoFileTypeProjectFiles,

        /// <summary>
        /// 文档影像文件类型
        /// </summary>
        msoFileTypeDocumentImagingFiles,

        /// <summary>
        /// Visio文件类型
        /// </summary>
        msoFileTypeVisioFiles,

        /// <summary>
        /// Designer文件类型
        /// </summary>
        msoFileTypeDesignerFiles,

        /// <summary>
        /// 网页文件类型
        /// </summary>
        msoFileTypeWebPages
    }
}
