using System.ComponentModel.DataAnnotations;
using System.Net;
using Microsoft.Extensions.Http.Resilience;

namespace Roxit.ZGW.Notificaties.Messaging.Configuration;

public class ResiliencePipelineOptions
{
    [Required]
    public HttpRetryStrategyOptions Retry { get; set; } = new();

    [Required]
    public HttpTimeoutStrategyOptions Timeout { get; set; } = new();

    public IEnumerable<HttpStatusCode> AddRetryOnHttpStatusCodes { get; set; } = [];
}
