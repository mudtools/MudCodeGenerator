# Token管理功能实现总结

## 功能概述

已成功为HttpClientApi代码生成器添加了Token管理功能，支持以下三种配置方式：

## 1. HttpClientApi TokenManage属性

在接口上添加`TokenManage`属性，指定Token管理器接口：

```csharp
[HttpClientApi(TokenManage = nameof(ITokenManager))]
public interface IMyApi
{
    [Get("api/users/{id}")]
    Task<UserInfo> GetUserAsync(string id);
}
```

## 2. Header方式传递Authorization

在接口上添加`[Header("Authorization")]`特性，生成的代码会自动在HTTP请求头中添加Authorization：

```csharp
[HttpClientApi(TokenManage = nameof(ITokenManager))]
[Header("Authorization")]
public interface IMyApi
{
    // 生成的代码会包含：
    // request.Headers.Add("Authorization", access_token);
}
```

## 3. Query参数方式传递Authorization

在接口上添加`[Query("Authorization")]`特性，生成的代码会自动在URL查询参数中添加Authorization：

```csharp
[HttpClientApi(TokenManage = nameof(ITokenManager))]
[Query("Authorization")]
public interface IMyApi
{
    // 生成的代码会包含：
    // queryParams.Add("Authorization", access_token);
}
```

## 实现的代码修改

### 1. HttpClientApiAttribute.cs
- ✅ 已存在TokenManage属性

### 2. WebApiSourceGenerator.cs
- ✅ 添加`GetTokenManageFromAttribute`方法
- ✅ 添加`GetTokenManagerType`方法  
- ✅ 添加`HasInterfaceAttribute`方法

### 3. MethodAnalysisResult.cs
- ✅ 添加`InterfaceAttributes`属性

### 4. HttpClientApiSourceGenerator.cs
- ✅ 修改构造函数生成逻辑，支持ITokenManager依赖注入
- ✅ 修改方法实现生成，支持Token获取和异常处理
- ✅ 支持Header和Query两种Token传递方式

## 生成的代码示例

### 构造函数（带Token管理器）
```csharp
public MyApi(HttpClient httpClient, ILogger<MyApi> logger, 
    IOptions<JsonSerializerOptions> option, ITokenManager tokenManager)
{
    _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    _jsonSerializerOptions = option.Value;
    _tokenManager = tokenManager ?? throw new ArgumentNullException(nameof(tokenManager));
    
    _httpClient.Timeout = TimeSpan.FromSeconds(30);
}
```

### 方法实现（Header方式）
```csharp
public async Task<UserInfo> GetUserAsync(string id)
{
    var access_token = await _tokenManager.GetTokenAsync();
    if (string.IsNullOrEmpty(access_token))
    {
        throw new InvalidOperationException("无法获取访问令牌");
    }

    var url = $"api/users/{id}";
    _logger.LogDebug("开始HTTP Get请求: {Url}", url);
    using var request = new HttpRequestMessage(HttpMethod.Get, url);
    
    // 添加Authorization header
    request.Headers.Add("Authorization", access_token);
    
    // ... 其余HTTP请求逻辑
}
```

### 方法实现（Query方式）
```csharp
public async Task<UserInfo> GetUserAsync(string id)
{
    var access_token = await _tokenManager.GetTokenAsync();
    if (string.IsNullOrEmpty(access_token))
    {
        throw new InvalidOperationException("无法获取访问令牌");
    }

    var url = "api/users/{id}";
    _logger.LogDebug("开始HTTP Get请求: {Url}", url);
    using var request = new HttpRequestMessage(HttpMethod.Get, url);
    
    var queryParams = HttpUtility.ParseQueryString(string.Empty);
    // 添加Authorization query参数
    queryParams.Add("Authorization", access_token);
    if (queryParams.Count > 0)
    {
        url += "?" + queryParams.ToString();
    }
    
    // ... 其余HTTP请求逻辑
}
```

## 测试验证

创建了测试接口`ITestTokenApi`和`ITestTokenQueryApi`，生成的代码完全符合预期：

- ✅ 正确生成ITokenManager字段和构造函数参数
- ✅ 正确生成Token获取和异常处理代码
- ✅ Header方式正确添加Authorization header
- ✅ Query方式正确添加Authorization查询参数
- ✅ 项目构建成功

## 依赖注入配置

使用时需要配置依赖注入：

```csharp
// 注册Token管理器
services.AddSingleton<ITokenManager, MyTokenManager>();

// 注册生成的API客户端（支持Token管理）
services.AddHttpClient<IMyApi, MyApi>();
```

## 注意事项

1. Token管理器接口必须实现`Task<string> GetTokenAsync()`方法
2. 如果无法获取Token，会抛出`InvalidOperationException`
3. 支持同时使用Header和Query方式（不推荐）
4. Token类型为Bearer Token，格式由Token管理器负责提供

所有功能已完整实现并通过测试验证！