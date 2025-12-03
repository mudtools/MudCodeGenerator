# Mud 服务代码生成器

## 功能介绍

Mud 服务代码生成器是一个基于 Roslyn 的源代码生成器，用于自动生成服务层相关代码，提高开发效率。它包含以下主要功能：

1. **HttpClient API 代码生成** - 为标记了 HTTP 方法特性的接口生成完整的 HttpClient 实现类
2. **依赖注入代码生成** - 自动为类生成构造函数注入代码，包括日志、缓存、用户管理等常用服务
3. **自动服务注册生成** - 自动生成服务注册扩展方法，简化依赖注入配置
4. **服务类代码生成** - 根据实体类自动生成服务接口和服务实现类
5. **HttpClient API 包装生成** - 为 HttpClient API 接口生成包装接口和实现类，简化 Token 管理等复杂逻辑
6. **COM对象包装生成** - 为COM对象生成.NET包装类，简化COM互操作

## 项目参数配置

### 通用配置参数

```xml
<PropertyGroup>
  <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>  <!-- 在obj目录下保存生成的代码 -->
  
  <!-- HttpClient API 配置 -->
  <HttpClientOptionsName>HttpClientOptions</HttpClientOptionsName>  <!-- HttpClient配置类名 -->
  
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
  <CompilerVisibleProperty Include="HttpClientOptionsName" />
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
  <PackageReference Include="Mud.ServiceCodeGenerator" Version="1.2.3"/>
</ItemGroup>
```

## 1. HttpClient API 代码生成

### 基本用法

为接口添加 `[HttpClientApi]` 特性并使用HTTP方法特性：

```CSharp
[HttpClientApi("https://api.example.com", Timeout = 30)]
public interface IUserApi
{
    [Get("users/{id}")]
    Task<UserInfo> GetUserAsync(string id);
    
    [Post("users")]
    Task<UserInfo> CreateUserAsync([Body] CreateUserRequest request);
    
    [Put("users/{id}")]
    Task<UserInfo> UpdateUserAsync(string id, [Body] UpdateUserRequest request);
    
    [Delete("users/{id}")]
    Task DeleteUserAsync(string id);
    
    [Get("users")]
    Task<List<UserInfo>> GetUsersAsync([Query] string? name = null, [Query] int? page = 1);
}
```

### 支持的HTTP方法

- `[Get("path")]` - GET请求
- `[Post("path")]` - POST请求  
- `[Put("path")]` - PUT请求
- `[Delete("path")]` - DELETE请求
- `[Patch("path")]` - PATCH请求
- `[Head("path")]` - HEAD请求
- `[Options("path")]` - OPTIONS请求

### 参数类型支持

#### 路径参数
```CSharp
[Get("users/{userId}/posts/{postId}")]
Task<Post> GetPostAsync(string userId, string postId);
```

#### 查询参数
```CSharp
[Get("users")]
Task<List<User>> GetUsersAsync([Query] string name, [Query] int page = 1);
```

#### 数组查询参数
```CSharp
[Get("users")]
Task<List<User>> GetUsersAsync([ArrayQuery] string[] tags, [ArrayQuery(",", "categories")] string[] categories);
```

#### 请求体参数
```CSharp
[Post("users")]
Task<User> CreateUserAsync([Body] CreateUserRequest request);

[Post("data")]
Task<Result> SendDataAsync([Body(UseStringContent = true, ContentType = "text/plain")] string rawData);
```

#### 请求头参数
```CSharp
[Post("data")]
Task<Result> PostDataAsync([Body] DataRequest request, [Header("Authorization")] string token);
```

#### 文件下载
```CSharp
[Get("files/{fileId}")]
Task<byte[]> DownloadFileAsync(string fileId);

[Get("files/{fileId}")]
Task DownloadFileAsync(string fileId, [FilePath] string filePath);
```

### Token管理集成

#### 定义Token管理器
```CSharp
public interface ITokenManager
{
    Task<string> GetTokenAsync();
}
```

#### 使用Header传递Token
```CSharp
[HttpClientApi(TokenManage = nameof(ITokenManager))]
[Header("Authorization")]
public interface IProtectedApi
{
    [Get("protected/data")]
    Task<Data> GetDataAsync();
}
```

#### 使用Query参数传递Token
```CSharp
[HttpClientApi(TokenManage = nameof(ITokenManager))]
[Query("Authorization")]
public interface IProtectedApi
{
    [Get("protected/data")]
    Task<Data> GetDataAsync();
}
```

### 部分方法事件钩子

为每个HTTP方法自动生成事件钩子：

```CSharp
public partial class ExampleApi
{
    // 方法级别事件钩子
    partial void OnGetUserBefore(HttpRequestMessage request, string url);
    partial void OnGetUserAfter(HttpResponseMessage response, string url);
    partial void OnGetUserFail(HttpResponseMessage response, string url);
    partial void OnGetUserError(Exception error, string url);
    
    // 接口级别事件钩子
    partial void OnExampleApiRequestBefore(HttpRequestMessage request, string url);
    partial void OnExampleApiRequestAfter(HttpResponseMessage response, string url);
    partial void OnExampleApiRequestFail(HttpResponseMessage response, string url);
    partial void OnExampleApiRequestError(Exception error, string url);
}
```

### 高级功能

#### 抽象类支持
```CSharp
[HttpClientApi(IsAbstract = true)]
public abstract class BaseApiClient
{
    protected BaseApiClient(HttpClient httpClient, ILogger logger, /* ... */)
    {
        // 基础初始化逻辑
    }
}

[HttpClientApi(InheritedFrom = "BaseApiClient")]
public interface IMyApi : BaseApiClient
{
    [Get("data")]
    Task<Data> GetDataAsync();
}
```

#### 按组注册功能
```CSharp
[HttpClientApi("https://api.dingtalk.com", RegistryGroupName = "Dingtalk")]
public interface IDingtalkApi
{
    [Get("user/info")]
    Task<UserInfo> GetUserInfoAsync();
}
```

### HttpClientApi特性参数

| 参数名 | 类型 | 必需 | 默认值 | 说明 |
|--------|------|------|--------|------|
| BaseAddress | string | 否 | null | API 基础地址 |
| Timeout | int | 否 | 50 | 请求超时时间（秒） |
| ContentType | string | 否 | application/json | 默认内容类型 |
| RegistryGroupName | string | 否 | null | 注册分组名称 |
| TokenManage | string | 否 | null | Token管理器接口名 |
| IsAbstract | bool | 否 | false | 是否生成抽象类 |
| InheritedFrom | string | 否 | null | 继承的基类名 |

## 2. 依赖注入代码生成

### 基本用法

使用各种注入特性自动生成构造函数注入代码：

```CSharp
[ConstructorInject]     // 字段构造函数注入
[LoggerInject]          // 日志注入
[CacheInject]           // 缓存管理器注入
[UserInject]            // 用户管理器注入
[CustomInject<IRepository<SysUser>>(VarName = "_userRepository")]  // 自定义注入
public partial class SysUserService
{
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;
    
    // 生成的代码将包含所有注入项和构造函数
}
```

### 注入特性详解

#### ConstructorInjectAttribute 字段注入
扫描类中所有私有只读字段，生成相应的构造函数参数和赋值语句。

#### LoggerInjectAttribute 日志注入
注入 `ILogger<T>` 类型的日志记录器，自动生成 Logger 实例创建代码。

#### CacheInjectAttribute 缓存管理器注入
注入缓存管理器实例，默认类型为 `ICacheManager`，默认字段名为 `_cacheManager`。

#### UserInjectAttribute 用户管理器注入
注入用户管理器实例，默认类型为 `IUserManager`，默认字段名为 `_userManager`。

#### OptionsInjectAttribute 配置项注入
根据指定的配置项类型注入配置实例，支持泛型语法：
```CSharp
[OptionsInject<TenantOptions>]  // 推荐的泛型方式
[OptionsInject(OptionType = "TenantOptions")]  // 传统方式
```

#### CustomInjectAttribute 自定义注入
注入任意类型的依赖项，支持泛型语法：
```CSharp
[CustomInject<IRepository<SysUser>>(VarName = "_userRepository")]  // 推荐
[CustomInject(VarType = "IRepository<SysUser>", VarName = "_userRepository")]  // 传统
```

### 组合注入示例

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
    
    // 生成的代码将包含所有注入项、字段、构造函数参数和赋值语句
}
```

### 忽略字段注入

对于不需要通过构造函数注入的字段，可以使用 `[IgnoreGenerator]` 特性标记：

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

## 3. 自动服务注册生成

### AutoRegisterAttribute 自动注册

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

### AutoRegisterKeyedAttribute 键控服务注册

```CSharp
// 键控服务注册（Microsoft.Extensions.DependencyInjection 8.0+）
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

### 生成的注册代码

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

### 使用方式

```CSharp
var builder = WebApplication.CreateBuilder(args);

// 自动注册所有标记的服务
builder.Services.AddAutoRegister();

// 或者与其他注册一起使用
builder.Services
    .AddControllers()
    .AddAutoRegister();
```

## 4. HttpClient API 包装生成

### 功能特点

- **自动Token管理**：自动处理标记了 `[Token]` 特性的参数
- **错误处理和日志记录**：包含完整的异常处理和日志记录
- **XML注释保留**：自动保留原始方法的XML文档注释
- **重载方法支持**：正确处理重载方法的XML注释
- **灵活的配置**：支持自定义Token管理接口和包装接口名称

### 基本用法

#### 1. 定义Token管理接口
```CSharp
public interface ITokenManager
{
    Task<string> GetTokenAsync();
}
```

#### 2. 定义HTTP API接口并添加包装特性
```CSharp
[HttpClientApi("https://api.dingtalk.com", Timeout = 60)]
[HttpClientApiWrap(TokenManage = nameof(ITokenManager))]
public interface IDingtalkApi
{
    [Get("user/info")]
    Task<UserInfo> GetUserInfoAsync([Token][Header("Authorization")] string token);
    
    [Post("message/send")]
    Task SendMessageAsync([Token][Query("access_token")] string token, [Body] MessageRequest request);
}
```

#### 3. 生成的包装接口和实现

自动生成：
- 包装接口（移除Token参数）
- 包装实现类（自动处理Token获取和传递）

#### 4. 使用方式
```CSharp
public class UserService
{
    private readonly IDingtalkApiWrap _dingtalkApiWrap;
    
    public UserService(IDingtalkApiWrap dingtalkApiWrap)
    {
        _dingtalkApiWrap = dingtalkApiWrap;
    }
    
    public async Task<UserInfo> GetUserInfoAsync()
    {
        // 无需手动处理Token，包装类会自动处理
        return await _dingtalkApiWrap.GetUserInfoAsync();
    }
}
```

### HttpClientApiWrap特性参数

| 参数名 | 类型 | 必需 | 默认值 | 说明 |
|--------|------|------|--------|------|
| TokenManage | string | 否 | "ITokenManager" | Token管理器接口名 |
| WrapInterface | string | 否 | "{OriginalInterface}Wrap" | 包装接口名称 |

## 5. 服务类代码生成

基于实体类自动生成服务接口和实现类：

```CSharp
[ServiceGenerator(EntityType = nameof(SysDeptEntity))]
public partial class SysDeptService
{
}
```

生成的代码将包含基于实体的完整服务接口和实现类。

## 6. COM对象包装生成

支持为COM对象生成.NET包装类：

```CSharp
[ComObjectWrap]
[ComCollectionWrap]
[ComPropertyWrap(Name = "Items", PropertyType = PropertyType.ReadOnly)]
public interface IMyComObject
{
    [ComPropertyWrap]
    string Name { get; set; }
    
    [ComPropertyWrap(PropertyType = PropertyType.Method)]
    void DoSomething();
}
```

生成的包装类将自动处理COM对象的创建、属性访问和方法调用。

## 7. 依赖注入和配置

### HttpClient配置示例
```CSharp
// 在Program.cs中配置
var builder = WebApplication.CreateBuilder(args);

// 配置HttpClient选项
builder.Services.Configure<HttpClientOptions>(options =>
{
    options.BaseUrl = "https://api.example.com";
    options.TimeOut = "30";
    options.EnableLogging = true;
});

// 注册Token管理器
builder.Services.AddSingleton<ITokenManager, MyTokenManager>();

// 自动注册所有服务
builder.Services
    .AddAutoRegister()
    .AddWebApiHttpClient();
```

### 使用生成的服务
```CSharp
public class MyController : ControllerBase
{
    private readonly IUserApi _userApi;
    private readonly ILogger<MyController> _logger;
    
    public MyController(IUserApi userApi, ILogger<MyController> logger)
    {
        _userApi = userApi;
        _logger = logger;
    }
    
    [HttpGet("users/{id}")]
    public async Task<UserInfo> GetUser(string id)
    {
        try
        {
            return await _userApi.GetUserAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户信息失败: {UserId}", id);
            throw;
        }
    }
}
```

## 生成的代码结构

```
obj/Debug/net8.0/generated/
├── Mud.ServiceCodeGenerator/
    ├── HttpInvoke/
    │   ├── HttpClientApiSourceGenerator/
    │   │   └── YourNamespace.UserApi.g.cs
    │   ├── HttpInvokeRegistrationGenerator/
    │   │   └── HttpClientApiExtensions.g.cs
    │   └── HttpInvokeWrapSourceGenerator/
    │       ├── YourNamespace.DingtalkApi.Wrap.g.cs
    │       └── YourNamespace.DingtalkApi.WrapImpl.g.cs
    ├── CodeInject/
    │   └── AutoRegisterExtension.g.cs
    └── ServiceCode/
        └── YourNamespace.SysDeptService.g.cs
```

## 最佳实践

1. **统一配置**：在项目配置中统一设置所有生成参数
2. **合理命名**：遵循接口命名规范（I{ServiceName}Api）
3. **错误处理**：在业务层处理API调用异常
4. **日志记录**：利用生成的日志记录功能监控API调用
5. **Token管理**：为不同类型的API使用不同的Token管理策略
6. **生命周期管理**：根据业务需求选择合适的服务生命周期

## 故障排除

### 常见问题

1. **生成的代码不出现**：检查 `<EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>` 配置
2. **依赖注入失败**：确保已注册所有必要的服务（如Token管理器）
3. **编译错误**：检查特性使用是否正确，参数类型是否匹配
4. **运行时异常**：检查HttpClient配置和API地址是否正确

### 调试技巧

1. 启用 `<EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>` 查看生成的代码
2. 查看生成器诊断信息，了解代码生成过程
3. 使用部分方法事件钩子添加调试日志
4. 检查生成的代码结构是否符合预期

通过Mud服务代码生成器，开发者可以显著减少重复代码编写，专注于业务逻辑实现，提高开发效率和代码质量。