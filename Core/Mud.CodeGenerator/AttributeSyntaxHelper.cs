using System.Globalization;

namespace Mud.CodeGenerator;

/// <summary>
/// Attribute 参数提取选项
/// </summary>
internal class AttributeExtractionOptions
{
    /// <summary>
    /// 是否使用语义模型获取精确值（推荐为true）
    /// </summary>
    public bool UseSemanticModel { get; set; } = true;

    /// <summary>
    /// 当无法获取编译时常量值时是否回退到语法分析
    /// </summary>
    public bool FallbackToSyntax { get; set; } = true;

    /// <summary>
    /// 对于 nameof 表达式，是否返回完全限定名
    /// </summary>
    public bool UseFullNameForNameOf { get; set; }

    /// <summary>
    /// 默认值，当参数不存在时返回
    /// </summary>
    public object? DefaultValue { get; set; }
}

/// <summary>
/// 扩展性强的 Attribute 参数提取器
/// </summary>
internal static class AttributeSyntaxHelper
{
    /// <summary>
    /// 从 AttributeSyntax 中获取指定属性的值
    /// </summary>
    /// <param name="attributeSyntax">特性语法节点</param>
    /// <param name="semanticModel">语义模型</param>
    /// <param name="propertyName">属性名</param>
    /// <param name="options">提取选项</param>
    /// <returns>属性值，如果不存在则返回默认值</returns>
    public static object GetPropertyValue(
        this AttributeSyntax attributeSyntax,
        SemanticModel semanticModel,
        string propertyName,
        AttributeExtractionOptions options = null)
    {
        options ??= new AttributeExtractionOptions();

        if (attributeSyntax?.ArgumentList == null)
            return options.DefaultValue;

        // 查找指定属性名的参数
        var argument = FindArgumentByName(attributeSyntax, propertyName);
        if (argument == null)
            return options.DefaultValue;

        return ExtractArgumentValue(argument.Expression, semanticModel, options);
    }

    /// <summary>
    /// 从 AttributeSyntax 中获取指定属性的值
    /// </summary>
    /// <param name="attributeSyntax">特性语法节点</param>
    /// <param name="propertyName">属性名</param>
    /// <param name="options">提取选项</param>
    /// <returns>属性值，如果不存在则返回默认值</returns>
    public static object GetPropertyValue(
        this AttributeSyntax attributeSyntax,
        string propertyName,
        AttributeExtractionOptions options = null)
    {
        options ??= new AttributeExtractionOptions();
        options.UseSemanticModel = false;

        return GetPropertyValue(attributeSyntax, null, propertyName, options);
    }

    /// <summary>
    /// 从 AttributeSyntax 中获取所有命名属性的值
    /// </summary>
    /// <param name="attributeSyntax">特性语法节点</param>
    /// <param name="semanticModel">语义模型</param>
    /// <param name="options">提取选项</param>
    /// <returns>属性名到值的字典</returns>
    public static Dictionary<string, object> GetAllPropertyValues(
        this AttributeSyntax attributeSyntax,
        SemanticModel semanticModel,
        AttributeExtractionOptions options = null)
    {
        options ??= new AttributeExtractionOptions();

        var result = new Dictionary<string, object>();

        if (attributeSyntax?.ArgumentList == null)
            return result;

        foreach (var argument in attributeSyntax.ArgumentList.Arguments)
        {
            if (argument.NameEquals != null)
            {
                var propertyName = argument.NameEquals.Name.Identifier.ValueText;
                object value = ExtractArgumentValue(argument.Expression, semanticModel, options);
                result[propertyName] = value;
            }
        }

        return result;
    }

    /// <summary>
    /// 从 AttributeSyntax 中获取构造函数参数的值
    /// </summary>
    /// <param name="attributeSyntax">特性语法节点</param>
    /// <param name="semanticModel">语义模型</param>
    /// <param name="parameterIndex">参数索引</param>
    /// <param name="options">提取选项</param>
    /// <returns>参数值，如果不存在则返回默认值</returns>
    public static object GetConstructorArgument(
        this AttributeSyntax attributeSyntax,
        SemanticModel semanticModel,
        int parameterIndex,
        AttributeExtractionOptions options = null)
    {
        options ??= new AttributeExtractionOptions();

        if (attributeSyntax?.ArgumentList == null)
            return options.DefaultValue;

        // 获取位置参数（非命名参数）
        var positionalArguments = attributeSyntax.ArgumentList.Arguments
            .Where(arg => arg.NameEquals == null)
            .ToList();

        if (parameterIndex < 0 || parameterIndex >= positionalArguments.Count)
            return options.DefaultValue;

        return ExtractArgumentValue(positionalArguments[parameterIndex].Expression, semanticModel, options);
    }

    /// <summary>
    /// 从 AttributeSyntax 中获取所有构造函数参数的值
    /// </summary>
    /// <param name="attributeSyntax">特性语法节点</param>
    /// <param name="semanticModel">语义模型</param>
    /// <param name="options">提取选项</param>
    /// <returns>参数值列表</returns>
    public static List<object> GetAllConstructorArguments(
        this AttributeSyntax attributeSyntax,
        SemanticModel semanticModel,
        AttributeExtractionOptions options = null)
    {
        options ??= new AttributeExtractionOptions();

        var result = new List<object>();

        if (attributeSyntax?.ArgumentList == null)
            return result;

        // 获取所有位置参数
        var positionalArguments = attributeSyntax.ArgumentList.Arguments
            .Where(arg => arg.NameEquals == null);

        foreach (var argument in positionalArguments)
        {
            object value = ExtractArgumentValue(argument.Expression, semanticModel, options);
            result.Add(value);
        }

        return result;
    }

    /// <summary>
    /// 检查 Attribute 是否包含指定属性
    /// </summary>
    public static bool HasProperty(
        this AttributeSyntax attributeSyntax,
        string propertyName)
    {
        if (attributeSyntax?.ArgumentList == null)
            return false;

        return FindArgumentByName(attributeSyntax, propertyName) != null;
    }

    #region 私有辅助方法

    private static AttributeArgumentSyntax FindArgumentByName(AttributeSyntax attribute, string propertyName)
    {
        return attribute.ArgumentList.Arguments
            .FirstOrDefault(arg =>
                arg.NameEquals != null &&
                arg.NameEquals.Name.Identifier.ValueText == propertyName);
    }

    private static object ExtractArgumentValue(
        ExpressionSyntax expression,
        SemanticModel semanticModel,
        AttributeExtractionOptions options)
    {
        if (expression == null)
            return options.DefaultValue;

        // 优先使用语义模型获取精确值
        if (options.UseSemanticModel)
        {
            var value = ExtractValueWithSemanticModel(expression, semanticModel, options);
            if (value != null || !options.FallbackToSyntax)
                return value ?? options.DefaultValue;
        }

        // 回退到语法分析
        return ExtractValueFromSyntax(expression);
    }

    private static object ExtractValueWithSemanticModel(
        ExpressionSyntax expression,
        SemanticModel semanticModel,
        AttributeExtractionOptions options)
    {
        try
        {
            // 处理 nameof 表达式
            if (expression is InvocationExpressionSyntax invocation &&
                invocation.Expression is IdentifierNameSyntax identifier &&
                identifier.Identifier.ValueText == "nameof")
            {
                return ExtractNameOfValue(invocation, semanticModel, options);
            }

            // 处理 typeof 表达式
            if (expression is TypeOfExpressionSyntax typeOfExpression)
            {
                return ExtractTypeOfValue(typeOfExpression, semanticModel, options);
            }

            // 尝试获取编译时常量值
            Optional<object> constantValue = semanticModel.GetConstantValue(expression);
            if (constantValue.HasValue)
            {
                return constantValue.Value;
            }

            // 尝试获取符号信息
            SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(expression);
            if (symbolInfo.Symbol != null)
            {
                return options.UseFullNameForNameOf ?
                    symbolInfo.Symbol.ToDisplayString() :
                    symbolInfo.Symbol.Name;
            }

            // 尝试获取类型信息
            TypeInfo typeInfo = semanticModel.GetTypeInfo(expression);
            if (typeInfo.Type != null && typeInfo.Type.SpecialType != SpecialType.None)
            {
                return typeInfo.Type.Name;
            }

            return null;
        }
        catch (Exception)
        {
            // 如果语义分析失败，返回 null 让调用方决定是否回退
            return null;
        }
    }

    private static object ExtractNameOfValue(
        InvocationExpressionSyntax nameofExpression,
        SemanticModel semanticModel,
        AttributeExtractionOptions options)
    {
        if (nameofExpression.ArgumentList.Arguments.Count == 0)
            return null;

        var argumentExpression = nameofExpression.ArgumentList.Arguments[0].Expression;

        // 获取符号信息
        SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(argumentExpression);
        if (symbolInfo.Symbol != null)
        {
            return options.UseFullNameForNameOf ?
                symbolInfo.Symbol.ToDisplayString() :
                symbolInfo.Symbol.Name;
        }

        // 获取类型信息
        TypeInfo typeInfo = semanticModel.GetTypeInfo(argumentExpression);
        if (typeInfo.Type != null)
        {
            return options.UseFullNameForNameOf ?
                typeInfo.Type.ToDisplayString() :
                typeInfo.Type.Name;
        }

        // 回退到语法分析
        return argumentExpression.ToString();
    }

    private static object ExtractTypeOfValue(
        TypeOfExpressionSyntax typeOfExpression,
        SemanticModel semanticModel,
        AttributeExtractionOptions options)
    {
        TypeInfo typeInfo = semanticModel.GetTypeInfo(typeOfExpression.Type);
        if (typeInfo.Type != null)
        {
            return options.UseFullNameForNameOf ?
                typeInfo.Type.ToDisplayString() :
                typeInfo.Type.Name;
        }

        return typeOfExpression.Type.ToString();
    }
    /// <summary>
    /// 从表达式语法中提取值。
    /// </summary>
    /// <param name="expression">表达式语法。</param>
    /// <returns>表达式的值。</returns>
    public static object ExtractValueFromSyntax(ExpressionSyntax expression)
    {
        if (expression == null)
            return null;

        // 处理字面量表达式
        if (expression is LiteralExpressionSyntax literal)
        {
            var kind = literal.Kind();
            switch (kind)
            {
                case SyntaxKind.StringLiteralExpression:
                    return literal.Token.ValueText;
                case SyntaxKind.NumericLiteralExpression:
                    // 更安全的数值类型处理
                    return HandleNumericLiteral(literal.Token);
                case SyntaxKind.FalseLiteralExpression:
                    return false;
                case SyntaxKind.TrueLiteralExpression:
                    return true;
                case SyntaxKind.NullLiteralExpression:
                    return null;
                case SyntaxKind.CharacterLiteralExpression:
                    return literal.Token.ValueText;
                default:
                    // 对于其他未处理的字面量类型，返回原始值
                    return literal.Token.Value ?? literal.ToString();
            }
        }

        // 处理 nameof 表达式
        if (expression is InvocationExpressionSyntax invocation &&
            invocation.Expression is IdentifierNameSyntax identifier &&
            identifier.Identifier.ValueText == "nameof" &&
            invocation.ArgumentList.Arguments.Count > 0)
        {
            return invocation.ArgumentList.Arguments[0].Expression.ToString();
        }

        // 处理 typeof 表达式
        if (expression is TypeOfExpressionSyntax typeOfExpression)
        {
            return typeOfExpression.Type.ToString();
        }

        // 处理其他调用表达式（非nameof）
        if (expression is InvocationExpressionSyntax otherInvocation)
        {
            var arguments = otherInvocation.ArgumentList.Arguments
                .Select(arg => ExtractValueFromSyntax(arg.Expression))
                .Where(arg => arg != null)
                .ToList();

            return arguments.Any() ? string.Join(",", arguments) : string.Empty;
        }

        // 默认返回表达式的字符串表示
        return expression.ToString();
    }

    /// <summary>
    /// 处理数值字面量，支持多种数值类型
    /// </summary>
    private static object HandleNumericLiteral(SyntaxToken token)
    {
        try
        {
            var valueText = token.ValueText;
            var value = token.Value;

            if (value == null)
                return 0;

            // 根据后缀判断类型
            if (valueText.EndsWith("f", StringComparison.OrdinalIgnoreCase) ||
                valueText.EndsWith("F", StringComparison.OrdinalIgnoreCase))
            {
                return Convert.ToSingle(value, CultureInfo.InvariantCulture);
            }
            else if (valueText.EndsWith("d", StringComparison.OrdinalIgnoreCase) ||
                     valueText.EndsWith("D", StringComparison.OrdinalIgnoreCase))
            {
                return Convert.ToDouble(value, CultureInfo.InvariantCulture);
            }
            else if (valueText.EndsWith("m", StringComparison.OrdinalIgnoreCase) ||
                     valueText.EndsWith("M", StringComparison.OrdinalIgnoreCase))
            {
                return Convert.ToDecimal(value, CultureInfo.InvariantCulture);
            }
            else if (valueText.EndsWith("l", StringComparison.OrdinalIgnoreCase) ||
                     valueText.EndsWith("L", StringComparison.OrdinalIgnoreCase))
            {
                return Convert.ToInt64(value, CultureInfo.InvariantCulture);
            }
            else if (valueText.EndsWith("u", StringComparison.OrdinalIgnoreCase) ||
                     valueText.EndsWith("U", StringComparison.OrdinalIgnoreCase))
            {
                if (valueText.EndsWith("ul", StringComparison.OrdinalIgnoreCase) ||
                    valueText.EndsWith("UL", StringComparison.OrdinalIgnoreCase))
                {
                    return Convert.ToUInt64(value, CultureInfo.InvariantCulture);
                }
                return Convert.ToUInt32(value, CultureInfo.InvariantCulture);
            }
            else
            {
                // 默认返回int，但如果值超出int范围则返回long
                var numericValue = Convert.ToInt64(value, CultureInfo.InvariantCulture);
                return numericValue <= int.MaxValue && numericValue >= int.MinValue
                    ? (int)numericValue
                    : numericValue;
            }
        }
        catch
        {
            // 转换失败时返回0
            return 0;
        }
    }

    #endregion
}
