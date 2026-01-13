using Hangfire;
using Hangfire.Console;
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
using OneGround.ZGW.Common.Web.HealthChecks;
using OneGround.ZGW.DataAccess;
using OneGround.ZGW.Notificaties.DataModel;
using OneGround.ZGW.Notificaties.Messaging.CircuitBreaker.Extensions;
using OneGround.ZGW.Notificaties.Messaging.Configuration;
using OneGround.ZGW.Notificaties.Messaging.Consumers;
using OneGround.ZGW.Notificaties.Messaging.Jobs;
using OneGround.ZGW.Notificaties.Messaging.Jobs.Extensions;
using OneGround.ZGW.Notificaties.Messaging.Jobs.Notificatie;
using OneGround.ZGW.Notificaties.Messaging.Services;
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

        services.AddOneGroundHealthChecks();

        services.AddTransient<IAbonnementService, AbonnementService>();
        services.AddCircuitBreaker(_configuration);

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

        services.AddOptions<ApplicationOptions>().Bind(_configuration.GetSection(ApplicationOptions.Application));

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

                    cfg.UseConsumeFilter(typeof(RsinFilter<>), context);
                    cfg.UseConsumeFilter(typeof(CorrelationIdFilter<>), context);

                    cfg.UseConsumeFilter(typeof(BatchIdConsumingFilter<>), context);

                    cfg.ConfigureEndpoints(context);
                }
            );
        });

        services.AddNotificatiesJobs(o => o.ConnectionString = _configuration.GetConnectionString("HangfireConnectionString"));
        services.AddNotificatiesServerJobs();

        services.AddHangfireServer(o =>
        {
            o.ServerName = Constants.NrcListenerMainServer;
            o.Queues = [Constants.NrcListenerMainQueue];
        });

        services.AddHangfireServer(o =>
        {
            o.ServerName = Constants.NrcListenerRetryServer;
            o.Queues = [Constants.NrcListenerRetryQueue];
        });

        services.AddHangfire(
            (s, o) =>
            {
                var connectionFactory = s.GetRequiredService<NotificatiesHangfireConnectionFactory>();
                o.UsePostgreSqlStorage(o => o.UseConnectionFactory(connectionFactory));

                var retryPolicy = GetRetryPolicyFromConfig();
                o.UseFilter(retryPolicy);

                if (IsExpireFailedJobsScannerEnabled)
                {
                    var expireFailedJobsScanAtCronExpr = GetExpireFailedJobsScanAtCronExpr();

                    RecurringJob.AddOrUpdate<NotificatieManagementJob>(
                        "expire-failed-jobs",
                        h => h.ExpireFailedJobsScanAt(_hangfireConfiguration.ExpireFailedJobAfter, null),
                        expireFailedJobsScanAtCronExpr
                    );
                }
                else
                {
                    RecurringJob.RemoveIfExists("expire-failed-jobs");
                }

                o.UseConsole();
            }
        );
    }

    private bool IsExpireFailedJobsScannerEnabled =>
        !string.IsNullOrWhiteSpace(_hangfireConfiguration.ExpireFailedJobsScanAt)
        && !DisabledValues.Contains(_hangfireConfiguration.ExpireFailedJobsScanAt.Trim());

    private static readonly HashSet<string> DisabledValues = new(StringComparer.OrdinalIgnoreCase) { "never", "disabled", "n/a" };

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
        // First remove the default AutomaticRetryAttribute filter (which has 10 retries). If we don't do this we got two instances of AutomaticRetryAttribute
        var automaticRetryFilter = GlobalJobFilters.Filters.FirstOrDefault(f => f.Instance is AutomaticRetryAttribute);
        if (automaticRetryFilter != null)
        {
            GlobalJobFilters.Filters.Remove(automaticRetryFilter.Instance);
        }

        if (_hangfireConfiguration.RetryScheduleTimeSpanList == null || _hangfireConfiguration.RetryScheduleTimeSpanList.Length == 0)
        {
            // No retries
            return new AutomaticRetryAttribute
            {
                OnAttemptsExceeded = AttemptsExceededAction.Fail,
                Attempts = 0,
                LogEvents = false,
            };
        }

        // Configure retries based on the configured timespan list
        return new AutomaticRetryAttribute
        {
            OnAttemptsExceeded = AttemptsExceededAction.Fail,
            Attempts = _hangfireConfiguration.RetryScheduleTimeSpanList.Length,
            DelaysInSeconds = _hangfireConfiguration.RetryScheduleTimeSpanList.Select(c => (int)c.TotalSeconds).ToArray(),
            LogEvents = false,
        };
    }
}
