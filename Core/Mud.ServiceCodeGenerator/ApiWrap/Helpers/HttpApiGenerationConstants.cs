// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.CodeGenerator 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.ServiceCodeGenerator.ApiWrap.Helpers;

/// <summary>
/// HTTP API 生成器常量
/// </summary>
/// <remarks>
/// 集中管理代码生成过程中使用的常量
/// </remarks>
internal static class HttpApiGenerationConstants
{
    /// <summary>
    /// HTTP客户端变量名
    /// </summary>
    public const string HttpClientVariableName = "_httpClient";

    /// <summary>
    /// 日志记录器变量名
    /// </summary>
    public const string LoggerVariableName = "_logger";

    /// <summary>
    /// JSON序列化选项变量名
    /// </summary>
    public const string JsonSerializerOptionsVariableName = "_jsonSerializerOptions";

    /// <summary>
    /// Token管理器变量名
    /// </summary>
    public const string TokenManagerVariableName = "_tokenManager";

    /// <summary>
    /// 默认内容类型
    /// </summary>
    public const string DefaultContentType = "application/json";

    /// <summary>
    /// 默认超时时间（秒）
    /// </summary>
    public const int DefaultTimeoutSeconds = 30;

    /// <summary>
    /// 默认缓冲区大小
    /// </summary>
    public const int DefaultBufferSize = 81920;

    /// <summary>
    /// 默认文件下载缓冲区大小（80KB）
    /// </summary>
    public const int DefaultFileDownloadBufferSize = 81920;

    /// <summary>
    /// 请求变量名
    /// </summary>
    public const string RequestVariableName = "request";

    /// <summary>
    /// URL变量名
    /// </summary>
    public const string UrlVariableName = "url";

    /// <summary>
    /// 响应变量名
    /// </summary>
    public const string ResponseVariableName = "response";

    /// <summary>
    /// 查询参数变量名
    /// </summary>
    public const string QueryParamsVariableName = "queryParams";

    /// <summary>
    /// 访问令牌变量名
    /// </summary>
    public const string AccessTokenVariableName = "access_token";

    /// <summary>
    /// 支持的HTTP方法
    /// </summary>
    public static readonly string[] SupportedHttpMethods =
    [
        "Get", "Post", "Put", "Delete", "Patch", "Head", "Options",
        "GetAttribute", "PostAttribute", "PutAttribute",
        "DeleteAttribute", "PatchAttribute", "HeadAttribute", "OptionsAttribute"
    ];

    /// <summary>
    /// 简单类型列表
    /// </summary>
    public static readonly string[] SimpleTypes =
    [
        "string", "int", "long", "float", "double", "decimal", "bool",
        "DateTime", "System.DateTime", "Guid", "System.Guid",
        "string[]", "int[]", "long[]", "float[]", "double[]", "decimal[]",
        "DateTime[]", "System.DateTime[]", "Guid[]", "System.Guid[]"
    ];

    /// <summary>
    /// 异常类型映射
    /// </summary>
    public static readonly Dictionary<Type, string> ExceptionTypeMapping = new()
    {
        { typeof(InvalidOperationException), "HTTPCLIENT003" },
        { typeof(ArgumentException), "HTTPCLIENT004" },
        { typeof(NotSupportedException), "HTTPCLIENT005" },
        { typeof(NullReferenceException), "HTTPCLIENT021" },
        { typeof(FormatException), "HTTPCLIENT022" }
    };

    /// <summary>
    /// 默认缩进级别
    /// </summary>
    public const int DefaultIndentLevel = 1;

    /// <summary>
    /// 方法体缩进级别
    /// </summary>
    public const int MethodBodyIndentLevel = 2;

    /// <summary>
    /// 代码块缩进级别
    /// </summary>
    public const int CodeBlockIndentLevel = 3;

    /// <summary>
    /// 内部代码块缩进级别
    /// </summary>
    public const int InnerCodeBlockIndentLevel = 4;

    /// <summary>
    /// 默认前缀
    /// </summary>
    public static class DefaultPrefixes
    {
        public const string Interface = "I";
        public const string Implementation = "";
        public const string Wrap = "Wrap";
        public const string PartialMethod = "On";
    }

    /// <summary>
    /// 默认后缀
    /// </summary>
    public static class DefaultSuffixes
    {
        public const string Implementation = "Impl";
        public const string Wrap = "Wrap";
        public const string Generated = ".g";
    }

    /// <summary>
    /// 文档注释模板
    /// </summary>
    public static class DocumentationTemplates
    {
        public const string MethodSummary = "/// <summary>\n/// <inheritdoc cref=\"{0}.{1}\"/>\n/// </summary>";
        public const string Parameter = "/// <param name=\"{0}\">{1}</param>";
        public const string Returns = "/// <returns>{0}</returns>";
        public const string ClassSummary = "/// <summary>\n/// <inheritdoc cref=\"{0}\"/>\n/// </summary>";
        public const string ConstructorSummary = "/// <summary>\n/// 构建 <see cref=\"{0}\"/> 类的实例。\n/// </summary>";
    }

    /// <summary>
    /// 代码片段模板
    /// </summary>
    public static class CodeTemplates
    {
        public const string VariableDeclaration = "var {0} = {1};";
        public const string NullCheck = "if ({0} == null) throw new ArgumentNullException(nameof({0}));";
        public const string StringNullOrEmptyCheck = "if (string.IsNullOrEmpty({0}))";
        public const string QueryParameterAdd = "queryParams.Add(\"{0}\", {1});";
        public const string HeaderAdd = "request.Headers.Add(\"{0}\", {1});";
        public const string LogDebug = "_logger.LogDebug(\"{0}\", {1});";
        public const string LogError = "_logger.LogError(ex, \"{0}\", {1});";
        public const string ThrowInvalidOperationException = "throw new InvalidOperationException(\"{0}\");";
    }

    /// <summary>
    /// 正则表达式模式
    /// </summary>
    public static class RegexPatterns
    {
        public const string PathParameter = @"\{([^}]+)\}";
        public const string QueryString = @"\?([^#]*)";
        public const string HttpUrl = @"^https?://";
    }

    /// <summary>
    /// 特性名称映射
    /// </summary>
    public static class AttributeNameMapping
    {
        public const string Query = "QueryAttribute";
        public const string ArrayQuery = "ArrayQueryAttribute";
        public const string Header = "HeaderAttribute";
        public const string Body = "BodyAttribute";
        public const string Path = "PathAttribute";
        public const string FilePath = "FilePathAttribute";
        public const string Token = "TokenAttribute";
        public const string IgnoreImplement = "IgnoreImplementAttribute";
        public const string IgnoreWrapInterface = "IgnoreWrapInterfaceAttribute";
    }
}