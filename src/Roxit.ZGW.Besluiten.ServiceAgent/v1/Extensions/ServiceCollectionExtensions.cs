using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Roxit.ZGW.Common.Constants;
using Roxit.ZGW.Common.ServiceAgent.Extensions;

namespace Roxit.ZGW.Besluiten.ServiceAgent.v1.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddBesluitenServiceAgent(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddServiceAgent<IBesluitenServiceAgent, BesluitenServiceAgent>(ServiceRoleName.BRC, configuration);
    }
}
