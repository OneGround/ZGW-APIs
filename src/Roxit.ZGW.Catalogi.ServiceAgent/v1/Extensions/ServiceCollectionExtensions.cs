using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Roxit.ZGW.Common.Caching;
using Roxit.ZGW.Common.Constants;
using Roxit.ZGW.Common.ServiceAgent.Extensions;

namespace Roxit.ZGW.Catalogi.ServiceAgent.v1.Extensions;

public static class CatalogiServiceAgentServiceCollectionExtensions
{
    public static void AddCatalogiServiceAgent(this IServiceCollection services, IConfiguration configuration)
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
