namespace HttpClientApiTest.HttpClientApiTestApis;


using HttpClientApiTest.Models;
using Mud.Common.CodeGenerator;


/// <summary>
/// HttpContentType 使用方式测试接口
/// 测试场景：
/// 1. 构造函数参数方式
/// 2. 命名参数方式
/// 3. 各种常见的内容类型
/// </summary>
[HttpClientApi("https://api.mudtools.cn/")]
public interface IContentTypeUsageTestApi
{
    /// <summary>
    /// 测试1：构造函数参数方式 - application/json
    /// </summary>
    [Post("/api/usage/json")]
    [HttpContentType("application/json")]
    Task<TestResponse> TestApplicationJsonAsync([Body] TestData data);

    /// <summary>
    /// 测试2：构造函数参数方式 - application/xml
    /// </summary>
    [Post("/api/usage/xml")]
    [HttpContentType("application/xml")]
    Task<TestResponse> TestApplicationXmlAsync([Body] TestData data);

    /// <summary>
    /// 测试3：命名参数方式 - application/x-www-form-urlencoded
    /// </summary>
    [Post("/api/usage/form")]
    [HttpContentType(ContentType = "application/x-www-form-urlencoded")]
    Task<TestResponse> TestFormUrlEncodedAsync([Body] TestData data);

    /// <summary>
    /// 测试4：构造函数参数方式 - multipart/form-data
    /// </summary>
    [Post("/api/usage/multipart")]
    [HttpContentType("multipart/form-data")]
    Task<TestResponse> TestMultipartFormDataAsync([Body] TestData data);

    /// <summary>
    /// 测试5：构造函数参数方式 - text/plain
    /// </summary>
    [Post("/api/usage/text")]
    [HttpContentType("text/plain")]
    Task<TestResponse> TestTextPlainAsync([Body] TestData data);

    /// <summary>
    /// 测试6：构造函数参数方式 - text/html
    /// </summary>
    [Post("/api/usage/html")]
    [HttpContentType("text/html")]
    Task<TestResponse> TestTextHtmlAsync([Body] TestData data);

    /// <summary>
    /// 测试7：构造函数参数方式 - application/yaml
    /// </summary>
    [Post("/api/usage/yaml")]
    [HttpContentType("application/yaml")]
    Task<TestResponse> TestApplicationYamlAsync([Body] TestData data);

    /// <summary>
    /// 测试8：命名参数方式 - application/protobuf
    /// </summary>
    [Post("/api/usage/protobuf")]
    [HttpContentType(ContentType = "application/protobuf")]
    Task<TestResponse> TestProtobufAsync([Body] TestData data);
}
