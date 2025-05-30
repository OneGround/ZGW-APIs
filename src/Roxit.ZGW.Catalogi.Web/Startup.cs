using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;
using Roxit.ZGW.Autorisaties.ServiceAgent.Extensions;
using Roxit.ZGW.Catalogi.DataModel;
using Roxit.ZGW.Catalogi.Web.BusinessRules;
using Roxit.ZGW.Catalogi.Web.Controllers;
using Roxit.ZGW.Catalogi.Web.Handlers.v1.EntityUpdaters;
using Roxit.ZGW.Catalogi.Web.Services;
using Roxit.ZGW.Common.Batching;
using Roxit.ZGW.Common.CorrelationId;
using Roxit.ZGW.Common.Extensions;
using Roxit.ZGW.Common.Messaging;
using Roxit.ZGW.Common.Messaging.Configuration;
using Roxit.ZGW.Common.Messaging.Filters;
using Roxit.ZGW.Common.Web;
using Roxit.ZGW.Common.Web.Extensions.ApplicationBuilder;
using Roxit.ZGW.Common.Web.Extensions.ServiceCollection;
using Roxit.ZGW.Common.Web.Logging;
using Roxit.ZGW.Common.Web.Middleware;
using Roxit.ZGW.Common.Web.Services;
using Roxit.ZGW.Common.Web.Swagger;
using Roxit.ZGW.DataAccess;
using Roxit.ZGW.Referentielijsten.ServiceAgent.Extensions;

namespace Roxit.ZGW.Catalogi.Web;

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

        services.AddScoped<INotificatieService, NotificatieService>();
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

        services.AddAppSettingsServiceEndpoints(Configuration);

        services.AddRedisCacheInvalidation();

        services.AddScoped<IDbUserContext, DbUserContext>();

        services.AddDatabaseInitializerService<ZtcDbContext, ZtcDbContextFactory, ZtcDbSeeder>();
        services.AddMassTransitHostedService(waitUntilStarted: true);

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
