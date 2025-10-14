using Mud.Common.CodeGenerator;

namespace CodeGeneratorTest.Entites;

[DtoGenerator]
[Builder]
public partial class TestEntity
{
    private string _name;

    private int _age;

    public string Name
    {
        get { return _name; }
        set { _name = value; }
    }

    public int Age
    {
        get { return _age; }
        set { _age = value; }
    }
}