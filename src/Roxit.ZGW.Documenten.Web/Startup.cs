using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Features;
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
using Roxit.ZGW.Documenten.DataModel;
using Roxit.ZGW.Documenten.Services;
using Roxit.ZGW.Documenten.Services.Ceph;
using Roxit.ZGW.Documenten.Services.FileSystem;
using Roxit.ZGW.Documenten.Web.BusinessRules.v1;
using Roxit.ZGW.Documenten.Web.BusinessRules.v1._5;
using Roxit.ZGW.Documenten.Web.Controllers;
using Roxit.ZGW.Documenten.Web.Expands.v1._5;
using Roxit.ZGW.Documenten.Web.Handlers.v1._5.EntityUpdaters;
using Roxit.ZGW.Documenten.Web.Handlers.v1.EntityUpdaters;
using Roxit.ZGW.Documenten.Web.Middleware;
using Roxit.ZGW.Documenten.Web.Services;
using Roxit.ZGW.Zaken.ServiceAgent.v1.Extensions;

namespace Roxit.ZGW.Documenten.Web;

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

        services.AddZGWDbContext<DrcDbContext>(Configuration);

        services.AddZGWApi(
            "Documenten",
            Configuration,
            Api.LatestVersion_1_5,
            c =>
            {
                c.MvcOptions = (o) =>
                {
                    o.AllowEmptyInputInBodyModelBinding = true;
                };

                c.SwaggerGenOptions = (o) =>
                {
                    o.OperationFilter<IfNoneMatchHeaderOperationFilter>();
                    o.OperationFilter<RemoveApiVersionHeaderOperationFilter>();
                    o.OperationFilter<ExpandQueryOperationFilter>();
                };

                c.ApiServiceSettings.RegisterSharedAudittrailHandlers = true;
            }
        );

        services.AddZGWAuditTrail<DrcDbContext>();

        services.AddAutorisatiesServiceAgent(Configuration);
        services.AddZakenServiceAgent(Configuration);
        services.AddBesluitenServiceAgent(Configuration);
        services.AddCatalogiServiceAgent(Configuration);
        services.AddCatalogiServiceAgent_v1_3(Configuration);

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
        });

        services.Configure<FormOptions>(opt =>
        {
            // NOTE: also KestrelServerLimits.MaxRequestBodySize limit is removed in Program.cs
            opt.MultipartBodyLengthLimit = Configuration.GetValue("Application:UploadLargeDocumentChunkSizeMB", defaultValue: 128) * 1024 * 1024;
        });

        services.AddCommonServices();

        services.AddZGWNummerGenerator<DrcDbContext>();

        services.AddScoped<INotificatieService, NotificatieService>();
        services.AddScoped<IDocumentServicesResolver, DocumentServicesResolver>();
        services.AddScoped<IDocumentService, FileSystemDocumentService>();
        services.AddScoped<IDocumentService, CephDocumentServices>();
        services.AddScoped<IEnkelvoudigInformatieObjectBusinessRuleService, EnkelvoudigInformatieObjectBusinessRuleService>();
        services.AddScoped<IObjectInformatieObjectBusinessRuleService, ObjectInformatieObjectBusinessRuleService>();
        services.AddScoped<IVerzendingBusinessRuleService, VerzendingBusinessRuleService>();
        services.AddTransient<IInformatieObjectAuthorizationTempTableService, InformatieObjectAuthorizationTempTableService>();

        services.AddSingleton<ILockGenerator, LockGenerator>();

        services.AddSingleton<IApiMetaData, ApiMetaData>();

        services.AddSingleton<IEntityUpdater<GebruiksRecht>, GebruiksRechtUpdater>();
        services.AddSingleton<IEntityUpdater<Verzending>, VerzendingUpdater>();

        services.AddSingleton<IDistributedCacheHelper, DistributedCacheHelper>();

        services.AddCorrelationId();
        services.AddBatchId();

        services.AddAppSettingsServiceEndpoints(Configuration);

        services.AddScoped<IDbUserContext, DbUserContext>();

        services.AddDatabaseInitializerService<DrcDbContext, DrcDbContextFactory>();
        services.AddMassTransitHostedService(waitUntilStarted: true);

        //Note: this should be AFTER all httpclients being added!
        services.Replace(ServiceDescriptor.Singleton<IHttpMessageHandlerBuilderFilter, HttpLoggingFilter>());
    }

    public static void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseCorrelationId();
        app.UseBatchId();

        app.ConfigureZGWApi(
            env,
            dontRegisterLogBadRequestMiddleware: false,
            registerMiddleware: a => a.UseMiddleware<InterceptBase64ContentMiddleware>()
        );

        app.ConfigureZGWSwagger();
    }
}
