using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;
using OneGround.ZGW.Autorisaties.ServiceAgent.Extensions;
using OneGround.ZGW.Catalogi.DataModel;
using OneGround.ZGW.Catalogi.Web.BusinessRules;
using OneGround.ZGW.Catalogi.Web.Controllers;
using OneGround.ZGW.Catalogi.Web.Handlers.v1.EntityUpdaters;
using OneGround.ZGW.Catalogi.Web.Services;
using OneGround.ZGW.Common.Batching;
using OneGround.ZGW.Common.CorrelationId;
using OneGround.ZGW.Common.Extensions;
using OneGround.ZGW.Common.Messaging;
using OneGround.ZGW.Common.Messaging.Configuration;
using OneGround.ZGW.Common.Messaging.Filters;
using OneGround.ZGW.Common.Web;
using OneGround.ZGW.Common.Web.Extensions.ApplicationBuilder;
using OneGround.ZGW.Common.Web.Extensions.ServiceCollection;
using OneGround.ZGW.Common.Web.Logging;
using OneGround.ZGW.Common.Web.Middleware;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Swagger;
using OneGround.ZGW.DataAccess;
using OneGround.ZGW.Notificaties.ServiceAgent.Extensions;
using OneGround.ZGW.Referentielijsten.ServiceAgent.Extensions;

namespace OneGround.ZGW.Catalogi.Web;

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

        services.AddZGWDbContext<ZtcDbContext>(Configuration);

        services.AddZGWApi(
            "Catalogi",
            Configuration,
            Api.LatestVersion_1_3,
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

        services.AddZGWAuditTrail<ZtcDbContext>();

        services.AddTransient<ICatalogEventService, CatalogEventService>();

        services.AddAutorisatiesServiceAgent(Configuration);
        services.AddReferentielijstenServiceAgent(Configuration);

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

        services.AddNotificatiesService(Configuration);
        services.AddScoped<IZaakTypeInformatieObjectTypenBusinessRuleService, ZaakTypeInformatieObjectTypenBusinessRuleService>();
        services.AddScoped<IConceptBusinessRule, ConceptBusinessRule>();
        services.AddScoped<IBesluitTypeRelationsValidator, BesluitTypeRelationsValidator>();
        services.AddScoped<IResultaatTypeBusinessRuleService, ResultaatTypeBusinessRuleService>();
        services.AddScoped<IEindStatusResolver, EindStatusResolver>();

        services.AddSingleton<IApiMetaData, ApiMetaData>();

        services.AddSingleton<IEntityUpdater<Eigenschap>, EigenschapUpdater>();
        services.AddSingleton<IEntityUpdater<ResultaatType>, ResultaatTypeUpdater>();
        services.AddSingleton<IEntityUpdater<StatusType>, StatusTypeUpdater>();
        services.AddSingleton<IEntityUpdater<ZaakType>, ZaakTypeUpdater>();
        services.AddSingleton<IEntityUpdater<ZaakTypeInformatieObjectType>, ZaakTypeInformatieObjectTypeUpdater>();
        services.AddSingleton<IEntityUpdater<InformatieObjectType>, InformatieObjectTypeUpdater>();
        services.AddSingleton<IEntityUpdater<RolType>, RolTypeUpdater>();
        services.AddSingleton<IEntityUpdater<BesluitType>, BesluitTypeUpdater>();
        services.AddSingleton<IEntityUpdater<ZaakObjectType>, ZaakObjectTypeUpdater>();

        services.AddScoped<IZaakTypeDataService, ZaakTypeDataService>();
        services.AddScoped<IBesluitTypeDataService, BesluitTypeDataService>();
        services.AddScoped<IInformatieObjectTypeDataService, InformatieObjectTypeDataService>();

        services.AddCorrelationId();
        services.AddBatchId();

        services.AddServiceEndpoints(Configuration);

        services.AddRedisCacheInvalidation();

        services.AddScoped<IDbUserContext, DbUserContext>();

        services.AddDatabaseInitializerService<ZtcDbContext, ZtcDbContextFactory, ZtcDbSeeder>();
        services.Configure<MassTransitHostOptions>(options =>
        {
            options.WaitUntilStarted = true;
        });

        //Note: this should be AFTER all httpclients being added!
        services.Replace(ServiceDescriptor.Singleton<IHttpMessageHandlerBuilderFilter, HttpLoggingFilter>());
    }

    public static void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseCorrelationId();
        app.UseBatchId();

        app.ConfigureZGWApi(env);
        app.ConfigureZGWSwagger();
    }
}
