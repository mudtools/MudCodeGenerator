using System.Collections.Immutable;

namespace Mud.ServiceCodeGenerator;

public abstract class WebApiSourceGenerator : TransitiveCodeGenerator
{
    private readonly string[] HttpClientApiAttributeName = ["HttpClientApiAttribute", "HttpClientApi"];
    public override void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // 使用自定义方法查找标记了[HttpClientApi]的接口
        var interfaceDeclarations = GetClassDeclarationProvider<InterfaceDeclarationSyntax>(context, HttpClientApiAttributeName);

        // 组合编译和接口声明
        var compilationAndInterfaces = context.CompilationProvider.Combine(interfaceDeclarations);

        // 注册源生成
        context.RegisterSourceOutput(compilationAndInterfaces,
             (spc, source) => Execute(source.Left, source.Right, spc));
    }

    protected abstract void Execute(Compilation compilation, ImmutableArray<InterfaceDeclarationSyntax> interfaces, SourceProductionContext context);

    protected string GetImplementationClassName(string interfaceName)
    {
        return interfaceName.StartsWith("I", StringComparison.Ordinal) && interfaceName.Length > 1 && char.IsUpper(interfaceName[1])
            ? interfaceName.Substring(1)
            : interfaceName + "Impl";
    }
}
