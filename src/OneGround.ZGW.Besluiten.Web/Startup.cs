using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;
using OneGround.ZGW.Autorisaties.ServiceAgent.Extensions;
using OneGround.ZGW.Besluiten.DataModel;
using OneGround.ZGW.Besluiten.ServiceAgent.v1.Extensions;
using OneGround.ZGW.Besluiten.Web.BusinessRules;
using OneGround.ZGW.Besluiten.Web.Controllers;
using OneGround.ZGW.Besluiten.Web.Expands.v1;
using OneGround.ZGW.Besluiten.Web.Handlers;
using OneGround.ZGW.Besluiten.Web.Handlers.v1.EntityUpdaters;
using OneGround.ZGW.Besluiten.Web.Services;
using OneGround.ZGW.Catalogi.ServiceAgent.v1._3.Extensions;
using OneGround.ZGW.Catalogi.ServiceAgent.v1.Extensions;
using OneGround.ZGW.Common.Batching;
using OneGround.ZGW.Common.CorrelationId;
using OneGround.ZGW.Common.Extensions;
using OneGround.ZGW.Common.Messaging.Configuration;
using OneGround.ZGW.Common.Messaging.Filters;
using OneGround.ZGW.Common.Web;
using OneGround.ZGW.Common.Web.Extensions.ApplicationBuilder;
using OneGround.ZGW.Common.Web.Extensions.ServiceCollection;
using OneGround.ZGW.Common.Web.HealthChecks;
using OneGround.ZGW.Common.Web.Logging;
using OneGround.ZGW.Common.Web.Middleware;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Swagger;
using OneGround.ZGW.DataAccess;
using OneGround.ZGW.Documenten.ServiceAgent.v1._5.Extensions;
using OneGround.ZGW.Documenten.ServiceAgent.v1.Extensions;
using OneGround.ZGW.Notificaties.ServiceAgent.Extensions;
using OneGround.ZGW.Zaken.ServiceAgent.v1.Extensions;

namespace OneGround.ZGW.Besluiten.Web;

public class Startup
{
    private readonly EventBusConfiguration _rabbitMqConfiguration;

    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;

        _rabbitMqConfiguration = configuration.GetSection("Eventbus").Get<EventBusConfiguration>();
    }

    private IConfiguration Configuration { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(Serilog.Log.Logger);

        services.AddZGWDbContext<BrcDbContext>(Configuration);

        services.AddZGWApi(
            "Besluiten",
            Configuration,
            Api.LatestVersion_1_0,
            c =>
            {
                c.MvcOptions = o =>
                {
                    o.AllowEmptyInputInBodyModelBinding = true;
                };

                c.SwaggerGenOptions = o =>
                {
                    o.OperationFilter<IfNoneMatchHeaderOperationFilter>();
                    o.OperationFilter<RemoveApiVersionHeaderOperationFilter>();
                    o.OperationFilter<ExpandQueryOperationFilter>();
                };

                c.ApiServiceSettings.RegisterSharedAudittrailHandlers = true;
            }
        );

        services
            .AddOneGroundHealthChecks()
            .AddRedisCheck()
            .Build();

        services.AddZGWAuditTrail<BrcDbContext>();

        services.AddAutorisatiesServiceAgent(Configuration);
        services.AddZakenServiceAgent(Configuration);
        services.AddBesluitenServiceAgent(Configuration);
        services.AddCatalogiServiceAgent(Configuration);
        services.AddCatalogiServiceAgent_v1_3(Configuration);
        services.AddDocumentenServiceAgent(Configuration);
        services.AddServiceAuthDocumentenServiceAgent_v1_5(Configuration);
        services.AddUserAuthDocumentenServiceAgent_v1_5(Configuration);

        // Expanders support _expand in responses
        services.AddExpandables();

        services.AddMassTransit(x =>
        {
            x.DisableUsageTelemetry();

            x.UsingRabbitMq(
                (bus, conf) =>
                {
                    conf.Host(
                        _rabbitMqConfiguration.HostName,
                        _rabbitMqConfiguration.VirtualHost,
                        h =>
                        {
                            h.Username(_rabbitMqConfiguration.UserName);
                            h.Password(_rabbitMqConfiguration.Password);
                        }
                    );
                    conf.UseSendFilter(typeof(BatchIdSendingFilter<>), bus);
                    conf.UsePublishFilter(typeof(BatchIdPublishFilter<>), bus);
                }
            );
        });

        services.AddCommonServices();

        services.AddZGWNummerGenerator<BrcDbContext>();

        services.AddNotificatiesService(Configuration);
        services.AddScoped<IBesluitBusinessRuleService, BesluitBusinessRuleService>();
        services.AddScoped<IBesluitInformatieObjectBusinessRuleService, BesluitInformatieObjectBusinessRuleService>();

        services.AddSingleton<IApiMetaData, ApiMetaData>();

        services.AddSingleton<IEntityUpdater<Besluit>, BesluitUpdater>();

        services.AddTransient<IBesluitAuthorizationTempTableService, BesluitAuthorizationTempTableService>();
        services.AddScoped<IBesluitKenmerkenResolver, BesluitKenmerkenResolver>();

        services.AddCorrelationId();
        services.AddBatchId();

        services.AddServiceEndpoints(Configuration);

        services.AddScoped<IDbUserContext, DbUserContext>();

        services.AddDatabaseInitializerService<BrcDbContext, BrcDbContextFactory>();
        services.Configure<MassTransitHostOptions>(options =>
        {
            options.WaitUntilStarted = true;
        });

        //Note: this should be AFTER all httpclients being added!
        services.Replace(ServiceDescriptor.Singleton<IHttpMessageHandlerBuilderFilter, HttpLoggingFilter>());
    }

    public static void Configure(WebApplication app, IWebHostEnvironment env)
    {
        app.UseCorrelationId();
        app.UseBatchId();

        app.ConfigureZgwApi(env);
        app.ConfigureZgwSwagger();
        app.MapOneGroundHealthChecks(c =>
        {
            // Note: backwards compatibility with the old health check endpoint
            c.PingEndpoints.Endpoints.Add("/health");
        });
    }
}
