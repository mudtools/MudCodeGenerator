# Mud 代码生成器详解

## 功能介绍

Mud 代码生成器是一个基于Roslyn的源代码生成器，专为.NET开发者设计，用于根据实体类自动生成各种相关的代码，从而显著提升开发效率。它具有以下核心功能：

1. **DTO代码生成** - 根据实体类自动生成数据传输对象（DTO）
2. **VO代码生成** - 根据实体类自动生成视图对象（VO）
3. **查询输入类生成** - 根据实体类自动生成查询输入类（QueryInput）
4. **创建输入类生成** - 根据实体类自动生成创建输入类（CrInput）
5. **更新输入类生成** - 根据实体类自动生成更新输入类（UpInput）
6. **实体映射方法生成** - 自动生成实体与DTO之间的映射方法

通过这些功能，开发者可以专注于业务逻辑的实现，而无需花费大量时间在重复性的代码编写上.
## 代码生成项目参数配置

在使用Mud 代码生成器时，可以通过在项目文件中配置以下参数来自定义生成行为：

### 通用配置参数

```xml
<PropertyGroup>
  <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>  <!-- 在obj目录下保存生成的代码 -->
  <EntitySuffix>Entity</EntitySuffix>  <!-- 实体类后缀配置 -->
  <EntityAttachAttributes>SuppressSniffer</EntityAttachAttributes>  <!-- 实体类加上Attribute特性配置，多个特性时使用','分隔 -->
  <PropertyNameLowerCaseFirstLetter>true</PropertyNameLowerCaseFirstLetter>  <!-- 是否将生成的属性名首字母小写 -->
</PropertyGroup>

<ItemGroup>
  <CompilerVisibleProperty Include="EntitySuffix" />
  <CompilerVisibleProperty Include="EntityAttachAttributes" />
  <CompilerVisibleProperty Include="PropertyNameLowerCaseFirstLetter" />
</ItemGroup>
```

### 依赖项配置

```xml
<ItemGroup>
  <!-- 引入的代码生成器程序集，注意后面的参数 -->
  <PackageReference Include="Mud.EntityCodeGenerator" Version="1.0.5" PrivateAssets="all" OutputItemType="Analyzer" ReferenceOutputAssembly="false"/>
</ItemGroup>
```

### 配置参数说明

| 参数名 | 默认值 | 说明 |
|--------|--------|------|
| EmitCompilerGeneratedFiles | false | 是否在obj目录下保存生成的代码，设为true便于调试 |
| EntitySuffix | Entity | 实体类后缀，用于识别实体类 |
| EntityAttachAttributes | (空) | 实体类上需要附加的特性，多个特性用逗号分隔 |
| PropertyNameLowerCaseFirstLetter | true | 是否将生成的属性名首字母小写，设为false保持原有大小写 |

## 代码生成功能及样例

### DTO/VO/输入类代码生成

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

基于以上实体，将**自动生成**以下几类代码：

#### 实体类属性
```csharp
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
        return voObj;
    }
}
```

#### VO类 (视图对象)
```csharp
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
```csharp
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
```csharp
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
```csharp
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

### 特性控制参数

DtoGenerator特性支持以下参数控制代码生成行为：

| 参数名 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| GenMapMethod | bool | true | 是否生成实体映射方法 |
| GenVoClass | bool | true | 是否生成VO类 |
| GenQueryInputClass | bool | true | 是否生成查询输入类 |
| GenBoClass | bool | true | 是否生成BO类 |
| DtoNamespace | string | "Dto" | DTO类命名空间 |

使用示例：

```csharp
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

## 与传统代码生成器的比较

相较于传统的代码生成器（如CodeSmith）和低代码平台的代码生成器，Mud 代码生成器有着独特的优势：

### 零散添加字段不需要整体重新生成实体

> 传统的代码生成器通常需要在模型变更时重新生成整个文件，这可能导致已有的自定义代码丢失或者需要手动合并。而Mud代码生成器采用增量式生成方式，在添加新字段时只需重新编译项目即可自动更新相关代码，无需重新生成整个实体。

### 零散添加字段不需要手动添加至其它DTO

>当实体新增字段时，传统代码生成器往往需要手动将新字段添加到各个相关的DTO中，容易遗漏且繁琐。Mud 代码生成器会在编译时自动检测实体变化并同步更新所有相关的DTO、VO以及各种输入类，保证代码的一致性。

### 代码整洁，关注核心字段

>Mud 代码生成器遵循"关注点分离"原则，将生成的代码与手写的业务逻辑完全隔离。开发者只需要关注核心业务字段的定义，其他辅助代码会自动生成，使代码更加整洁易维护。

### 实时生成

>代码在编译时自动生成，无需额外的操作步骤。开发者只需关注业务逻辑代码的编写，当修改实体类并重新编译时，所有相关的DTO、VO和输入类都会自动更新，大大提升了开发效率。

### 强类型安全

>基于Roslyn编译器平台，提供强类型的代码生成和验证。生成的代码与项目中的其他代码一样，都经过编译器的严格检查，避免了运行时错误，提高了代码质量和可靠性.

### 高度可定制

>支持多种配置选项，可以根据项目需求灵活调整生成规则。开发者可以通过项目配置文件控制生成行为，如实体类后缀、需要附加的特性等，满足不同项目的个性化需求。

### 无缝集成

>作为.NET项目的一部分，与现有开发流程完美融合。无需额外的工具或复杂的配置，只需添加NuGet包引用并在项目中进行简单配置，即可享受代码自动生成带来的便利。

### 版本控制友好

>生成的代码不会污染版本历史，便于团队协作。由于代码是在编译时生成的，不会产生大量人工编写的重复代码，使得版本控制系统中的变更记录更加清晰，更容易进行代码审查和团队协作。

## 使用方法

1. 在您的项目中添加对 `Mud.EntityCodeGenerator` 包的引用
2. 根据需要配置项目参数，如实体后缀、特性等
3. 将实体类标记为 `partial` 并添加 `[DtoGenerator]` 特性
4. 定义实体字段，使用适当的特性进行标注
5. 编译项目，代码生成器将自动生成相关代码
6. 在业务代码中使用生成的DTO、VO等类

通过以上步骤，您可以轻松地使用Mud Entity代码生成器来提升开发效率，减少重复劳动，让团队更专注于业务逻辑的实现。