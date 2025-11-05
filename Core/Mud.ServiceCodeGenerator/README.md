# Mud 服务代码生成器

## 功能介绍

Mud 服务代码生成器是一个基于 Roslyn 的源代码生成器，用于自动生成服务层相关代码，提高开发效率。它包含以下主要功能：

1. **服务类代码生成** - 根据实体类自动生成服务接口和服务实现类
2. **依赖注入代码生成** - 自动为类生成构造函数注入代码，包括日志、缓存、用户管理等常用服务
3. **服务注册代码生成** - 自动生成服务注册扩展方法，简化依赖注入配置
4. **HttpClient API 代码生成** - 自动为标记了 HTTP 方法特性的接口生成 HttpClient 实现类

## 项目参数配置

在使用 Mud 服务代码生成器时，可以通过在项目文件中配置以下参数自定义生成行为：

### 通用配置参数

```xml
<PropertyGroup>
  <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>  <!-- 在obj目录下保存生成的代码 -->
  
  <!-- 依赖注入相关配置 -->
  <DefaultCacheManagerType>ICacheManager</DefaultCacheManagerType>  <!-- 缓存管理器类型默认值 -->
  <DefaultUserManagerType>IUserManager</DefaultUserManagerType>  <!-- 用户管理器类型默认值 -->
  <DefaultLoggerVariable>_logger</DefaultLoggerVariable>  <!-- 日志变量名默认值 -->
  <DefaultCacheManagerVariable>_cacheManager</DefaultCacheManagerVariable>  <!-- 缓存管理器变量名默认值 -->
  <DefaultUserManagerVariable>_userManager</DefaultUserManagerVariable>  <!-- 用户管理器变量名默认值 -->
  
  <!-- 服务生成相关配置 -->
  <ServiceGenerator>true</ServiceGenerator>  <!-- 是否生成服务端代码 -->
  <EntitySuffix>Entity</EntitySuffix>  <!-- 实体类后缀配置 -->
  <ImpAssembly>Mud.System</ImpAssembly>  <!-- 需要生成代码的接口实现程序集 -->
  
  <!-- DTO生成相关配置 -->
  <EntityAttachAttributes>SuppressSniffer</EntityAttachAttributes>  <!-- 实体类加上Attribute特性配置，多个特性时使用','分隔 -->
</PropertyGroup>

<ItemGroup>
  <CompilerVisibleProperty Include="DefaultCacheManagerType" />
  <CompilerVisibleProperty Include="DefaultUserManagerType" />
  <CompilerVisibleProperty Include="DefaultLoggerVariable" />
  <CompilerVisibleProperty Include="DefaultCacheManagerVariable" />
  <CompilerVisibleProperty Include="DefaultUserManagerVariable" />
  <CompilerVisibleProperty Include="ServiceGenerator" />
  <CompilerVisibleProperty Include="EntitySuffix" />
  <CompilerVisibleProperty Include="ImpAssembly" />
  <CompilerVisibleProperty Include="EntityAttachAttributes" />
</ItemGroup>
```

### 依赖项配置

```xml
<ItemGroup>
  <!-- 引入的代码生成器程序集 -->
  <PackageReference Include="Mud.ServiceCodeGenerator" Version="1.2.0"/>
</ItemGroup>
```

## 代码生成功能及样例

### 依赖注入代码生成

使用各种注入特性为类自动生成构造函数注入代码：

```CSharp
[ConstructorInject]  // 字段构造函数注入
[LoggerInject]       // 日志注入
[CacheInject]        // 缓存管理器注入
[UserInject]         // 用户管理器注入
[CustomInject(VarType = "IRepository<SysUser>", VarName = "_userRepository")]  // 自定义注入
public partial class SysUserService
{
    // 生成的代码将包含以下内容：
    // 1. 构造函数参数
    // 2. 私有只读字段
    // 3. 构造函数赋值语句
}
```

### 自动服务注册代码生成

使用 [AutoRegister] 和 [AutoRegisterKeyed] 特性自动生成服务注册代码，简化依赖注入配置：

```CSharp
// 自动注册服务到DI容器
[AutoRegister]
[AutoRegister<ISysUserService>]
[AutoRegisterKeyed<ISysUserService>("user")]
public partial class SysUserService : ISysUserService
{
    // 生成的代码将包含服务注册扩展方法
}
```

#### 构造函数注入详解

##### ConstructorInjectAttribute 字段注入
使用 [ConstructorInject] 特性可以将类中已存在的字段通过构造函数注入初始化。该注入方式会扫描类中的所有私有只读字段，并为其生成相应的构造函数参数和赋值语句。

示例：
```CSharp
[ConstructorInject]
public partial class UserService
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    
    // 生成的代码将包含:
    public UserService(IUserRepository userRepository, IRoleRepository roleRepository)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
    }
}
```

##### LoggerInjectAttribute 日志注入
使用 [LoggerInject] 特性可以为类注入 ILogger<> 类型的日志记录器。该注入会自动生成 ILoggerFactory 参数，并在构造函数中创建对应类的 Logger 实例。

示例：
```CSharp
[LoggerInject]
public partial class UserService
{
    // 生成的代码将包含:
    private readonly ILogger<UserService> _logger;
    
    public UserService(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<UserService>();
    }
}
```

##### CacheInjectAttribute 缓存管理器注入
使用 [CacheInject] 特性可以注入缓存管理器实例。默认类型为 ICacheManager，默认字段名为 _cacheManager，可通过项目配置修改。

示例：
```CSharp
[CacheInject]
public partial class UserService
{
    // 生成的代码将包含:
    private readonly ICacheManager _cacheManager;
    
    public UserService(ICacheManager cacheManager)
    {
        _cacheManager = cacheManager;
    }
}
```

项目配置示例：
```xml
<PropertyGroup>
  <DefaultCacheManagerType>MyCustomCacheManager</DefaultCacheManagerType>
  <DefaultCacheManagerVariable>_myCacheManager</DefaultCacheManagerVariable>
</PropertyGroup>
```

##### UserInjectAttribute 用户管理器注入
使用 [UserInject] 特性可以注入用户管理器实例。默认类型为 IUserManager，默认字段名为 _userManager，可通过项目配置修改。

示例：
```CSharp
[UserInject]
public partial class UserService
{
    // 生成的代码将包含:
    private readonly IUserManager _userManager;
    
    public UserService(IUserManager userManager)
    {
        _userManager = userManager;
    }
}
```

项目配置示例：
```xml
<PropertyGroup>
  <DefaultUserManagerType>MyCustomUserManager</DefaultUserManagerType>
  <DefaultUserManagerVariable>_myUserManager</DefaultUserManagerVariable>
</PropertyGroup>
```

##### OptionsInjectAttribute 配置项注入
使用 [OptionsInject] 特性可以根据指定的配置项类型注入配置实例。支持泛型语法，提供更简洁的配置方式。

示例：
```CSharp
// 传统方式
[OptionsInject(OptionType = "TenantOptions")]
// 泛型方式（推荐）
[OptionsInject<TenantOptions>]
public partial class UserService
{
    // 生成的代码将包含:
    private readonly TenantOptions _tenantOptions;
    
    public UserService(IOptions<TenantOptions> tenantOptions)
    {
        _tenantOptions = tenantOptions.Value;
    }
}
```

##### CustomInjectAttribute 自定义注入
使用 [CustomInject] 特性可以注入任意类型的依赖项。支持泛型语法，提供更简洁的类型安全配置方式。

示例：
```CSharp
// 传统方式
[CustomInject(VarType = "IRepository<SysUser>", VarName = "_userRepository")]
[CustomInject(VarType = "INotificationService", VarName = "_notificationService")]
// 泛型方式（推荐）
[CustomInject<IRepository<SysUser>>(VarName = "_userRepository")]
[CustomInject<INotificationService>(VarName = "_notificationService")]
public partial class UserService
{
    // 生成的代码将包含:
    private readonly IRepository<SysUser> _userRepository;
    private readonly INotificationService _notificationService;
    
    public UserService(IRepository<SysUser> userRepository, INotificationService notificationService)
    {
        _userRepository = userRepository;
        _notificationService = notificationService;
    }
}
```

#### 组合注入示例

多种注入特性可以组合使用，生成器会自动合并所有注入需求。推荐使用泛型语法以获得更好的类型安全性：

```CSharp
[ConstructorInject]
[LoggerInject]
[CacheInject]
[UserInject]
[OptionsInject<TenantOptions>]
[CustomInject<IRepository<SysUser>>(VarName = "_userRepository")]
public partial class UserService
{
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;
    
    // 生成的代码将包含所有注入项:
    private readonly ILogger<UserService> _logger;
    private readonly ICacheManager _cacheManager;
    private readonly IUserManager _userManager;
    private readonly TenantOptions _tenantOptions;
    private readonly IRepository<SysUser> _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;
    
    public UserService(
        ILoggerFactory loggerFactory,
        ICacheManager cacheManager,
        IUserManager userManager,
        IOptions<TenantOptions> tenantOptions,
        IRepository<SysUser> userRepository,
        IRoleRepository roleRepository,
        IPermissionRepository permissionRepository)
    {
        _logger = loggerFactory.CreateLogger<UserService>();
        _cacheManager = cacheManager;
        _userManager = userManager;
        _tenantOptions = tenantOptions.Value;
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
    }
}
```

### 自动服务注册代码生成

AutoRegisterSourceGenerator 自动为标记了 [AutoRegister] 和 [AutoRegisterKeyed] 特性的类生成服务注册代码，简化依赖注入配置。

#### AutoRegisterAttribute 自动注册

使用 [AutoRegister] 特性自动将服务注册到DI容器中：

```CSharp
// 基本用法：注册实现类本身
[AutoRegister]
public class UserService
{
    // 生成的注册代码：services.AddScoped<UserService>();
}

// 注册为接口实现
[AutoRegister<IUserService>]
public class UserService : IUserService
{
    // 生成的注册代码：services.AddScoped<IUserService, UserService>();
}

// 指定生命周期
[AutoRegister(ServiceLifetime.Singleton)]
[AutoRegister<IUserService>(ServiceLifetime.Transient)]
public class UserService : IUserService
{
    // 生成的注册代码：
    // services.AddSingleton<UserService>();
    // services.AddTransient<IUserService, UserService>();
}
```

#### AutoRegisterKeyedAttribute 键控服务注册

使用 [AutoRegisterKeyed] 特性注册键控服务（Microsoft.Extensions.DependencyInjection 8.0+）：

```CSharp
// 键控服务注册
[AutoRegisterKeyed("user")]
[AutoRegisterKeyed<IUserService>("user")]
public class UserService : IUserService
{
    // 生成的注册代码：
    // services.AddKeyedScoped<UserService>("user");
    // services.AddKeyedScoped<IUserService, UserService>("user");
}

// 键控服务指定生命周期
[AutoRegisterKeyed<IUserService>("user", ServiceLifetime.Singleton)]
public class UserService : IUserService
{
    // 生成的注册代码：services.AddKeyedSingleton<IUserService, UserService>("user");
}
```

#### 生成的注册代码

自动生成的注册扩展方法位于 `AutoRegisterExtension` 类中：

```CSharp
// 自动生成的代码
public static partial class AutoRegisterExtension
{
    /// <summary>
    /// 自动注册标注的服务
    /// </summary>
    public static IServiceCollection AddAutoRegister(this IServiceCollection services)
    {
        services.AddScoped<UserService>();
        services.AddScoped<IUserService, UserService>();
        services.AddKeyedScoped<UserService>("user");
        services.AddKeyedScoped<IUserService, UserService>("user");
        return services;
    }
}
```

#### 使用方式

在应用程序启动时调用生成的扩展方法：

```CSharp
var builder = WebApplication.CreateBuilder(args);

// 自动注册所有标记的服务
builder.Services.AddAutoRegister();

// 或者与其他注册一起使用
builder.Services
    .AddControllers()
    .AddAutoRegister();
```

#### 特性组合使用

自动注册特性可以与其他注入特性组合使用：

```CSharp
[AutoRegister<IUserService>]
[ConstructorInject]
[LoggerInject]
[CacheInject]
public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    
    // 同时生成构造函数注入和服务注册代码
}
```

### 忽略字段注入

对于某些不需要通过构造函数注入的字段，可以使用 [IgnoreGenerator] 特性标记：

```CSharp
[ConstructorInject]
public partial class UserService
{
    private readonly IUserRepository _userRepository;
    
    [IgnoreGenerator]
    private readonly string _connectionString = "default_connection_string"; // 不会被注入
    
    // 只有_userRepository会被构造函数注入
}
```

## HttpClient API 代码生成

HttpClientApiSourceGenerator 自动为标记了 [HttpClientApi] 特性的接口生成 HttpClient 实现类，支持 RESTful API 调用。

### 基本用法

#### 1. 定义 HTTP API 接口

```CSharp
[HttpClientApi]
public interface IDingTalkApi
{
    [Get("/api/v1/user/{id}")]
    Task<UserDto> GetUserAsync([Query] string id);
    
    [Post("/api/v1/user")]
    Task<UserDto> CreateUserAsync([Body] UserDto user);
    
    [Put("/api/v1/user/{id}")]
    Task<UserDto> UpdateUserAsync([Path] string id, [Body] UserDto user);
    
    [Delete("/api/v1/user/{id}")]
    Task<bool> DeleteUserAsync([Path] string id);
}
```

#### 2. 生成的 HttpClient 实现类

自动生成的实现类包含完整的 HTTP 请求处理逻辑：

```CSharp
// 自动生成的代码
public partial class DingTalkApi : IDingTalkApi
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<DingTalkApi> _logger;
    private readonly JsonSerializerOptions _jsonSerializerOptions;
    
    public DingTalkApi(HttpClient httpClient, ILogger<DingTalkApi> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            PropertyNameCaseInsensitive = true
        };
    }
    
    public async Task<UserDto> GetUserAsync(string id)
    {
        // 自动生成的 HTTP GET 请求逻辑
        _logger.LogDebug("开始HTTP GET请求: {Url}", "/api/v1/user/{id}");
        
        var url = $"/api/v1/user/{id}";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        
        // 处理查询参数
        var queryParams = new List<string>();
        if (id != null)
            queryParams.Add($"id={id}");
        
        if (queryParams.Any())
            url += "?" + string.Join("&", queryParams);
        
        // 发送请求并处理响应
        // ... 完整的请求处理逻辑
    }
}
```

### 支持的 HTTP 方法特性

支持所有标准的 HTTP 方法：

```CSharp
[HttpClientApi]
public interface IExampleApi
{
    [Get("/api/resource/{id}")]
    Task<ResourceDto> GetResourceAsync([Path] string id);
    
    [Post("/api/resource")]
    Task<ResourceDto> CreateResourceAsync([Body] ResourceDto resource);
    
    [Put("/api/resource/{id}")]
    Task<ResourceDto> UpdateResourceAsync([Path] string id, [Body] ResourceDto resource);
    
    [Delete("/api/resource/{id}")]
    Task<bool> DeleteResourceAsync([Path] string id);
    
    [Patch("/api/resource/{id}")]
    Task<ResourceDto> PatchResourceAsync([Path] string id, [Body] object patchData);
    
    [Head("/api/resource/{id}")]
    Task<bool> CheckResourceExistsAsync([Path] string id);
    
    [Options("/api/resource")]
    Task<HttpResponseMessage> GetResourceOptionsAsync();
}
```

### 参数特性详解

#### 1. Path 参数特性

用于替换 URL 模板中的路径参数：

```CSharp
[Get("/api/users/{userId}/orders/{orderId}")]
Task<OrderDto> GetOrderAsync([Path] string userId, [Path] string orderId);
```

#### 2. Query 参数特性

用于生成查询字符串参数：

```CSharp
[Get("/api/users")]
Task<List<UserDto>> GetUsersAsync(
    [Query] string name, 
    [Query] int? page, 
    [Query] int? pageSize);
```

#### 3. Body 参数特性

用于设置请求体内容：

```CSharp
[Post("/api/users")]
Task<UserDto> CreateUserAsync([Body] UserDto user);

// 支持自定义内容类型
[Post("/api/users")]
Task<UserDto> CreateUserAsync([Body(ContentType = "application/xml")] UserDto user);

// 支持字符串内容
[Post("/api/logs")]
Task LogMessageAsync([Body(UseStringContent = true)] string message);
```

#### 4. Header 参数特性

用于设置请求头：

```CSharp
[Get("/api/protected")]
Task<ProtectedData> GetProtectedDataAsync([Header] string authorization);

// 自定义头名称
[Get("/api/protected")]
Task<ProtectedData> GetProtectedDataAsync([Header("X-API-Key")] string apiKey);
```

### 复杂参数处理

#### 1. 复杂查询参数

支持复杂对象作为查询参数，自动展开为键值对：

```CSharp
[Get("/api/search")]
Task<List<UserDto>> SearchUsersAsync([Query] UserSearchCriteria criteria);

public class UserSearchCriteria
{
    public string Name { get; set; }
    public int? Age { get; set; }
    public string Department { get; set; }
}

// 生成的查询字符串：?Name=John&Age=30&Department=IT
```

#### 2. 路径参数自动替换

自动处理 URL 模板中的路径参数：

```CSharp
[Get("/api/users/{userId}/orders/{orderId}/items/{itemId}")]
Task<OrderItemDto> GetOrderItemAsync(
    [Path] string userId, 
    [Path] string orderId, 
    [Path] string itemId);

// 自动替换：/api/users/123/orders/456/items/789
```

### 错误处理与日志记录

生成的代码包含完整的错误处理和日志记录：

```CSharp
public async Task<UserDto> GetUserAsync(string id)
{
    try
    {
        _logger.LogDebug("开始HTTP GET请求: {Url}", "/api/v1/user/{id}");
        
        // 请求处理逻辑
        
        using var response = await _httpClient.SendAsync(request);
        var responseContent = await response.Content.ReadAsStringAsync();
        
        _logger.LogDebug("HTTP请求完成: {StatusCode}, 响应长度: {ContentLength}", 
            (int)response.StatusCode, responseContent?.Length ?? 0);
        
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("HTTP请求失败: {StatusCode}, 响应: {Response}", 
                (int)response.StatusCode, responseContent);
            throw new HttpRequestException($"HTTP请求失败: {(int)response.StatusCode} - {response.ReasonPhrase}");
        }
        
        // 响应处理逻辑
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "HTTP请求异常: {Url}", url);
        throw;
    }
}
```

### 配置选项

#### 1. 自定义 JsonSerializerOptions

生成的构造函数包含默认的 JsonSerializerOptions 配置：

```CSharp
_jsonSerializerOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = false,
    PropertyNameCaseInsensitive = true
};
```

#### 2. 支持可空返回值

自动处理可空返回值类型：

```CSharp
[Get("/api/users/{id}")]
Task<UserDto?> GetUserOrNullAsync([Path] string id);
```

### 使用示例

#### 1. 在依赖注入中注册

```CSharp
// 在 Startup.cs 或 Program.cs 中
services.AddHttpClient<IDingTalkApi, DingTalkApi>(client =>
{
    client.BaseAddress = new Uri("https://api.dingtalk.com");
    client.DefaultRequestHeaders.Add("User-Agent", "MyApp/1.0");
});
```

#### 2. 在服务中使用

```CSharp
public class UserService
{
    private readonly IDingTalkApi _dingTalkApi;
    
    public UserService(IDingTalkApi dingTalkApi)
    {
        _dingTalkApi = dingTalkApi;
    }
    
    public async Task<UserDto> GetUserAsync(string userId)
    {
        return await _dingTalkApi.GetUserAsync(userId);
    }
}
```

### 高级功能

#### 1. 组合使用多个参数特性

```CSharp
[Post("/api/users/{userId}/permissions")]
Task<bool> AssignPermissionsAsync(
    [Path] string userId,
    [Body] List<string> permissions,
    [Header("X-Request-ID")] string requestId,
    [Query] bool? overwrite);
```

#### 2. 自定义内容序列化

```CSharp
[Post("/api/data")]
Task<ResponseDto> SendDataAsync([Body(ContentType = "application/xml", UseStringContent = true)] string xmlData);
```

## HttpClient API 注册代码生成

HttpClientApiRegisterSourceGenerator 自动为标记了 [HttpClientApi] 特性的接口生成依赖注入注册代码，简化 HttpClient 服务的配置。

### 基本用法

#### 1. 定义 HTTP API 接口

```CSharp
[HttpClientApi("https://api.dingtalk.com", Timeout = 30)]
public interface IDingTalkApi
{
    [Get("/api/v1/user/{id}")]
    Task<UserDto> GetUserAsync([Query] string id);
    
    [Post("/api/v1/user")]
    Task<UserDto> CreateUserAsync([Body] UserDto user);
}

[HttpClientApi("https://api.wechat.com", Timeout = 60)]
public interface IWeChatApi
{
    [Get("/api/v1/user/{id}")]
    Task<UserDto> GetUserAsync([Query] string id);
}
```

#### 2. 生成的注册代码

自动生成的依赖注入注册扩展方法：

```CSharp
// 自动生成的代码 - HttpClientApiExtensions.g.cs
using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class HttpClientApiExtensions
    {
        public static IServiceCollection AddWebApiHttpClient(this IServiceCollection services)
        {
            services.AddHttpClient<global::YourNamespace.IDingTalkApi, global::YourNamespace.DingTalkApi>(client =>
            {
                client.BaseAddress = new Uri("https://api.dingtalk.com");
                client.Timeout = TimeSpan.FromSeconds(30);
            });
            
            services.AddHttpClient<global::YourNamespace.IWeChatApi, global::YourNamespace.WeChatApi>(client =>
            {
                client.BaseAddress = new Uri("https://api.wechat.com");
                client.Timeout = TimeSpan.FromSeconds(60);
            });
            
            return services;
        }
    }
}
```

### 配置选项

#### 1. HttpClientApi 特性参数

```CSharp
// 基本配置
[HttpClientApi("https://api.example.com")]
public interface IExampleApi { }

// 配置超时时间
[HttpClientApi("https://api.example.com", Timeout = 120)]
public interface IExampleApi { }

// 使用命名参数
[HttpClientApi(BaseUrl = "https://api.example.com", Timeout = 60)]
public interface IExampleApi { }
```

#### 2. 生成的 HttpClient 配置

生成的注册代码包含以下配置：
- **BaseAddress**: 从 [HttpClientApi] 特性的第一个参数获取
- **Timeout**: 从 Timeout 命名参数获取，默认 100 秒
- **服务注册**: 使用 AddHttpClient 方法注册接口和实现类

### 使用方式

#### 1. 在应用程序启动时调用

```CSharp
// 在 Program.cs 或 Startup.cs 中
var builder = WebApplication.CreateBuilder(args);

// 自动注册所有 HttpClient API 服务
builder.Services.AddWebApiHttpClient();

// 或者与其他服务注册一起使用
builder.Services
    .AddControllers()
    .AddWebApiHttpClient();
```

#### 2. 在控制台应用程序中使用

```CSharp
// 在控制台应用程序中
var services = new ServiceCollection();

// 注册 HttpClient API 服务
services.AddWebApiHttpClient();

var serviceProvider = services.BuildServiceProvider();
var dingTalkApi = serviceProvider.GetRequiredService<IDingTalkApi>();
```

### 与 HttpClientApiSourceGenerator 配合使用

HttpClientApiRegisterSourceGenerator 与 HttpClientApiSourceGenerator 完美配合：

1. **HttpClientApiSourceGenerator** 生成接口的实现类
2. **HttpClientApiRegisterSourceGenerator** 生成依赖注入注册代码
3. **完整的开发体验**：定义接口 → 自动生成实现 → 自动注册服务

#### 完整示例

```CSharp
// 1. 定义接口
[HttpClientApi("https://api.dingtalk.com", Timeout = 30)]
public interface IDingTalkApi
{
    [Get("/api/v1/user/{id}")]
    Task<UserDto> GetUserAsync([Query] string id);
}

// 2. 自动生成实现类 (由 HttpClientApiSourceGenerator 生成)
// public partial class DingTalkApi : IDingTalkApi { ... }

// 3. 自动生成注册代码 (由 HttpClientApiRegisterSourceGenerator 生成)
// public static class HttpClientApiExtensions { ... }

// 4. 在应用程序中使用
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddWebApiHttpClient(); // 自动注册

var app = builder.Build();

// 5. 在服务中注入使用
public class UserService
{
    private readonly IDingTalkApi _dingTalkApi;
    
    public UserService(IDingTalkApi dingTalkApi)
    {
        _dingTalkApi = dingTalkApi;
    }
    
    public async Task<UserDto> GetUserAsync(string userId)
    {
        return await _dingTalkApi.GetUserAsync(userId);
    }
}
```

### 高级配置

#### 1. 自定义 HttpClient 配置

如果需要更复杂的 HttpClient 配置，可以在注册后继续配置：

```CSharp
builder.Services.AddWebApiHttpClient()
    .ConfigureHttpClientDefaults(httpClient =>
    {
        httpClient.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            UseProxy = false,
            AllowAutoRedirect = false
        });
    });
```

#### 2. 添加自定义请求头

```CSharp
builder.Services.AddHttpClient<IDingTalkApi, DingTalkApi>(client =>
{
    client.BaseAddress = new Uri("https://api.dingtalk.com");
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("User-Agent", "MyApp/1.0");
    client.DefaultRequestHeaders.Add("X-API-Key", "your-api-key");
});
```

### 错误处理

注册生成器会自动处理以下错误情况：
- **无效的 [HttpClientApi] 特性**：忽略没有有效特性的接口
- **特性参数验证**：确保 BaseUrl 和 Timeout 参数的有效性
- **命名空间处理**：正确处理全局命名空间引用

### 生成的代码结构

```
obj/Debug/net8.0/generated/
├── Mud.ServiceCodeGenerator/
    ├── HttpClientApiSourceGenerator/
    │   └── YourNamespace.DingTalkApi.g.cs
    └── HttpClientApiRegisterSourceGenerator/
        └── HttpClientApiExtensions.g.cs
```

### 最佳实践

1. **统一配置**：在 [HttpClientApi] 特性中统一配置所有 API 的基础设置
2. **合理超时**：根据 API 的响应时间设置合理的超时时间
3. **命名规范**：遵循接口命名规范（I{ServiceName}Api）
4. **错误处理**：在服务层处理 API 调用异常
5. **日志记录**：利用生成的日志记录功能监控 API 调用

## HttpClient API 注册代码生成

HttpClientApiRegisterSourceGenerator 自动为标记了 [HttpClientApi] 特性的接口生成依赖注入注册代码，简化 HttpClient 服务的配置。

### 基本用法

#### 1. 定义 HTTP API 接口

```CSharp
[HttpClientApi("https://api.dingtalk.com", Timeout = 30)]
public interface IDingTalkApi
{
    [Get("/api/v1/user/{id}")]
    Task<UserDto> GetUserAsync([Query] string id);
    
    [Post("/api/v1/user")]
    Task<UserDto> CreateUserAsync([Body] UserDto user);
}

[HttpClientApi("https://api.wechat.com", Timeout = 60)]
public interface IWeChatApi
{
    [Get("/api/v1/user/{id}")]
    Task<UserDto> GetUserAsync([Query] string id);
}
```

#### 2. 生成的注册代码

自动生成的依赖注入注册扩展方法：

```CSharp
// 自动生成的代码 - HttpClientApiExtensions.g.cs
using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class HttpClientApiExtensions
    {
        public static IServiceCollection AddWebApiHttpClient(this IServiceCollection services)
        {
            services.AddHttpClient<global::YourNamespace.IDingTalkApi, global::YourNamespace.DingTalkApi>(client =>
            {
                client.BaseAddress = new Uri("https://api.dingtalk.com");
                client.Timeout = TimeSpan.FromSeconds(30);
            });
            
            services.AddHttpClient<global::YourNamespace.IWeChatApi, global::YourNamespace.WeChatApi>(client =>
            {
                client.BaseAddress = new Uri("https://api.wechat.com");
                client.Timeout = TimeSpan.FromSeconds(60);
            });
            
            return services;
        }
    }
}
```

### 配置选项

#### 1. HttpClientApi 特性参数

```CSharp
// 基本配置
[HttpClientApi("https://api.example.com")]
public interface IExampleApi { }

// 配置超时时间
[HttpClientApi("https://api.example.com", Timeout = 120)]
public interface IExampleApi { }

// 使用命名参数
[HttpClientApi(BaseUrl = "https://api.example.com", Timeout = 60)]
public interface IExampleApi { }
```

#### 2. 生成的 HttpClient 配置

生成的注册代码包含以下配置：
- **BaseAddress**: 从 [HttpClientApi] 特性的第一个参数获取
- **Timeout**: 从 Timeout 命名参数获取，默认 100 秒
- **服务注册**: 使用 AddHttpClient 方法注册接口和实现类

### 使用方式

#### 1. 在应用程序启动时调用

```CSharp
// 在 Program.cs 或 Startup.cs 中
var builder = WebApplication.CreateBuilder(args);

// 自动注册所有 HttpClient API 服务
builder.Services.AddWebApiHttpClient();

// 或者与其他服务注册一起使用
builder.Services
    .AddControllers()
    .AddWebApiHttpClient();
```

#### 2. 在控制台应用程序中使用

```CSharp
// 在控制台应用程序中
var services = new ServiceCollection();

// 注册 HttpClient API 服务
services.AddWebApiHttpClient();

var serviceProvider = services.BuildServiceProvider();
var dingTalkApi = serviceProvider.GetRequiredService<IDingTalkApi>();
```

### 与 HttpClientApiSourceGenerator 配合使用

HttpClientApiRegisterSourceGenerator 与 HttpClientApiSourceGenerator 完美配合：

1. **HttpClientApiSourceGenerator** 生成接口的实现类
2. **HttpClientApiRegisterSourceGenerator** 生成依赖注入注册代码
3. **完整的开发体验**：定义接口 → 自动生成实现 → 自动注册服务

#### 完整示例

```CSharp
// 1. 定义接口
[HttpClientApi("https://api.dingtalk.com", Timeout = 30)]
public interface IDingTalkApi
{
    [Get("/api/v1/user/{id}")]
    Task<UserDto> GetUserAsync([Query] string id);
}

// 2. 自动生成实现类 (由 HttpClientApiSourceGenerator 生成)
// public partial class DingTalkApi : IDingTalkApi { ... }

// 3. 自动生成注册代码 (由 HttpClientApiRegisterSourceGenerator 生成)
// public static class HttpClientApiExtensions { ... }

// 4. 在应用程序中使用
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddWebApiHttpClient(); // 自动注册

var app = builder.Build();

// 5. 在服务中注入使用
public class UserService
{
    private readonly IDingTalkApi _dingTalkApi;
    
    public UserService(IDingTalkApi dingTalkApi)
    {
        _dingTalkApi = dingTalkApi;
    }
    
    public async Task<UserDto> GetUserAsync(string userId)
    {
        return await _dingTalkApi.GetUserAsync(userId);
    }
}
```

### 高级配置

#### 1. 自定义 HttpClient 配置

如果需要更复杂的 HttpClient 配置，可以在注册后继续配置：

```CSharp
builder.Services.AddWebApiHttpClient()
    .ConfigureHttpClientDefaults(httpClient =>
    {
        httpClient.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            UseProxy = false,
            AllowAutoRedirect = false
        });
    });
```

#### 2. 添加自定义请求头

```CSharp
builder.Services.AddHttpClient<IDingTalkApi, DingTalkApi>(client =>
{
    client.BaseAddress = new Uri("https://api.dingtalk.com");
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("User-Agent", "MyApp/1.0");
    client.DefaultRequestHeaders.Add("X-API-Key", "your-api-key");
});
```

### 生成的代码结构

```
obj/Debug/net8.0/generated/
├── Mud.ServiceCodeGenerator/
    ├── HttpClientApiSourceGenerator/
    │   └── YourNamespace.DingTalkApi.g.cs
    └── HttpClientApiRegisterSourceGenerator/
        └── HttpClientApiExtensions.g.cs
```

### 最佳实践

1. **统一配置**：在 [HttpClientApi] 特性中统一配置所有 API 的基础设置
2. **合理超时**：根据 API 的响应时间设置合理的超时时间
3. **命名规范**：遵循接口命名规范（I{ServiceName}Api）
4. **错误处理**：在服务层处理 API 调用异常
5. **日志记录**：利用生成的日志记录功能监控 API 调用


## 生成代码查看

要查看生成的代码，可以在项目文件中添加以下配置：

```xml
<PropertyGroup>
  <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
</PropertyGroup>
```

生成的代码将位于 `obj/[Configuration]/[TargetFramework]/generated/` 目录下，文件名以 `.g.cs` 结尾。

## 维护者

[倔强的泥巴](https://gitee.com/mudtools)


### 许可证

本项目采用MIT许可证模式：

- [MIT 许可证](https://gitee.com/mudtools/mud-code-generator/blob/master/LICENSE)

### 免责声明

本项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。

不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目二次开发而产生的一切法律纠纷和责任，我们不承担任何责任。