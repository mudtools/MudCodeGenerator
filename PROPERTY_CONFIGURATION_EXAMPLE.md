# 属性名首字母大小写配置示例

## 功能说明

Mud 代码生成器现在支持通过 `PropertyNameLowerCaseFirstLetter` 配置项控制生成的属性名是否首字母小写。

## 使用方法

在项目文件（`.csproj`）中添加以下配置：

```xml
<PropertyGroup>
  <!-- 设置为 true（默认值）时，生成的属性名首字母小写 -->
  <PropertyNameLowerCaseFirstLetter>true</PropertyNameLowerCaseFirstLetter>
  
  <!-- 设置为 false 时，生成的属性名保持原有大小写（通常为首字母大写） -->
  <!-- <PropertyNameLowerCaseFirstLetter>false</PropertyNameLowerCaseFirstLetter> -->
</PropertyGroup>

<ItemGroup>
  <CompilerVisibleProperty Include="PropertyNameLowerCaseFirstLetter" />
</ItemGroup>
```

## 效果示例

假设有一个实体类：

```csharp
[DtoGenerator]
public partial class ProductEntity
{
    public string ProductName { get; set; }
    
    public decimal Price { get; set; }
}
```

### 当 PropertyNameLowerCaseFirstLetter=true 时（默认）

生成的 DTO 属性名首字母小写：

```csharp
public partial class Product
{
    public string productName { get; set; }
    
    public decimal price { get; set; }
}
```

### 当 PropertyNameLowerCaseFirstLetter=false 时

生成的 DTO 属性名保持原有大小写：

```csharp
public partial class Product
{
    public string ProductName { get; set; }
    
    public decimal Price { get; set; }
}
```

## 注意事项

1. 此配置项默认值为 `true`，即默认将生成的属性名首字母小写
2. 配置项对所有自动生成的属性均生效，包括 DTO、VO、BO、QueryInput、CrInput、UpInput 等
3. 配置更改后需要重新编译项目才能生效