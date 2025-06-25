using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Notificaties.Messaging.Jobs.Notificatie;

namespace OneGround.ZGW.Notificaties.Messaging.Jobs.Extensions;

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

        Task.Run(async () =>
        {
            await Task.Delay(10_000);

            var job = BackgroundJob.Schedule<NotificatieJob>(
                h =>
                    h.ReQueueNotificatieAsync(
                        new SubscriberNotificatie() { Rsin = "000000000", ChannelUrl = "http://example.com" },
                        TimeSpan.FromMinutes(1)
                    ),
                TimeSpan.FromMinutes(1)
            );
        });
    }
}
