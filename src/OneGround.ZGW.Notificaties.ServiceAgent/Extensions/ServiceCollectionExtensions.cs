using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.ServiceAgent.Extensions;

namespace OneGround.ZGW.Notificaties.ServiceAgent.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddNotificatiesServiceAgent(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddServiceAgent<INotificatiesServiceAgent, NotificatiesServiceAgent>(ServiceRoleName.NRC, configuration);
    }
}
