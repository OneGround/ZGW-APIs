using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Roxit.ZGW.Common.Caching;
using Roxit.ZGW.Common.Constants;
using Roxit.ZGW.Common.ServiceAgent.Extensions;

namespace Roxit.ZGW.Autorisaties.ServiceAgent.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddAutorisatiesServiceAgent(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddServiceAgent<IAutorisatiesServiceAgent, AutorisatiesServiceAgent>(
            ServiceRoleName.AC,
            configuration,
            caching =>
            {
                caching.CacheEntities.Add(CacheEntity.Applicatie);
            }
        );
    }
}
