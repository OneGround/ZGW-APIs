using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;
using OneGround.ZGW.Autorisaties.ServiceAgent.Extensions;
using OneGround.ZGW.Common.Batching;
using OneGround.ZGW.Common.CorrelationId;
using OneGround.ZGW.Common.Extensions;
using OneGround.ZGW.Common.Web;
using OneGround.ZGW.Common.Web.Extensions.ApplicationBuilder;
using OneGround.ZGW.Common.Web.Extensions.ServiceCollection;
using OneGround.ZGW.Common.Web.HealthChecks;
using OneGround.ZGW.Common.Web.Logging;
using OneGround.ZGW.Common.Web.Middleware;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.DataAccess;
using OneGround.ZGW.Gesprekken.DataModel;
using OneGround.ZGW.Gesprekken.Web.Afsluiten;
using OneGround.ZGW.Gesprekken.Web.Bestanden;
using OneGround.ZGW.Gesprekken.Web.BusinessRules;
using OneGround.ZGW.Gesprekken.Web.Controllers;
using OneGround.ZGW.Gesprekken.Web.Events;
using OneGround.ZGW.Gesprekken.Web.Extensions;
using OneGround.ZGW.Zaken.ServiceAgent.v1.Extensions;

namespace OneGround.ZGW.Gesprekken.Web;

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    private IConfiguration Configuration { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(Serilog.Log.Logger);

        services.AddZGWDbContext<GrcDbContext>(Configuration);

        services.AddZGWApi(
            "Gesprekken",
            Configuration,
            Api.LatestVersion_1_0,
            c =>
            {
                c.MvcOptions = o =>
                {
                    o.AllowEmptyInputInBodyModelBinding = true;
                };
            }
        );

        services.AddOneGroundHealthChecks().AddRedisCheck();

        services.AddAutorisatiesServiceAgent(Configuration);
        services.AddZakenServiceAgent(Configuration);

        services.AddCommonServices();

        services.AddZGWNummerGenerator<GrcDbContext>();

        services.AddScoped<IGesprekBusinessRuleService, GesprekBusinessRuleService>();
        services.AddScoped<IGesprekEventLog, GesprekEventLog>();
        services.AddScoped<IGesprekEventReader, GesprekEventReader>();
        services.AddHttpClient<IKlantcontactService, KlantinteractiesKlantcontactService>();
        services.AddScoped<IGesprekAfsluiter, GesprekAfsluiter>();
        services.AddScoped<IAutomatischAntwoord, AutomatischAntwoord>();
        services.AddSingleton<IBestandOpslag, S3BestandOpslag>();
        services.AddScoped<IBestandService, BestandService>();
        services.AddSingleton<AutomatischSluitenService>();
        services.AddHostedService(provider => provider.GetRequiredService<AutomatischSluitenService>());

        services.AddSingleton<IApiMetaData, ApiMetaData>();

        services.AddCorrelationId();
        services.AddBatchId();

        services.AddServiceEndpoints(Configuration);

        services.AddScoped<IDbUserContext, DbUserContext>();

        services.AddDatabaseInitializerService<GrcDbContext, GrcDbContextFactory>();

        //Note: this should be AFTER all httpclients being added!
        services.Replace(ServiceDescriptor.Singleton<IHttpMessageHandlerBuilderFilter, HttpLoggingFilter>());

        services.AddQueryParameterValidations();
    }

    public static void Configure(WebApplication app, IWebHostEnvironment env)
    {
        app.UseCorrelationId();
        app.UseBatchId();

        app.ConfigureZgwApi(env);
        app.ConfigureZgwSwagger();
        app.MapOneGroundHealthChecks();
    }
}
