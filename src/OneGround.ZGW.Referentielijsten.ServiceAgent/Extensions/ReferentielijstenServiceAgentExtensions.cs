using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.ServiceAgent.Extensions;

namespace OneGround.ZGW.Referentielijsten.ServiceAgent.Extensions;

public static class ReferentielijstenServiceAgentExtensions
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
