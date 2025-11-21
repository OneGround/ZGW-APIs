using Hangfire;
using Hangfire.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OneGround.ZGW.Common.Authentication;
using OneGround.ZGW.Common.Configuration;
using OneGround.ZGW.Common.CorrelationId;
using OneGround.ZGW.Common.Services;
using OneGround.ZGW.Notificaties.ServiceAgent;

namespace Roxit.ZGW.Documenten.Jobs.Subscription;

public class ManageSubscriptionsJob : SubscriptionJobBase<ManageSubscriptionsJob>
{
    private readonly INotificatiesServiceAgent _notificatieServiceAgent;
    private readonly IOptionsMonitor<ZgwServiceAccountConfiguration> _optionsMonitor;

    public ManageSubscriptionsJob(
        ILogger<ManageSubscriptionsJob> logger,
        ICorrelationContextAccessor correlationContextAccessor,
        IOrganisationContextAccessor organisationContextAccessor,
        IOrganisationContextFactory organisationContextFactory,
        IServiceDiscovery serviceDiscovery,
        INotificatiesServiceAgent notificatieServiceAgent,
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
        _notificatieServiceAgent = notificatieServiceAgent;
        _optionsMonitor = optionsMonitor;
    }

    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 5, 30, 120 }, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    [Queue(Constants.DrcSubscriptionsQueue)]
    public async Task ExecuteAsync()
    {
        _logger.LogInformation("{ManageSubscriptionsJob} job started.", nameof(ManageSubscriptionsJob));

        var currentServiceAccountCredentials = CurrentServiceAccountCredentials;
        var currentRecurringHangfireTokenRefreshJobs = CurrentRecurringHangfireTokenRefreshJobs;

        // 1. Add mising subscriptions for service accounts that do not have one yet
        foreach (var serviceaccount in currentServiceAccountCredentials)
        {
            if (!currentRecurringHangfireTokenRefreshJobs.Any(job => job.Id == $"create-or-patch-subscription-{serviceaccount.Rsin}-job"))
            {
                BackgroundJob.Enqueue<CreateOrPatchSubscriptionJob>(job => job.ExecuteAsync(serviceaccount.Rsin));
            }
        }

        // 2. Remove subscriptions for service accounts that are no longer configured
        foreach (var recurringJob in currentRecurringHangfireTokenRefreshJobs)
        {
            if (recurringJob.Id.StartsWith("create-or-patch-subscription-"))
            {
                var rsin = recurringJob.Id.Replace("create-or-patch-subscription-", "").Replace("-job", "");
                if (!currentServiceAccountCredentials.Any(c => c.Rsin == rsin))
                {
                    RecurringJob.RemoveIfExists(recurringJob.Id);
                }

                // TODO: Bot TESTED yet
                _correlationContextAccessor.SetCorrelationId(Guid.NewGuid().ToString());
                _organisationContextAccessor.OrganisationContext = _organisationContextFactory.Create(rsin);

                var result = await _notificatieServiceAgent.GetAllAbonnementenAsync();
                if (result.Success)
                {
                    var organisationSubscribers = result.Response.Where(a => a.Owner == rsin && IsDocumentListenerSubscription(a.CallbackUrl));
                    foreach (var subscriber in organisationSubscribers)
                    {
                        var deleteResult = await _notificatieServiceAgent.DeleteAbonnementByIdAsync(subscriber.Id);
                        if (!deleteResult.Success)
                        {
                            throw new InvalidOperationException(
                                $"Failed to delete subscription in NRC API for Rsin {rsin}. Error: {deleteResult.Error.Title}"
                            );
                        }
                    }
                }
            }
        }

        _logger.LogInformation("{ManageSubscriptionsJob} job finished.", nameof(ManageSubscriptionsJob));
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
