using System.Text;

namespace Mud.CodeGenerator;

/// <summary>
/// <see cref="SourceProductionContext"/>类扩展。
/// </summary>
internal static class SourceProductionContextExtensions
{
    /// <summary>
    /// 将<see cref="CompilationUnitSyntax"/>对象输出至当前项目的源代码中。
    /// </summary>
    /// <param name="context"><see cref="SourceProductionContext"/>对象。</param>
    /// <param name="name">代码文件名。</param>
    /// <param name="compilationUnit"><see cref="CompilationUnitSyntax"/>对象</param>
    public static void AddSource(this SourceProductionContext context, string name, CompilationUnitSyntax compilationUnit)
    {
        context.AddSource(name, compilationUnit.GetText(Encoding.UTF8));
    }
}
