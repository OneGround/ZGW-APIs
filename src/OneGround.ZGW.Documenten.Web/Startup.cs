using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Features;
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
using OneGround.ZGW.Documenten.DataModel;
using OneGround.ZGW.Documenten.Services;
using OneGround.ZGW.Documenten.Services.Ceph;
using OneGround.ZGW.Documenten.Services.FileSystem;
using OneGround.ZGW.Documenten.Web.BusinessRules.v1;
using OneGround.ZGW.Documenten.Web.BusinessRules.v1._5;
using OneGround.ZGW.Documenten.Web.Controllers;
using OneGround.ZGW.Documenten.Web.Expands.v1._5;
using OneGround.ZGW.Documenten.Web.Handlers.v1._5.EntityUpdaters;
using OneGround.ZGW.Documenten.Web.Handlers.v1.EntityUpdaters;
using OneGround.ZGW.Documenten.Web.Middleware;
using OneGround.ZGW.Documenten.Web.Services;
using OneGround.ZGW.Zaken.ServiceAgent.v1.Extensions;

namespace OneGround.ZGW.Documenten.Web;

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

        app.ConfigureZGWApi(
            env,
            dontRegisterLogBadRequestMiddleware: false,
            registerMiddleware: a => a.UseMiddleware<InterceptBase64ContentMiddleware>()
        );

        app.ConfigureZGWSwagger();
    }
}
