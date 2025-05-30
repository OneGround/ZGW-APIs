using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Roxit.ZGW.Common.Constants;
using Roxit.ZGW.Common.ServiceAgent.Extensions;

namespace Roxit.ZGW.Zaken.ServiceAgent.v1.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddZakenServiceAgent(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddServiceAgent<IZakenServiceAgent, ZakenServiceAgent>(ServiceRoleName.ZRC, configuration);
    }
}
