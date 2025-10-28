namespace CodeGeneratorTest.Services;

[AutoRegister(typeof(IUserManager), ServiceLifetime = ServiceLifetime.Transient)]
[AutoRegister(typeof(IUserService), ServiceLifetime = ServiceLifetime.Singleton)]
public class UserManager : IUserManager, IUserService
{
}
