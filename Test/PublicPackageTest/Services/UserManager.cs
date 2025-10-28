namespace CodeGeneratorTest.Services;

[ConstructorInject, CacheInject]
[OptionsInject(OptionType = nameof(CacheOptions))]
[CustomInject<IMenuRepository>]
[AutoRegister(typeof(IUserManager), ServiceLifetime = ServiceLifetime.Transient)]
[AutoRegister(typeof(IUserService), ServiceLifetime = ServiceLifetime.Singleton)]
public partial class UserManager : IUserManager, IUserService
{

}
