# Mud 代码生成器

## 功能介绍

Mud 代码生成器是一套基于 Roslyn 的源代码生成器，用于根据实体类和服务类自动生成相关代码，提高开发效率。该套件包含以下两个主要组件：

1. **Mud.EntityCodeGenerator** - 实体代码生成器，根据实体类自动生成各种相关代码
   - DTO代码生成 - 根据实体类自动生成数据传输对象（DTO）
   - VO代码生成 - 根据实体类自动生成视图对象（VO）
   - 查询输入类生成 - 根据实体类自动生成查询输入类（QueryInput）
   - 创建输入类生成 - 根据实体类自动生成创建输入类（CrInput）
   - 更新输入类生成 - 根据实体类自动生成更新输入类（UpInput）
   - 实体映射方法生成 - 自动生成实体与DTO之间的映射方法

2. **Mud.ServiceCodeGenerator** - 服务代码生成器，用于自动生成服务层相关代码
   - 服务类代码生成 - 根据实体类自动生成服务接口和服务实现类
   - 依赖注入代码生成 - 自动为类生成构造函数注入代码，包括日志、缓存、用户管理等常用服务

### 模块概览

| 模块 | 当前版本 | 开源协议 | 
|---|---|---|
| [![Mud.EntityCodeGenerator](https://img.shields.io/badge/Mud.EntityCodeGenerator-success.svg)](https://gitee.com/mudtools/mud-code-generator) | [![Nuget](https://img.shields.io/nuget/v/Mud.EntityCodeGenerator.svg)](https://www.nuget.org/packages/Mud.EntityCodeGenerator/) | [![License](https://img.shields.io/badge/License-MIT-blue.svg)](https://gitee.com/mudtools/mud-code-generator/blob/master/LICENSE)
| [![Mud.EntityCodeGenerator](https://img.shields.io/badge/Mud.EntityCodeGenerator-success.svg)](https://gitee.com/mudtools/mud-code-generator) | [![Nuget](https://img.shields.io/nuget/v/Mud.ServiceCodeGenerator.svg)](https://www.nuget.org/packages/Mud.ServiceCodeGenerator/) | [![License](https://img.shields.io/badge/License-MIT-blue.svg)](https://gitee.com/mudtools/mud-code-generator/blob/master/LICENSE)

## 项目参数配置

在使用 Mud 代码生成器时，可以通过在项目文件中配置以下参数来自定义生成行为：

### 实体代码生成器配置参数

```xml
<PropertyGroup>
  <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>  <!-- 在obj目录下保存生成的代码 -->
  <EntitySuffix>Entity</EntitySuffix>  <!-- 实体类后缀配置 -->
  <EntityAttachAttributes>SuppressSniffer</EntityAttachAttributes>  <!-- 实体类加上Attribute特性配置，多个特性时使用','分隔 -->
</PropertyGroup>

<ItemGroup>
  <CompilerVisibleProperty Include="EntitySuffix" />
  <CompilerVisibleProperty Include="EntityAttachAttributes" />
</ItemGroup>
```

### 服务代码生成器配置参数

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
  <!-- 引入的代码生成器程序集，注意后面的参数 -->
  <PackageReference Include="Mud.EntityCodeGenerator" Version="1.0.5" PrivateAssets="all" OutputItemType="Analyzer" ReferenceOutputAssembly="false"/>
  <PackageReference Include="Mud.ServiceCodeGenerator" Version="1.0.5" PrivateAssets="all" OutputItemType="Analyzer" ReferenceOutputAssembly="false"/>
</ItemGroup>
```

### 配置参数说明

| 参数名 | 默认值 | 说明 |
|--------|--------|------|
| EmitCompilerGeneratedFiles | false | 是否在obj目录下保存生成的代码，设为true便于调试 |
| EntitySuffix | Entity | 实体类后缀，用于识别实体类 |
| EntityAttachAttributes | (空) | 实体类上需要附加的特性，多个特性用逗号分隔 |
| DefaultCacheManagerType | ICacheManager | 缓存管理器类型默认值 |
| DefaultUserManagerType | IUserManager | 用户管理器类型默认值 |
| DefaultLoggerVariable | _logger | 日志变量名默认值 |
| DefaultCacheManagerVariable | _cacheManager | 缓存管理器变量名默认值 |
| DefaultUserManagerVariable | _userManager | 用户管理器变量名默认值 |
| ServiceGenerator | true | 是否生成服务端代码 |
| ImpAssembly | (空) | 需要生成代码的接口实现程序集 |

## 代码生成功能及样例

### 1. 实体相关代码生成

在实体程序项目中添加生成器及配置相关参数：

```xml
<PropertyGroup>
  <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
  <EntitySuffix>Entity</EntitySuffix>
  <EntityAttachAttributes>SuppressSniffer</EntityAttachAttributes>
</PropertyGroup>
<ItemGroup>
  <CompilerVisibleProperty Include="EntitySuffix" />
  <CompilerVisibleProperty Include="EntityAttachAttributes"/>
</ItemGroup>
```

在实体中添加DtoGenerator特性：

```csharp
/// <summary>
/// 客户端信息实体类
/// </summary>
[DtoGenerator]
[Table(Name = "sys_client"),SuppressSniffer]
public partial class SysClientEntity
{
    /// <summary>
    /// id
    /// </summary>
    [property: Column(Name = "id", IsPrimary = true, Position = 1)]
    [property: Required(ErrorMessage = "id不能为空")]
    private long? _id;

    /// <summary>
    /// 客户端key
    /// </summary>
    [property: Column(Name = "client_key", Position = 3)]
    [property: Required(ErrorMessage = "客户端key不能为空")]
    [property: ExportProperty("客户端key")]
    private string _clientKey;

    /// <summary>
    /// 删除标志（0代表存在 2代表删除）
    /// </summary>
    [property: Column(Name = "del_flag", Position = 10)]
    [property: ExportProperty("删除标志")]
    [IgnoreQuery]
    private string _delFlag;
}
```

### 2. 服务类代码生成

在服务类程序项目中添加服务代码生成配置：

```xml
<PropertyGroup>
  <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
  <EntityAssemblyPrefix>TestClassLibrary</EntityAssemblyPrefix>  <!-- 实体程序集前缀配置，用于业务代码生成时搜索对应的实体类型 -->
</PropertyGroup>
<ItemGroup>
  <CompilerVisibleProperty Include="EntityAssemblyPrefix" />
</ItemGroup>
```

在服务中添加服务代码生成特性：

```csharp
[ServiceGenerator(EntityType = nameof(SysDeptEntity))]
public partial class SysDeptService
{
}
```

生成的代码将包含基于实体的完整服务接口和实现类。

### 3. 依赖注入代码生成

使用各种注入特性为类自动生成构造函数注入代码：

```csharp
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

生成的代码示例：

```csharp
public partial class SysUserService
{
    private readonly ILogger<SysUserService> _logger;
    private readonly ICacheManager _cacheManager;
    private readonly IUserManager _userManager;
    private readonly IRepository<SysUser> _userRepository;

    public SysUserService(
        ILogger<SysUserService> logger,
        ICacheManager cacheManager,
        IUserManager userManager,
        IRepository<SysUser> userRepository)
    {
        _logger = logger;
        _cacheManager = cacheManager;
        _userManager = userManager;
        _userRepository = userRepository;
    }
}
```

## 项目结构

```
Mud.CodeGenerator
├── Core
│   ├── Mud.CodeGenerator                // 代码生成器核心基类库
│   ├── Mud.EntityCodeGenerator          // 实体代码生成器
│   └── Mud.ServiceCodeGenerator         // 服务代码生成器
├── Test
│   ├── CodeGeneratorTest                // 代码生成器测试项目
│   └── Mud.Common.CodeGenerator         // 通用代码生成器特性定义
└── README.md
```

## 使用方法

1. 在您的项目中添加对 `Mud.Common.CodeGenerator` 包的引用
2. 根据需要配置项目参数
3. 在实体类或服务类上添加相应的特性标记
4. 编译项目，代码生成器将自动生成相关代码

## 注意事项

1. 使用 `EmitCompilerGeneratedFiles=true` 可以在 obj 目录下查看生成的代码，便于调试
2. 生成的代码文件名以 `.g.cs` 结尾
3. 所有生成的代码都是 partial 类，不会影响您手动编写的代码
4. 建议在实体类和服务类上使用 partial 关键字，以便代码生成器可以扩展它们


## 维护者

[倔强的泥巴](https://gitee.com/mudtools)


## 许可证

本项目采用MIT许可证模式：

- [MIT 许可证](../../LICENSE-MIT)

## 免责声明

本项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。

不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目二次开发而产生的一切法律纠纷和责任，我们不承担任何责任。