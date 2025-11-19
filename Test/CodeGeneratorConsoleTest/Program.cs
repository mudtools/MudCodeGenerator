
using CodeGeneratorTest;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder
        .AddFilter("Microsoft", LogLevel.Warning)
        .AddFilter("System", LogLevel.Warning)
        .AddFilter("ConsoleApp", LogLevel.Debug)
        .AddConsole();
});

// 手动创建JsonSerializerOptions
var jsonOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = true,
    PropertyNameCaseInsensitive = true
};

// 手动创建IOptions包装器
IOptions<JsonSerializerOptions> options = Options.Create(jsonOptions);

// 创建HttpClient（注意：应该复用HttpClient实例）
using var httpClient = new HttpClient();

var apiTest = FeishuAuthenticationTest.CreateInstance(httpClient, loggerFactory, options);
var token = await apiTest.GetTenantAccessTokenAsync(
            new CodeGeneratorTest.WebApi.AppCredentials
            {
                AppId = "",
                AppSecret = ""
            });
Console.WriteLine(token.TenantAccessToken);
Console.ReadLine();



