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
using OneGround.ZGW.Common.ServiceAgent;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.DataAccess;
using OneGround.ZGW.Documenten.DataModel;
using OneGround.ZGW.Documenten.Jobs;
using OneGround.ZGW.Documenten.Messaging.Configuration;
using OneGround.ZGW.Documenten.Messaging.Services;
using OneGround.ZGW.Documenten.ServiceAgent.v1.Extensions;
using OneGround.ZGW.Notificaties.ServiceAgent.Extensions;

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
        services.AddZGWDbContext<DrcDbContext>(_configuration);

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

        services.AddDocumentenJobs(o => o.ConnectionString = _configuration.GetConnectionString("HangfireConnectionString"));

        services.AddSingleton<DocumentenHangfireConnectionFactory>();

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

        services.AddHangfireServer(o =>
        {
            o.ServerName = Constants.DrcListenerServer;
            o.Queues = [Constants.DrcListenerQueue, Constants.DrcListenerLowPriorityQueue, Constants.DrcSubscriptionsQueue];
        });

        services.AddHangfire(
            (s, o) =>
            {
                var connectionFactory = s.GetRequiredService<DocumentenHangfireConnectionFactory>();
                o.UsePostgreSqlStorage(o => o.UseConnectionFactory(connectionFactory));

                var retryPolicy = GetRetryPolicyFromConfig();
                o.UseFilter(retryPolicy);

                o.UseConsole();
            }
        );

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
            OnAttemptsExceeded = AttemptsExceededAction.Fail,
            Attempts = _hangfireConfiguration.ScheduledRetries.Length,
            DelaysInSeconds = _hangfireConfiguration.ScheduledRetries.Select(c => (int)c.TotalSeconds).ToArray(),
        };
    }
}
