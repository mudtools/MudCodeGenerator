using Mud.Common.CodeGenerator.Com;

namespace ComObjectWrapTest
{
    [ComObjectWrap(ComNamespace = "MsWord", ComClassName = "Range")]
    public interface ITestMethodGenerator : IDisposable
    {
        /// <summary>
        /// 递增左侧位置 - 处理可空参数
        /// </summary>
        /// <param name="deltaX">水平偏移量，可为空</param>
        void IncrementLeft(float? deltaX = null);

        /// <summary>
        /// 复制对象 - 无参数方法
        /// </summary>
        void Copy();

        /// <summary>
        /// 选择对象 - 带可选参数
        /// </summary>
        /// <param name="replace">是否替换选择</param>
        void Select(bool replace = true);
    }
}