# Mud 服务代码生成器

## 功能介绍

Mud 服务代码生成器是一个基于 Roslyn 的源代码生成器，用于自动生成服务层相关代码，提高开发效率。它包含以下主要功能：

1. **服务类代码生成** - 根据实体类自动生成服务接口和服务实现类
2. **依赖注入代码生成** - 自动为类生成构造函数注入代码，包括日志、缓存、用户管理等常用服务
3. **DTO代码生成** - 根据实体类自动生成数据传输对象

## 项目参数配置

在使用 Mud 服务代码生成器时，可以通过在项目文件中配置以下参数来自定义生成行为：

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
  <!-- 引入的代码生成器程序集，注意后面的参数 -->
  <PackageReference Include="Mud.Common.CodeGenerator" Version="1.0.1" PrivateAssets="all" OutputItemType="Analyzer" ReferenceOutputAssembly="false"/>
</ItemGroup>
```

## 代码生成功能及样例

### 1. 服务类代码生成

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

```cs
[ServiceGenerator(EntityType = nameof(SysDeptEntity))]
public partial class SysDeptService
{
}
```

生成的代码将包含基于实体的完整服务接口和实现类。

### 2. 依赖注入代码生成

使用各种注入特性为类自动生成构造函数注入代码：

```cs
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

```cs
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

### 3. DTO代码生成

在实体程序项目中添加DTO代码生成配置：

```xml
<PropertyGroup>
  <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
  <EntitySuffix>Entity</EntitySuffix>  <!-- 实体类前缀配置 -->
  <EntityAttachAttributes>SuppressSniffer</EntityAttachAttributes>  <!-- 实体类加上Attribute特性配置，多个特性时使用','分隔 -->
</PropertyGroup>
<ItemGroup>
  <CompilerVisibleProperty Include="EntitySuffix" />
  <CompilerVisibleProperty Include="EntityAttachAttributes"/>
</ItemGroup>
```

在实体中添加DTO代码生成DtoGenerator特性：

```cs
[DtoGenerator]
public class SysClientEntity : BaseEntity
{
    // 无需生成DTO代码
    [IgnoreGenerator]
    public string DelFlag { get; set; }
    
    // 其他属性将自动生成到DTO类中
    public string Name { get; set; }
    public string Code { get; set; }
}
```

## 维护者

[倔强的泥巴](https://gitee.com/mudtools)


## 许可证

本项目采用MIT许可证模式：

- [MIT 许可证](../../LICENSE-MIT)

## 免责声明

本项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。

不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目二次开发而产生的一切法律纠纷和责任，我们不承担任何责任。