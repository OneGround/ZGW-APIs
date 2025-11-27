using System.Collections.Generic;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace OneGround.ZGW.Common.Web.HealthChecks.Options;

public class OneGroundHealthChecksEndpointOptions
{
    public List<string> Endpoints { get; set; } = [];
    public bool RequireAuthorization { get; set; } = false;
    public string[] AuthorizationPolicies { get; set; } = null;
    public Dictionary<HealthStatus, int> ResultStatusCode = null;
}

public class OneGroundHealthChecksOptions
{
    internal List<string> RegisteredHealthChecks { get; set; } = [];

    public OneGroundHealthChecksEndpointOptions ReadyEndpoints { get; set; } =
        new()
        {
            Endpoints = ["/health/ready"],
            RequireAuthorization = false,
            AuthorizationPolicies = null,
        };

    public OneGroundHealthChecksEndpointOptions CheckEndpoints { get; set; } =
        new()
        {
            Endpoints = ["/health/check"],
            RequireAuthorization = false,
            AuthorizationPolicies = null,
        };

    public OneGroundHealthChecksEndpointOptions PingEndpoints { get; set; } =
        new()
        {
            Endpoints = ["/health"],
            RequireAuthorization = false,
            AuthorizationPolicies = null,
        };

    public Dictionary<HealthStatus, int> DefaultResultStatusCode = new()
    {
        [HealthStatus.Healthy] = 200,
        [HealthStatus.Degraded] = 207,
        [HealthStatus.Unhealthy] = 503,
    };
}
