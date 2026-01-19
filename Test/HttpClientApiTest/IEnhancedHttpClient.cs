namespace HttpClientApiTest;


/// <summary>
/// 增强的HTTP客户端接口，用于发送HTTP请求
/// </summary>
public interface IEnhancedHttpClient
{
    /// <summary>
    /// 发送请求并返回指定类型的结果
    /// </summary>
    /// <typeparam name="TResult">期望的返回结果类型</typeparam>
    /// <param name="request">要发送的HTTP请求消息</param>
    /// <param name="cancellationToken">用于取消操作的取消令牌</param>
    /// <returns>返回类型为TResult的异步任务，可能为null</returns>
    Task<TResult?> SendAsync<TResult>(
        HttpRequestMessage request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 下载文件内容并以字节数组形式返回
    /// </summary>
    /// <param name="request">要发送的HTTP请求消息</param>
    /// <param name="cancellationToken">用于取消操作的取消令牌</param>
    /// <returns>返回字节数组的异步任务，可能为null</returns>
    Task<byte[]?> DownloadAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步下载大文件并保存到指定路径
    /// </summary>
    /// <param name="request">要发送的HTTP请求消息</param>
    /// <param name="filePath">用于保存文件的本地路径</param>
    /// <param name="overwrite">是否覆盖已存在的文件</param>
    /// <param name="cancellationToken">用于取消操作的取消令牌</param>
    /// <returns>表示异步操作的任务</returns>
    Task<System.IO.FileInfo> DownloadLargeAsync(
        HttpRequestMessage request,
        string filePath,
        bool overwrite = true,
        CancellationToken cancellationToken = default);
}
