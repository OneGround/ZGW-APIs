using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Roxit.ZGW.Common.Constants;
using Roxit.ZGW.Common.ServiceAgent.Extensions;

namespace Roxit.ZGW.Referentielijsten.ServiceAgent.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddReferentielijstenServiceAgent(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddServiceAgent<IReferentielijstenServiceAgent, ReferentielijstenServiceAgent>(
            ServiceRoleName.RL,
            configuration,
            authorizationType: AuthorizationType.AnonymousClient
        );
    }
}
