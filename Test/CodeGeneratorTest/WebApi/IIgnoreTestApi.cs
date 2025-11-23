using Mud.Common.CodeGenerator;

namespace CodeGeneratorTest.WebApi;

[HttpClientApi("https://api.test.com")]
[HttpClientApiWrap(TokenManage = "ITokenManage")]
public interface IIgnoreTestApi
{
    // 这个方法应该被完全忽略 - 不生成实现和包装
    [Get("/test/ignore")]
    [IgnoreImplement]
    [IgnoreWrapInterface]
    Task<string> IgnoreBothMethod();

    // 这个方法只忽略实现，但生成包装接口
    [Get("/test/ignore-implement")]
    [IgnoreImplement]
    Task<string> IgnoreImplementMethod();

    // 这个方法只忽略包装，但生成实现
    [Get("/test/ignore-wrap")]
    [IgnoreWrapInterface]
    Task<string> IgnoreWrapMethod();

    // 这个方法正常生成所有代码
    [Get("/test/normal")]
    Task<string> NormalMethod();
}