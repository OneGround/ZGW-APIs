using Microsoft.Extensions.Http.Resilience;

namespace OneGround.ZGW.Common.ServiceAgent.Configuration;

public class HttpResiliencePipelineOptions
{
    public HttpRetryStrategyOptions Retry { get; set; } = HttpResiliencePipelineDefaults.RetryStrategyOptions;
    public HttpTimeoutStrategyOptions Timeout { get; set; } = HttpResiliencePipelineDefaults.TimeoutStrategyOptions;

    public string AddRetryOnHttpStatusCodes { get; set; } = "";

    public static string GetKey(string serviceName) => $"PollyConfig:{serviceName}";
}
