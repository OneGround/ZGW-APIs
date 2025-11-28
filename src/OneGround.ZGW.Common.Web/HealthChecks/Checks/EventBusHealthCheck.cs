using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace OneGround.ZGW.Common.Web.HealthChecks.Checks;

public class EventBusHealthCheck(IBusControl busControl) : IHealthCheck
{
    public const string HealthCheckName = "EventBus";

    private readonly IBusControl _busControl = busControl ?? throw new ArgumentNullException(nameof(busControl));

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var healthResult = _busControl.CheckHealth();

            var data = new Dictionary<string, object> { { "status", healthResult.Status.ToString() }, { "description", healthResult.Description } };

            var result = healthResult.Status switch
            {
                BusHealthStatus.Healthy => HealthCheckResult.Healthy("Event bus is healthy", data),
                BusHealthStatus.Degraded => HealthCheckResult.Degraded($"Event bus is degraded: {healthResult.Description}", null, data),
                BusHealthStatus.Unhealthy => HealthCheckResult.Unhealthy($"Event bus is unhealthy: {healthResult.Description}", null, data),
                _ => HealthCheckResult.Unhealthy("Event bus status is unknown", null, data),
            };

            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Event bus health check failed", ex));
        }
    }
}
