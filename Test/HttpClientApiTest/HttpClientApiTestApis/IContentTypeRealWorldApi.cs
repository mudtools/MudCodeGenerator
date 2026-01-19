namespace HttpClientApiTest.HttpClientApiTestApis;


using HttpClientApiTest.Models;
using Mud.Common.CodeGenerator;


/// <summary>
/// HttpContentType 真实场景测试接口
/// 模拟实际开发中常见的API使用场景
/// </summary>
[HttpClientApi("https://api.example.com")]
[HttpContentType("application/json")]
public interface IContentTypeRealWorldApi
{
    /// <summary>
    /// 场景1：钉钉部门API - JSON格式
    /// 使用 application/json 格式发送请求（继承接口级别）
    /// </summary>
    [Post("/api/dingtalk/dept")]
    Task<TestResponse> CreateDingTalkDepartmentAsync([Body] Department dept);

    /// <summary>
    /// 场景2：企业微信API - XML格式
    /// 使用 application/xml 格式发送请求（方法覆盖）
    /// </summary>
    [Post("/api/wechat/department")]
    [HttpContentType("application/xml")]
    Task<TestResponse> CreateWeChatDepartmentAsync([Body] Department dept);

    /// <summary>
    /// 场景3：表单提交 - URL编码格式
    /// 使用 application/x-www-form-urlencoded 格式
    /// </summary>
    [Post("/api/form/submit")]
    [HttpContentType(ContentType = "application/x-www-form-urlencoded")]
    Task<TestResponse> SubmitFormDataAsync([Body] TestData formData);

    /// <summary>
    /// 场景4：文件上传 - multipart格式
    /// 使用 multipart/form-data 格式上传文件
    /// </summary>
    [Post("/api/file/upload")]
    [HttpContentType("multipart/form-data")]
    Task<TestResponse> UploadFileAsync([Body] TestData fileData);

    /// <summary>
    /// 场景5：GraphQL查询 - JSON格式
    /// 使用 application/json 格式发送GraphQL查询（继承接口级别）
    /// </summary>
    [Post("/api/graphql")]
    Task<TestResponse> ExecuteGraphQLQueryAsync([Body] TestData query);

    /// <summary>
    /// 场景6：文本处理 - 纯文本格式
    /// 使用 text/plain 格式处理文本
    /// </summary>
    [Post("/api/text/process")]
    [HttpContentType("text/plain")]
    Task<TestResponse> ProcessTextAsync([Body] TestData text);

    /// <summary>
    /// 场景7：HTML内容 - HTML格式
    /// 使用 text/html 格式处理HTML
    /// </summary>
    [Post("/api/html/process")]
    [HttpContentType("text/html")]
    Task<TestResponse> ProcessHtmlAsync([Body] TestData html);

    /// <summary>
    /// 场景8：混合使用 - 接口级JSON，方法级XML
    /// 接口使用默认的 application/json，部分方法覆盖为 application/xml
    /// </summary>
    [Post("/api/mixed/default-json")]
    Task<TestResponse> DefaultJsonOperationAsync([Body] TestData data);

    /// <summary>
    /// 场景9：混合使用 - 接口级JSON，特定方法级XML
    /// </summary>
    [Post("/api/mixed/xml-operation")]
    [HttpContentType("application/xml")]
    Task<TestResponse> XmlOperationAsync([Body] TestData data);
}
