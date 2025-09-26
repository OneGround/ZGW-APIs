using Hangfire;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Common.Messaging;
using OneGround.ZGW.Notificaties.DataModel;
using OneGround.ZGW.Notificaties.Messaging.Consumers;

namespace OneGround.ZGW.Notificaties.Messaging.Jobs.Notificatie;

public interface INotificatieJob
{
    Task ReQueueNotificatieAsync(Guid abonnementId, SubscriberNotificatie notificatie);
}

[DisableConcurrentExecution(10)]
[Queue(Constants.NrcListenerQueue)]
public class NotificatieJob : INotificatieJob
{
    private readonly INotificationSender _notificationSender;
    private readonly NrcDbContext _context;
    private readonly ILogger<NotificatieJob> _logger;

    public NotificatieJob(
        INotificationSender notificationSender,
        ISendEndpointProvider sendEndpointProvider,
        IConfiguration configuration,
        NrcDbContext context,
        ILogger<NotificatieJob> logger
    )
    {
        _notificationSender = notificationSender;
        _context = context;
        _logger = logger;
    }

    public async Task ReQueueNotificatieAsync(Guid abonnementId, SubscriberNotificatie notificatie)
    {
        ArgumentNullException.ThrowIfNull(nameof(notificatie));

        using (GetLoggingScope(notificatie.Rsin, notificatie.CorrelationId))
        {
            // Get deliver data for this subscriber like callback url and auth
            var subscriber = _context.Abonnementen.SingleOrDefault(a => a.Id == abonnementId);
            if (subscriber == null)
            {
                throw new GeneralException(
                    $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {notificatie.CorrelationId}: Could not find abonnement with id '{abonnementId}'"
                );
            }
            if (string.IsNullOrWhiteSpace(subscriber.CallbackUrl))
            {
                throw new GeneralException(
                    $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {notificatie.CorrelationId}: Abonnement with id '{abonnementId}' has no valid callback url"
                );
            }

            // Notify subscriber on channel....
            var result = await _notificationSender.SendAsync(notificatie, subscriber.CallbackUrl, subscriber.Auth);
            if (!result.Success)
            {
                throw new NotDeliveredException(
                    $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {notificatie.CorrelationId}: Could not deliver notificatie to subscriber '{notificatie.Rsin}', channel '{notificatie.Kanaal}', endpoint '{subscriber.CallbackUrl}'"
                );
            }
        }
    }

    private IDisposable GetLoggingScope(string rsin, Guid correlationId)
    {
        return _logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId, ["RSIN"] = rsin });
    }
}

public static class NotificatieExtensions
{
    public static SubscriberNotificatie ToInstance(this INotificatie notificatie)
    {
        return new SubscriberNotificatie
        {
            Kanaal = notificatie.Kanaal,
            HoofdObject = notificatie.HoofdObject,
            Resource = notificatie.Resource,
            ResourceUrl = notificatie.ResourceUrl,
            Actie = notificatie.Actie,
            Kenmerken = notificatie.Kenmerken,
            CorrelationId = notificatie.CorrelationId,
            Rsin = notificatie.Rsin,
        };
    }
}
