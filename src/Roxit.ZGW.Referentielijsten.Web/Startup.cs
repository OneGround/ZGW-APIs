using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;
using Roxit.ZGW.Common.Batching;
using Roxit.ZGW.Common.CorrelationId;
using Roxit.ZGW.Common.Web.Extensions.ApplicationBuilder;
using Roxit.ZGW.Common.Web.Extensions.ServiceCollection;
using Roxit.ZGW.Common.Web.Logging;
using Roxit.ZGW.Common.Web.Middleware;
using Roxit.ZGW.Common.Web.Services;
using Roxit.ZGW.Common.Web.Swagger;
using Roxit.ZGW.Referentielijsten.Web.Configuration;
using Roxit.ZGW.Referentielijsten.Web.Controllers;
using Roxit.ZGW.Referentielijsten.Web.Services;

namespace Roxit.ZGW.Referentielijsten.Web;

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
    public static void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseCorrelationId();
        app.UseBatchId();

        app.ConfigureZGWApi(env, dontRegisterLogBadRequestMiddleware: true);
        app.ConfigureZGWSwagger();
    }
}
