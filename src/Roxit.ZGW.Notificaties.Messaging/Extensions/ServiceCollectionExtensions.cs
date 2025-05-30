using Microsoft.Extensions.DependencyInjection;

namespace Roxit.ZGW.Notificaties.Messaging.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddHangfireNotificatieReQueuer(this IServiceCollection services)
    {
        services.AddScoped<INotificatieJob, NotificatieJob>();
    }
}
