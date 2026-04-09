namespace ComObjectWrapTest;


/// <summary>
/// Office操作异常
/// </summary>
[Serializable]
public class OfficeOperationException : Exception
{
    /// <summary>
    /// 初始化 <see cref="OfficeOperationException"/> 类的新实例。
    /// </summary>
    public OfficeOperationException()
    {
    }

    /// <summary>
    /// 使用指定的错误消息初始化 <see cref="OfficeOperationException"/> 类的新实例。
    /// </summary>
    /// <param name="message">描述错误的消息。</param>
    public OfficeOperationException(string message) : base(message)
    {
    }

    /// <summary>
    /// 使用指定的错误消息和对作为此异常原因的内部异常的引用来初始化 <see cref="OfficeOperationException"/> 类的新实例。
    /// </summary>
    /// <param name="message">描述错误的消息。</param>
    /// <param name="inner">导致当前异常的异常；如果未指定内部异常，则为空引用。</param>
    public OfficeOperationException(string message, Exception inner) : base(message, inner)
    {
    }
}