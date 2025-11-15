namespace Mud.Common.CodeGenerator;

[AttributeUsage(AttributeTargets.Interface, AllowMultiple = false)]
public class HttpClientApiWrapAttribute : Attribute
{
    public HttpClientApiWrapAttribute()
    {
    }

    public HttpClientApiWrapAttribute(string wrapInterface, string tokenManage)
    {
        WrapInterface = wrapInterface;
        TokenManage = tokenManage;
    }

#if NET8_0_OR_GREATER
    public required string WrapInterface { get; set; }

    public required string TokenManage { get; set; }
#else
    public string WrapInterface { get; set; }

    public string TokenManage { get; set; }
#endif
}
