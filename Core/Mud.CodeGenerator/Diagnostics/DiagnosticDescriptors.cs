namespace Mud.CodeGenerator;

/// <summary>
/// 诊断描述符集合，用于统一管理代码生成过程中的诊断信息
/// </summary>
internal static class DiagnosticDescriptors
{
    #region DTO生成器诊断信息
    public static readonly DiagnosticDescriptor DtoGenerationError = new(
        id: "DTO001",
        title: "DTO代码生成错误",
        messageFormat: "生成类 {0} 时发生错误: {1}",
        category: "代码生成",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DtoInitializationError = new(
        id: "DTO002",
        title: "DTO代码生成初始化错误",
        messageFormat: "初始化DTO代码生成器时发生错误: {0}",
        category: "代码生成",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DtoGenerationFailure = new(
        id: "DTO003",
        title: "DTO类生成失败",
        messageFormat: "无法为类 {0} 生成DTO类",
        category: "代码生成",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
    #endregion

    #region BO生成器诊断信息
    public static readonly DiagnosticDescriptor BoGenerationFailure = new(
        id: "BO001",
        title: "BO类生成失败",
        messageFormat: "无法为类 {0} 生成BO类",
        category: "代码生成",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor BoGenerationError = new(
        id: "BO002",
        title: "BO类生成错误",
        messageFormat: "生成类 {0} 的BO类时发生错误: {1}",
        category: "代码生成",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);
    #endregion

    #region VO生成器诊断信息
    public static readonly DiagnosticDescriptor VoGenerationFailure = new(
        id: "VO001",
        title: "VO类生成失败",
        messageFormat: "无法为类 {0} 生成VO类",
        category: "代码生成",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor VoGenerationError = new(
        id: "VO002",
        title: "VO类生成错误",
        messageFormat: "生成类 {0} 的VO类时发生错误: {1}",
        category: "代码生成",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);
    #endregion

    #region 实体映射方法生成器诊断信息
    public static readonly DiagnosticDescriptor EntityMethodGenerationFailure = new(
        id: "EM001",
        title: "实体映射方法生成失败",
        messageFormat: "无法为类 {0} 生成实体映射方法",
        category: "代码生成",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor EntityMethodGenerationError = new(
        id: "EM002",
        title: "实体映射方法生成错误",
        messageFormat: "生成类 {0} 的实体映射方法时发生错误: {1}",
        category: "代码生成",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);
    #endregion

    #region 实体建造者模式代码生成器诊断信息
    public static readonly DiagnosticDescriptor EntityBuilderGenerationError = new(
        id: "BE001",
        title: "实体建造者模式代码生成错误",
        messageFormat: "生成类 {0} 的建造者模式代码时发生错误: {1}",
        category: "代码生成",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);
    #endregion

    #region 查询输入类生成器诊断信息
    public static readonly DiagnosticDescriptor QueryInputGenerationFailure = new(
        id: "QI001",
        title: "查询输入类生成失败",
        messageFormat: "无法为类 {0} 生成查询输入类",
        category: "代码生成",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor QueryInputGenerationError = new(
        id: "QI002",
        title: "查询输入类生成错误",
        messageFormat: "生成类 {0} 的查询输入类时发生错误: {1}",
        category: "代码生成",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);
    #endregion

    #region AutoRegister代码生成器诊断信息
    public static readonly DiagnosticDescriptor AutoRegisterGenerationError = new(
        id: "AR001",
        title: "AutoRegister代码生成错误",
        messageFormat: "生成类 {0} 的AutoRegister代码时发生错误: {1}",
        category: "代码生成",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor AutoRegisterMetadataExtractionFailed = new(
        id: "AR002",
        title: "AutoRegister元数据提取失败",
        messageFormat: "无法为类 {0} 提取AutoRegister元数据。找到的特性: {1}。特性详情: {2}",
        category: "代码生成",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor AutoRegisterGenerationSkipped = new(
        id: "AR003",
        title: "AutoRegister代码生成跳过",
        messageFormat: "在程序集 {0} 中未找到AutoRegister服务。已处理的类: {1}。正在检查类是否有AutoRegister特性...",
        category: "代码生成",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor AutoRegisterMetadataExtracted = new(
        id: "AR004",
        title: "AutoRegister元数据已提取",
        messageFormat: "已提取 {0} 个AutoRegister服务: {1}",
        category: "代码生成",
        DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor AutoRegisterMetadataDetails = new(
        id: "AR005",
        title: "AutoRegister元数据详情",
        messageFormat: "为类 {0} 提取的元数据: ImplType={1}, BaseType={2}, LifeTime={3}",
        category: "代码生成",
        DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor AutoRegisterAttributesFound = new(
        id: "AR006",
        title: "AutoRegister特性已找到",
        messageFormat: "类 {0} 有AutoRegister特性: {1}。完整特性详情: {2}",
        category: "代码生成",
        DiagnosticSeverity.Info,
        isEnabledByDefault: true);
    #endregion
}