# Mud 服务代码生成器

## 功能介绍

Mud 服务代码生成器是一个基于 Roslyn 的源代码生成器，用于自动生成服务层相关代码，提高开发效率。它包含以下主要功能：

1. **服务类代码生成** - 根据实体类自动生成服务接口和服务实现类
2. **依赖注入代码生成** - 自动为类生成构造函数注入代码，包括日志、缓存、用户管理等常用服务

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
  <PackageReference Include="Mud.ServiceCodeGenerator" Version="1.1.6"/>
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
使用 [OptionsInject] 特性可以根据指定的配置项类型注入配置实例。

示例：
```CSharp
[OptionsInject(OptionType = "TenantOptions")]
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
使用 [CustomInject] 特性可以注入任意类型的依赖项。需要指定注入类型(VarType)和字段名(VarName)。

示例：
```CSharp
[CustomInject(VarType = "IRepository<SysUser>", VarName = "_userRepository")]
[CustomInject(VarType = "INotificationService", VarName = "_notificationService")]
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

多种注入特性可以组合使用，生成器会自动合并所有注入需求：

```CSharp
[ConstructorInject]
[LoggerInject]
[CacheInject]
[UserInject]
[OptionsInject(OptionType = "TenantOptions")]
[CustomInject(VarType = "IRepository<SysUser>", VarName = "_userRepository")]
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