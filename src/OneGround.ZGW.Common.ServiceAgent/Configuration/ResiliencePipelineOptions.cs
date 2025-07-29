using Microsoft.Extensions.Http.Resilience;

namespace OneGround.ZGW.Common.ServiceAgent.Configuration;

public class ResiliencePipelineOptions
{
    public required HttpRetryStrategyOptions Retry { get; set; } = ResiliencePipelineDefaults.RetryStrategyOptions;
    public required HttpTimeoutStrategyOptions Timeout { get; set; } = ResiliencePipelineDefaults.TimeoutStrategyOptions;
}
