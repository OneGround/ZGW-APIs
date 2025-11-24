using Hangfire;
using Hangfire.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OneGround.ZGW.Common.Authentication;
using OneGround.ZGW.Common.Configuration;
using OneGround.ZGW.Common.CorrelationId;
using OneGround.ZGW.Common.Services;

namespace Roxit.ZGW.Documenten.Jobs.Subscription;

public class ManageSubscriptionsJob : SubscriptionJobBase<ManageSubscriptionsJob>
{
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

    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 5, 30, 120 }, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    [Queue(Constants.DrcSubscriptionsQueue)]
    public Task ExecuteAsync()
    {
        _logger.LogInformation("{ManageSubscriptionsJob} job started.", nameof(ManageSubscriptionsJob));

        var currentServiceAccountCredentials = CurrentServiceAccountCredentials;
        var currentRecurringHangfireTokenRefreshJobs = CurrentRecurringHangfireTokenRefreshJobs;

        // 1. Add missing subscriptions for service accounts that do not have one yet
        foreach (var serviceaccount in currentServiceAccountCredentials)
        {
            if (!currentRecurringHangfireTokenRefreshJobs.Any(job => job.Id == $"create-or-patch-subscription-{serviceaccount.Rsin}-job"))
            {
                BackgroundJob.Enqueue<CreateOrPatchSubscriptionJob>(job => job.ExecuteAsync(serviceaccount.Rsin));
            }
        }

        _logger.LogInformation("{ManageSubscriptionsJob} job finished.", nameof(ManageSubscriptionsJob));
        return Task.CompletedTask;
    }

    private static List<RecurringJobDto> CurrentRecurringHangfireTokenRefreshJobs => JobStorage.Current.GetConnection().GetRecurringJobs();

    private IEnumerable<ZgwServiceAccountCredential> CurrentServiceAccountCredentials =>
        _optionsMonitor.CurrentValue.Credentials.DistinctBy(x => new
        {
            x.Rsin,
            x.ClientId,
            x.ClientSecret,
        });
}
