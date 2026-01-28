// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.CodeGenerator 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.HttpUtils.Generators;

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
        // 如果接口上标注了 Token 特性，添加 _tokenType 字段
        if (!string.IsNullOrEmpty(_context.Config.TokenType) || _context.HasTokenManager)
        {
            var tokeType = string.IsNullOrEmpty(_context.Config.TokenType)
                ? TokenHelper.GetDefaultTokenType()
                : _context.Config.TokenType;
            _codeBuilder.AppendLine("        /// <summary>");
            _codeBuilder.AppendLine("        /// Token类型，用于标识使用的Token类型。");
            _codeBuilder.AppendLine("        /// </summary>");
            _codeBuilder.AppendLine($"        private readonly TokenType _tokenType = TokenType.{tokeType};");
        }

        if (_context.HasInheritedFrom) return;

        _codeBuilder.AppendLine("        /// <summary>");
        _codeBuilder.AppendLine("        /// 用于JSON内容序列化与反序列化操作的<see cref = \"JsonSerializerOptions\"/> 参数实例。");
        _codeBuilder.AppendLine("        /// </summary>");
        _codeBuilder.AppendLine($"        {_context.FieldAccessibility}IMudAppContext _appContext;");

        _codeBuilder.AppendLine("        /// <summary>");
        _codeBuilder.AppendLine("        /// 用于JSON内容序列化与反序列化操作的<see cref = \"JsonSerializerOptions\"/> 参数实例。");
        _codeBuilder.AppendLine("        /// </summary>");
        _codeBuilder.AppendLine($"        {_context.FieldAccessibility}readonly JsonSerializerOptions _jsonSerializerOptions;");

        if (_context.HasTokenManager)
        {
            _codeBuilder.AppendLine("        /// <summary>");
            _codeBuilder.AppendLine($"        /// 用于HttpClient客户端操作操作使用的的<see cref = \"{_context.Config.TokenManagerType}\"/> 令牌管理实例。");
            _codeBuilder.AppendLine("        /// </summary>");
            _codeBuilder.AppendLine($"        {_context.FieldAccessibility}readonly {_context.Config.TokenManagerType} _appManager;");
        }


        _codeBuilder.AppendLine("        /// <summary>");
        _codeBuilder.AppendLine("        /// 用于HttpClient客户端操作的内容类型。");
        _codeBuilder.AppendLine("        /// </summary>");
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
        _codeBuilder.AppendLine("        /// <param name=\"option\">Json序列化参数</param>");

        if (_context.HasTokenManager)
        {
            _codeBuilder.AppendLine("        /// <param name=\"appManager\">应用令牌管理器</param>");
        }
    }

    /// <summary>
    /// 生成构造函数签名
    /// </summary>
    private void GenerateConstructorSignature(string className)
    {
        var parameters = new List<string>
        {
            "IOptions<JsonSerializerOptions> option"
        };

        if (_context.HasTokenManager)
        {
            parameters.Add($"{_context.Config.TokenManagerType} appManager");
        }

        var signature = $"        public {className}({string.Join(", ", parameters)})";
        _codeBuilder.Append(signature);

        if (_context.HasInheritedFrom)
        {
            var baseParameters = new List<string>
            {
                "option",
            };
            if (_context.HasTokenManager)
            {
                baseParameters.Add("appManager");
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
            _codeBuilder.AppendLine("            _jsonSerializerOptions = option.Value;");

            if (_context.HasTokenManager)
            {
                _codeBuilder.AppendLine("            _appManager = appManager ?? throw new ArgumentNullException(nameof(appManager));");
                _codeBuilder.AppendLine("            _appContext = appManager.GetDefaultApp();");
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
        GenerateGetTokeTypeMethod();

        if (_context.HasInheritedFrom) return;

        GenerateGetMediaTypeMethod();
        GenerateUseAppMethod();
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
        _codeBuilder.AppendLine("                return _defaultContentType;");
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


    private void GenerateGetTokeTypeMethod()
    {
        if (string.IsNullOrEmpty(_context.Config.TokenType) && string.IsNullOrEmpty(_context.Config.TokenManager))
            return;

        string accessibility = _context.Config.IsAbstract ? "virtual" : "override";
        if (!_context.HasInheritedFrom && !_context.Config.IsAbstract)
            accessibility = "virtual";

        _codeBuilder.AppendLine("        /// <summary>");
        _codeBuilder.AppendLine("        /// 获取用于远程API访问的<see cref = \"TokenType\"/>令牌类型。");
        _codeBuilder.AppendLine("        /// </summary>");
        _codeBuilder.AppendLine("        /// <returns>返回<see cref = \"TokenType\"/>令牌类型。</returns>");
        _codeBuilder.AppendLine($"        protected {accessibility} TokenType GetTokeType() => _tokenType;");
        _codeBuilder.AppendLine();
    }

    private void GenerateUseAppMethod()
    {
        if (!_context.HasTokenManager)
            return;

        _codeBuilder.AppendLine("        /// <summary>");
        _codeBuilder.AppendLine("        /// 切换到指定的应用上下文。");
        _codeBuilder.AppendLine("        /// </summary>");
        _codeBuilder.AppendLine("        /// <returns>返回切换后的应用上下文。</returns>");
        _codeBuilder.AppendLine($"        public IMudAppContext UseApp(string appKey)");
        _codeBuilder.AppendLine("        {");
        _codeBuilder.AppendLine("            _appContext = _appManager.GetApp(appKey);");
        _codeBuilder.AppendLine("            if(_appContext == null)");
        _codeBuilder.AppendLine("                throw new InvalidOperationException($\"无法找到指定的应用上下文，AppKey: {appKey}\");");
        _codeBuilder.AppendLine("            return _appContext;");
        _codeBuilder.AppendLine("        }");
        _codeBuilder.AppendLine();

        _codeBuilder.AppendLine("        /// <summary>");
        _codeBuilder.AppendLine("        /// 切换到默认的应用上下文。");
        _codeBuilder.AppendLine("        /// </summary>");
        _codeBuilder.AppendLine("        /// <returns>返回默认的应用上下文。</returns>");
        _codeBuilder.AppendLine($"        public IMudAppContext UseDefaultApp()");
        _codeBuilder.AppendLine("        {");
        _codeBuilder.AppendLine("            _appContext = _appManager.GetDefaultApp();");
        _codeBuilder.AppendLine("            if(_appContext == null)");
        _codeBuilder.AppendLine("                throw new InvalidOperationException($\"无法找到默认的应用上下文。\");");
        _codeBuilder.AppendLine("            return _appContext;");
        _codeBuilder.AppendLine("        }");
        _codeBuilder.AppendLine();

        _codeBuilder.AppendLine("        /// <summary>");
        _codeBuilder.AppendLine("        /// 获取当前应用的访问令牌。");
        _codeBuilder.AppendLine("        /// </summary>");
        _codeBuilder.AppendLine("        /// <returns>返回当前应用的访问令牌。</returns>");
        _codeBuilder.AppendLine($"        protected virtual async Task<string> GetTokenAsync()");
        _codeBuilder.AppendLine("        {");
        _codeBuilder.AppendLine("            if(_appContext == null)");
        _codeBuilder.AppendLine("                throw new InvalidOperationException($\"无法找到当前服务的应用上下文。\");");
        _codeBuilder.AppendLine("            var tokenType = GetTokeType();");
        _codeBuilder.AppendLine("            var tokenManager = _appContext.GetTokenManager(tokenType);");
        _codeBuilder.AppendLine("            if(tokenManager == null)");
        _codeBuilder.AppendLine("                throw new InvalidOperationException($\"无法找到当前服务的令牌管理器，TokenType: {tokenType}\");");
        _codeBuilder.AppendLine("            var token = await tokenManager.GetTokenAsync();");
        _codeBuilder.AppendLine("            if(string.IsNullOrEmpty(token))");
        _codeBuilder.AppendLine("                throw new InvalidOperationException($\"无法获取到有效的访问令牌，TokenType: {tokenType}\");");
        _codeBuilder.AppendLine("            return token;");
        _codeBuilder.AppendLine("        }");
        _codeBuilder.AppendLine();
    }
}
