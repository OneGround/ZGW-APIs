using Hangfire;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using OneGround.ZGW.Common.Authentication;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.CorrelationId;
using OneGround.ZGW.Common.Helpers;
using OneGround.ZGW.Common.ServiceAgent.Extensions;
using OneGround.ZGW.Common.Services;
using OneGround.ZGW.Notificaties.Contracts.v1;
using OneGround.ZGW.Notificaties.ServiceAgent;

namespace OneGround.ZGW.Documenten.Jobs.Subscription;

public class CreateOrPatchSubscriptionJob : SubscriptionJobBase<CreateOrPatchSubscriptionJob>
{
    private const int ExpiresMinutesBefore = 10;

    public static string GetJobId(string rsin) => $"create-or-patch-subscription-{rsin}-job";

    private readonly INotificatiesServiceAgent _notificatieServiceAgent;
    private readonly ICachedZGWSecrets _cachedSecrets;
    private readonly IZgwTokenService _zgwTokenService;

    public CreateOrPatchSubscriptionJob(
        ILogger<CreateOrPatchSubscriptionJob> logger,
        ICorrelationContextAccessor correlationContextAccessor,
        IOrganisationContextAccessor organisationContextAccessor,
        IOrganisationContextFactory organisationContextFactory,
        INotificatiesServiceAgent notificatieServiceAgent,
        IServiceDiscovery serviceDiscovery,
        ICachedZGWSecrets cachedSecrets,
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
        _cachedSecrets = cachedSecrets;
        _zgwTokenService = zgwTokenService;
        _notificatieServiceAgent = notificatieServiceAgent;
    }

    [AutomaticRetry(Attempts = 3, DelaysInSeconds = [5, 30, 120], OnAttemptsExceeded = AttemptsExceededAction.Fail)]
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
            // 1. Create a new or patch an existing DRC_LISTENER subscription for this Rsin

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
                var callback = $"{DocumentListenerApiUrl}/v1/notificatie/{rsin}";

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
                                    // Note: define a filter to receive a notification only if resource is 'zaakinformatieobject' and actie is 'create'
                                    { "#resource", "zaakinformatieobject" },
                                    { "#actie", "create" },
                                },
                            },
                            new AbonnementKanaalDto
                            {
                                Naam = "zaken", // Channel 'zaken'
                                Filters = new Dictionary<string, string>
                                {
                                    // Note: define a filter to receive a notification only if resource is 'zaakinformatieobject' and actie is 'destroy'
                                    { "#resource", "zaakinformatieobject" },
                                    { "#actie", "destroy" },
                                },
                            },
                            new AbonnementKanaalDto
                            {
                                Naam = "besluiten", // Channel 'besluiten'
                                Filters = new Dictionary<string, string>
                                {
                                    // Note: define a filter to receive a notification only if resource is 'besluitinformatieobject' and actie is 'create'
                                    { "#resource", "besluitinformatieobject" },
                                    { "#actie", "create" },
                                },
                            },
                            new AbonnementKanaalDto
                            {
                                Naam = "besluiten", // Channel 'besluiten'
                                Filters = new Dictionary<string, string>
                                {
                                    // Note: define a filter to receive a notification only if resource is 'besluitinformatieobject' and actie is 'destroy'
                                    { "#resource", "besluitinformatieobject" },
                                    { "#actie", "destroy" },
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
            }

            // Create a recurring job to renew the token before it expires (use ExpiresMinutesBefore)
            double refreshInMinutes =
                token.expiresIn.TotalMinutes >= ExpiresMinutesBefore
                    ? Math.Max(1, (int)Math.Floor(token.expiresIn.TotalMinutes - ExpiresMinutesBefore))
                    : Math.Max(1, (int)Math.Floor(token.expiresIn.TotalMinutes / 2));

            var refreshCronExpression = CronHelper.CreateOneTimeCron((int)refreshInMinutes);
            RecurringJob.AddOrUpdate<CreateOrPatchSubscriptionJob>(GetJobId(rsin), h => h.ExecuteAsync(rsin), refreshCronExpression);
        }

        _logger.LogInformation("{CreateOrPatchSubscriptionJob} job finished.", nameof(CreateOrPatchSubscriptionJob));
    }

    private async Task<(string bearer, TimeSpan expiresIn)> TryGetTokenAsync(string rsin)
    {
        var value = await _cachedSecrets.GetServiceSecretAsync(rsin, ServiceRoleName.DRC, CancellationToken.None);
        if (value == null)
        {
            throw new InvalidOperationException($"No service secret configured for rsin: {rsin}");
        }

        var token = await _zgwTokenService.GetTokenAsync(value.ClientId, value.Secret, CancellationToken.None);
        var expiresIn = TimeSpan.FromSeconds(token.ExpiresIn);
        return ($"Bearer {token.AccessToken}", expiresIn);
    }
}
