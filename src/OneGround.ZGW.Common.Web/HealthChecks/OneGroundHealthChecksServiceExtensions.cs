using System;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Routing;
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

    private static void RegisterHealthEndpoints(
        this IEndpointRouteBuilder endpoints,
        OneGroundHealthChecksEndpointOptions endpointsOptions,
        HealthCheckOptions options
    )
    {
        foreach (var endpoint in endpointsOptions.Endpoints)
        {
            var healthEndpoint = endpoints.MapHealthChecks(endpoint, options);

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

    public static IEndpointRouteBuilder UseOneGroundHealthChecks(this IEndpointRouteBuilder endpoints)
    {
        var optionsService = endpoints.ServiceProvider.GetService<IOptions<OneGroundHealthChecksOptions>>();
        if (optionsService == null)
        {
            throw new InvalidOperationException(
                "OneGroundHealthChecksOptions is not configured. Please make sure to call AddOneGroundHealthChecks().Build() and configure the options before using UseOneGroundHealthChecks."
            );
        }
        var options = optionsService.Value;

        endpoints.RegisterHealthEndpoints(
            options.DetailedEndpoints,
            new HealthCheckOptions()
            {
                ResponseWriter = OneGroundHealthChecksResponseWriter.ResponseWriter,
                Predicate = x => options.RegisteredHealthChecks.Any(y => x.Name.Equals(y)),
                ResultStatusCodes = options.DetailedEndpoints.ResultStatusCode ?? options.DefaultResultStatusCode,
            }
        );

        endpoints.RegisterHealthEndpoints(
            options.CheckEndpoints,
            new HealthCheckOptions()
            {
                Predicate = x => options.RegisteredHealthChecks.Any(y => x.Name.Equals(y)),
                ResultStatusCodes = options.CheckEndpoints.ResultStatusCode ?? options.DefaultResultStatusCode,
            }
        );

        endpoints.RegisterHealthEndpoints(
            options.PingEndpoints,
            new HealthCheckOptions()
            {
                Predicate = _ => false,
                ResultStatusCodes = options.PingEndpoints.ResultStatusCode ?? options.DefaultResultStatusCode,
            }
        );

        return endpoints;
    }
}
