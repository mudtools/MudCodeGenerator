namespace Mud.Common.CodeGenerator;

/// <summary>
/// 服务生命周期
/// </summary>
public enum ServiceLifetime
{
    Singleton = 1,
    Transient = 2,
    Scoped = 4,
}

/// <summary>
/// AutoInject
/// </summary>
/// <param name="baseType">NULL表示服务自身</param>
/// <param name="serviceLifetime">服务生命周期</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public class AutoRegisterAttribute(Type baseType = null, ServiceLifetime serviceLifetime = ServiceLifetime.Scoped) : Attribute
{
    public ServiceLifetime ServiceLifetime { get; set; } = serviceLifetime;

    public Type BaseType { get; set; } = baseType;
}


/// <summary>
/// AutoRegisterKeyed
/// </summary>
/// <param name="key">服务Key，不能为空</param>
/// <param name="baseType">NULL表示服务自身</param>
/// <param name="serviceLifetime">服务生命周期</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public class AutoRegisterKeyedAttribute(string key, Type baseType = null, ServiceLifetime serviceLifetime = ServiceLifetime.Scoped) : Attribute
{
    public ServiceLifetime ServiceLifetime { get; set; } = serviceLifetime;

    public Type BaseType { get; set; } = baseType;

    public string Key { get; set; } = key ?? throw new ArgumentNullException(nameof(key));

}



#if NET7_0_OR_GREATER

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public class AutoRegisterAttribute<T>(ServiceLifetime serviceLifetime = ServiceLifetime.Scoped) : AutoRegisterAttribute(typeof(T), serviceLifetime)
{
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public class AutoRegisterKeyedAttribute<T>(string key, ServiceLifetime serviceLifetime = ServiceLifetime.Scoped) : AutoRegisterAttribute(typeof(T), serviceLifetime)
{
    public string Key { get; set; } = key ?? throw new ArgumentNullException(nameof(key));
}

#endif