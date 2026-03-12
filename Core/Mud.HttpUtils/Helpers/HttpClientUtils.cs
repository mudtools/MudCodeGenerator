// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.CodeGenerator 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.HttpUtils.Attributes;
using System.Globalization;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json.Serialization;

namespace Mud.HttpUtils;

/// <summary>
/// HttpClient 工具类，提供根据请求对象构建 MultipartFormDataContent 和根据文件路径获取 ByteArrayContent 的方法，支持异步操作和取消功能。
/// </summary>
public sealed class HttpClientUtils
{
    /// <summary>
    /// 根据请求对象异步构建MultipartFormDataContent，支持文件路径属性自动添加文件内容
    /// </summary>
    public static async Task<MultipartFormDataContent> GetFormDataContentAsync(object requestBoey, CancellationToken cancellationToken = default)
    {
        var formData = new MultipartFormDataContent();
        var properties = requestBoey.GetType().GetProperties();

        foreach (var property in properties)
        {
            var value = property.GetValue(requestBoey);
            if (value == null) continue;

            var jsonPropertyName = property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name;
            var filePathAttr = property.GetCustomAttribute<FilePathAttribute>();
            var fieldName = jsonPropertyName ?? property.Name.ToLower();

            string stringValue = value switch
            {
                string s => s,
                int i => i.ToString(),
                long l => l.ToString(),
                double d => d.ToString(CultureInfo.InvariantCulture),
                decimal m => m.ToString(CultureInfo.InvariantCulture),
                float f => f.ToString(CultureInfo.InvariantCulture),
                bool b => b.ToString().ToLower(),
                DateTime dt => dt.ToString("yyyy-MM-dd HH:mm:ss"),
                _ => value.ToString() ?? string.Empty
            };

            if (filePathAttr != null)
            {
                var fileContent = await GetByteArrayContentAsync(stringValue, cancellationToken);
                formData.Add(fileContent, fieldName, Path.GetFileName(stringValue));
            }
            else
            {
                if (!string.IsNullOrEmpty(stringValue))
                    formData.Add(new StringContent(stringValue), fieldName);
            }
        }

        return formData;
    }

    /// <summary>
    /// 根据文件路径异步获取 ByteArrayContent 对象（读取整个文件）
    /// </summary>
    public static async Task<ByteArrayContent> GetByteArrayContentAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        return await GetByteArrayContentAsync(filePath, 0, null, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// 根据文件路径异步获取 ByteArrayContent 对象（读取文件的指定部分）
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <param name="offset">读取的起始位置（字节）</param>
    /// <param name="count">要读取的字节数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>包含文件指定部分内容的 ByteArrayContent 对象</returns>
    public static async Task<ByteArrayContent> GetByteArrayContentAsync(
        string filePath,
        long offset,
        int count,
        CancellationToken cancellationToken = default)
    {
        return await GetByteArrayContentAsync(filePath, offset, (int?)count, cancellationToken)
            .ConfigureAwait(false);
    }

    // 私有实现方法
    private static async Task<ByteArrayContent> GetByteArrayContentAsync(
        string filePath,
        long offset,
        int? count,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(filePath))
            throw new ArgumentNullException(nameof(filePath));

        if (!File.Exists(filePath))
            throw new FileNotFoundException($"文件未找到: {filePath}");

        if (offset < 0)
            throw new ArgumentOutOfRangeException(nameof(offset), "起始位置不能为负数");

        if (count.HasValue && count.Value <= 0)
            throw new ArgumentOutOfRangeException(nameof(count), "读取字节数必须大于0");

        try
        {
            var fileInfo = new FileInfo(filePath);

            // 验证 offset 是否有效
            if (offset >= fileInfo.Length)
                throw new ArgumentOutOfRangeException(nameof(offset),
                    $"起始位置({offset})超出文件大小({fileInfo.Length})");

            // 计算实际要读取的字节数
            var bytesToRead = count.HasValue
                ? (int)Math.Min(count.Value, fileInfo.Length - offset)
                : (int)(fileInfo.Length - offset);

            byte[] fileBytes;

#if NETSTANDARD2_0
            if (offset == 0 && bytesToRead == fileInfo.Length)
            {
                // 优化：读取整个文件
                fileBytes = File.ReadAllBytes(filePath);
            }
            else
            {
                // 使用 FileStream 读取指定范围
                using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                fileStream.Seek(offset, SeekOrigin.Begin);
                fileBytes = new byte[bytesToRead];
                var totalBytesRead = 0;

                while (totalBytesRead < bytesToRead)
                {
                    var bytesRead = fileStream.Read(
                        fileBytes,
                        totalBytesRead,
                        bytesToRead - totalBytesRead);

                    if (bytesRead == 0) break; // 到达文件末尾
                    totalBytesRead += bytesRead;
                }

                // 如果实际读取的字节数少于请求的，调整数组大小
                if (totalBytesRead < bytesToRead)
                {
                    Array.Resize(ref fileBytes, totalBytesRead);
                }
            }
#else
            using (var fileStream = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 4096,
                useAsync: true))
            {
                if (offset > 0)
                {
                    fileStream.Seek(offset, SeekOrigin.Begin);
                }

                fileBytes = new byte[bytesToRead];
                var totalBytesRead = 0;

                while (totalBytesRead < bytesToRead)
                {
                    var bytesRead = await fileStream.ReadAsync(fileBytes.AsMemory(totalBytesRead, bytesToRead - totalBytesRead), cancellationToken)
                        .ConfigureAwait(false);

                    if (bytesRead == 0) break; // 到达文件末尾
                    totalBytesRead += bytesRead;
                }

                // 如果实际读取的字节数少于请求的，调整数组大小
                if (totalBytesRead < bytesToRead)
                {
                    Array.Resize(ref fileBytes, totalBytesRead);
                }
            }
#endif

            var fileContent = new ByteArrayContent(fileBytes);
            var contentType = GetContentType(filePath);
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);

            return fileContent;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) when (ex is IOException ||
                                   ex is UnauthorizedAccessException ||
                                   ex is PathTooLongException)
        {
            throw new InvalidOperationException($"读取文件失败: {filePath}", ex);
        }
    }

    private static readonly Dictionary<string, string> ContentTypeMappings = new(StringComparer.OrdinalIgnoreCase)
    {
        // 图片
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".gif"] = "image/gif",
        [".bmp"] = "image/bmp",
        [".webp"] = "image/webp",
        [".ico"] = "image/x-icon",
        [".tiff"] = "image/tiff",
        [".tif"] = "image/tiff",
        [".heic"] = "image/heic",
        [".svg"] = "image/svg+xml",

        // 文档
        [".pdf"] = "application/pdf",
        [".doc"] = "application/msword",
        [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        [".xls"] = "application/vnd.ms-excel",
        [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        [".ppt"] = "application/vnd.ms-powerpoint",
        [".pptx"] = "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        [".txt"] = "text/plain",
        [".csv"] = "text/csv",
        [".md"] = "text/markdown",

        // 音频/视频
        [".mp3"] = "audio/mpeg",
        [".mp4"] = "video/mp4",
        [".wav"] = "audio/wav",
        [".avi"] = "video/x-msvideo",
        [".mov"] = "video/quicktime",

        // 压缩文件
        [".zip"] = "application/zip",
        [".rar"] = "application/vnd.rar",
        [".7z"] = "application/x-7z-compressed",
        [".tar"] = "application/x-tar",
        [".gz"] = "application/gzip",

        // JSON/XML
        [".json"] = "application/json",
        [".xml"] = "application/xml",
    };

    // 根据文件扩展名获取对应的 Content-Type
    private static string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        return ContentTypeMappings.TryGetValue(extension, out var contentType)
            ? contentType
            : "application/octet-stream";
    }
}
