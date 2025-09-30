namespace CodeGeneratorTest;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class TestCustomAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public class CustomVo1Attribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public class CustomVo2Attribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public class CustomBo1Attribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public class CustomBo2Attribute : Attribute
{
}

