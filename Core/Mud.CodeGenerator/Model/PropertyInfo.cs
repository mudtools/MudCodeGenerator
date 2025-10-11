namespace Mud.CodeGenerator;

/// <summary>
/// 属性信息
/// </summary>
public class PropertyInfo
{
    public string Name { get; set; }
    public string Type { get; set; }
    public string DeclaringType { get; set; }
    public string DeclaringTypeShortName { get; set; }
    public bool CanRead { get; set; }
    public bool CanWrite { get; set; }
    public bool IsAbstract { get; set; }
    public bool IsVirtual { get; set; }
    public bool IsOverride { get; set; }
    public Accessibility Accessibility { get; set; }
    public string OriginalType { get; set; }
    public string Documentation { get; set; }

    public override string ToString()
    {
        return $"{Type} {Name} {{ {(CanRead ? "get; " : "")}{(CanWrite ? "set; " : "")}}} // From {DeclaringTypeShortName}";
    }
}