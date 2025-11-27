using Hangfire;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Common.Authentication;
using OneGround.ZGW.Common.CorrelationId;
using OneGround.ZGW.Common.Services;
using OneGround.ZGW.Notificaties.ServiceAgent;

namespace OneGround.ZGW.Documenten.Jobs.Subscription;

public class DeleteSubscriptionJob : SubscriptionJobBase<DeleteSubscriptionJob>
{
    private readonly INotificatiesServiceAgent _notificatieServiceAgent;

    public DeleteSubscriptionJob(
        ILogger<DeleteSubscriptionJob> logger,
        ICorrelationContextAccessor correlationContextAccessor,
        IOrganisationContextAccessor organisationContextAccessor,
        IOrganisationContextFactory organisationContextFactory,
        INotificatiesServiceAgent notificatieServiceAgent,
        IServiceDiscovery serviceDiscovery
    )
        : base(
            logger,
            organisationContextFactory: organisationContextFactory,
            correlationContextAccessor: correlationContextAccessor,
            organisationContextAccessor: organisationContextAccessor,
            serviceDiscovery: serviceDiscovery
        )
    {
        _notificatieServiceAgent = notificatieServiceAgent;
    }

    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 5, 30, 120 }, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    [Queue(Constants.DrcSubscriptionsQueue)]
    public async Task ExecuteAsync(string rsin)
    {
        ArgumentNullException.ThrowIfNull(rsin, nameof(rsin));

        _logger.LogInformation("{DeleteSubscriptionJob} job started.", nameof(DeleteSubscriptionJob));

        _correlationContextAccessor.SetCorrelationId(Guid.NewGuid().ToString());
        _organisationContextAccessor.OrganisationContext = _organisationContextFactory.Create(rsin);

        using (GetLoggingScope(rsin, _correlationContextAccessor.CorrelationId))
        {
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

        _logger.LogInformation("{DeleteSubscriptionJob} job finished.", nameof(DeleteSubscriptionJob));
    }
}
