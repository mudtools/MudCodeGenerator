using CodeGeneratorTest.WebApi;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Text.Json;

namespace CodeGeneratorTest;

public class FeishuAuthenticationTest
{
    public static IFeishuAuthenticationApi CreateInstance(
        HttpClient httpClient,
        ILoggerFactory loggerFactory,
        IOptions<JsonSerializerOptions> options)
    {
        ILogger<FeishuAuthenticationApi> logger = loggerFactory.CreateLogger<FeishuAuthenticationApi>();
        return new FeishuAuthenticationApi(httpClient, logger, options);
    }
}
