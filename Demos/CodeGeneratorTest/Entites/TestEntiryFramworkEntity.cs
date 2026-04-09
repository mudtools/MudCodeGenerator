namespace CodeGeneratorTest.Entites;

[DtoGenerator]
public partial class TestEntiryFramworkEntity
{
    [property: Key]
    private int _id;

    private string _name;
}
