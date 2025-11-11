using Hangfire;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using OneGround.ZGW.Common.Authentication;
using OneGround.ZGW.Common.CorrelationId;
using OneGround.ZGW.Common.Helpers;
using OneGround.ZGW.Common.ServiceAgent.Extensions;
using OneGround.ZGW.Common.Services;
using OneGround.ZGW.Notificaties.Contracts.v1;
using OneGround.ZGW.Notificaties.ServiceAgent;
using Roxit.ZGW.Internal.Common.Consul.Secrets.ConsulAuthManager;

namespace Roxit.ZGW.Documenten.Jobs.Subscription;

public class CreateOrPatchSubscriptionJob : SubscriptionJobBase<CreateOrPatchSubscriptionJob>
{
    private const int ExpiresMinutesBefore = 10;

    private readonly IZgwAuthManager _zgwAuthManager;
    private readonly IZgwTokenService _zgwTokenService;
    private readonly INotificatiesServiceAgent _notificatieServiceAgent;

    public CreateOrPatchSubscriptionJob(
        ILogger<CreateOrPatchSubscriptionJob> logger,
        ICorrelationContextAccessor correlationContextAccessor,
        IOrganisationContextAccessor organisationContextAccessor,
        IOrganisationContextFactory organisationContextFactory,
        INotificatiesServiceAgent notificatieServiceAgent,
        IServiceDiscovery serviceDiscovery,
        IZgwAuthManager zgwAuthManager,
        IZgwTokenService zgwTokenService
    )
        : base(
            logger,
            correlationContextAccessor: correlationContextAccessor,
            organisationContextAccessor: organisationContextAccessor,
            organisationContextFactory: organisationContextFactory,
            serviceDiscovery: serviceDiscovery
        )
    {
        _zgwAuthManager = zgwAuthManager;
        _zgwTokenService = zgwTokenService;
        _notificatieServiceAgent = notificatieServiceAgent;
    }

    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 5, 30, 120 }, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    [Queue(Constants.DrcSubscriptionsQueue)]
    public async Task ExecuteAsync(string rsin)
    {
        ArgumentNullException.ThrowIfNull(rsin, nameof(rsin));

        _logger.LogInformation("{CreateOrPatchSubscriptionJob} job started.", nameof(CreateOrPatchSubscriptionJob));

        _correlationContextAccessor.SetCorrelationId(Guid.NewGuid().ToString());
        _organisationContextAccessor.OrganisationContext = _organisationContextFactory.Create(rsin);

        using (GetLoggingScope(rsin, _correlationContextAccessor.CorrelationId))
        {
            var token = await TryGetTokenAsync(rsin);

            //
            // 1. Create a new or patch an existing DRC_LISTEBER subscription for this Rsin

            var abonnementen = await _notificatieServiceAgent.GetAllAbonnementenAsync();
            if (!abonnementen.Success)
            {
                throw new InvalidOperationException($"Failed to get subscriptions from NRC API for Rsin {rsin}.");
            }

            var organisationSubscribers = abonnementen.Response.Where(a => a.Owner == rsin && IsDocumentListenerSubscription(a.CallbackUrl));
            if (organisationSubscribers.Any())
            {
                // There is already an abonnement for this Rsin so patch it if needed
                foreach (var subscriber in organisationSubscribers)
                {
                    var patched = await _notificatieServiceAgent.PatchAbonnementByUrlAsync(
                        subscriber.Url,
                        new JObject(new JProperty("auth", token.bearer))
                    );

                    if (!patched.Success)
                    {
                        throw new InvalidOperationException(
                            $"Failed to patch subscription in NRC API for Rsin {rsin}. Error(s): {patched.GetErrorsFromResponse()}"
                        );
                    }
                }
            }
            else
            {
                // Abonnement does not exists so create new one for this Rsin
                string callback = $"{DocumentListenerApiUrl}/v1/notificatie/{rsin}";

                var added = await _notificatieServiceAgent.AddAbonnementAsync(
                    new AbonnementDto
                    {
                        Auth = token.bearer,
                        CallbackUrl = callback,
                        Kanalen = new List<AbonnementKanaalDto>
                        {
                            new AbonnementKanaalDto
                            {
                                Naam = "zaken", // Channel 'zaken'
                                Filters = new Dictionary<string, string>
                                {
                                    // Note: define a filter to receive a notification only if resource is 'zaakinformatieobject' (action is 'create' or 'destroy')
                                    { "#resource", "zaakinformatieobject" },
                                },
                            },
                            new AbonnementKanaalDto
                            {
                                Naam = "besluiten", // Channel 'besluiten'
                                Filters = new Dictionary<string, string>
                                {
                                    // Note: define a filter to receive a notification only if resource is 'besluitinformatieobject' (action is 'create' or 'destroy')
                                    { "#resource", "besluitinformatieobject" },
                                },
                            },
                        },
                    }
                );

                if (!added.Success)
                {
                    throw new InvalidOperationException(
                        $"Failed to create subscription in NRC API for Rsin {rsin}. Error(s): {added.GetErrorsFromResponse()}"
                    );
                }

                //
                // 2. Create a recurring job to renew the token before it expires (use ExpiresMinutesBefore)

                double refeshInMinutes =
                    token.expiresIn.TotalMinutes >= ExpiresMinutesBefore
                        ? Math.Max(1, (int)Math.Floor(token.expiresIn.TotalMinutes - ExpiresMinutesBefore))
                        : Math.Max(1, (int)Math.Floor(token.expiresIn.TotalMinutes / 2));

                // Create a cron expression (using minute segment)
                var refreshCronExpression = CronHelper.CreateCronForIntervalMinutes((int)refeshInMinutes);

                RecurringJob.AddOrUpdate<CreateOrPatchSubscriptionJob>(
                    $"create-or-patch-subscription-{rsin}-job",
                    h => h.ExecuteAsync(rsin),
                    refreshCronExpression
                );
            }
        }

        _logger.LogInformation("{CreateOrPatchSubscriptionJob} job finished.", nameof(CreateOrPatchSubscriptionJob));
    }

    private async Task<(string bearer, TimeSpan expiresIn)> TryGetTokenAsync(string rsin)
    {
        // Note: we can have multiple ClientIs's for one Rsin (DEV-651621343, oneground-651621343, rx.mission-651621343), take the "oneground" one
        var allClientIds = await _zgwAuthManager.ListOrganizationClientSecretsAsync(rsin, CancellationToken.None);

        var clientId = allClientIds.FirstOrDefault(c => c.ClientId.Contains("oneground"))?.ClientId;
        if (clientId == null)
        {
            throw new InvalidOperationException($"No client ID found for RSIN {rsin}. Cannot schedule token refresh job.");
        }

        var clientSecret = await _zgwAuthManager.GetClientSecretAsync(clientId, CancellationToken.None);
        if (clientSecret == null)
        {
            throw new InvalidOperationException($"Client secret not found for clientId {clientId}. Cannot schedule token refresh job.");
        }

        var token = await _zgwTokenService.GetTokenAsync(clientId, clientSecret.Secret, CancellationToken.None);
        if (token == null)
        {
            // Http request failed or invalid client credentials. By throwing Hangfire does a retry
            throw new InvalidOperationException($"Token retrieval failed for clientId {clientId}. Cannot schedule token refresh job.");
        }

        var expiresIn = TimeSpan.FromSeconds(token.ExpiresIn);

        return ($"Bearer {token.AccessToken}", expiresIn);
    }
}
