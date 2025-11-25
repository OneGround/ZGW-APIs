using System.Collections.Generic;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace OneGround.ZGW.Common.Web.HealthChecks.Models;

public class OneGroundHealthChecksResult
{
    public required HealthStatus Status { get; set; }
    public string Impact { get; set; } = null;
    public string Reason { get; set; } = null;
    public List<OneGroundHealthChecksDetail> Details { get; set; } = [];
}
