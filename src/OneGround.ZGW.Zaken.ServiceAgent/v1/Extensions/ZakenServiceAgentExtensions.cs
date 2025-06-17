using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.ServiceAgent.Extensions;

namespace OneGround.ZGW.Zaken.ServiceAgent.v1.Extensions;

public static class ZakenServiceAgentExtensions
{
    public static void AddZakenServiceAgent(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddServiceAgent<IZakenServiceAgent, ZakenServiceAgent>(ServiceRoleName.ZRC, configuration);
    }
}
