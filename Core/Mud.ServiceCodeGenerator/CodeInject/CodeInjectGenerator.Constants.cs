// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.CodeGenerator 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.ServiceCodeGenerator.CodeInject;

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