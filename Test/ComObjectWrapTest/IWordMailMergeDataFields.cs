namespace ComObjectWrapTest
{
    /// <summary>
    /// 邮件合并过程中表示数据字段集合的接口。
    /// </summary>
    [ComCollectionWrap(ComNamespace = "MsWord")]
    public interface IWordMailMergeDataFields : IEnumerable<IWordMailMergeDataField>, IDisposable
    {
        /// <summary>
        /// 获取此数据字段集合所属的 Word 应用程序对象。
        /// </summary> 
        [ComPropertyWrap(NeedDispose = false)]
        IWordApplication? Application { get; }

        /// <summary>
        /// 获取此数据字段集合的父对象）。
        /// </summary>
        /// [IgnoreGenerator]
        object? Parent { get; }

        /// <summary>
        /// 获取当前活动记录中数据字段的总数。
        /// </summary>
        int Count { get; }

        /// <summary>
        /// 获取集合中指定索引处的数据字段。索引从 1 开始。
        /// </summary>
        /// <param name="index">数据字段的索引（从 1 开始）。</param>
        /// <returns>指定索引处的 <see cref="IWordMailMergeDataField"/> 对象，如果索引无效则返回 null。</returns>
        IWordMailMergeDataField? this[int index] { get; }


        /// <summary>
        /// 获取集合中具有指定名称的数据字段。
        /// </summary>
        /// <param name="fieldName">要查找的字段名称。</param>
        /// <returns>具有指定名称的 <see cref="IWordMailMergeDataField"/> 对象，如果未找到则返回 null。</returns>
        /// <exception cref="ArgumentNullException">当 <paramref name="fieldName"/> 为 null 或空时抛出。</exception>
        IWordMailMergeDataField? this[string fieldName] { get; }
    }
}
