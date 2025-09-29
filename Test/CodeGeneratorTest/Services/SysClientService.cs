using FreeSql;
using Mapster;

namespace CodeGeneratorTest.Services;

/// <summary>
/// 服务端代码生成测试。
/// </summary>
[ConstructorInject, UserInject, CacheInject]
[CustomInject(VarType = nameof(IMenuRepository))]
[CustomInject(VarType = nameof(IRoleRepository))]
public partial class SysClientService
{
    private readonly IBaseRepository<SysClientEntity> _baseRepository;

    public async Task<List<SysClientListOutput>> GetList(SysClientQueryInput input)
    {
        var where = input.BuildQueryWhere();
        var query = _baseRepository.Select.Where(where);
        var list = await query.ToListAsync();

        return list.Adapt<List<SysClientListOutput>>();
    }
}
