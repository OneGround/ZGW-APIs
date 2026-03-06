using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OneGround.ZGW.Common.Handlers;
using Polly;
using Polly.Retry;

namespace OneGround.ZGW.Documenten.Web.Concurrency;

public class ResilienceConcurrencyRetryPipeline<TObjectType>
    where TObjectType : class
{
    private readonly ILogger _logger;
    private readonly IOptionsMonitor<HttpRetryStrategyOptions> _retryOptionsMonitor;

    public ResilienceConcurrencyRetryPipeline(
        ILogger<ResilienceConcurrencyRetryPipeline<TObjectType>> logger,
        IOptionsMonitor<HttpRetryStrategyOptions> retryOptionsMonitor
    )
    {
        _logger = logger;
        _retryOptionsMonitor = retryOptionsMonitor;
    }

    // Method which executes pipeline and return (null, CommandStatus.Conflict) if all retries fail due to ConcurrencyConflictException
    public async Task<(TObjectType enkelvoudiginformatieobject, CommandStatus status)> ExecuteWithResultAsync(
        Func<CancellationToken, Task<(TObjectType enkelvoudiginformatieobject, CommandStatus status)>> action,
        CancellationToken cancellationToken
    )
    {
        // Build pipeline dynamically with current configuration values
        var options = _retryOptionsMonitor.CurrentValue;

        var concurrencyRetryPipeline = new ResiliencePipelineBuilder<(TObjectType, CommandStatus)>()
            .AddRetry(
                new RetryStrategyOptions<(TObjectType, CommandStatus)>
                {
                    ShouldHandle = new PredicateBuilder<(TObjectType, CommandStatus)>().Handle<ConcurrencyConflictException>(),
                    MaxRetryAttempts = options.MaxRetryAttempts,
                    BackoffType = options.BackoffType,
                    Delay = options.Delay,
                    UseJitter = options.UseJitter,
                    OnRetry = args =>
                    {
                        _logger.LogWarning(
                            "Retry {AttemptNumber} after concurrency conflict for {ObjectType}...",
                            args.AttemptNumber + 1,
                            typeof(TObjectType).Name
                        );
                        return default;
                    },
                }
            )
            .Build();

        try
        {
            return await concurrencyRetryPipeline.ExecuteAsync(async ct => await action(ct), cancellationToken);
        }
        catch (ConcurrencyConflictException ex)
        {
            _logger.LogWarning("All retries for concurrency conflict exhausted for {ObjectType} with ID {Id}", typeof(TObjectType).Name, ex.Id);
            return (null, CommandStatus.Conflict);
        }
    }
}
