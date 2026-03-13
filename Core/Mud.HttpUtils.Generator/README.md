# Mud.HttpUtils

## 概述

Mud.HttpUtils 是一个基于 Roslyn 的源代码生成器，自动为标记了 `[HttpClientApi]` 特性的接口生成 HttpClient 实现类。

## 功能特性

- **自动代码生成**：根据接口定义自动生成 HttpClient 实现
- **HTTP 方法支持**：支持 GET、POST、PUT、DELETE、PATCH、HEAD、OPTIONS 等 HTTP 方法
- **参数处理**：自动处理 Path、Query、Header、Body 参数
- **Token 管理**：支持多种 Token 类型（TenantAccessToken、UserAccessToken、AppAccessToken、Both）
- **依赖注入**：自动生成服务注册扩展方法
- **类型安全**：强类型的 API 调用，编译时检查

## 使用方法

### 1. 定义 API 接口

```csharp
[HttpClientApi("https://api.example.com", Timeout = 60)]
public interface IExampleApi
{
    [Get("/users/{id}")]
    Task<UserInfo> GetUserAsync([Path] int id);
    
    [Post("/users")]
    Task<UserInfo> CreateUserAsync([Body] CreateUserRequest request);
    
    [Get("/users")]
    Task<List<UserInfo>> GetUsersAsync([Query] string? name = null, [Query] int page = 1);
}
```

### 2. 注册服务

```csharp
// 在 Program.cs 或 Startup.cs 中
services.AddWebApiHttpClient();
```

### 3. 使用 API

```csharp
public class UserService
{
    private readonly IExampleApi _api;
    
    public UserService(IExampleApi api)
    {
        _api = api;
    }
    
    public async Task<UserInfo> GetUserAsync(int id)
    {
        return await _api.GetUserAsync(id);
    }
}
```

## 特性说明

### HttpClientApi 特性

```csharp
[HttpClientApi(
    baseUrl: "https://api.example.com",  // API 基础地址
    Timeout = 30,                        // 超时时间（秒）
    TokenManage = "ITokenManager",       // Token 管理器类型
    RegistryGroupName = "Example"         // 注册组名称
)]
```

### HTTP 方法特性

- `[Get]` - GET 请求
- `[Post]` - POST 请求
- `[Put]` - PUT 请求
- `[Delete]` - DELETE 请求
- `[Patch]` - PATCH 请求
- `[Head]` - HEAD 请求
- `[Options]` - OPTIONS 请求

### 参数特性

- `[Path]` - URL 路径参数
- `[Query]` - URL 查询参数
- `[Header]` - HTTP 头参数
- `[Body]` - 请求体参数
- `[Token]` - Token 参数

### Token 类型

```csharp
public enum TokenType
{
    TenantAccessToken,  // 租户访问令牌
    UserAccessToken,    // 用户访问令牌
    AppAccessToken,     // 应用访问令牌
    Both                // 两者都支持
}
```

## 项目结构

```
Mud.HttpUtils/
├── HttpInvoke/
│   ├── Generators/          # 代码生成器
│   │   ├── InterfaceCodeGenerator.cs
│   │   ├── ClassStructureGenerator.cs
│   │   ├── ConstructorGenerator.cs
│   │   └── RequestBuilder.cs
│   ├── Helpers/             # 辅助类
│   │   ├── BaseClassValidator.cs
│   │   └── ParameterValidationHelper.cs
│   └── Models/              # 数据模型
│       ├── HttpClientApiInfo.cs
│       ├── HttpClientApiInfoBase.cs
│       ├── MethodAnalysisResult.cs
│       ├── ParameterAttributeInfo.cs
│       └── ParameterInfo.cs
├── HttpInvokeBaseSourceGenerator.cs
├── HttpInvokeClassSourceGenerator.cs
├── HttpInvokeRegistrationGenerator.cs
├── GlobalUsings.cs
├── Mud.HttpUtils.csproj
└── README.md
```

## 依赖项

- .NET Standard 2.0
- Microsoft.CodeAnalysis.Analyzers
- Microsoft.CodeAnalysis.CSharp

## 版本历史

### 1.0.0
- 初始版本
- 从 Mud.ServiceCodeGenerator 项目中独立出来
- 支持基本的 HTTP API 代码生成功能

## 许可证

本项目遵循 MIT 许可证。详细信息请参见 [LICENSE](../../LICENSE-MIT) 文件。

## 贡献

欢迎提交 Issue 和 Pull Request 来改进这个项目。

## 相关项目

- [Mud.CodeGenerator](../Mud.CodeGenerator/) - 基础代码生成框架
- [Mud.EntityCodeGenerator](../Mud.EntityCodeGenerator/) - 实体代码生成器
- [Mud.ServiceCodeGenerator](../Mud.ServiceCodeGenerator/) - 服务代码生成器
