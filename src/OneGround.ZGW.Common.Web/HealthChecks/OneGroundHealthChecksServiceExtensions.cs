using System;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OneGround.ZGW.Common.Web.HealthChecks.Builder;
using OneGround.ZGW.Common.Web.HealthChecks.Options;

namespace OneGround.ZGW.Common.Web.HealthChecks;

public static class OneGroundHealthChecksServiceExtensions
{
    public static OneGroundHealthCheckBuilder AddOneGroundHealthChecks(this IServiceCollection services)
    {
        var healthChecksBuilder = services.AddHealthChecks();
        return new OneGroundHealthCheckBuilder(services, healthChecksBuilder);
    }

    private static void RegisterEndpoints(this WebApplication app, OneGroundHealthChecksEndpointOptions endpointsOptions, HealthCheckOptions options)
    {
        foreach (var endpoint in endpointsOptions.Endpoints)
        {
            var healthEndpoint = app.MapHealthChecks(endpoint, options);

            if (!endpointsOptions.RequireAuthorization)
            {
                healthEndpoint.AllowAnonymous();
            }
            else if (endpointsOptions.AuthorizationSchema != null)
            {
                healthEndpoint.RequireAuthorization(endpointsOptions.AuthorizationSchema);
            }
            else
            {
                healthEndpoint.RequireAuthorization();
            }
        }
    }

    public static WebApplication UseOneGroundHealthChecks(this WebApplication app)
    {
        var optionsService = app.Services.GetService<IOptions<OneGroundHealthChecksOptions>>();
        if (optionsService == null)
        {
            throw new InvalidOperationException("OneGroundHealthChecksOptions is not configured. Please make sure to call AddOneGroundHealthChecks().Build() and configure the options before using UseOneGroundHealthChecks.");
        }
        var options = optionsService.Value;

        app.RegisterEndpoints(
            options.DetailedEndpoints,
            new HealthCheckOptions()
            {
                ResponseWriter = OneGroundHealthChecksResponseWriter.ResponseWriter,
                Predicate = x => options.RegisteredHealthChecks.Any(y => x.Name.Equals(y)),
                ResultStatusCodes = options.DetailedEndpoints.ResultStatusCode ?? options.DefaultResultStatusCode,
            }
        );

        app.RegisterEndpoints(
            options.CheckEndpoints,
            new HealthCheckOptions()
            {
                Predicate = x => options.RegisteredHealthChecks.Any(y => x.Name.Equals(y)),
                ResultStatusCodes = options.CheckEndpoints.ResultStatusCode ?? options.DefaultResultStatusCode,
            }
        );

        app.RegisterEndpoints(
            options.PingEndpoints,
            new HealthCheckOptions()
            {
                Predicate = _ => false,
                ResultStatusCodes = options.PingEndpoints.ResultStatusCode ?? options.DefaultResultStatusCode,
            }
        );

        return app;
    }
}
