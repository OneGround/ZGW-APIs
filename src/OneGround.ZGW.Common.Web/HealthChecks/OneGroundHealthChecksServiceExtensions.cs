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

    public static IEndpointRouteBuilder MapOneGroundHealthChecks(
        this IEndpointRouteBuilder endpoints,
        Action<OneGroundHealthChecksOptions> configureOptions = null
    )
    {
        var optionsService = endpoints.ServiceProvider.GetService<IOptions<OneGroundHealthChecksOptions>>();
        if (optionsService == null)
        {
            throw new InvalidOperationException(
                $"{nameof(OneGroundHealthChecksOptions)} is not configured. Please make sure to call {nameof(AddOneGroundHealthChecks)}().${nameof(OneGroundHealthCheckBuilder.Build)}() and configure the options before using {nameof(MapOneGroundHealthChecks)}()."
            );
        }

        var options = optionsService.Value;
        configureOptions?.Invoke(options);

        endpoints.MapHealthEndpointsGroup(
            options.DetailedEndpoints,
            new HealthCheckOptions()
            {
                ResponseWriter = OneGroundHealthChecksResponseWriter.ResponseWriter,
                Predicate = x => options.RegisteredHealthChecks.Any(y => x.Name.Equals(y)),
                ResultStatusCodes = options.DetailedEndpoints.ResultStatusCode ?? options.DefaultResultStatusCode,
            }
        );

        endpoints.MapHealthEndpointsGroup(
            options.CheckEndpoints,
            new HealthCheckOptions()
            {
                Predicate = x => options.RegisteredHealthChecks.Any(y => x.Name.Equals(y)),
                ResultStatusCodes = options.CheckEndpoints.ResultStatusCode ?? options.DefaultResultStatusCode,
            }
        );

        endpoints.MapHealthEndpointsGroup(
            options.PingEndpoints,
            new HealthCheckOptions()
            {
                Predicate = _ => false,
                ResultStatusCodes = options.PingEndpoints.ResultStatusCode ?? options.DefaultResultStatusCode,
            }
        );

        return endpoints;
    }

    private static void MapHealthEndpointsGroup(
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
            else if (endpointsOptions.AuthorizationPolicies != null)
            {
                healthEndpoint.RequireAuthorization(endpointsOptions.AuthorizationPolicies);
            }
            else
            {
                healthEndpoint.RequireAuthorization();
            }
        }
    }
}
