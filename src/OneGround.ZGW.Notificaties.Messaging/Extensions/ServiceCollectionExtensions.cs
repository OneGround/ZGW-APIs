using Microsoft.Extensions.DependencyInjection;

namespace OneGround.ZGW.Notificaties.Messaging.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddHangfireNotificatieReQueuer(this IServiceCollection services)
    {
        services.AddScoped<INotificatieJob, NotificatieJob>();
    }
}
