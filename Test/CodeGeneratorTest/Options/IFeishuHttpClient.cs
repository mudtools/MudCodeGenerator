namespace CodeGeneratorTest
{
    /// <summary>
    /// 飞书HTTP客户端接口，用于发送飞书相关的HTTP请求
    /// </summary>
    public interface IFeishuHttpClient
    {
        /// <summary>
        /// 发送飞书请求并返回指定类型的结果
        /// </summary>
        /// <typeparam name="TResult">期望的返回结果类型，必须是类类型并具有无参构造函数</typeparam>
        /// <param name="request">要发送的HTTP请求消息</param>
        /// <param name="cancellationToken">用于取消操作的取消令牌，默认为default</param>
        /// <returns>返回类型为TResult的异步任务，可能为null</returns>
        Task<TResult?> SendFeishuRequestAsync<TResult>(HttpRequestMessage request, CancellationToken cancellationToken = default);


        /// <summary>
        /// 发送飞书请求并返回指定类型的结果
        /// </summary>
        /// <typeparam name="TResult">期望的返回结果类型，必须是类类型并具有无参构造函数</typeparam>
        /// <param name="request">要发送的HTTP请求消息</param>
        /// <returns>返回类型为TResult的异步任务，可能为null</returns>
        Task<TResult?> SendFeishuRequestAsync<TResult>(HttpRequestMessage request);

        Task<byte[]?> DownloadFileRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken = default);

        Task<byte[]?> DownloadFileRequestAsync(HttpRequestMessage request);

        Task DownloadLargeFileRequestAsync(HttpRequestMessage request, string largeFile, CancellationToken cancellationToken = default);

        Task DownloadLargeFileRequestAsync(HttpRequestMessage request, string largeFile);

    }
}
