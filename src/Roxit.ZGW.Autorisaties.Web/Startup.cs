using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;
using Roxit.ZGW.Autorisaties.Common.BusinessRules;
using Roxit.ZGW.Autorisaties.DataModel;
using Roxit.ZGW.Autorisaties.Web.Controllers;
using Roxit.ZGW.Autorisaties.Web.Handlers.EntityUpdaters;
using Roxit.ZGW.Autorisaties.Web.Services;
using Roxit.ZGW.Catalogi.ServiceAgent.v1.Extensions;
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

namespace Roxit.ZGW.Autorisaties.Web;

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

        services.AddZGWDbContext<AcDbContext>(Configuration);
        services.AddZGWApi(
            "Autorisaties",
            Configuration,
            Api.LatestVersion_1_0,
            c =>
            {
                c.MvcOptions = (o) =>
                {
                    o.AllowEmptyInputInBodyModelBinding = true;
                };

                c.SwaggerGenOptions = o =>
                {
                    o.OperationFilter<IfNoneMatchHeaderOperationFilter>();
                    o.OperationFilter<RemoveApiVersionHeaderOperationFilter>();
                    o.OperationFilter<ExpandQueryOperationFilter>();
                };

                c.ApiServiceSettings.RegisterSharedAudittrailHandlers = false;
            }
        );

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

        services.AddCatalogiServiceAgent(Configuration);

        services.AddSingleton<IApiMetaData, ApiMetaData>();

        services.AddSingleton<IEntityUpdater<Applicatie>, ApplicatieUpdater>();

        services.AddScoped<INotificatieService, NotificatieService>();
        services.AddScoped<IApplicatieBusinessRuleService, ApplicatieBusinessRuleService>();

        services.AddAppSettingsServiceEndpoints(Configuration);

        services.AddRedisCacheInvalidation();

        services.AddCorrelationId();
        services.AddBatchId();

        services.AddScoped<IDbUserContext, DbUserContext>();

        services.AddDatabaseInitializerService<AcDbContext, AcDbContextFactory, AcDbSeeder>();
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
