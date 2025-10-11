namespace Mud.CodeGenerator;

/// <summary>
/// 类层次结构信息
/// </summary>
public class ClassHierarchyInfo
{
    public string ClassName { get; set; }
    public string FullName { get; set; }
    public Accessibility Accessibility { get; set; }
    public bool IsAbstract { get; set; }
    public bool IsSealed { get; set; }
    public TypeKind Kind { get; set; }
    public Location Location { get; set; }
    public string BaseTypeName { get; set; }
    public List<string> Interfaces { get; set; } = new List<string>();
    public string AssemblyName { get; set; }
    public string Namespace { get; set; }

    public override string ToString()
    {
        return $"{Accessibility} {Kind} {FullName} : {BaseTypeName}";
    }
}
