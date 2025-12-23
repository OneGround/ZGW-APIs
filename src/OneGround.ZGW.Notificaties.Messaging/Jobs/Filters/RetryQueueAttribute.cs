using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;

namespace OneGround.ZGW.Notificaties.Messaging.Jobs.Filters;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class RetryQueueAttribute : JobFilterAttribute, IApplyStateFilter, IElectStateFilter
{
    private const string RetryCountKey = "RetryAttempt";
    private readonly string _retryQueue;

    public RetryQueueAttribute(string retryQueue)
    {
        _retryQueue = retryQueue;
    }

    void IApplyStateFilter.OnStateApplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
    {
        // Only when the job actually goes into a queue
        if (context.NewState is not EnqueuedState enqueued)
            return;

        // Only for retries (not first run)
        var retryCount = context.GetJobParameter<int>(RetryCountKey);
        if (retryCount <= 0)
            return;

        // This is the ONLY time the queue may be adjusted
        enqueued.Queue = _retryQueue;
    }

    void IApplyStateFilter.OnStateUnapplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
    {
        // Not needed for this implementation
    }

    void IElectStateFilter.OnStateElection(ElectStateContext context)
    {
        var failedState = context.CandidateState as FailedState;
        if (failedState == null)
        {
            // Not a failure, don't intervene
            return;
        }

        // Get current retry attempt count (starts at 0)
        var retryAttempt = context.GetJobParameter<int>(RetryCountKey);

        // Get from current job the max attempts querying AutomaticRetryAttribute filter
        var retryQueueFilter =
            JobFilterProviders
                .Providers.SelectMany(p => p.GetFilters(context.BackgroundJob.Job))
                .Where(f => f.Instance is AutomaticRetryAttribute)
                .Select(f => new
                {
                    Filter = (AutomaticRetryAttribute)f.Instance,
                    f.Scope,
                    f.Order,
                })
                .SingleOrDefault()
            ?? throw new InvalidOperationException($"{nameof(AutomaticRetryAttribute)} not found on current job ({context.BackgroundJob.Id}).");

        // Check if we've exceeded max retries
        if (retryAttempt >= retryQueueFilter.Filter.Attempts)
        {
            // Let it fail - max retries exceeded
            return;
        }

        context.SetJobParameter(RetryCountKey, retryAttempt + 1);
    }
}
