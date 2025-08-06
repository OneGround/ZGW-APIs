using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.ServiceAgent.Extensions;
using OneGround.ZGW.Common.Web.Services;

namespace OneGround.ZGW.Notificaties.ServiceAgent.Extensions;

public static class NotificatiesServiceAgentExtensions
{
    public static void AddNotificatiesServiceAgent(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddServiceAgent<INotificatiesServiceAgent, NotificatiesServiceAgent>(ServiceRoleName.NRC, configuration);
    }

    public static IServiceCollection AddNotificatiesService(this IServiceCollection services, IConfiguration configuration)
    {
        AddNotificatiesServiceAgent(services, configuration);

        var notificatieServiceConfiguration = configuration.GetSection("NotificatieService").Get<NotificatieServiceConfiguration>();

        if (notificatieServiceConfiguration?.Type == NotificatieServiceType.Http)
        {
            services.AddScoped<INotificatieService, NotificatieHttpService>();
        }
        else
        {
            services.AddScoped<INotificatieService, NotificatieMessageQueueService>();
        }

        return services;
    }
}
