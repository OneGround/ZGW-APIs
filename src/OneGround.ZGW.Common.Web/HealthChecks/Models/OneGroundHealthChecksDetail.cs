using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace OneGround.ZGW.Common.Web.HealthChecks.Models;

public class OneGroundHealthChecksDetail
{
    public required string Name { get; set; }
    public required HealthStatus Status { get; set; }
    public string Reason { get; set; } = string.Empty;
}
