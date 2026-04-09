namespace ComObjectWrapTest;

[ComObjectWrap(ComNamespace = "MsWord")]
public interface IWordApplication : IDisposable
{
}
