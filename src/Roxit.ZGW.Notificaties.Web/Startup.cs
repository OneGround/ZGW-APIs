using Dapper;
using Hangfire;
using Hangfire.PostgreSql;
using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;
using Roxit.ZGW.Autorisaties.ServiceAgent.Extensions;
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
using Roxit.ZGW.Notificaties.DataModel;
using Roxit.ZGW.Notificaties.Messaging;
using Roxit.ZGW.Notificaties.Web.Controllers;
using Roxit.ZGW.Notificaties.Web.Converters;
using Roxit.ZGW.Notificaties.Web.Services;

namespace Roxit.ZGW.Notificaties.Web;

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

        services.AddAppSettingsServiceEndpoints(Configuration);
        services.AddSingleton<IApiMetaData, ApiMetaData>();

        services.AddScoped<IDbUserContext, DbUserContext>();

        services.AddDatabaseInitializerService<NrcDbContext, NrcDbContextFactory, NrcDbSeeder>();
        services.AddMassTransitHostedService(waitUntilStarted: true);

        services.AddHangfire(
            (_, options) =>
                options.UsePostgreSqlStorage(
                    o => o.UseNpgsqlConnection(Configuration.GetConnectionString("UserConnectionString")),
                    new PostgreSqlStorageOptions
                    {
                        PrepareSchemaIfNecessary = false, // Schema will be prepared in Roxit.ZGW.Notificaties.Messaging.Listener project
                    }
                )
        );

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
        var _ = new NotificatieJob(null, null);
        app.UseHangfireDashboard();
#endif
    }
}
