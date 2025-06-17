using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Common.Caching;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.ServiceAgent.Extensions;

namespace OneGround.ZGW.Catalogi.ServiceAgent.v1._3.Extensions;

public static class CatalogiServiceAgentV13Extensions
{
    public static void AddCatalogiServiceAgent_v1_3(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddServiceAgent<ICatalogiServiceAgent, CatalogiServiceAgent>(
            ServiceRoleName.ZTC,
            configuration,
            caching =>
            {
                caching.CacheEntities.Add(CacheEntity.ZaakType);
                caching.CacheEntities.Add(CacheEntity.InformatieObjectType);
                caching.CacheEntities.Add(CacheEntity.BesluitType);
                caching.CacheEntities.Add(CacheEntity.StatusType);
                caching.CacheEntities.Add(CacheEntity.ResultaatType);
                caching.CacheEntities.Add(CacheEntity.RolType);
                caching.CacheEntities.Add(CacheEntity.Eigenschap);
            }
        );
    }
}
