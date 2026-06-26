using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;
using OneGround.ZGW.Notificaties.Messaging.Consumers;
using OneGround.ZGW.Notificaties.Messaging.Jobs.Notificatie;

namespace OneGround.ZGW.Notificaties.Messaging.Jobs.Extensions;

public static class NotificatiesJobsServiceExtensions
{
    public static void AddNotificatiesJobs(this IServiceCollection services, Action<NotificatiesJobsOptions> configureOptions)
    {
        services.AddOptions<NotificatiesJobsOptions>().Configure(configureOptions).ValidateOnStart();

        services.AddKeyedSingleton<NpgsqlDataSource>(HangfireServiceKeys.DataSource, (sp, _) =>
            new NpgsqlDataSourceBuilder(sp.GetRequiredService<IOptions<NotificatiesJobsOptions>>().Value.ConnectionString).Build());

        services.AddSingleton<NotificatiesHangfireConnectionFactory>();

        services.AddSingleton<INotificatieScheduler, NotificatieScheduler>();
    }

    public static void AddNotificatiesServerJobs(this IServiceCollection services)
    {
        services.AddTransient<NotificatieJob>();
        services.AddTransient<BlockFailingSubscriptionsJob>();
    }
}
