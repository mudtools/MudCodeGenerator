// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.CodeGenerator 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text;

namespace Mud.ServiceCodeGenerator.HttpInvoke.Generators;

/// <summary>
/// 构造函数生成器，负责生成类的字段和构造函数
/// </summary>
internal class ConstructorGenerator
{
    private readonly StringBuilder _codeBuilder;
    private readonly InterfaceImpCodeGenerator.GenerationContext _context;

    public ConstructorGenerator(StringBuilder codeBuilder, InterfaceImpCodeGenerator.GenerationContext context)
    {
        _codeBuilder = codeBuilder;
        _context = context;
    }

    /// <summary>
    /// 生成类字段和构造函数
    /// </summary>
    public void Generate(string className)
    {
        GenerateClassFields();
        GenerateConstructorDocumentation(className);
        GenerateConstructorSignature(className);
        GenerateConstructorBody();
        GenerateHelperMethods();
    }

    /// <summary>
    /// 生成类字段
    /// </summary>
    private void GenerateClassFields()
    {
        if (_context.HasInheritedFrom) return;

        _codeBuilder.AppendLine("        /// <summary>");
        _codeBuilder.AppendLine("        /// 用于HttpClient客户端操作的<see cref = \"IEnhancedHttpClient\"/> 实例。");
        _codeBuilder.AppendLine("        /// </summary>");
        _codeBuilder.AppendLine($"        {_context.FieldAccessibility}readonly IEnhancedHttpClient _httpClient;");
        _codeBuilder.AppendLine("        /// <summary>");
        _codeBuilder.AppendLine("        /// 用于JSON内容序列化与反序列化操作的<see cref = \"JsonSerializerOptions\"/> 参数实例。");
        _codeBuilder.AppendLine("        /// </summary>");
        _codeBuilder.AppendLine($"        {_context.FieldAccessibility}readonly JsonSerializerOptions _jsonSerializerOptions;");

        if (_context.HasTokenManager)
        {
            _codeBuilder.AppendLine("        /// <summary>");
            _codeBuilder.AppendLine($"        /// 用于HttpClient客户端操作操作使用的的<see cref = \"{_context.Config.TokenManagerType}\"/> 令牌管理实例。");
            _codeBuilder.AppendLine("        /// </summary>");
            _codeBuilder.AppendLine($"        {_context.FieldAccessibility}readonly {_context.Config.TokenManagerType} _tokenManager;");
        }
        _codeBuilder.AppendLine($"        {_context.FieldAccessibility}readonly string _defaultContentType = \"{_context.Config.DefaultContentType}\";");
        _codeBuilder.AppendLine();
    }

    /// <summary>
    /// 生成构造函数文档注释
    /// </summary>
    private void GenerateConstructorDocumentation(string className)
    {
        _codeBuilder.AppendLine("        /// <summary>");
        _codeBuilder.AppendLine($"        /// 构建 <see cref = \"{className}\"/> 类的实例。");
        _codeBuilder.AppendLine("        /// </summary>");
        _codeBuilder.AppendLine("        /// <param name=\"httpClient\">FeishuHttpClient实例</param>");
        _codeBuilder.AppendLine("        /// <param name=\"option\">Json序列化参数</param>");

        if (_context.HasTokenManager)
        {
            _codeBuilder.AppendLine("        /// <param name=\"tokenManager\">Token管理器</param>");
        }
    }

    /// <summary>
    /// 生成构造函数签名
    /// </summary>
    private void GenerateConstructorSignature(string className)
    {
        var parameters = new List<string>
        {
            "IEnhancedHttpClient httpClient",
            "IOptions<JsonSerializerOptions> option"
        };

        if (_context.HasTokenManager)
        {
            parameters.Add($"{_context.Config.TokenManagerType} tokenManager");
        }

        var signature = $"        public {className}({string.Join(", ", parameters)})";
        _codeBuilder.Append(signature);

        if (_context.HasInheritedFrom)
        {
            var baseParameters = new List<string>
            {
                "httpClient",
                "option",
            };
            if (_context.HasTokenManager)
            {
                baseParameters.Add("tokenManager");
            }
            _codeBuilder.AppendLine($" : base({string.Join(", ", baseParameters)})");
        }
        else
        {
            _codeBuilder.AppendLine();
        }
    }

    /// <summary>
    /// 生成构造函数体
    /// </summary>
    private void GenerateConstructorBody()
    {
        _codeBuilder.AppendLine("        {");

        if (!_context.HasInheritedFrom)
        {
            _codeBuilder.AppendLine("            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));");
            _codeBuilder.AppendLine("            _jsonSerializerOptions = option.Value;");

            if (_context.HasTokenManager)
            {
                _codeBuilder.AppendLine("            _tokenManager = tokenManager ?? throw new ArgumentNullException(nameof(tokenManager));");
            }
        }

        _codeBuilder.AppendLine("        }");
        _codeBuilder.AppendLine();
    }

    /// <summary>
    /// 生成辅助方法
    /// </summary>
    private void GenerateHelperMethods()
    {
        if (_context.HasInheritedFrom) return;

        GenerateGetMediaTypeMethod();
    }

    /// <summary>
    /// 生成 GetMediaType 方法
    /// </summary>
    private void GenerateGetMediaTypeMethod()
    {
        _codeBuilder.AppendLine("        /// <summary>");
        _codeBuilder.AppendLine("        /// 从Content-Type字符串中提取媒体类型部分，去除字符集信息。");
        _codeBuilder.AppendLine("        /// </summary>");
        _codeBuilder.AppendLine("        /// <param name=\"contentType\">完整的Content-Type字符串</param>");
        _codeBuilder.AppendLine("        /// <returns>媒体类型部分</returns>");
        _codeBuilder.AppendLine($"        {_context.FieldAccessibility}string GetMediaType(string contentType)");
        _codeBuilder.AppendLine("        {");
        _codeBuilder.AppendLine("            if (string.IsNullOrEmpty(contentType))");
        _codeBuilder.AppendLine("                return \"application/json\";");
        _codeBuilder.AppendLine();
        _codeBuilder.AppendLine("            // Content-Type可能包含字符集信息，如 \"application/json; charset=utf-8\"");
        _codeBuilder.AppendLine("            // 需要分号前的媒体类型部分");
        _codeBuilder.AppendLine("            var semicolonIndex = contentType.IndexOf(';');");
        _codeBuilder.AppendLine("            if (semicolonIndex >= 0)");
        _codeBuilder.AppendLine("            {");
        _codeBuilder.AppendLine("                return contentType.Substring(0, semicolonIndex).Trim();");
        _codeBuilder.AppendLine("            }");
        _codeBuilder.AppendLine();
        _codeBuilder.AppendLine("            return contentType.Trim();");
        _codeBuilder.AppendLine("        }");
        _codeBuilder.AppendLine();
    }
}
