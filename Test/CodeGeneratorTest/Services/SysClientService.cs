using FreeSql;
using Mapster;
using System.Linq.Expressions;

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
        Expression<Func<SysClientEntity, bool>> where = x => true;
        where = where.And(x => x.Id == this.id);
        where = where.AndIF(!string.IsNullOrEmpty(this.clientKey), x => x.ClientKey == this.clientKey);
        where = where.AndIF(!string.IsNullOrEmpty(this.delFlag), x => x.DelFlag == this.delFlag);
        return where;

        var where = input.BuildQueryWhere();
        var query = _baseRepository.Select.Where(where);
        var list = await query.ToListAsync();

        return list.Adapt<List<SysClientListOutput>>();
    }
}
