// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.CodeGenerator 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.ServiceCodeGenerator.CodeInject;

/// <summary>
/// 字段注入、日志注入、缓存注入、用户注入代码生成器。
/// </summary>
[Generator(LanguageNames.CSharp)]
public partial class CodeInjectGenerator : TransitiveCodeGenerator
{
    #region 配置和上下文类
    #region 配置
    private record struct ProjectConfiguration(
        string DefaultCacheManagerType,
        string DefaultUserManagerType,
        string DefaultLoggerVariable,
        string DefaultCacheManagerVariable,
        string DefaultUserManagerVariable
    );
    #endregion

    #region 上下文类
    private class InjectionContext
    {
        public string ClassName { get; }
        public ClassDeclarationSyntax ClassDeclaration { get; }

        private readonly List<ParameterSyntax> _parameters = [];
        private readonly List<StatementSyntax> _statements = [];
        private readonly List<FieldDeclarationSyntax> _fields = [];
        private readonly HashSet<string> _parameterNames = [];
        private readonly HashSet<string> _fieldNames = []; // 添加字段名去重

        public InjectionContext(string className, ClassDeclarationSyntax classDeclaration)
        {
            ClassName = className ?? throw new ArgumentNullException(nameof(className));
            ClassDeclaration = classDeclaration ?? throw new ArgumentNullException(nameof(classDeclaration));

            // 预先收集原始类中已有的字段名
            var existingFields = SyntaxHelper.GetClassMemberField(classDeclaration);
            foreach (var field in existingFields)
            {
                foreach (var variable in field.Declaration.Variables)
                {
                    _fieldNames.Add(variable.Identifier.ValueText);
                }
            }
        }

        public void AddParameter(ParameterSyntax parameter)
        {
            if (parameter == null) return;

            var parameterName = parameter.Identifier.ValueText;
            if (_parameterNames.Add(parameterName))
            {
                _parameters.Add(parameter);
            }
        }

        public void AddStatement(StatementSyntax statement)
        {
            if (statement != null)
            {
                _statements.Add(statement);
            }
        }

        public void AddField(FieldDeclarationSyntax field)
        {
            if (field == null) return;

            // 检查字段是否已存在（避免重复添加）
            foreach (var variable in field.Declaration.Variables)
            {
                var fieldName = variable.Identifier.ValueText;
                if (_fieldNames.Add(fieldName)) // 只有不存在的字段才添加
                {
                    _fields.Add(field);
                }
            }
        }

        public bool HasParameter(string name) => _parameterNames.Contains(name);

        public ParameterListSyntax BuildParameterList()
        {
            return _parameters.Count > 0
                ? SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(_parameters))
                : SyntaxFactory.ParameterList();
        }

        public BlockSyntax BuildConstructorBody()
        {
            return _statements.Count > 0
                ? SyntaxFactory.Block(_statements)
                : SyntaxFactory.Block();
        }

        public IReadOnlyList<FieldDeclarationSyntax> Fields => _fields;
    }
    #endregion

    private class InjectionRequirements
    {
        public IEnumerable<AttributeSyntax> ConstructorInject { get; set; } = [];
        public IEnumerable<AttributeSyntax> LoggerInject { get; set; } = [];
        public IEnumerable<AttributeSyntax> OptionsInject { get; set; } = [];
        public IEnumerable<AttributeSyntax> CacheManagerInject { get; set; } = [];
        public IEnumerable<AttributeSyntax> UserManagerInject { get; set; } = [];
        public IEnumerable<AttributeSyntax> CustomInject { get; set; } = [];

        public bool HasAnyInjection =>
            ConstructorInject.Any() || LoggerInject.Any() || OptionsInject.Any() ||
            CacheManagerInject.Any() || UserManagerInject.Any() || CustomInject.Any();
    }
    #endregion

    #region 编译单元创建
    private CompilationUnitSyntax CreateCompilationUnit(InjectionContext context)
    {
        var constructor = CreateConstructor(context);
        var classDeclaration = CreateClassDeclaration(context, constructor);
        var namespaceDeclaration = CreateNamespaceDeclaration(context, classDeclaration);

        return CreateCompilationUnitWithUsings(context, namespaceDeclaration);
    }

    private ConstructorDeclarationSyntax CreateConstructor(InjectionContext context)
    {
        var constructor = SyntaxFactory.ConstructorDeclaration(context.ClassName)
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
            .WithParameterList(context.BuildParameterList())
            .WithBody(context.BuildConstructorBody());

        // 保留原有注释
        var leadingTrivia = context.ClassDeclaration.GetLeadingTrivia();
        if (leadingTrivia != null && leadingTrivia.Count > 0)
        {
            constructor = constructor.WithLeadingTrivia(leadingTrivia);
        }

        return constructor;
    }

    private ClassDeclarationSyntax CreateClassDeclaration(InjectionContext context, ConstructorDeclarationSyntax constructor)
    {
        var classDeclaration = SyntaxFactory.ClassDeclaration(context.ClassName)
            .WithModifiers(SyntaxFactory.TokenList(
                SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                SyntaxFactory.Token(SyntaxKind.PartialKeyword)));

        // 保留原有注释
        var leadingTrivia = context.ClassDeclaration.GetLeadingTrivia();
        if (leadingTrivia != null && leadingTrivia.Count > 0)
        {
            classDeclaration = classDeclaration.WithLeadingTrivia(leadingTrivia);
        }

        // 添加字段和构造函数
        if (context.Fields.Any())
        {
            classDeclaration = classDeclaration.AddMembers(context.Fields.ToArray());
        }

        return classDeclaration.AddMembers(constructor);
    }

    private NamespaceDeclarationSyntax CreateNamespaceDeclaration(InjectionContext context, ClassDeclarationSyntax classDeclaration)
    {
        var namespaceName = SyntaxHelper.GetNamespaceName(context.ClassDeclaration);
        return SyntaxFactory.NamespaceDeclaration(SyntaxFactory.IdentifierName(namespaceName))
            .AddMembers(classDeclaration);
    }

    private CompilationUnitSyntax CreateCompilationUnitWithUsings(InjectionContext context, NamespaceDeclarationSyntax namespaceDeclaration)
    {
        var root = context.ClassDeclaration.SyntaxTree.GetCompilationUnitRoot();
        var compilationUnit = SyntaxFactory.CompilationUnit();

        // 添加原有的using指令
        foreach (var usingDirective in root.Usings)
        {
            compilationUnit = compilationUnit.AddUsings(usingDirective);
        }

        // 添加必需的using指令
        compilationUnit = compilationUnit
            .AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.IdentifierName("System")))
            .AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.IdentifierName("Microsoft.Extensions.Logging")))
            .AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.IdentifierName("Microsoft.Extensions.Options")));

        // 添加命名空间和自动生成头
        compilationUnit = compilationUnit
            .AddMembers(namespaceDeclaration)
            .NormalizeWhitespace()
            .WithLeadingTrivia(CreateAutoGeneratedHeader());

        return compilationUnit;
    }

    private SyntaxTriviaList CreateAutoGeneratedHeader()
    {
        return SyntaxFactory.TriviaList([
            SyntaxFactory.Comment("// <auto-generated/>\n"),
            SyntaxFactory.Comment("// 此代码由Mud代码生成器自动生成，请不要手动修改。\n"),
            SyntaxFactory.Comment("// 生成时间: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\n"),
            SyntaxFactory.Comment("")
        ]);
    }
    #endregion
}
