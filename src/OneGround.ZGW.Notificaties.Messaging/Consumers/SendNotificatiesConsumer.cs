using System.Linq.Expressions;
using Hangfire;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OneGround.ZGW.Common.Messaging;
using OneGround.ZGW.Notificaties.DataModel;
using OneGround.ZGW.Notificaties.Messaging.Configuration;
using OneGround.ZGW.Notificaties.Messaging.Jobs.Notificatie;

namespace OneGround.ZGW.Notificaties.Messaging.Consumers;

public interface INotificatieScheduler
{
    string Enqueue<T>(Expression<Func<T, Task>> methodCall);
}

public class NotificatieScheduler : INotificatieScheduler
{
    public string Enqueue<T>(Expression<Func<T, Task>> methodCall)
    {
        return BackgroundJob.Enqueue(methodCall);
    }
}

public class SendNotificatiesConsumer : ConsumerBase<SendNotificatiesConsumer>, IConsumer<ISendNotificaties>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IMemoryCache _memoryCache;
    private readonly INotificatieScheduler _notificatieScheduler;
    private readonly INotificationFilterService _notificationFilterService;
    private readonly IOptions<ApplicationOptions> _applicationOptions;

    public SendNotificatiesConsumer(
        ILogger<SendNotificatiesConsumer> logger,
        IServiceProvider serviceProvider,
        IMemoryCache memoryCache,
        INotificatieScheduler notificatieScheduler,
        IOptions<ApplicationOptions> applicationOptions,
        INotificationFilterService notificationFilterService
    )
        : base(logger)
    {
        _serviceProvider = serviceProvider;
        _memoryCache = memoryCache;
        _notificatieScheduler = notificatieScheduler;
        _notificationFilterService = notificationFilterService;
        _applicationOptions = applicationOptions;
    }

    public async Task Consume(ConsumeContext<ISendNotificaties> context)
    {
        ArgumentNullException.ThrowIfNull(context.Message, nameof(context.Message));

        using (GetLoggingScope(context.Message, context.Message.CorrelationId))
        {
            try
            {
                Logger.LogInformation("Message body: {@Message}", context.Message);

                // Notify abonnees (subscribers)....
                var notificatie = context.Message;

                var abonnementen = await GetCachedAbonnementenAsync(notificatie.Rsin, context.CancellationToken);

                var kenmerken = notificatie.Kenmerken != null ? notificatie.Kenmerken.ToDictionary(k => k.Key.ToLower(), v => v.Value) : [];

                Logger.LogDebug("Notification owner matched these subscriptions: {@Subscriptions}", abonnementen.Select(s => s.Id));

                foreach (var abonnement in abonnementen)
                {
                    if (ShouldNotifySubscriber(notificatie, kenmerken, abonnement, out var resolvedKenmerkBronnen))
                    {
                        SendNotificatieToSubscriber(context, notificatie, abonnement, resolvedKenmerkBronnen);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(
                    ex,
                    "{Consumer} has raised in an unexpected exception. Failed to process {Message}",
                    nameof(SendNotificatiesConsumer),
                    nameof(ISendNotificaties)
                );
            }
        }
    }

    private bool ShouldNotifySubscriber(
        ISendNotificaties notificatie,
        Dictionary<string, string> kenmerken,
        Abonnement abonnement,
        out string resolvedKenmerkBronnen
    )
    {
        var kanalen = abonnement.AbonnementKanalen.Where(k => k.Kanaal.Naam == notificatie.Kanaal).ToArray();

        Logger.LogInformation(
            "Found following channels: {@ChannelIds} maching subscription: {SubscriptionId}",
            kanalen.Select(k => k.Id),
            abonnement.Id
        );

        resolvedKenmerkBronnen = "";

        bool shouldNotify = false;

        string resolvingKenmerkBronnen = "";

        foreach (var kanaal in kanalen)
        {
            var filters = kanaal.Filters.ToDictionary(k => k.Key.ToLower(), v => v.Value);

            Logger.LogDebug("Channel {ChannelId} filters: {@ChannelFilters}", kanaal.Id, filters);

            if (filters.ContainsKey("#actie") && !(filters.TryGetValue("#actie", out var actie) && notificatie.Actie == actie))
            {
                continue;
            }

            if (filters.ContainsKey("#resource") && !(filters.TryGetValue("#resource", out var resource) && notificatie.Resource == resource))
            {
                continue;
            }

            if (
                filters.Count != 0
                && !filters
                    .Where(f => f.Key != "#actie" && f.Key != "#resource")
                    .All(filter => Filter(kenmerken, filter, ref resolvingKenmerkBronnen))
            )
            {
                continue;
            }

            if (_notificationFilterService.IsIgnored(notificatie, abonnement, kanaal))
            {
                continue;
            }

            shouldNotify = true;
        }

        resolvedKenmerkBronnen = resolvingKenmerkBronnen;

        return shouldNotify;
    }

    private void SendNotificatieToSubscriber(
        ConsumeContext<ISendNotificaties> context,
        ISendNotificaties notificatie,
        Abonnement abonnement,
        string resolvedKenmerkBronnen
    )
    {
        SubscriberNotificatie subscriberNotificatie = notificatie.ToInstance();

        // Are there any resolved kenmerk_bronnen from filters? If so, add/update the kenmerk_bron in the kenmerken of the notificatie for this subscriber
        if (resolvedKenmerkBronnen.Length > 0 && subscriberNotificatie.Kenmerken.ContainsKey("kenmerk_bron"))
        {
            subscriberNotificatie.Kenmerken["kenmerk_bron"] = resolvedKenmerkBronnen;
        }

        // Get optional BatchId header
        context.TryGetHeader<Guid>("X-Batch-Id", out var batchId);

        // Enqueue Hangfire job which sends the notificatie message (for each subscriber on channel)
        var job = _notificatieScheduler.Enqueue<NotificatieJob>(h => h.ReQueueNotificatieAsync(abonnement.Id, subscriberNotificatie, null, batchId));

        Logger.LogInformation(
            "{SendNotificatiesConsumer}: Hangfire job '{job}' enqueued for delivering notificatie to subscriber '{Rsin}', channel '{Kanaal}', endpoint {CallbackUrl}",
            nameof(SendNotificatiesConsumer),
            job,
            notificatie.Rsin,
            notificatie.Kanaal,
            abonnement.CallbackUrl
        );
    }

    private async Task<List<Abonnement>> GetCachedAbonnementenAsync(string rsin, CancellationToken cancellationToken)
    {
        return await _memoryCache.GetOrCreate(
            $"abonnementen_{rsin}",
            async e =>
            {
                e.AbsoluteExpirationRelativeToNow = _applicationOptions.Value.AbonnementenCacheExpirationTime;

                var dbContext = _serviceProvider.GetRequiredService<NrcDbContext>();

                var _abonnementen = await dbContext
                    .Abonnementen.AsNoTracking()
                    .Where(a => a.Owner == rsin)
                    .Include(a => a.AbonnementKanalen)
                    .ThenInclude(a => a.Kanaal)
                    .Include(a => a.AbonnementKanalen)
                    .ThenInclude(a => a.Filters)
                    .ToListAsync(cancellationToken);

                Logger.LogDebug("{Count} abonnementen retrieved and cached for {Rsin}", _abonnementen.Count, rsin);

                return _abonnementen;
            }
        );
    }

    private bool Filter(IReadOnlyDictionary<string, string> kenmerken, KeyValuePair<string, string> filter, ref string resolvedKenmerkBronnen)
    {
        // Early exit if no kenmerken provided
        if (kenmerken == null || kenmerken.Count == 0)
        {
            return false;
        }

        // Special handling for kenmerk_bron filter
        if (filter.Key == "kenmerk_bron")
        {
            return FilterKenmerkBronAndResolve(kenmerken, filter, ref resolvedKenmerkBronnen);
        }

        // Standard filter matching - check if key exists
        if (!kenmerken.TryGetValue(filter.Key, out var kenmerkValue))
        {
            return false;
        }

        return MatchesFilterValue(filter.Key, filter.Value, kenmerkValue);
    }

    private bool FilterKenmerkBronAndResolve(
        IReadOnlyDictionary<string, string> kenmerken,
        KeyValuePair<string, string> filter,
        ref string resolvedKenmerkBronnen
    )
    {
        // Check if any kenmerk_bron entries exist
        if (!kenmerken.ContainsKey("kenmerk_bron"))
        {
            return false;
        }

        // Wildcard matches if any kenmerk_bron exists
        if (filter.Value == "*")
        {
            resolvedKenmerkBronnen = ResolveKenmerkBronnen(resolvedKenmerkBronnen, kenmerken["kenmerk_bron"]);

            Logger.LogDebug(">Filter:{filterKey}=\"*\"", filter.Key);
            return true;
        }

        // Check if specific value exists in any kenmerk_bron
        var bronnen = kenmerken["kenmerk_bron"].Split(';', StringSplitOptions.RemoveEmptyEntries);

        var matches = bronnen.FirstOrDefault(b => b == filter.Value);
        if (matches != null)
        {
            resolvedKenmerkBronnen = ResolveKenmerkBronnen(resolvedKenmerkBronnen, matches);

            Logger.LogDebug(">Filter:{filterKey}=\"{filterValue}\"", filter.Key, filter.Value);
            return true;
        }

        return false;
    }

    private static string ResolveKenmerkBronnen(string resolvedKenmerkBronnen, string matches)
    {
        //
        // Prevent duplicate entries in resolvedKenmerkBronnen when multiple filters match the same kenmerk_bron values

        // Split existing resolved values into a HashSet to track unique elements
        var existingBronnen =
            resolvedKenmerkBronnen.Length > 0
                ? resolvedKenmerkBronnen.Split(';', StringSplitOptions.RemoveEmptyEntries).ToHashSet()
                : new HashSet<string>();

        // Split incoming matches by semicolons and add only unique values
        var newBronnen = matches.Split(';', StringSplitOptions.RemoveEmptyEntries);

        foreach (var bron in newBronnen)
        {
            existingBronnen.Add(bron);
        }

        // Reconstruct the semicolon-separated string with unique values
        return string.Join(";", existingBronnen);
    }

    private bool MatchesFilterValue(string filterKey, string filterValue, string kenmerkValue)
    {
        // Wildcard matches any value for this key
        if (filterValue == "*")
        {
            Logger.LogDebug(">Filter:{filterKey}=\"*\"", filterKey);
            return true;
        }

        // Try boolean comparison
        if (bool.TryParse(filterValue, out var filterAsBool) && bool.TryParse(kenmerkValue, out var featureAsBool) && featureAsBool == filterAsBool)
        {
            Logger.LogDebug(">Boolean Filter:{filterKey}={filterValue}", filterKey, filterValue);
            return true;
        }

        // String comparison
        if (kenmerkValue == filterValue)
        {
            Logger.LogDebug(">Filter:{filterKey}=\"{filterValue}\"", filterKey, filterValue);
            return true;
        }

        return false;
    }
}
