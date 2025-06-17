using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.ServiceAgent.Extensions;

namespace OneGround.ZGW.Besluiten.ServiceAgent.v1.Extensions;

public static class BesluitenServiceAgentExtensions
{
    public static void AddBesluitenServiceAgent(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddServiceAgent<IBesluitenServiceAgent, BesluitenServiceAgent>(ServiceRoleName.BRC, configuration);
    }
}
