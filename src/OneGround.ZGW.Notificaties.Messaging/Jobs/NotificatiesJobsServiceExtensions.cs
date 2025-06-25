using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Notificaties.Messaging.Jobs.Notificatie;

namespace OneGround.ZGW.Notificaties.Messaging.Jobs;

public static class NotificatiesJobsServiceExtensions
{
    public static void AddNotificatiesJobs(this IServiceCollection services, Action<NotificatiesJobsOptions> configureOptions)
    {
        services.AddOptions<NotificatiesJobsOptions>().Configure(configureOptions).ValidateOnStart();

        services.AddSingleton<NotificatiesHangfireConnectionFactory>();
    }

    public static void AddNotificatiesJobsAgent(this IServiceCollection services)
    {
        services.AddTransient<NotificatieJob>();
    }
}
