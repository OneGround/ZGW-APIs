using Hangfire;
using Hangfire.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OneGround.ZGW.Common.Authentication;
using OneGround.ZGW.Common.Configuration;
using OneGround.ZGW.Common.CorrelationId;
using OneGround.ZGW.Common.Services;

namespace OneGround.ZGW.Documenten.Jobs.Subscription;

public class ManageSubscriptionsJob : SubscriptionJobBase<ManageSubscriptionsJob>
{
    public const string JobId = "manage-subscription-job";

    private readonly IOptionsMonitor<ZgwServiceAccountConfiguration> _optionsMonitor;

    public ManageSubscriptionsJob(
        ILogger<ManageSubscriptionsJob> logger,
        ICorrelationContextAccessor correlationContextAccessor,
        IOrganisationContextAccessor organisationContextAccessor,
        IOrganisationContextFactory organisationContextFactory,
        IServiceDiscovery serviceDiscovery,
        IOptionsMonitor<ZgwServiceAccountConfiguration> optionsMonitor
    )
        : base(
            logger,
            correlationContextAccessor: correlationContextAccessor,
            organisationContextAccessor: organisationContextAccessor,
            organisationContextFactory: organisationContextFactory,
            serviceDiscovery: serviceDiscovery
        )
    {
        _optionsMonitor = optionsMonitor;
    }

    [AutomaticRetry(Attempts = 3, DelaysInSeconds = [5, 30, 120], OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    [Queue(Constants.DrcSubscriptionsQueue)]
    public Task ExecuteAsync()
    {
        _logger.LogInformation("{ManageSubscriptionsJob} job started.", nameof(ManageSubscriptionsJob));

        var currentServiceAccountCredentials = CurrentServiceAccountCredentials;

        using var connection = JobStorage.Current.GetConnection();
        var currentRecurringJobs = connection.GetRecurringJobs();

        var data = currentServiceAccountCredentials
            // 1. Add missing subscriptions for service accounts that do not have one yet
            .Select(x => new { rsin = x.Rsin, jobId = CreateOrPatchSubscriptionJob.GetJobId(x.Rsin) })
            // Ensure it was triggerred only once
            .Where(x => currentRecurringJobs.All(job => job.Id != x.jobId));

        foreach (var value in data)
        {
            _logger.LogInformation("Creating subscription job for RSIN {Rsin}", value.jobId);
            RecurringJob.AddOrUpdate<CreateOrPatchSubscriptionJob>(value.jobId, job => job.ExecuteAsync(value.rsin), Cron.Never);
            RecurringJob.TriggerJob(value.jobId);
        }

        _logger.LogInformation("{ManageSubscriptionsJob} job finished.", nameof(ManageSubscriptionsJob));
        return Task.CompletedTask;
    }

    private IEnumerable<ZgwServiceAccountCredential> CurrentServiceAccountCredentials =>
        _optionsMonitor.CurrentValue.Credentials.DistinctBy(x => new
        {
            x.Rsin,
            x.ClientId,
            x.ClientSecret,
        });
}
