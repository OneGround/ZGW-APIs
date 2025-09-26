using System.Linq.Expressions;
using Hangfire;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
    private readonly ApplicationConfiguration _applicationConfiguration;

    public SendNotificatiesConsumer(
        ILogger<SendNotificatiesConsumer> logger,
        IServiceProvider serviceProvider,
        IMemoryCache memoryCache,
        INotificatieScheduler notificatieScheduler,
        IConfiguration configuration,
        INotificationFilterService notificationFilterService
    )
        : base(logger)
    {
        _serviceProvider = serviceProvider;
        _memoryCache = memoryCache;
        _notificatieScheduler = notificatieScheduler;
        _notificationFilterService = notificationFilterService;

        _applicationConfiguration = configuration.GetSection("Application").Get<ApplicationConfiguration>() ?? new ApplicationConfiguration();
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
                    var kanalen = abonnement.AbonnementKanalen.Where(k => k.Kanaal.Naam == notificatie.Kanaal).ToArray();

                    Logger.LogDebug(
                        "Found following channels: {@ChannelIds} maching subscription: {SubscriptionId}",
                        kanalen.Select(k => k.Id),
                        abonnement.Id
                    );

                    foreach (var kanaal in kanalen)
                    {
                        var filters = kanaal.Filters.ToDictionary(k => k.Key.ToLower(), v => v.Value);

                        Logger.LogDebug("Channel {ChannelId} filters: {@ChannelFilters}", kanaal.Id, filters);

                        if (filters.ContainsKey("#actie") && !(filters.TryGetValue("#actie", out var actie) && notificatie.Actie == actie))
                        {
                            continue;
                        }

                        if (
                            filters.ContainsKey("#resource")
                            && !(filters.TryGetValue("#resource", out var resource) && notificatie.Resource == resource)
                        )
                        {
                            continue;
                        }

                        if (
                            filters.Count != 0
                            && !filters.Where(f => f.Key != "#actie" && f.Key != "#resource").All(filter => Filter(kenmerken, filter))
                        )
                        {
                            continue;
                        }

                        if (_notificationFilterService.IsIgnored(notificatie, abonnement, kanaal))
                        {
                            continue;
                        }

                        // Enqueue Hangfire job which sends the notificatie message (for each subscriber on channel)
                        var job = _notificatieScheduler.Enqueue<NotificatieJob>(h =>
                            h.ReQueueNotificatieAsync(abonnement.Id, notificatie.ToInstance())
                        );

                        Logger.LogInformation(
                            "{SendNotificatiesConsumer}: Hangfire job '{job}' enqueued for delivering notificatie to subscriber '{Rsin}', channel '{Kanaal}', endpoint {CallbackUrl}",
                            nameof(SendNotificatiesConsumer),
                            job,
                            notificatie.Rsin,
                            notificatie.Kanaal,
                            abonnement.CallbackUrl
                        );
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

    private async Task<List<Abonnement>> GetCachedAbonnementenAsync(string rsin, CancellationToken cancellationToken)
    {
        return await _memoryCache.GetOrCreate(
            $"abonnementen_{rsin}",
            async e =>
            {
                e.AbsoluteExpirationRelativeToNow = _applicationConfiguration.AbonnementenCacheExpirationTime;

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

    private bool Filter(IReadOnlyDictionary<string, string> kenmerken, KeyValuePair<string, string> filter)
    {
        if (kenmerken != null && kenmerken.TryGetValue(filter.Key, out var value))
        {
            if (filter.Value == "*")
            {
                Logger.LogDebug(">Filter:{filterKey}=\"*\"", filter.Key);
                return true;
            }

            // Note: we support boolean filters now. So handle these if relevant
            if (bool.TryParse(filter.Value, out var filterAsBool))
            {
                if (bool.TryParse(value, out var featureAsBool) && featureAsBool == filterAsBool)
                {
                    Logger.LogDebug(">Boolean Filter:{filterKey}={filterValue}", filter.Key, filter.Value);
                    return true;
                }
            }
            else if (value == filter.Value)
            {
                Logger.LogDebug(">Filter:{filterKey}=\"{filterValue}\"", filter.Key, filter.Value);
                return true;
            }
        }
        return false;
    }
}
