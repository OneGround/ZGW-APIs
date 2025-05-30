using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Http.Resilience;

namespace Roxit.ZGW.Common.ServiceAgent.Configuration;

public class ResiliencePipelineOptions
{
    [Required]
    public HttpRetryStrategyOptions Retry { get; set; } = new();

    [Required]
    public HttpTimeoutStrategyOptions Timeout { get; set; } = new();
}
