using Dapper;
using Hangfire;
using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;
using OneGround.ZGW.Autorisaties.ServiceAgent.Extensions;
using OneGround.ZGW.Common.Batching;
using OneGround.ZGW.Common.Converters;
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
using OneGround.ZGW.Notificaties.DataModel;
using OneGround.ZGW.Notificaties.Messaging.Jobs.Extensions;
using OneGround.ZGW.Notificaties.Web.Controllers;
using OneGround.ZGW.Notificaties.Web.Services;

namespace OneGround.ZGW.Notificaties.Web;

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

        services.AddZGWDbContext<NrcDbContext>(Configuration);

        services.AddZGWApi(
            "Notificaties",
            Configuration,
            Api.LatestVersion_1_0,
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

                c.ApiServiceSettings.RegisterSharedAudittrailHandlers = false;
            }
        );

        services.AddAutorisatiesServiceAgent(Configuration);

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

        services.AddScoped<INotificatieService, NotificatieService>();

        services.AddCorrelationId();
        services.AddBatchId();

        services.AddServiceEndpoints(Configuration);
        services.AddSingleton<IApiMetaData, ApiMetaData>();

        services.AddScoped<IDbUserContext, DbUserContext>();
        services.AddDatabaseInitializerService<NrcDbContext, NrcDbContextFactory, NrcDbSeeder>();

        services.Configure<MassTransitHostOptions>(options =>
        {
            options.WaitUntilStarted = true;
        });

#if DEBUG
        services.AddHangfire(
            (_s, _o) => {
                //Note: Adding Hangfire for Dashboard only
            }
        );
        services.AddNotificatiesJobs(o => o.ConnectionString = Configuration.GetConnectionString("UserConnectionString"));
#endif
        SqlMapper.AddTypeHandler(new LocalDateTypeHandler());

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

#if DEBUG
        app.UseNotificatiesHangfireDashboard();
#endif
    }
}
