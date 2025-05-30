using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Roxit.ZGW.Common.Constants;
using Roxit.ZGW.Common.ServiceAgent.Extensions;

namespace Roxit.ZGW.Notificaties.ServiceAgent.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddNotificatiesServiceAgent(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddServiceAgent<INotificatiesServiceAgent, NotificatiesServiceAgent>(ServiceRoleName.NRC, configuration);
    }
}
