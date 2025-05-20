using System.Collections.Immutable;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Common.Messaging;
using OneGround.ZGW.Notificaties.DataModel;
using OneGround.ZGW.Notificaties.Messaging.Configuration;

namespace OneGround.ZGW.Notificaties.Messaging.Consumers;

public class SendNotificatiesConsumer : ConsumerBase<SendNotificatiesConsumer>, IConsumer<ISendNotificaties>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IMemoryCache _memoryCache;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly INotificationFilterService _notificationFilterService;
    private readonly ApplicationConfiguration _applicationConfiguration;

    public SendNotificatiesConsumer(
        ILogger<SendNotificatiesConsumer> logger,
        IServiceProvider serviceProvider,
        IMemoryCache memoryCache,
        IPublishEndpoint publishEndpoint,
        IConfiguration configuration,
        INotificationFilterService notificationFilterService
    )
        : base(logger)
    {
        _serviceProvider = serviceProvider;
        _memoryCache = memoryCache;
        _publishEndpoint = publishEndpoint;
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

                var abonnementen = await _memoryCache.GetOrCreate(
                    $"abonnementen_{notificatie.Rsin}",
                    async e =>
                    {
                        e.AbsoluteExpirationRelativeToNow = _applicationConfiguration.AbonnementenCacheExpirationTime;

                        var dbContext = _serviceProvider.GetRequiredService<NrcDbContext>();

                        var abonnements = await dbContext
                            .Abonnementen.AsNoTracking()
                            .Where(a => a.Owner == notificatie.Rsin)
                            .Include(a => a.AbonnementKanalen)
                            .ThenInclude(a => a.Kanaal)
                            .Include(a => a.AbonnementKanalen)
                            .ThenInclude(a => a.Filters)
                            .ToListAsync(context.CancellationToken);

                        Logger.LogDebug("{Count} abonnementen retrieved and cached for {Rsin}", abonnements.Count, notificatie.Rsin);

                        return abonnements;
                    }
                );

                Logger.LogDebug("Notification owner matched these subscriptions: {@Subscriptions}", abonnementen.Select(s => s.Id));

                IReadOnlyDictionary<string, string> notificatieKenmerken;
                if (notificatie.Kenmerken == null)
                {
                    notificatieKenmerken = ImmutableDictionary.Create<string, string>();
                }
                else
                {
                    notificatieKenmerken = notificatie.Kenmerken.ToDictionary(k => k.Key.ToLower(), v => v.Value);
                }

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

                        if (filters.Count != 0 && !filters.Any(filter => Filter(notificatieKenmerken, filter)))
                            continue;

                        if (_notificationFilterService.IsIgnored(notificatie, abonnement, kanaal))
                            continue;

                        const byte priority = (byte)MessagePriority.Normal;

                        // Post notificatie message on notificatie-subscriber message queue (for each subscriber on channel)
                        var subscriberNotificatie = new SubscriberNotificatie
                        {
                            Kanaal = notificatie.Kanaal,
                            HoofdObject = notificatie.HoofdObject,
                            Resource = notificatie.Resource,
                            ResourceUrl = notificatie.ResourceUrl,
                            Actie = notificatie.Actie,
                            Kenmerken = notificatie.Kenmerken,
                            Rsin = notificatie.Rsin,
                            CorrelationId = notificatie.CorrelationId,
                            // subscriber on THIS channel...
                            ChannelUrl = abonnement.CallbackUrl,
                            ChannelAuth = abonnement.Auth,
                            // First time creation
                            CreationTime = DateTime.Now,
                            // Rescheduling not active this moment
                            RescheduledAt = null,
                            NextScheduled = null,
                        };

                        await _publishEndpoint.Publish<INotifySubscriber>(
                            subscriberNotificatie,
                            publishContext => publishContext.SetPriority(priority)
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

    private bool Filter(IReadOnlyDictionary<string, string> kenmerken, KeyValuePair<string, string> filter)
    {
        if (kenmerken != null && kenmerken.TryGetValue(filter.Key, out var value))
        {
            if (filter.Value == "*")
            {
                Logger.LogDebug(">Filter:{filterKey}=\"*\"", filter.Key);
                return true;
            }

            if (value == filter.Value)
            {
                Logger.LogDebug(">Filter:{filterKey}=\"{filterValue}\"", filter.Key, filter.Value);
                return true;
            }
        }

        return false;
    }
}
