using Microsoft.Extensions.Http.Resilience;

namespace OneGround.ZGW.Common.ServiceAgent.Configuration;

public class ResiliencePipelineOptions
{
    public HttpRetryStrategyOptions Retry { get; set; } = ResiliencePipelineDefaults.RetryStrategyOptions;
    public HttpTimeoutStrategyOptions Timeout { get; set; } = ResiliencePipelineDefaults.TimeoutStrategyOptions;
}
