using Hangfire;
using Hangfire.PostgreSql;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Common.Batching;
using OneGround.ZGW.Common.CorrelationId;
using OneGround.ZGW.Common.Extensions;
using OneGround.ZGW.Common.Messaging.Filters;
using OneGround.ZGW.Common.ServiceAgent;
using OneGround.ZGW.Common.ServiceAgent.Configuration;
using OneGround.ZGW.DataAccess;
using OneGround.ZGW.Notificaties.DataModel;
using OneGround.ZGW.Notificaties.Messaging.Configuration;
using OneGround.ZGW.Notificaties.Messaging.Consumers;
using OneGround.ZGW.Notificaties.Messaging.Jobs;
using OneGround.ZGW.Notificaties.Messaging.Jobs.Extensions;
using OneGround.ZGW.Notificaties.Messaging.Jobs.Notificatie;
using Polly;

namespace OneGround.ZGW.Notificaties.Messaging;

public class ServiceConfiguration
{
    private readonly IConfiguration _configuration;
    private readonly HangfireConfiguration _hangfireConfiguration;

    public ServiceConfiguration(IConfiguration configuration)
    {
        _configuration = configuration;

        _hangfireConfiguration = _configuration.GetSection("Hangfire").Get<HangfireConfiguration>() ?? new HangfireConfiguration();
    }

    public void ConfigureServices(IServiceCollection services)
    {
        if (string.IsNullOrWhiteSpace(_configuration.GetConnectionString("UserConnectionString")))
            throw new InvalidOperationException("No valid UserConnectionString specified in appSettings.json section: ConnectionStrings.");

        services.AddZGWDbContext<NrcDbContext>(_configuration);
        services.AddBatchId();
        services.AddOrganisationContext();
        services.AddCorrelationId();
        services.AddServiceEndpoints(_configuration);
        services.AddTransient<CorrelationIdHandler>();
        services.AddScoped<BatchIdHandler>();

        const string serviceName = "NotificatiesSender";
        var optionsKey = HttpResiliencePipelineOptions.GetKey(serviceName);
        services.Configure<HttpResiliencePipelineOptions>(optionsKey, _configuration.GetRequiredSection(optionsKey));

        services
            .AddHttpClient<INotificationSender, NotificationSender>()
            .AddResilienceHandler(
                serviceName,
                (builder, context) =>
                {
                    context.EnableReloads<HttpResiliencePipelineOptions>(optionsKey);
                    var options = context.GetOptions<HttpResiliencePipelineOptions>(optionsKey);

                    builder.AddRetry(options.Retry);
                    builder.AddTimeout(options.Timeout);
                }
            );

        services.Configure<MassTransitHostOptions>(options =>
        {
            options.WaitUntilStarted = true;
        });

        services.AddMassTransit(x =>
        {
            var eventbusConfiguration =
                _configuration.GetSection("Eventbus").Get<NotificatiesEventBusConfiguration>() ?? new NotificatiesEventBusConfiguration();

            x.DisableUsageTelemetry();
            x.SetKebabCaseEndpointNameFormatter();

            x.AddConsumer<SendNotificatiesConsumer>();

            x.UsingRabbitMq(
                (context, cfg) =>
                {
                    cfg.Host(
                        eventbusConfiguration.HostName,
                        eventbusConfiguration.VirtualHost,
                        h =>
                        {
                            h.Username(eventbusConfiguration.UserName);
                            h.Password(eventbusConfiguration.Password);
                        }
                    );

                    cfg.ReceiveEndpoint(
                        "notificatie",
                        e =>
                        {
                            e.PrefetchCount = eventbusConfiguration.ReceivePrefetchCount;
                            e.EnablePriority(2);
                            e.ConfigureConsumer<SendNotificatiesConsumer>(context);
                        }
                    );

                    cfg.UseHangfireScheduler(queueName: Constants.NrcHangfireQueue);

                    cfg.UseConsumeFilter(typeof(RsinFilter<>), context);
                    cfg.UseConsumeFilter(typeof(CorrelationIdFilter<>), context);

                    cfg.UseConsumeFilter(typeof(BatchIdConsumingFilter<>), context);
                    //cfg.UseSendFilter(typeof(BatchIdSendingFilter<>), context);
                    //cfg.UsePublishFilter(typeof(BatchIdPublishFilter<>), context);

                    cfg.ConfigureEndpoints(context);
                }
            );

            x.AddMessageScheduler(new Uri($"queue:{Constants.NrcHangfireQueue}"));
        });

        services.AddHostedService<FailedQueueInitializationService>();

        services.AddNotificatiesJobs(o => o.ConnectionString = _configuration.GetConnectionString("HangfireConnectionString"));
        services.AddNotificatiesServerJobs();

        services.AddHangfireServer(o =>
        {
            o.ServerName = Constants.NrcListenerServer;
            o.Queues = [Constants.NrcListenerQueue];
        });

        services.AddHangfire(
            (s, o) =>
            {
                var connectionFactory = s.GetRequiredService<NotificatiesHangfireConnectionFactory>();
                o.UsePostgreSqlStorage(o => o.UseConnectionFactory(connectionFactory));

                var retryPolicy = GetRetryPolicyFromConfig();
                o.UseFilter(retryPolicy);

                var expireFailedJobsScanAtCronExpr = GetExpireFailedJobsScanAtCronExpr();

                RecurringJob.AddOrUpdate<ManagementJob>(
                    "expire-failed-jobs",
                    h => h.ExpireFailedJobsScanAt(_hangfireConfiguration.ExpireFailedJobAfter),
                    expireFailedJobsScanAtCronExpr
                );
            }
        );
    }

    private string GetExpireFailedJobsScanAtCronExpr()
    {
        // Set default cron expression weekly each Monday
        string cronExpression = Cron.Weekly(DayOfWeek.Monday);

        if (Enum.TryParse<DayOfWeek>(_hangfireConfiguration.ExpireFailedJobsScanAt, out var dayOfWeek))
        {
            // For example: "ExpireFailedJobsScanAt": "Thursday"
            cronExpression = Cron.Weekly(dayOfWeek);
        }
        else if (TimeOnly.TryParseExact(_hangfireConfiguration.ExpireFailedJobsScanAt, "HH:mm", out var dailyAt))
        {
            // For example: "ExpireFailedJobsScanAt": "13:30"
            cronExpression = Cron.Daily(dailyAt.Hour, dailyAt.Minute);
        }
        else
        {
            // For example: "ExpireFailedJobsScanAt": "Thursday 12:00"
            var parts = _hangfireConfiguration.ExpireFailedJobsScanAt.Split(' ');
            if (parts.Length == 2 && Enum.TryParse<DayOfWeek>(parts[0], out var day) && TimeOnly.TryParse(parts[1], out var at))
            {
                cronExpression = Cron.Weekly(day, at.Hour, at.Minute);
            }
        }
        return cronExpression;
    }

    private AutomaticRetryAttribute GetRetryPolicyFromConfig()
    {
        return new AutomaticRetryAttribute
        {
            ExceptOn = [typeof(GeneralException)],
            OnAttemptsExceeded = AttemptsExceededAction.Fail,
            Attempts = _hangfireConfiguration.ScheduledRetries.Length,
            DelaysInSeconds = _hangfireConfiguration.ScheduledRetries.Select(c => (int)c.TotalSeconds).ToArray(),
        };
    }
}
