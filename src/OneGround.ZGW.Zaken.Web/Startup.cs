using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;
using OneGround.ZGW.Autorisaties.ServiceAgent.Extensions;
using OneGround.ZGW.Besluiten.ServiceAgent.v1.Extensions;
using OneGround.ZGW.Catalogi.ServiceAgent.v1._3.Extensions;
using OneGround.ZGW.Catalogi.ServiceAgent.v1.Extensions;
using OneGround.ZGW.Common.Batching;
using OneGround.ZGW.Common.Caching;
using OneGround.ZGW.Common.CorrelationId;
using OneGround.ZGW.Common.Extensions;
using OneGround.ZGW.Common.JsonConverters;
using OneGround.ZGW.Common.Messaging;
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
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.DataModel.ZaakObject;
using OneGround.ZGW.Zaken.ServiceAgent.v1.Extensions;
using OneGround.ZGW.Zaken.Web.BusinessRules;
using OneGround.ZGW.Zaken.Web.Controllers;
using OneGround.ZGW.Zaken.Web.Expands.v1._5;
using OneGround.ZGW.Zaken.Web.Handlers;
using OneGround.ZGW.Zaken.Web.Handlers.v1._2.EntityUpdaters;
using OneGround.ZGW.Zaken.Web.Handlers.v1.EntityUpdaters;
using OneGround.ZGW.Zaken.Web.Services;
using OneGround.ZGW.Zaken.Web.Validators.v1.ZaakObject;

namespace OneGround.ZGW.Zaken.Web;

public class Startup
{
    private readonly EventBusConfiguration _rabbitMqConfiguration;

    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;

        _rabbitMqConfiguration = configuration.GetSection("Eventbus").Get<EventBusConfiguration>();
    }

    private IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(Serilog.Log.Logger);

        services.AddZGWDbContext<ZrcDbContext>(Configuration);

        services.AddZGWApi(
            "Zaken",
            Configuration,
            Api.LatestVersion_1_5,
            c =>
            {
                c.NewtonsoftJsonOptions = (o) =>
                {
                    o.SerializerSettings.Converters.Add(new Contracts.v1.Converters.ZaakObjectRequestDtoJsonConverter());
                    o.SerializerSettings.Converters.Add(new Contracts.v1.Converters.ZaakRolRequestDtoJsonConverter());
                    o.SerializerSettings.Converters.Add(new Contracts.v1._5.Converters.ZaakObjectRequestDtoJsonConverter());
                    o.SerializerSettings.Converters.Add(new Contracts.v1._5.Converters.ZaakRolRequestDtoJsonConverter());
                    o.SerializerSettings.Converters.Add(new GeometryJsonConverter());
                };

                c.SwaggerGenOptions = (o) =>
                {
                    o.OperationFilter<RequiresAcceptCrsHeaderOperationFilter>();
                    o.OperationFilter<RequiresContentCrsHeaderOperationFilter>();
                    o.OperationFilter<IfNoneMatchHeaderOperationFilter>();
                    o.OperationFilter<RemoveApiVersionHeaderOperationFilter>();
                    o.OperationFilter<ExpandQueryOperationFilter>();
                    o.OperationFilter<DeprecatedOperationFilter>();
                };

                c.ApiServiceSettings.RegisterSharedAudittrailHandlers = true;
            }
        );

        services.AddOneGroundHealthChecks().AddRedisCheck();

        services.AddZGWAuditTrail<ZrcDbContext>();

        services.AddCatalogiServiceAgent(Configuration);
        services.AddCatalogiServiceAgent_v1_3(Configuration);
        services.AddDocumentenServiceAgent(Configuration);
        services.AddServiceAuthDocumentenServiceAgent_v1_5(Configuration);
        services.AddUserAuthDocumentenServiceAgent_v1_5(Configuration);
        services.AddAutorisatiesServiceAgent(Configuration);
        services.AddZakenServiceAgent(Configuration);
        services.AddBesluitenServiceAgent(Configuration);

        // Expanders support _expand in responses (>= v1.5)
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

            x.AddRequestClient<ISendNotificaties>();
        });

        services.AddCommonServices();

        services.AddZGWNummerGenerator<ZrcDbContext>();

        services.AddNotificatiesService(Configuration);
        services.AddScoped<IClosedZaakModificationBusinessRule, ClosedZaakModificationBusinessRule>();
        services.AddScoped<IZaakBusinessRuleService, ZaakBusinessRuleService>();
        services.AddScoped<IZaakInformatieObjectBusinessRuleService, ZaakInformatieObjectBusinessRuleService>();
        services.AddScoped<IBronDateServiceFactory, BronDateServiceFactory>();

        services.AddSingleton<IApiMetaData, ApiMetaData>();

        services.AddSingleton<IEntityUpdater<ZaakInformatieObject>, ZaakInformatieObjectUpdater>();
        services.AddSingleton<IEntityUpdater<ZaakResultaat>, ZaakResultaatUpdater>();
        services.AddSingleton<IEntityUpdater<Zaak>, ZaakUpdater>();
        services.AddSingleton<IEntityUpdater<ZaakEigenschap>, ZaakEigenschapUpdater>(); // Note: v1.2

        services.AddSingleton<IEntityUpdater<ZaakObject>, ZaakObjectUpdater>(); // Note: v1.2
        services.AddScoped<IZaakObjectValidatorService, ZaakObjectValidatorService>(); // Note: <= v1.2
        services.AddScoped<Validators.v1._5.ZaakObject.IZaakObjectValidatorService, Validators.v1._5.ZaakObject.ZaakObjectValidatorService>(); // Note: v1.5

        services.AddSingleton<IDistributedCacheHelper, DistributedCacheHelper>();

        services.AddTransient<IZaakAuthorizationTempTableService, ZaakAuthorizationTempTableService>();
        services.AddScoped<IZaakKenmerkenResolver, ZaakKenmerkenResolver>();

        services.AddCorrelationId();
        services.AddBatchId();

        services.AddServiceEndpoints(Configuration);

        services.AddScoped<IDbUserContext, DbUserContext>();

        services.AddDatabaseInitializerService<ZrcDbContext, ZrcDbContextFactory, ZrcDbSeeder>();
        services.Configure<MassTransitHostOptions>(options =>
        {
            options.WaitUntilStarted = true;
        });

        //Note: this should be AFTER all httpclients being added!
        services.Replace(ServiceDescriptor.Singleton<IHttpMessageHandlerBuilderFilter, HttpLoggingFilter>());
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public static void Configure(WebApplication app, IWebHostEnvironment env)
    {
        app.UseCorrelationId();
        app.UseBatchId();

        app.ConfigureZgwApi(env);
        app.ConfigureZgwSwagger();
        app.MapOneGroundHealthChecks();
    }
}
