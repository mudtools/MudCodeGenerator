using FreeSql;

namespace CodeGeneratorTest.Services;

/// <summary>
/// 服务端代码生成测试。
/// </summary>
[ConstructorInject, UserInject, CacheInject]
[OptionsInject(nameof(TenantOptions))]
[CustomInject(nameof(IMenuRepository))]
[CustomInject(nameof(IRoleRepository))]
public partial class SysClientService
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
}
