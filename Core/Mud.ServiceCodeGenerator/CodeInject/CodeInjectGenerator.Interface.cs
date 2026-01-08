// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.CodeGenerator 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.ServiceCodeGenerator.CodeInject;


public partial class CodeInjectGenerator
{
    #region 注入器接口和基类
    private interface IInjector
    {
        void Inject(InjectionContext context, InjectionRequirements requirements);
    }

    #region 基类注入器
    private abstract class BaseInjector : IInjector
    {
        public abstract void Inject(InjectionContext context, InjectionRequirements requirements);

        protected void AddParameter(InjectionContext context, string type, string name)
        {
            if (context.HasParameter(name) || string.IsNullOrEmpty(type) || string.IsNullOrEmpty(name))
                return;

            var parameter = SyntaxFactory.Parameter(
                SyntaxFactory.List<AttributeListSyntax>(),
                SyntaxFactory.TokenList(),
                SyntaxFactory.ParseTypeName(type),
                SyntaxFactory.Identifier(name),
                null);

            context.AddParameter(parameter);
        }

        protected void AddField(InjectionContext context, string type, string name)
        {
            if (string.IsNullOrEmpty(type) || string.IsNullOrEmpty(name))
                return;

            var fieldCode = $"private readonly {type} {name};";
            var fieldDeclaration = ParseFieldDeclaration(fieldCode);
            if (fieldDeclaration != null)
            {
                context.AddField(fieldDeclaration);
            }
        }

        protected void AddStatement(InjectionContext context, string statement)
        {
            if (string.IsNullOrEmpty(statement))
                return;

            try
            {
                var parsedStatement = SyntaxFactory.ParseStatement(statement);
                if (parsedStatement is ExpressionStatementSyntax expressionStatement)
                {
                    context.AddStatement(expressionStatement);
                }
            }
            catch
            {
                // 忽略语法解析错误
            }
        }

        protected string GetSafePropertyValue(AttributeSyntax attribute, string propertyName, string defaultValue = "")
        {
            try
            {
                return attribute.GetPropertyValue(propertyName)?.ToString() ?? defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// 检查字段是否应该被添加
        /// </summary>
        protected bool ShouldAddField(InjectionContext context, string fieldName)
        {
            return !string.IsNullOrEmpty(fieldName);
        }

        protected string GetGenericTypeName(AttributeSyntax attribute)
        {
            try
            {
                // 检查是否是泛型属性，例如 [CustomInject<IMenuRepository>]
                if (attribute.Name is GenericNameSyntax genericName)
                {
                    // 获取泛型参数
                    var typeArguments = genericName.TypeArgumentList?.Arguments;
                    if (typeArguments.HasValue && typeArguments.Value.Any())
                    {
                        // 返回第一个泛型参数的名称
                        var typeName = typeArguments.Value.First().ToString();

                        // 确保返回的是类型名称，而不是字符串字面量
                        // 如果类型名称包含引号，说明是字符串字面量，需要去除引号
                        if (typeName.StartsWith("\"", StringComparison.OrdinalIgnoreCase) && typeName.EndsWith("\"", StringComparison.OrdinalIgnoreCase))
                        {
                            typeName = typeName.Substring(1, typeName.Length - 2);
                        }

                        return typeName;
                    }
                }
            }
            catch
            {
                // 忽略解析错误
            }

            return string.Empty;
        }
    }
    #endregion
    #endregion

    #region 构造函数注入器
    private class ConstructorInjector : BaseInjector
    {
        public override void Inject(InjectionContext context, InjectionRequirements requirements)
        {
            if (!requirements.ConstructorInject.Any())
                return;

            var fields = SyntaxHelper.GetClassMemberField(context.ClassDeclaration);
            foreach (var field in fields)
            {
                if (ShouldIgnoreField(field))
                    continue;

                ProcessFieldInjection(context, field);
            }
        }

        private bool ShouldIgnoreField(FieldDeclarationSyntax field)
        {
            var ignoreAttributes = AttributeSyntaxHelper.GetAttributeSyntaxes(field, IgnoreGeneratorAttribute);
            return ignoreAttributes?.Any() == true;
        }

        private void ProcessFieldInjection(InjectionContext context, FieldDeclarationSyntax field)
        {
            // 处理字段声明中的每个变量（支持多个变量声明）
            foreach (var variable in field.Declaration.Variables)
            {
                var fieldName = variable.Identifier.ValueText;
                var fieldTypeSyntax = field.Declaration.Type;
                var fieldTypeName = SyntaxHelper.GetTypeSyntaxName(fieldTypeSyntax);
                var parameterName = GenerateParameterName(fieldName);

                if (IsLoggerType(fieldTypeName))
                {
                    HandleLoggerFieldInjection(context, fieldName);
                }
                else
                {
                    HandleStandardFieldInjection(context, fieldTypeSyntax, fieldName, parameterName);
                }
            }
        }

        private void HandleLoggerFieldInjection(InjectionContext context, string fieldName)
        {
            // 确保loggerFactory参数只添加一次
            if (!context.HasParameter("loggerFactory"))
            {
                AddParameter(context, "ILoggerFactory", "loggerFactory");
            }
            AddStatement(context, $"{fieldName} = loggerFactory.CreateLogger<{context.ClassName}>();");
        }

        private void HandleStandardFieldInjection(InjectionContext context, TypeSyntax fieldType, string fieldName, string parameterName)
        {
            AddParameterWithTypeSyntax(context, fieldType, parameterName);
            AddStatement(context, $"{fieldName} = {parameterName};");
        }

        /// <summary>
        /// 使用TypeSyntax直接添加参数，避免泛型类型信息丢失
        /// </summary>
        private void AddParameterWithTypeSyntax(InjectionContext context, TypeSyntax typeSyntax, string name)
        {
            if (context.HasParameter(name) || string.IsNullOrEmpty(name))
                return;

            var parameter = SyntaxFactory.Parameter(
                SyntaxFactory.List<AttributeListSyntax>(),
                SyntaxFactory.TokenList(),
                typeSyntax, // 直接使用TypeSyntax，保留泛型信息
                SyntaxFactory.Identifier(name),
                null);

            context.AddParameter(parameter);
        }

        private bool IsLoggerType(string typeName)
        {
            return typeName?.StartsWith("ILogger<", StringComparison.OrdinalIgnoreCase) == true;
        }

        private string GenerateParameterName(string fieldName)
        {
            if (string.IsNullOrEmpty(fieldName))
                return "parameter";

            // 处理下划线前缀的字段名（如 "_baseRepository" -> "baseRepository"）
            if (fieldName.StartsWith("_", StringComparison.CurrentCulture))
            {
                var nameWithoutUnderscore = fieldName.Substring(1);
                if (nameWithoutUnderscore.Length > 0)
                {
                    // 将首字母小写
                    return char.ToLowerInvariant(nameWithoutUnderscore[0]) +
                           (nameWithoutUnderscore.Length > 1 ? nameWithoutUnderscore.Substring(1) : "");
                }
            }

            // 如果没有下划线，直接使用字段名（首字母小写）
            return char.ToLowerInvariant(fieldName[0]) +
                   (fieldName.Length > 1 ? fieldName.Substring(1) : "");
        }
    }
    #endregion

    #region 具体注入器实现 - 日志注入器
    private class LoggerInjector : BaseInjector
    {
        private readonly string _loggerVariable;

        public LoggerInjector(string loggerVariable)
        {
            _loggerVariable = string.IsNullOrEmpty(loggerVariable) ? CodeInjectGeneratorConstants.DefaultLoggerVariable : loggerVariable;
        }

        public override void Inject(InjectionContext context, InjectionRequirements requirements)
        {
            if (!requirements.LoggerInject.Any())
                return;

            AddParameter(context, "ILoggerFactory", CodeInjectGeneratorConstants.LoggerFactoryParameter);
            AddField(context, $"ILogger<{context.ClassName}>", _loggerVariable);
            AddStatement(context, $"{_loggerVariable} = {CodeInjectGeneratorConstants.LoggerFactoryParameter}.CreateLogger<{context.ClassName}>();");
        }
    }
    #endregion

    #region 具体注入器实现 - 缓存管理器注入器
    private class CacheManagerInjector : BaseInjector
    {
        private readonly string _cacheManagerType;
        private readonly string _cacheManagerVariable;

        public CacheManagerInjector(string cacheManagerType, string cacheManagerVariable)
        {
            _cacheManagerType = string.IsNullOrEmpty(cacheManagerType) ? CodeInjectGeneratorConstants.DefaultCacheManagerType : cacheManagerType;
            _cacheManagerVariable = string.IsNullOrEmpty(cacheManagerVariable) ? CodeInjectGeneratorConstants.DefaultCacheManagerVariable : cacheManagerVariable;
        }

        public override void Inject(InjectionContext context, InjectionRequirements requirements)
        {
            if (!requirements.CacheManagerInject.Any())
                return;

            var parameterName = GenerateParameterName(_cacheManagerVariable);
            AddParameter(context, _cacheManagerType, parameterName);
            AddField(context, _cacheManagerType, _cacheManagerVariable);
            AddStatement(context, $"{_cacheManagerVariable} = {parameterName};");
        }

        private string GenerateParameterName(string fieldName)
        {
            if (string.IsNullOrEmpty(fieldName))
                return "cacheManager";

            if (fieldName.StartsWith("_", StringComparison.CurrentCulture) && fieldName.Length > 1)
                return fieldName.Substring(1);

            return fieldName.StartsWith("_", StringComparison.CurrentCulture) ? fieldName.Substring(1) : fieldName;
        }
    }
    #endregion

    #region 具体注入器实现 - 用户管理器注入器
    private class UserManagerInjector : BaseInjector
    {
        private readonly string _userManagerType;
        private readonly string _userManagerVariable;

        public UserManagerInjector(string userManagerType, string userManagerVariable)
        {
            _userManagerType = string.IsNullOrEmpty(userManagerType) ? CodeInjectGeneratorConstants.DefaultUserManagerType : userManagerType;
            _userManagerVariable = string.IsNullOrEmpty(userManagerVariable) ? CodeInjectGeneratorConstants.DefaultUserManagerVariable : userManagerVariable;
        }

        public override void Inject(InjectionContext context, InjectionRequirements requirements)
        {
            if (!requirements.UserManagerInject.Any())
                return;

            var parameterName = GenerateParameterName(_userManagerVariable);
            AddParameter(context, _userManagerType, parameterName);
            AddField(context, _userManagerType, _userManagerVariable);
            AddStatement(context, $"{_userManagerVariable} = {parameterName};");
        }

        private string GenerateParameterName(string fieldName)
        {
            if (string.IsNullOrEmpty(fieldName))
                return "userManager";

            if (fieldName.StartsWith("_", StringComparison.CurrentCulture) && fieldName.Length > 1)
                return fieldName.Substring(1);

            return fieldName.StartsWith("_", StringComparison.CurrentCulture) ? fieldName.Substring(1) : fieldName;
        }
    }
    #endregion

    #region 具体注入器实现 - 配置项注入器
    private class OptionsInjector : BaseInjector
    {
        public override void Inject(InjectionContext context, InjectionRequirements requirements)
        {
            if (!requirements.OptionsInject.Any())
                return;

            foreach (var option in requirements.OptionsInject)
            {
                ProcessOptionInjection(context, option);
            }
        }

        private void ProcessOptionInjection(InjectionContext context, AttributeSyntax option)
        {
            // 首先尝试获取泛型类型参数
            var genericType = GetGenericTypeName(option);

            // 如果没有泛型类型参数，则尝试获取OptionType属性
            var variableType = !string.IsNullOrWhiteSpace(genericType) ? genericType : GetSafePropertyValue(option, CodeInjectGeneratorConstants.OptionTypeProperty);
            if (string.IsNullOrWhiteSpace(variableType))
                return;

            var varName = GetSafePropertyValue(option, CodeInjectGeneratorConstants.VarNameProperty);
            if (string.IsNullOrWhiteSpace(varName))
                varName = PrivateFieldNamingHelper.GeneratePrivateFieldName(variableType, FieldNamingStyle.UnderscoreCamel);

            var optionParameterName = GenerateOptionParameterName(variableType);

            AddParameter(context, $"IOptions<{variableType}>", optionParameterName);
            AddField(context, variableType, varName);
            AddStatement(context, $"{varName} = {optionParameterName}.Value;");
        }

        private string GenerateOptionParameterName(string variableType)
        {
            var baseName = PrivateFieldNamingHelper.GeneratePrivateFieldName(variableType, FieldNamingStyle.PureCamel);
            return string.IsNullOrEmpty(baseName) ? CodeInjectGeneratorConstants.OptionsParameter : baseName;
        }
    }
    #endregion

    #region 具体注入器实现 - 自定义注入器
    private class CustomInjector : BaseInjector
    {
        public override void Inject(InjectionContext context, InjectionRequirements requirements)
        {
            if (!requirements.CustomInject.Any())
                return;

            foreach (var customType in requirements.CustomInject)
            {
                //System.Diagnostics.Debugger.Launch();
                ProcessCustomInjection(context, customType);
            }
        }

        private void ProcessCustomInjection(InjectionContext context, AttributeSyntax customType)
        {
            // 首先尝试获取泛型类型参数
            var genericType = GetGenericTypeName(customType);

            // 如果没有泛型类型参数，则尝试获取VarType属性
            var variableType = !string.IsNullOrWhiteSpace(genericType) ? genericType : GetSafePropertyValue(customType, CodeInjectGeneratorConstants.VarTypeProperty);

            if (string.IsNullOrWhiteSpace(variableType))
                return;

            var varName = GetSafePropertyValue(customType, CodeInjectGeneratorConstants.VarNameProperty);
            if (string.IsNullOrWhiteSpace(varName))
                varName = PrivateFieldNamingHelper.GeneratePrivateFieldName(variableType, FieldNamingStyle.UnderscoreCamel);

            var parameterName = GenerateParameterName(varName);

            AddParameter(context, variableType, parameterName);
            AddField(context, variableType, varName);
            AddStatement(context, $"{varName} = {parameterName};");
        }

        private string GenerateParameterName(string fieldName)
        {
            if (string.IsNullOrEmpty(fieldName))
                return CodeInjectGeneratorConstants.CustomParameter;

            if (fieldName.StartsWith("_", StringComparison.CurrentCulture) && fieldName.Length > 1)
                return fieldName.Substring(1);

            return fieldName.StartsWith("_", StringComparison.CurrentCulture) ? fieldName.Substring(1) : fieldName;
        }
    }
    #endregion
}
