# Token管理功能使用示例

## 1. 定义Token管理器接口

```csharp
public interface ITokenManager
{
    Task<string> GetTokenAsync();
}
```

## 2. 在接口中使用Token管理功能

### 2.1 使用Authorization Header

```csharp
[HttpClientApi(TokenManage = nameof(ITokenManager))]
[Header("Authorization")]
public interface IMyApi
{
    [Get("api/users/{id}")]
    Task<UserInfo> GetUserAsync(string id);
}
```

生成的代码会自动添加Authorization header：
```csharp
var access_token = await _tokenManager.GetTokenAsync();
if (string.IsNullOrEmpty(access_token))
{
    throw new InvalidOperationException("无法获取访问令牌");
}
// ...
request.Headers.Add("Authorization", access_token);
```

### 2.2 使用Authorization Query参数

```csharp
[HttpClientApi(TokenManage = nameof(ITokenManager))]
[Query("Authorization")]
public interface IMyApi
{
    [Get("api/users/{id}")]
    Task<UserInfo> GetUserAsync(string id);
}
```

生成的代码会自动添加Authorization query参数：
```csharp
var access_token = await _tokenManager.GetTokenAsync();
if (string.IsNullOrEmpty(access_token))
{
    throw new InvalidOperationException("无法获取访问令牌");
}
// ...
queryParams.Add("Authorization", access_token);
```

## 3. 依赖注入配置

```csharp
// 注册Token管理器
services.AddSingleton<ITokenManager, MyTokenManager>();

// 注册生成的API客户端
services.AddHttpClient<IMyApi, MyApi>();
```

## 4. 生成的构造函数

如果接口配置了TokenManage属性，生成的构造函数会包含ITokenManager参数：

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

## 5. 功能特性

- ✅ 支持通过`[HttpClientApi(TokenManage = nameof(ITokenManager))]`配置Token管理器
- ✅ 支持通过`[Header("Authorization")]`自动添加Authorization header
- ✅ 支持通过`[Query("Authorization")]`自动添加Authorization query参数
- ✅ 自动依赖注入ITokenManager
- ✅ 自动调用`GetTokenAsync()`获取访问令牌
- ✅ 异常处理：无法获取令牌时抛出InvalidOperationException