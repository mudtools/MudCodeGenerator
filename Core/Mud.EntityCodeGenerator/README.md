# Mud 实体代码生成器

## 功能介绍

Mud 实体代码生成器是一个基于 Roslyn 的源代码生成器，用于根据实体类自动生成各种相关代码，提高开发效率。它包含以下主要功能：

1. **DTO代码生成** - 根据实体类自动生成数据传输对象（DTO）
2. **VO代码生成** - 根据实体类自动生成视图对象（VO）
3. **查询输入类生成** - 根据实体类自动生成查询输入类（QueryInput）
4. **创建输入类生成** - 根据实体类自动生成创建输入类（CrInput）
5. **更新输入类生成** - 根据实体类自动生成更新输入类（UpInput）
6. **实体映射方法生成** - 自动生成实体与DTO之间的映射方法

## 项目参数配置

在使用 Mud 实体代码生成器时，可以通过在项目文件中配置以下参数来自定义生成行为：

### 通用配置参数

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

### 依赖项配置

```xml
<ItemGroup>
  <!-- 引入的代码生成器程序集，注意后面的参数 -->
  <PackageReference Include="Mud.Common.CodeGenerator" Version="1.0.1" PrivateAssets="all" OutputItemType="Analyzer" ReferenceOutputAssembly="false"/>
</ItemGroup>
```

### 配置参数说明

| 参数名 | 默认值 | 说明 |
|--------|--------|------|
| EmitCompilerGeneratedFiles | false | 是否在obj目录下保存生成的代码，设为true便于调试 |
| EntitySuffix | Entity | 实体类后缀，用于识别实体类 |
| EntityAttachAttributes | (空) | 实体类上需要附加的特性，多个特性用逗号分隔 |

## 代码生成功能及样例

### 1. DTO/VO/输入类代码生成

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

```CSharp
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
    [property: TableField(Fille = FieldFill.Insert, Value = FillValue.Id)]
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

基于以上实体，将自动生成以下几类代码：

#### 实体类属性
```CSharp
/// <summary>
/// 客户端信息实体类
/// </summary>
public partial class SysClientEntity
{
    /// <summary>
    /// id
    /// </summary>
    [TableField(Fille = FieldFill.Insert, Value = FillValue.Id), Column(Name = "id", IsPrimary = true, Position = 1)]
    public long? Id
    {
        get
        {
            return _id;
        }

        set
        {
            _id = value;
        }
    }

    /// <summary>
    /// 客户端key
    /// </summary>
    [Column(Name = "client_key", Position = 3)]
    public string? ClientKey
    {
        get
        {
            return _clientKey;
        }

        set
        {
            _clientKey = value;
        }
    }

    /// <summary>
    /// 删除标志（0代表存在 2代表删除）
    /// </summary>
    [Column(Name = "del_flag", Position = 10)]
    public string? DelFlag
    {
        get
        {
            return _delFlag;
        }

        set
        {
            _delFlag = value;
        }
    }

    /// <summary>
    /// 通用的实体映射至VO对象方法。
    /// </summary>
    public virtual SysClientListOutput MapTo()
    {
        var voObj = new SysClientListOutput();
        voObj.id = this.Id;
        voObj.clientKey = this.ClientKey;
        voObj.delFlag = this.DelFlag;
        ConverterUtils.RaiseMapAfter(voObj);
        return voObj;
    }
}
```

#### VO类 (视图对象)
```CSharp
/// <summary>
/// 客户端信息实体类
/// </summary>
[SuppressSniffer, CompilerGenerated]
public partial class SysClientListOutput
{
    /// <summary>
    /// id
    /// </summary>
    public long? id { get; set; }

    /// <summary>
    /// 客户端key
    /// </summary>
    [ExportProperty("客户端key")]
    public string? clientKey { get; set; }

    /// <summary>
    /// 删除标志（0代表存在 2代表删除）
    /// </summary>
    [ExportProperty("删除标志")]
    public string? delFlag { get; set; }
}
```

#### QueryInput类 (查询输入对象)
```CSharp
// SysClientQueryInput.g.cs
/// <summary>
/// 客户端信息实体类
/// </summary>
[SuppressSniffer, CompilerGenerated]
public partial class SysClientQueryInput : DataQueryInput
{
    /// <summary>
    /// id
    /// </summary>
    public long? id { get; set; }
    /// <summary>
    /// 客户端key
    /// </summary>
    public string? clientKey { get; set; }
    /// <summary>
    /// 删除标志（0代表存在 2代表删除）
    /// </summary>
    public string? delFlag { get; set; }

    /// <summary>
    /// 构建通用的查询条件。
    /// </summary>
    public Expression<Func<SysClientEntity, bool>> BuildQueryWhere()
    {
        var where = LinqExtensions.True<SysClientEntity>();
        where = where.AndIF(this.id != null, x => x.Id == this.id);
        where = where.AndIF(!string.IsNullOrEmpty(this.clientKey), x => x.ClientKey == this.clientKey);
        where = where.AndIF(!string.IsNullOrEmpty(this.delFlag), x => x.DelFlag == this.delFlag);
        return where;
    }
}
```

#### CrInput类 (创建输入对象)
```CSharp
// SysClientCrInput.g.cs
/// <summary>
/// 客户端信息实体类
/// </summary>
[SuppressSniffer, CompilerGenerated]
public partial class SysClientCrInput
{
    /// <summary>
    /// 客户端key
    /// </summary>
    [Required(ErrorMessage = "客户端key不能为空")]
    public string? clientKey { get; set; }
    /// <summary>
    /// 删除标志（0代表存在 2代表删除）
    /// </summary>
    public string? delFlag { get; set; }

    /// <summary>
    /// 通用的BO对象映射至实体方法。
    /// </summary>
    public virtual SysClientEntity MapTo()
    {
        var entity = new SysClientEntity();
        entity.ClientKey = this.clientKey;
        entity.DelFlag = this.delFlag;
        return entity;
    }
}
```

#### UpInput类 (更新输入对象)
```CSharp
/// <summary>
/// 客户端信息实体类
/// </summary>
[SuppressSniffer, CompilerGenerated]
public partial class SysClientUpInput : SysClientCrInput
{
    /// <summary>
    /// id
    /// </summary>
    [Required(ErrorMessage = "id不能为空")]
    public long? id { get; set; }

    /// <summary>
    /// 通用的BO对象映射至实体方法。
    /// </summary>
    public override SysClientEntity MapTo()
    {
        var entity = base.MapTo();
        entity.Id = this.id;
        return entity;
    }
}
```

### 3. 特性控制参数

DtoGenerator特性支持以下参数控制代码生成行为：

| 参数名 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| GenMapMethod | bool | true | 是否生成实体映射方法 |
| GenVoClass | bool | true | 是否生成VO类 |
| GenQueryInputClass | bool | true | 是否生成查询输入类 |
| GenBoClass | bool | true | 是否生成BO类 |
| DtoNamespace | string | "Dto" | DTO类命名空间 |

使用示例：

```CSharp
[DtoGenerator(
    GenMapMethod = true,
    GenVoClass = true,
    GenQueryInputClass = false,
    DtoNamespace = "ViewModels"
)]
public class SysClientEntity : BaseEntity
{
    // 属性定义
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