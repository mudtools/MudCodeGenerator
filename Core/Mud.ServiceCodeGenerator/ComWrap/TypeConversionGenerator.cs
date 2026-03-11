using Microsoft.CodeAnalysis;

namespace Mud.ServiceCodeGenerator.ComWrap
{
    /// <summary>
    /// 类型转换代码生成器
    /// </summary>
    public static class TypeConversionGenerator
    {
        /// <summary>
        /// 生成类型转换代码
        /// </summary>
        /// <param name="targetType">目标类型</param>
        /// <param name="sourceExpression">源表达式</param>
        /// <param name="needConvert">是否需要转换</param>
        /// <param name="comNamespace">COM命名空间</param>
        /// <returns>转换代码</returns>
        public static string GenerateConversionCode(
            ITypeSymbol targetType,
            string sourceExpression,
            bool needConvert = false,
            string? comNamespace = null)
        {
            if (!needConvert)
            {
                return sourceExpression;
            }

            // 处理可空类型
            if (targetType is INamedTypeSymbol namedType &&
                namedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
            {
                var underlyingType = namedType.TypeArguments[0];
                return GenerateNullableConversionCode(underlyingType, sourceExpression);
            }

            // 处理普通类型
            return TypeSymbolHelper.GetBasicTypeConvertCode(targetType, sourceExpression);
        }

        /// <summary>
        /// 生成可空类型转换代码
        /// </summary>
        /// <param name="actualType">实际类型</param>
        /// <param name="fieldName">字段名</param>
        /// <returns>转换代码</returns>
        public static string GenerateNullableConversionCode(
            ITypeSymbol actualType,
            string fieldName)
        {
            var conversionCode = TypeSymbolHelper.GetBasicTypeConvertCode(actualType, $"{fieldName}!");
            return $"{fieldName} != null ? {conversionCode} : null";
        }

        /// <summary>
        /// 生成枚举转换代码
        /// </summary>
        /// <param name="enumType">枚举类型</param>
        /// <param name="sourceExpression">源表达式</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>转换代码</returns>
        public static string GenerateEnumConversionCode(
            ITypeSymbol enumType,
            string sourceExpression,
            string defaultValue)
        {
            return $"{sourceExpression}.EnumConvert({defaultValue})";
        }

        /// <summary>
        /// 生成 COM 对象转换代码
        /// </summary>
        /// <param name="sourceExpression">源表达式</param>
        /// <param name="comNamespace">COM命名空间</param>
        /// <param name="comType">COM类型</param>
        /// <param name="constructType">构造类型</param>
        /// <param name="needConvert">是否需要转换</param>
        /// <returns>转换代码</returns>
        public static string GenerateComObjectConversionCode(
            string sourceExpression,
            string comNamespace,
            string comType,
            string constructType,
            bool needConvert)
        {
            if (needConvert)
            {
                return $"if({sourceExpression} is {comNamespace}.{comType} rComObj)\n" +
                       $"    new {constructType}(rComObj)\n" +
                       $"else\n" +
                       $"    null";
            }
            else
            {
                return $"new {constructType}({sourceExpression})";
            }
        }
    }
}