using System.Linq;
using System.Reflection;
using Hangfire;
using Hangfire.Console;
using Hangfire.PostgreSql;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Autorisaties.ServiceAgent;
using OneGround.ZGW.Common.Batching;
using OneGround.ZGW.Common.CorrelationId;
using OneGround.ZGW.Common.Extensions;
using OneGround.ZGW.Common.Messaging.Filters;
using OneGround.ZGW.Common.ServiceAgent;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Documenten.Messaging.Configuration;
using OneGround.ZGW.Documenten.Messaging.Services;
using OneGround.ZGW.Documenten.ServiceAgent.v1.Extensions;
using OneGround.ZGW.Notificaties.ServiceAgent.Extensions;
using Roxit.ZGW.Documenten.Jobs;
using Roxit.ZGW.Internal.Common.Web.Authentication;

namespace OneGround.ZGW.Documenten.Messaging;

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
        services.AddInternalZgwAuthentication<AutorisatiesServiceAgentAuthorizationResolver>(_configuration);
        services.RegisterInternalZgwTokenClient(_configuration);

        services.AddHttpContextAccessor();
        services.AddHealthChecks();

        var callingAssembly = Assembly.GetCallingAssembly();
        var executingAssembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(x =>
        {
            x.RegisterServicesFromAssemblies(callingAssembly);
            x.RegisterServicesFromAssemblies(executingAssembly);
        });

        services.AddSingleton<IErrorResponseBuilder, ErrorResponseBuilder>();

        services.AddSingleton<DocumentenHangfireConnectionFactory>(); // See the other duplicate one

        services.AddDocumentenServiceAgent(_configuration);
        services.AddNotificatiesServiceAgent(_configuration);

        services.AddScoped<IAutorisatiesServiceAgent, AutorisatiesServiceAgent>();
        services.AddOrganisationContext();
        services.AddCorrelationId();
        services.AddScoped<BatchIdHandler>();
        services.AddBatchId();
        services.AddServiceEndpoints(_configuration);

        services.Configure<MassTransitHostOptions>(options =>
        {
            options.WaitUntilStarted = true;
        });

        services.AddSingleton<IEnkelvoudigInformatieObjectDeletionService, EnkelvoudigInformatieObjectDeletionService>();

        // Note: Install Background service for scanning document deletions (queued by DestroyEnkelvoudigInformatieObjectConsumer)
        //services.AddHostedService<DeleteDocumentService>();

        // NEW!!!!
        services.AddHangfireServer(o =>
        {
            o.ServerName = Constants.DrcListenerServer;
            o.Queues = [Constants.DrcListenerQueue, Constants.DrcListenerLowPriorityQueue, Constants.DrcSubscriptionsQueue]; // TODO: DrcSubscriptionsQueue Internal only!!!!
        });

        services.AddHangfire(
            (s, o) =>
            {
                var connectionFactory = s.GetRequiredService<DocumentenHangfireConnectionFactory>();
                o.UsePostgreSqlStorage(o => o.UseConnectionFactory(connectionFactory));

                var retryPolicy = GetRetryPolicyFromConfig(); // TODO: Move to ConfigureServices (common part)
                o.UseFilter(retryPolicy);

                o.UseConsole();
            }
        );
        // ----

        // TODO: RabbitMQ phase out? (so cleanup all nuget packages)
        services.AddMassTransit(x =>
        {
            x.DisableUsageTelemetry();
            x.SetKebabCaseEndpointNameFormatter();

            x.UsingRabbitMq(
                (context, cfg) =>
                {
                    var eventbusConfiguration = _configuration.GetSection("Eventbus").Get<DocumentenEventBusConfiguration>();

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
                        eventbusConfiguration.ReceiveQueue,
                        e =>
                        {
                            e.PrefetchCount = eventbusConfiguration.ReceivePrefetchCount;
                            e.EnablePriority(2);

                            e.UseMessageRetry(r =>
                                r.Interval(eventbusConfiguration.ReceiveEndpointRetries, eventbusConfiguration.ReceiveEndpointTimeout * 1000)
                            );
                        }
                    );

                    cfg.UseConsumeFilter(typeof(RsinFilter<>), context);
                    cfg.UseConsumeFilter(typeof(CorrelationIdFilter<>), context);

                    cfg.UseConsumeFilter(typeof(BatchIdConsumingFilter<>), context);
                    cfg.UseSendFilter(typeof(BatchIdSendingFilter<>), context);
                    cfg.UsePublishFilter(typeof(BatchIdPublishFilter<>), context);

                    cfg.ConfigureEndpoints(context);
                }
            );
        });

        services.AddControllers();
    }

    private AutomaticRetryAttribute GetRetryPolicyFromConfig()
    {
        if (_hangfireConfiguration.ScheduledRetries == null || _hangfireConfiguration.ScheduledRetries.Length == 0)
        {
            // No retries
            return new AutomaticRetryAttribute { Attempts = 0, OnAttemptsExceeded = AttemptsExceededAction.Fail };
        }

        return new AutomaticRetryAttribute
        {
            // ExceptOn = [typeof(GeneralException)],
            OnAttemptsExceeded = AttemptsExceededAction.Fail,
            Attempts = _hangfireConfiguration.ScheduledRetries.Length,
            DelaysInSeconds = _hangfireConfiguration.ScheduledRetries.Select(c => (int)c.TotalSeconds).ToArray(),
        };
    }
}
