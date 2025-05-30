using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;
using Roxit.ZGW.Autorisaties.ServiceAgent.Extensions;
using Roxit.ZGW.Besluiten.ServiceAgent.v1.Extensions;
using Roxit.ZGW.Catalogi.ServiceAgent.v1._3.Extensions;
using Roxit.ZGW.Catalogi.ServiceAgent.v1.Extensions;
using Roxit.ZGW.Common.Batching;
using Roxit.ZGW.Common.Caching;
using Roxit.ZGW.Common.CorrelationId;
using Roxit.ZGW.Common.Extensions;
using Roxit.ZGW.Common.JsonConverters;
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
using Roxit.ZGW.Documenten.Messaging.Contracts;
using Roxit.ZGW.Documenten.ServiceAgent.v1._5.Extensions;
using Roxit.ZGW.Documenten.ServiceAgent.v1.Extensions;
using Roxit.ZGW.Notificaties.ServiceAgent.Extensions;
using Roxit.ZGW.Zaken.DataModel;
using Roxit.ZGW.Zaken.DataModel.ZaakObject;
using Roxit.ZGW.Zaken.ServiceAgent.v1.Extensions;
using Roxit.ZGW.Zaken.Web.BusinessRules;
using Roxit.ZGW.Zaken.Web.Controllers;
using Roxit.ZGW.Zaken.Web.Expands.v1._5;
using Roxit.ZGW.Zaken.Web.Handlers.v1._2.EntityUpdaters;
using Roxit.ZGW.Zaken.Web.Handlers.v1.EntityUpdaters;
using Roxit.ZGW.Zaken.Web.Services;
using Roxit.ZGW.Zaken.Web.Validators.v1.ZaakObject;

namespace Roxit.ZGW.Zaken.Web;

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

        services.AddZGWAuditTrail<ZrcDbContext>();

        services.AddNotificatiesServiceAgent(Configuration);
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
            x.AddRequestClient<IAddObjectInformatieObject>();
            x.AddRequestClient<IDeleteObjectInformatieObject>();
        });

        services.AddCommonServices();

        services.AddZGWNummerGenerator<ZrcDbContext>();

        services.AddScoped<INotificatieService, NotificatieService>();
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

        services.AddCorrelationId();
        services.AddBatchId();

        services.AddAppSettingsServiceEndpoints(Configuration);

        services.AddScoped<IDbUserContext, DbUserContext>();

        services.AddDatabaseInitializerService<ZrcDbContext, ZrcDbContextFactory, ZrcDbSeeder>();
        services.AddMassTransitHostedService(waitUntilStarted: true);

        //Note: this should be AFTER all httpclients being added!
        services.Replace(ServiceDescriptor.Singleton<IHttpMessageHandlerBuilderFilter, HttpLoggingFilter>());
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public static void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseCorrelationId();
        app.UseBatchId();

        app.ConfigureZGWApi(env);
        app.ConfigureZGWSwagger();
    }
}
