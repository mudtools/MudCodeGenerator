using FreeSql;
using System.Net;

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
        var queryParams = new List<string>();
        if (input != null)
        {
            var properties = input.GetType().GetProperties();
            foreach (var prop in properties)
            {
                var value = prop.GetValue(input);
                if (value != null)
                {
                    switch (value)
                    {
                        case string strValue when !string.IsNullOrEmpty(strValue):
                            strValue = WebUtility.UrlEncode(strValue);
                            queryParams.Add($"{prop.Name}={strValue}");
                            break;
                        case int or long or decimal or float or double or bool:
                            queryParams.Add($"{prop.Name}={value}");
                            break;
                        case Guid:
                            queryParams.Add($"{prop.Name}={value}");
                            break;
                        default:
                            queryParams.Add($"{prop.Name}={value}");
                            break;
                    }
                }
            }
        }

        var pro = ProjectEntity.Builder()
                        .SetParentId("1")
                        .SetProNum("2")
                        .Build();
    }
}
