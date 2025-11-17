namespace Mud.ServiceCodeGenerator;


/// <summary>
/// 生成器常量配置
/// </summary>
internal static class GeneratorConstants
{
    // 特性名称
    public static readonly string[] HttpClientApiWrapAttributeNames = ["HttpClientApiWrapAttribute", "HttpClientApiWrap"];
    public static readonly string[] TokenAttributeNames = ["TokenAttribute", "Token"];
    /// <summary>
    /// HttpClientApi特性名称数组
    /// </summary>
    public static string[] HttpClientApiAttributeNames = ["HttpClientApiAttribute", "HttpClientApi"];

    /// <summary>
    /// 支持的HTTP方法名称数组
    /// </summary>
    public static readonly string[] SupportedHttpMethods = ["Get", "GetAttribute", "Post", "PostAttribute", "Put", "PutAttribute", "Delete", "DeleteAttribute", "Patch", "PatchAttribute", "Head", "HeadAttribute", "Options", "OptionsAttribute"];

    // 默认值
    public const string DefaultTokenManageInterface = "ITokenManage";
    public const string DefaultWrapSuffix = "Wrap";

    // 诊断ID
    public const string GeneratorErrorDiagnosticId = "MCG001";
    public const string GeneratorWarningDiagnosticId = "MCG002";
}
