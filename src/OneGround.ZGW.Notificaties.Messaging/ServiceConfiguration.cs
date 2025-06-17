using System.Net;
using Hangfire;
using Hangfire.PostgreSql;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Common.Batching;
using OneGround.ZGW.Common.CorrelationId;
using OneGround.ZGW.Common.Extensions;
using OneGround.ZGW.Common.Messaging.Filters;
using OneGround.ZGW.Common.ServiceAgent;
using OneGround.ZGW.Common.Services;
using OneGround.ZGW.DataAccess;
using OneGround.ZGW.Notificaties.DataModel;
using OneGround.ZGW.Notificaties.Messaging.Configuration;
using OneGround.ZGW.Notificaties.Messaging.Consumers;
using OneGround.ZGW.Notificaties.Messaging.Extensions;
using Polly;
using Polly.Retry;

namespace OneGround.ZGW.Notificaties.Messaging;

public class ServiceConfiguration
{
    private readonly IConfiguration _configuration;

    public ServiceConfiguration(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        if (string.IsNullOrWhiteSpace(_configuration.GetConnectionString("UserConnectionString")))
            throw new InvalidOperationException("No valid UserConnectionString specified in appSettings.json section: ConnectionStrings.");

        services.AddZGWDbContext<NrcDbContext>(_configuration);

        services.AddScoped<IServerCertificateValidator, ByPassServerCertificateValidator>();

        services.AddBatchId();
        services.AddOrganisationContext();
        services.AddCorrelationId();

        services.AddServiceEndpoints(_configuration);

        services.AddTransient<CorrelationIdHandler>();
        services.AddScoped<BatchIdHandler>();

        // Bind named ResiliencePipelineNotificaties from configuration (custom setting)
        services.Configure<ResiliencePipelineOptions>(
            "resilience-pipeline-notificaties",
            _configuration.GetRequiredSection("ResiliencePipelineNotificaties")
        );

        // Define the HTTP pipeline
        services
            .AddHttpClient<INotificationSender, NotificationSender>()
            .ConfigurePrimaryHttpMessageHandler(sp =>
            {
                var handler = new HttpClientHandler();

                var serverCertificateValidator = sp.GetService<IServerCertificateValidator>();
                handler.ServerCertificateCustomValidationCallback = serverCertificateValidator.ValidateCertificate;

                return handler;
            })
            .AddResilienceHandler(
                "resilience-pipeline-notificaties",
                (builder, context) =>
                {
                    // Enable dynamic reloads of this pipeline whenever the named ResiliencePipelineNotificaties change
                    context.EnableReloads<ResiliencePipelineOptions>("resilience-pipeline-notificaties");

                    // Retrieve the named options
                    var options = context.GetOptions<ResiliencePipelineOptions>("resilience-pipeline-notificaties");

                    builder.AddRetry(
                        new RetryStrategyOptions<HttpResponseMessage>
                        {
                            MaxRetryAttempts = options.Retry.MaxRetryAttempts,
                            BackoffType = options.Retry.BackoffType,
                            UseJitter = options.Retry.UseJitter,
                            Delay = options.Retry.Delay,
                            ShouldHandle = arg =>
                            {
                                if (arg.Outcome.Result == null) // This flow is when service did not repond at all (gives no HTTP statuscode)
                                {
                                    // Always retry on this flow
                                    return ValueTask.FromResult(true);
                                }

                                // Retry depending on the HTTP status code
                                var shouldRetry = DefaultRetryOnHttpStatusCodes
                                    .Concat(options.AddRetryOnHttpStatusCodes)
                                    .Any(statuscode => arg.Outcome.Result.StatusCode == statuscode);

                                return ValueTask.FromResult(shouldRetry);
                            },
                            OnRetry = arg =>
                            {
                                LogRetry(context, arg);

                                // Event handlers can be asynchronous; here, we return an empty ValueTask.
                                return default;
                            },
                        }
                    );

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
            x.AddConsumer<NotifySubscriberConsumer>();

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

                    cfg.ReceiveEndpoint(
                        "notificatie-subscriber",
                        e =>
                        {
                            e.PrefetchCount = eventbusConfiguration.ReceivePrefetchCount;
                            e.EnablePriority(2);
                            e.ConfigureConsumer<NotifySubscriberConsumer>(context);
                        }
                    );

                    cfg.UseHangfireScheduler();

                    cfg.UseConsumeFilter(typeof(RsinFilter<>), context);
                    cfg.UseConsumeFilter(typeof(CorrelationIdFilter<>), context);

                    cfg.UseConsumeFilter(typeof(BatchIdConsumingFilter<>), context);
                    //cfg.UseSendFilter(typeof(BatchIdSendingFilter<>), context);
                    //cfg.UsePublishFilter(typeof(BatchIdPublishFilter<>), context);

                    cfg.ConfigureEndpoints(context);
                }
            );
        });

        services.AddHostedService<FailedQueueInitializationService>();

        services.AddHangfireNotificatieReQueuer();

        services.AddHangfireServer();

        services.AddHangfire(
            (_, options) =>
                options.UsePostgreSqlStorage(o =>
                {
                    o.UseNpgsqlConnection(_configuration.GetConnectionString("HangfireConnectionString"));
                })
        );
    }

    private static void LogRetry(ResilienceHandlerContext context, OnRetryArguments<HttpResponseMessage> arg)
    {
        using var scope = context.ServiceProvider.CreateScope();

        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ServiceConfiguration>>();

        // Get the context in which a retry should be taken and log...
        if (
            arg.Context.Properties.TryGetValue(
                new ResiliencePropertyKey<HttpRequestMessage>("Resilience.Http.RequestMessage"),
                out var httpRequestMessage
            )
        )
        {
            if (arg.Outcome.Result is { } httpResponseMessage)
            {
                logger.LogInformation(
                    "OnRetry, Attempt# {AttemptNumber} on {Method} {RequestUri} => {ReasonPhrase} [{StatusCode}]. Next over {TotalSeconds} second(s).",
                    arg.AttemptNumber,
                    httpRequestMessage.Method,
                    httpRequestMessage.RequestUri,
                    httpResponseMessage.ReasonPhrase,
                    (int)httpResponseMessage.StatusCode,
                    arg.RetryDelay.TotalSeconds
                );
            }
            else
            {
                logger.LogInformation(
                    "OnRetry, Attempt# {AttemptNumber} on {Method} {RequestUri} => (no response). Next over {TotalSeconds} second(s).",
                    arg.AttemptNumber,
                    httpRequestMessage.Method,
                    httpRequestMessage.RequestUri,
                    arg.RetryDelay.TotalSeconds
                );
            }
        }
        else
        {
            logger.LogInformation(
                "OnRetry, Attempt# {AttemptNumber}. Next over {TotalSeconds} second(s).",
                arg.AttemptNumber,
                arg.RetryDelay.TotalSeconds
            );
        }
    }

    private static IEnumerable<HttpStatusCode> DefaultRetryOnHttpStatusCodes =>
        [
            HttpStatusCode.RequestTimeout,
            HttpStatusCode.TooManyRequests,
            HttpStatusCode.InternalServerError,
            HttpStatusCode.BadGateway,
            HttpStatusCode.ServiceUnavailable,
            HttpStatusCode.GatewayTimeout,
        ];
}
