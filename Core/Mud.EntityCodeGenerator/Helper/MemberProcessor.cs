using System.Text;

namespace Mud.EntityCodeGenerator.Helper;

/// <summary>
/// 通用成员处理器，提供统一的成员处理和属性映射功能
/// </summary>
public static class MemberProcessor
{
    /// <summary>
    /// 处理类成员并执行指定的操作
    /// </summary>
    /// <typeparam name="T">成员类型</typeparam>
    /// <param name="orgClassDeclaration">原始类的语法对象</param>
    /// <param name="compilation">编译上下文</param>
    /// <param name="memberProcessor">成员处理委托</param>
    /// <param name="primaryKeyOnly">是否只处理主键属性</param>
    /// <param name="isIgnoreGenerator">忽略生成器检查委托</param>
    /// <param name="isPrimary">主键检查委托</param>
    /// <param name="getPropertyNames">获取属性名委托</param>
    /// <param name="getPropertyType">获取属性类型委托</param>
    public static void ProcessMembers<T>(
        ClassDeclarationSyntax orgClassDeclaration,
        Compilation compilation,
        Action<T, string, string, string> memberProcessor,
        bool? primaryKeyOnly = null,
        Func<T, bool> isIgnoreGenerator = null,
        Func<T, bool> isPrimary = null,
        Func<T, (string orgPropertyName, string propertyName)> getPropertyNames = null,
        Func<T, string> getPropertyType = null) where T : MemberDeclarationSyntax
    {
        if (orgClassDeclaration == null || memberProcessor == null)
            return;

        var members = orgClassDeclaration.Members;

        // 只有在处理属性声明时才添加基类属性
        if (typeof(T) == typeof(PropertyDeclarationSyntax))
        {
            var baseProperty = ClassHierarchyAnalyzer.GetBaseClassPublicPropertyDeclarations(orgClassDeclaration, compilation);
            if (baseProperty.Count > 0)
                members = members.AddRange(baseProperty.Cast<MemberDeclarationSyntax>());
        }

        foreach (var member in members.OfType<T>())
        {
            try
            {
                if (member is FieldDeclarationSyntax fieldDeclaration && !SyntaxHelper.IsValidPrivateField(fieldDeclaration))
                    continue;

                if (isIgnoreGenerator?.Invoke(member) == true)
                    continue;

                var isPrimaryKey = isPrimary?.Invoke(member) ?? false;

                // 根据primaryKeyOnly参数决定是否处理该属性
                if (primaryKeyOnly.HasValue)
                {
                    if (primaryKeyOnly.Value && !isPrimaryKey)
                        continue;

                    if (!primaryKeyOnly.Value && isPrimaryKey)
                        continue;
                }

                var (orgPropertyName, propertyName) = getPropertyNames?.Invoke(member) ?? ("", "");
                var propertyType = getPropertyType?.Invoke(member) ?? "object";

                if (string.IsNullOrEmpty(orgPropertyName))
                    continue;

                // 确保属性名不为空
                if (string.IsNullOrEmpty(propertyName))
                    propertyName = orgPropertyName;

                memberProcessor(member, orgPropertyName, propertyName, propertyType);
            }
            catch (Exception ex)
            {
                // 即使单个属性处理失败也不影响其他属性
                Debug.WriteLine($"处理成员时发生错误: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 生成属性映射代码
    /// </summary>
    /// <typeparam name="T">成员类型</typeparam>
    /// <param name="orgClassDeclaration">原始类的语法对象</param>
    /// <param name="sb">字符串构建器</param>
    /// <param name="compilation">编译上下文</param>
    /// <param name="generateMappingLine">生成映射行委托</param>
    /// <param name="primaryKeyOnly">是否只处理主键属性</param>
    /// <param name="isIgnoreGenerator">忽略生成器检查委托</param>
    /// <param name="isPrimary">主键检查委托</param>
    /// <param name="getPropertyNames">获取属性名委托</param>
    /// <param name="getPropertyType">获取属性类型委托</param>
    public static void GeneratePropertyMappings<T>(
        ClassDeclarationSyntax orgClassDeclaration,
        StringBuilder sb,
        Compilation compilation,
        Func<string, string, string> generateMappingLine,
        bool? primaryKeyOnly = null,
        Func<T, bool> isIgnoreGenerator = null,
        Func<T, bool> isPrimary = null,
        Func<T, (string orgPropertyName, string propertyName)> getPropertyNames = null,
        Func<T, string> getPropertyType = null) where T : MemberDeclarationSyntax
    {
        ProcessMembers<T>(
            orgClassDeclaration,
            compilation,
            (member, orgPropertyName, propertyName, propertyType) =>
            {
                var mappingLine = generateMappingLine(orgPropertyName, propertyName);
                sb.AppendLine(mappingLine);
            },
            primaryKeyOnly,
            isIgnoreGenerator,
            isPrimary,
            getPropertyNames,
            getPropertyType);
    }

    /// <summary>
    /// 生成属性映射代码（包含属性类型）
    /// </summary>
    /// <typeparam name="T">成员类型</typeparam>
    /// <param name="orgClassDeclaration">原始类的语法对象</param>
    /// <param name="sb">字符串构建器</param>
    /// <param name="compilation">编译上下文</param>
    /// <param name="generateSetMethod">生成设置方法委托</param>
    /// <param name="isIgnoreGenerator">忽略生成器检查委托</param>
    /// <param name="isPrimary">主键检查委托</param>
    /// <param name="getPropertyNames">获取属性名委托</param>
    /// <param name="getPropertyType">获取属性类型委托</param>
    public static void GeneratePropertyMappings<T>(
        ClassDeclarationSyntax orgClassDeclaration,
        StringBuilder sb,
        Compilation compilation,
        Func<string, string, string, string> generateSetMethod,
        Func<T, bool> isIgnoreGenerator = null,
        Func<T, bool> isPrimary = null,
        Func<T, (string orgPropertyName, string propertyName)> getPropertyNames = null,
        Func<T, string> getPropertyType = null) where T : MemberDeclarationSyntax
    {
        ProcessMembers<T>(
            orgClassDeclaration,
            compilation,
            (member, orgPropertyName, propertyName, propertyType) =>
            {
                var mappingLine = generateSetMethod(orgPropertyName, propertyName, propertyType);
                sb.AppendLine(mappingLine);
            },
            null,
            isIgnoreGenerator,
            isPrimary,
            getPropertyNames,
            getPropertyType);
    }
}