using Mud.Common.CodeGenerator.Com;

namespace ComObjectWrapTest
{
    [ComObjectWrap(ComNamespace = "MsWord", ComClassName = "Range")]
    public interface ITestEnumDefaultValues : IDisposable
    {
        /// <summary>
        /// 测试枚举默认值 - 原始期望签名
        /// </summary>
        /// <param name="sortBy">排序方式，默认为文件名</param>
        /// <param name="sortOrder">排序顺序，默认为升序</param>
        /// <param name="alwaysAccurate">是否总是准确，默认为true</param>
        /// <returns>执行结果</returns>
        int Execute(
            MudTools.OfficeInterop.MsoSortBy sortBy = MudTools.OfficeInterop.MsoSortBy.msoSortByFileName,
            MudTools.OfficeInterop.MsoSortOrder sortOrder = MudTools.OfficeInterop.MsoSortOrder.msoSortOrderAscending,
            bool alwaysAccurate = true);
    }

    // 测试用的枚举类型
    namespace MudTools.OfficeInterop
    {
        public enum MsoSortBy
        {
            msoSortByFileName,
            msoSortBySize,
            msoSortByType,
            msoSortByDateModified,
            msoSortByCreated
        }

        public enum MsoSortOrder
        {
            msoSortOrderAscending,
            msoSortOrderDescending
        }
    }
}