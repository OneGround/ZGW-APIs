using Microsoft.Extensions.DependencyInjection;

namespace OneGround.ZGW.Notificaties.Messaging.Extensions;

public static class NotificatieJobExtensions
{
    public static void AddHangfireNotificatieReQueuer(this IServiceCollection services)
    {
        services.AddScoped<INotificatieJob, NotificatieJob>();
    }
}
