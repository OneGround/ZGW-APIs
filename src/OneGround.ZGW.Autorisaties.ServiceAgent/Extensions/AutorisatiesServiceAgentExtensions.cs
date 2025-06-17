using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Common.Caching;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.ServiceAgent.Extensions;

namespace OneGround.ZGW.Autorisaties.ServiceAgent.Extensions;

public static class AutorisatiesServiceAgentExtensions
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
