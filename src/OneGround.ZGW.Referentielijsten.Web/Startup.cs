using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;
using OneGround.ZGW.Common.Batching;
using OneGround.ZGW.Common.CorrelationId;
using OneGround.ZGW.Common.Web.Extensions.ApplicationBuilder;
using OneGround.ZGW.Common.Web.Extensions.ServiceCollection;
using OneGround.ZGW.Common.Web.HealthChecks;
using OneGround.ZGW.Common.Web.Logging;
using OneGround.ZGW.Common.Web.Middleware;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Swagger;
using OneGround.ZGW.Referentielijsten.Web.Configuration;
using OneGround.ZGW.Referentielijsten.Web.Controllers;
using OneGround.ZGW.Referentielijsten.Web.Services;

namespace OneGround.ZGW.Referentielijsten.Web;

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    private IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(Serilog.Log.Logger);

        services.AddZGWApi(
            "Referentielijsten",
            Configuration,
            Api.LatestVersion_1_0,
            c =>
            {
                c.SwaggerGenOptions = (o) =>
                {
                    o.OperationFilter<RemoveApiVersionHeaderOperationFilter>();
                };

                c.ApiServiceSettings.RegisterSharedAudittrailHandlers = false;
            }
        );

        services
            .AddOneGroundHealthChecks()
            .AddRedisCheck()
            .Build();

        services
            .AddOptions<ApplicationConfiguration>()
            .Bind(Configuration.GetSection(ApplicationConfiguration.ApplicationConfig))
            .ValidateDataAnnotations();

        services.AddCommonServices();

        services.AddSingleton<ReferentielijstenDataService>();

        services.AddSingleton<IApiMetaData, ApiMetaData>();

        services.AddCorrelationId();
        services.AddBatchId();

        //Note: this should be AFTER all httpclients being added!
        services.Replace(ServiceDescriptor.Singleton<IHttpMessageHandlerBuilderFilter, HttpLoggingFilter>());
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public static void Configure(WebApplication app, IWebHostEnvironment env)
    {
        app.UseCorrelationId();
        app.UseBatchId();

        app.ConfigureZgwApi(env, dontRegisterLogBadRequestMiddleware: true);
        app.ConfigureZgwSwagger();
        app.MapOneGroundHealthChecks(c =>
        {
            // Note: backwards compatibility with the old health check endpoint
            c.PingEndpoints.Endpoints.Add("/health");
        });
    }
}
