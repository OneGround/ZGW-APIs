using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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

namespace Roxit.ZGW.Documenten.Jobs.Subscription;

public class CreateOrPatchSubscriptionJob : SubscriptionJobBase<CreateOrPatchSubscriptionJob>
{
    private const int ExpiresMinutesBefore = 10;

    private readonly INotificatiesServiceAgent _notificatieServiceAgent;
    private readonly ICachedZGWSecrets _cachedSecrets;
    private readonly IZgwTokenCacheService _zgwTokenCacheService;

    public CreateOrPatchSubscriptionJob(
        ILogger<CreateOrPatchSubscriptionJob> logger,
        ICorrelationContextAccessor correlationContextAccessor,
        IOrganisationContextAccessor organisationContextAccessor,
        IOrganisationContextFactory organisationContextFactory,
        INotificatiesServiceAgent notificatieServiceAgent,
        IServiceDiscovery serviceDiscovery,
        ICachedZGWSecrets cachedSecrets,
        IZgwTokenCacheService zgwTokenCacheService
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
        _zgwTokenCacheService = zgwTokenCacheService;
        _notificatieServiceAgent = notificatieServiceAgent;
    }

    // TODO: Set retries when NRC API is more stable
    // [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 5, 30, 120 }, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    [AutomaticRetry(Attempts = 0, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
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
        var value = await _cachedSecrets.GetServiceSecretAsync(rsin, ServiceRoleName.DRC, CancellationToken.None);
        if (value == null)
        {
            throw new InvalidOperationException($"No service secret configured for rsin: {rsin}");
        }

        var response = await _zgwTokenCacheService.GetCachedTokenAsync(value.ClientId, value.Secret, CancellationToken.None);

        var token = new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, response);

        var handler = new JwtSecurityTokenHandler();

        // ReadJwtToken does NOT validate signature — it only parses the token
        var jwt = handler.ReadJwtToken(token.ToString().Replace("Bearer ", ""));

        // Use the convenience property (UTC)
        DateTime expiryUtc = jwt.ValidTo;

        var expiresIn = jwt.ValidTo - DateTime.UtcNow;

        return (token.ToString(), expiresIn);
    }
}
