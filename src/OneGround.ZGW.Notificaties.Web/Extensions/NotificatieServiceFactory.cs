using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Notificaties.Web.Services;

namespace OneGround.ZGW.Notificaties.Web.Extensions;

public static class NotificatieServiceFactory
{
    public static IServiceCollection AddNotificatieService(this IServiceCollection services, IConfiguration configuration)
    {
        var notificatieServiceOptions = configuration.GetSection("NotificatieServiceOptions").Get<NotificatieServiceOptions>();

        if (notificatieServiceOptions?.Type == NotificatieServiceType.Http)
        {
            services.AddScoped<INotificatieService, NotificatieHttpService>();
        }
        else
        {
            services.AddScoped<INotificatieService, NotificatieService>();
        }

        return services;
    }
}
