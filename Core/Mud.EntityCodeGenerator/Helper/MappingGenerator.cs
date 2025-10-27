using System.Text;

namespace Mud.EntityCodeGenerator.Helper;

/// <summary>
/// 映射生成器，提供通用的映射方法生成功能
/// </summary>
public static class MappingGenerator
{
    /// <summary>
    /// 生成实体到DTO的映射方法
    /// </summary>
    /// <param name="entityClassName">实体类名</param>
    /// <param name="dtoClassName">DTO类名</param>
    /// <param name="mappingLines">映射行列表</param>
    /// <param name="methodName">方法名（可选）</param>
    /// <returns>映射方法代码</returns>
    public static string GenerateEntityToDtoMapping(
        string entityClassName,
        string dtoClassName,
        List<string> mappingLines,
        string methodName = null)
    {
        methodName ??= $"MapTo{dtoClassName}";

        var sb = new StringBuilder();

        sb.AppendLine($"/// <summary>");
        sb.AppendLine($"/// 将 <see cref=\"{entityClassName}\"/> 映射到 <see cref=\"{dtoClassName}\"/> 实例。");
        sb.AppendLine($"/// </summary>");
        sb.AppendLine($"/// <param name=\"entity\">输入的 <see cref=\"{entityClassName}\"/> 实例。</param>");
        sb.AppendLine($"/// <param name=\"action\">映射后对DTO执行的操作。</param>");
        sb.AppendLine($"/// <returns>映射后的 <see cref=\"{dtoClassName}\"/> 实例。</returns>");
        sb.AppendLine($"public static {dtoClassName} {methodName}(this {entityClassName} entity, Action<{dtoClassName}>? action = null)");
        sb.AppendLine("{");
        sb.AppendLine($"    if(entity == null) return null;");
        sb.AppendLine($"    var result = new {dtoClassName}();");
        sb.AppendLine();

        foreach (var mappingLine in mappingLines)
        {
            sb.AppendLine($"    {mappingLine}");
        }

        sb.AppendLine($"    if(action != null)");
        sb.AppendLine($"        action(result);");
        sb.AppendLine($"    return result;");
        sb.AppendLine("}");

        return sb.ToString();
    }

    /// <summary>
    /// 生成DTO到实体的映射方法
    /// </summary>
    /// <param name="dtoClassName">DTO类名</param>
    /// <param name="entityClassName">实体类名</param>
    /// <param name="mappingLines">映射行列表</param>
    /// <param name="methodName">方法名（可选）</param>
    /// <returns>映射方法代码</returns>
    public static string GenerateDtoToEntityMapping(
        string dtoClassName,
        string entityClassName,
        List<string> mappingLines,
        string methodName = null)
    {
        methodName ??= $"MapToEntity";

        var sb = new StringBuilder();

        sb.AppendLine($"/// <summary>");
        sb.AppendLine($"/// 将 <see cref=\"{dtoClassName}\"/> 映射到 <see cref=\"{entityClassName}\"/> 实例。");
        sb.AppendLine($"/// </summary>");
        sb.AppendLine($"/// <param name=\"dto\">输入的 <see cref=\"{dtoClassName}\"/> 实例。</param>");
        sb.AppendLine($"/// <param name=\"action\">映射后对实体执行的操作。</param>");
        sb.AppendLine($"/// <returns>映射后的 <see cref=\"{entityClassName}\"/> 实例。</returns>");
        sb.AppendLine($"public static {entityClassName} {methodName}(this {dtoClassName} dto, Action<{entityClassName}>? action = null)");
        sb.AppendLine("{");
        sb.AppendLine($"    if(dto == null) return null;");
        sb.AppendLine($"    var result = new {entityClassName}();");
        sb.AppendLine();

        foreach (var mappingLine in mappingLines)
        {
            sb.AppendLine($"    {mappingLine}");
        }

        sb.AppendLine($"    if(action != null)");
        sb.AppendLine($"        action(result);");
        sb.AppendLine($"    return result;");
        sb.AppendLine("}");

        return sb.ToString();
    }

    /// <summary>
    /// 生成集合映射方法
    /// </summary>
    /// <param name="sourceType">源类型</param>
    /// <param name="targetType">目标类型</param>
    /// <param name="mappingMethodName">单个映射方法名</param>
    /// <param name="methodName">集合方法名（可选）</param>
    /// <returns>集合映射方法代码</returns>
    public static string GenerateCollectionMapping(
        string sourceType,
        string targetType,
        string mappingMethodName,
        string methodName = null)
    {
        methodName ??= $"MapToList";

        var sb = new StringBuilder();

        sb.AppendLine($"/// <summary>");
        sb.AppendLine($"/// 将 <see cref=\"{sourceType}\"/> 集合映射到 <see cref=\"{targetType}\"/> 集合。");
        sb.AppendLine($"/// </summary>");
        sb.AppendLine($"/// <param name=\"sources\">输入的 <see cref=\"{sourceType}\"/> 集合。</param>");
        sb.AppendLine($"/// <param name=\"action\">映射后对每个目标实例执行的操作。</param>");
        sb.AppendLine($"/// <returns>映射后的 <see cref=\"{targetType}\"/> 集合。</returns>");
        sb.AppendLine($"public static List<{targetType}> {methodName}(this IEnumerable<{sourceType}> sources, Action<{targetType}>? action = null)");
        sb.AppendLine("{");
        sb.AppendLine($"    if (sources == null)");
        sb.AppendLine($"        return [];");
        sb.AppendLine();
        sb.AppendLine($"    var results = new List<{targetType}>();");
        sb.AppendLine($"    foreach (var source in sources)");
        sb.AppendLine($"    {{");
        sb.AppendLine($"        var result = source.{mappingMethodName}();");
        sb.AppendLine($"        if(action != null)");
        sb.AppendLine($"            action(result);");
        sb.AppendLine($"        results.Add(result);");
        sb.AppendLine($"    }}");
        sb.AppendLine($"    return results;");
        sb.AppendLine("}");

        return sb.ToString();
    }

    /// <summary>
    /// 生成简单的属性映射行
    /// </summary>
    /// <param name="sourceProperty">源属性名</param>
    /// <param name="targetProperty">目标属性名</param>
    /// <param name="sourceObject">源对象名</param>
    /// <param name="targetObject">目标对象名</param>
    /// <returns>映射行代码</returns>
    public static string GeneratePropertyMappingLine(
        string sourceProperty,
        string targetProperty,
        string sourceObject = "entity",
        string targetObject = "result")
    {
        return $"{targetObject}.{targetProperty} = {sourceObject}.{sourceProperty}; ";
    }

    /// <summary>
    /// 生成带转换的属性映射行
    /// </summary>
    /// <param name="sourceProperty">源属性名</param>
    /// <param name="targetProperty">目标属性名</param>
    /// <param name="conversion">转换表达式</param>
    /// <param name="sourceObject">源对象名</param>
    /// <param name="targetObject">目标对象名</param>
    /// <returns>映射行代码</returns>
    public static string GeneratePropertyMappingLineWithConversion(
        string sourceProperty,
        string targetProperty,
        string conversion,
        string sourceObject = "entity",
        string targetObject = "result")
    {
        return $"{targetObject}.{targetProperty} = {conversion.Replace("{{value}}", $"{sourceObject}.{sourceProperty}")}; ";
    }

    /// <summary>
    /// 生成空值检查的属性映射行
    /// </summary>
    /// <param name="sourceProperty">源属性名</param>
    /// <param name="targetProperty">目标属性名</param>
    /// <param name="nullCheck">空值检查条件</param>
    /// <param name="sourceObject">源对象名</param>
    /// <param name="targetObject">目标对象名</param>
    /// <returns>映射行代码</returns>
    public static string GeneratePropertyMappingLineWithNullCheck(
        string sourceProperty,
        string targetProperty,
        string nullCheck,
        string sourceObject = "entity",
        string targetObject = "result")
    {
        return $"if({nullCheck.Replace("{{value}}", $"{sourceObject}.{sourceProperty}")}) {targetObject}.{targetProperty} = {sourceObject}.{sourceProperty}; ";
    }
}