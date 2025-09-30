using Microsoft.CodeAnalysis;

namespace Mud.EntityCodeGenerator.Diagnostics
{
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
    }
}