using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Common.Handlers;
using Polly;
using Polly.Retry;

namespace OneGround.ZGW.Documenten.Web.Concurrency;

public class ResilienceConcurrencyRetryPipeline<TObjectType>
    where TObjectType : class
{
    private readonly ILogger _logger;

    private readonly ResiliencePipeline<(TObjectType enkelvoudiginformatieobject, CommandStatus status)> _concurrencyRetryPipeline;

    public ResilienceConcurrencyRetryPipeline(ILogger<ResilienceConcurrencyRetryPipeline<TObjectType>> logger, IConfiguration configuration)
    {
        _logger = logger;

        var options =
            configuration.GetSection("PollyConfig:ConcurrencyConflict:Retry").Get<HttpRetryStrategyOptions>() ?? new HttpRetryStrategyOptions();

        _concurrencyRetryPipeline = new ResiliencePipelineBuilder<(TObjectType, CommandStatus)>()
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
    }

    // Method which executes pipeline and return (null, CommandStatus.Conflict) if all retries fail due to ConcurrencyConflictException
    public async Task<(TObjectType enkelvoudiginformatieobject, CommandStatus status)> ExecuteWithResultAsync(
        Func<CancellationToken, Task<(TObjectType enkelvoudiginformatieobject, CommandStatus status)>> action,
        CancellationToken cancellationToken
    )
    {
        try
        {
            return await _concurrencyRetryPipeline.ExecuteAsync(async ct => await action(ct), cancellationToken);
        }
        catch (ConcurrencyConflictException ex)
        {
            _logger.LogWarning("All retries for concurrency conflict exhausted for {ObjectType} with ID {Id}", typeof(TObjectType).Name, ex.Id);
            return (null, CommandStatus.Conflict);
        }
    }
}
