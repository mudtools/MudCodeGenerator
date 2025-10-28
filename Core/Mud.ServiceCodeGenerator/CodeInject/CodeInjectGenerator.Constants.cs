namespace Mud.ServiceCodeGenerator;

/// <summary>
/// 代码注入生成器常量定义
/// </summary>
internal static class CodeInjectGeneratorConstants
{
    #region 属性名称常量
    public const string ConstructorInjectAttribute = "ConstructorInjectAttribute";
    public const string LoggerInjectAttribute = "LoggerInjectAttribute";
    public const string OptionsInjectAttribute = "OptionsInjectAttribute";
    public const string CacheManagerInjectAttribute = "CacheInjectAttribute";
    public const string UserManagerInjectAttribute = "UserInjectAttribute";
    public const string CustomInjectAttribute = "CustomInjectAttribute";
    
    // 属性短名称（不带Attribute后缀）
    public const string ConstructorInject = "ConstructorInject";
    public const string LoggerInject = "LoggerInject";
    public const string OptionsInject = "OptionsInject";
    public const string CacheManagerInject = "CacheInject";
    public const string UserManagerInject = "UserInject";
    public const string CustomInject = "CustomInject";
    #endregion

    #region 配置键常量
    public const string DefaultCacheManagerTypeKey = "build_property.DefaultCacheManagerType";
    public const string DefaultUserManagerTypeKey = "build_property.DefaultUserManagerType";
    public const string DefaultLoggerVariableKey = "build_property.DefaultLoggerVariable";
    public const string DefaultCacheManagerVariableKey = "build_property.DefaultCacheManagerVariable";
    public const string DefaultUserManagerVariableKey = "build_property.DefaultUserManagerVariable";
    #endregion

    #region 默认值常量
    public const string DefaultCacheManagerType = "ICacheManager";
    public const string DefaultUserManagerType = "IUserManager";
    public const string DefaultLoggerVariable = "_logger";
    public const string DefaultCacheManagerVariable = "_cacheManager";
    public const string DefaultUserManagerVariable = "_userManager";
    #endregion

    #region 属性参数名称常量
    public const string OptionTypeProperty = "OptionType";
    public const string VarNameProperty = "VarName";
    public const string VarTypeProperty = "VarType";
    #endregion

    #region 字段命名常量
    public const string LoggerFactoryParameter = "loggerFactory";
    public const string CustomParameter = "customParameter";
    public const string OptionsParameter = "options";
    #endregion

    #region 泛型属性匹配模式
    public const string CustomInjectGenericPattern = "CustomInject<";
    public const string OptionsInjectGenericPattern = "OptionsInject<";
    public const string GenericSuffix = ">";
    #endregion
}