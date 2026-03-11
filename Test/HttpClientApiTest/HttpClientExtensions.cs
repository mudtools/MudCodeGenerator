using System.Globalization;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json.Serialization;

namespace HttpClientApiTest;

/// <summary>
/// 为HttpClient提供扩展方法的工具类
/// </summary>
internal static class HttpClientExtensions
{

    /// <summary>
    /// 根据文件路径异步获取 ByteArrayContent 对象
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>包含文件内容的 ByteArrayContent 对象</returns>
    public static async Task<ByteArrayContent> GetByteArrayContentAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(filePath))
            throw new ArgumentNullException(nameof(filePath));

        if (!File.Exists(filePath))
            throw new FileNotFoundException($"文件未找到: {filePath}");

#if NETSTANDARD2_0
        var fileBytes = File.ReadAllBytes(filePath);
#else
        var fileBytes = await File.ReadAllBytesAsync(filePath, cancellationToken);
#endif

        var fileContent = new ByteArrayContent(fileBytes);
        var contentType = GetContentType(filePath);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);

        return fileContent;
    }

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
                if (!File.Exists(stringValue))
                    throw new FileNotFoundException($"文件未找到: {stringValue}");


#if NETSTANDARD2_0
                var fileBytes =  File.ReadAllBytes(stringValue);
#else
                // 异步读取文件
                var fileBytes = await File.ReadAllBytesAsync(stringValue, cancellationToken);
#endif
                var fileContent = new ByteArrayContent(fileBytes);
                var contentType = GetContentType(stringValue);
                fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
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
