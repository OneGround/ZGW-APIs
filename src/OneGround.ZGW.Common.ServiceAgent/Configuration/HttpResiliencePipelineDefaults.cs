using System;
using Microsoft.Extensions.Http.Resilience;
using Polly;

namespace OneGround.ZGW.Common.ServiceAgent.Configuration;

public static class HttpResiliencePipelineDefaults
{
    public static readonly HttpRetryStrategyOptions RetryStrategyOptions = new()
    {
        MaxRetryAttempts = 3,
        BackoffType = DelayBackoffType.Exponential,
        Delay = TimeSpan.FromSeconds(2),
    };

    public static readonly HttpTimeoutStrategyOptions TimeoutStrategyOptions = new() { Timeout = TimeSpan.FromSeconds(30) };
}
