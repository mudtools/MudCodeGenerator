using FreeSql;

namespace CodeGeneratorTest.Services;

/// <summary>
/// 服务端代码生成测试。
/// </summary>
[ConstructorInject, UserInject, CacheInject]
[OptionsInject<TenantOptions>]
[CustomInject<IMenuRepository>]
[CustomInject(VarType = nameof(IRoleRepository))]
[AutoRegister<ISysClientService>(ServiceLifetime = ServiceLifetime.Transient)]
public partial class SysClientService : ISysClientService
{
    private readonly IBaseRepository<SysClientEntity> _baseRepository;

    public async Task<List<SysClientListOutput>> GetList(SysClientQueryInput input)
    {
        var where = input.BuildQueryWhere();
        var query = _baseRepository.Select.Where(where);
        var list = await query.ToListAsync();

        List<SysClientListOutput> listOutputs = list.MapToList(a => a.clientKey = "");
        return listOutputs;
    }

    public void TestBuilder(SysClientQueryInput input)
    {
        var properties = input.GetType().GetProperties();
        foreach (var prop in properties)
        {
            var value = prop.GetValue(input);

        }

        var pro = ProjectEntity.Builder()
                        .SetParentId("1")
                        .SetProNum("2")
                        .Build();
    }
}
