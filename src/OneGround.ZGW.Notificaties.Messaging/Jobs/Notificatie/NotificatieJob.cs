using Hangfire;
using Hangfire.Console;
using Hangfire.Server;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Common.Batching;
using OneGround.ZGW.Common.CorrelationId;
using OneGround.ZGW.Notificaties.Messaging.Consumers;
using OneGround.ZGW.Notificaties.Messaging.Jobs.Filters;
using OneGround.ZGW.Notificaties.Messaging.Services;

namespace OneGround.ZGW.Notificaties.Messaging.Jobs.Notificatie;

public interface INotificatieJob
{
    Task ReQueueNotificatieAsync(Guid abonnementId, SubscriberNotificatie notificatie, PerformContext context = null, Guid? batchId = null);
}

[Queue(Constants.NrcListenerMainQueue)]
[RetryQueue(Constants.NrcListenerRetryQueue)]
public class NotificatieJob : INotificatieJob
{
    private readonly INotificationSender _notificationSender;
    private readonly IBatchIdAccessor _batchIdAccessor;
    private readonly ICorrelationContextAccessor _correlationIdAccessor;
    private readonly ILogger<NotificatieJob> _logger;
    private readonly IAbonnementService _abonnementService;

    public NotificatieJob(
        INotificationSender notificationSender,
        IBatchIdAccessor batchIdAccessor,
        ICorrelationContextAccessor correlationIdAccessor,
        ILogger<NotificatieJob> logger,
        IAbonnementService abonnementService
    )
    {
        _notificationSender = notificationSender;
        _batchIdAccessor = batchIdAccessor;
        _correlationIdAccessor = correlationIdAccessor;
        _logger = logger;
        _abonnementService = abonnementService;
    }

    public async Task ReQueueNotificatieAsync(
        Guid abonnementId,
        SubscriberNotificatie notificatie,
        PerformContext context = null,
        Guid? batchId = null
    )
    {
        ArgumentNullException.ThrowIfNull(notificatie);

        using (GetLoggingScope(notificatie.Rsin, notificatie.CorrelationId, batchId))
        {
            // Get deliver data for this subscriber like callback url and auth
            var subscriber = await _abonnementService.GetByIdAsync(abonnementId, CancellationToken.None);
            if (subscriber == null)
            {
                _logger.LogWarning("Abonnement with id {abonnementId} not found. Probably deleted within the retry period of the job.", abonnementId);
                return; // Job Ignored->Succeeded
            }
            if (string.IsNullOrWhiteSpace(subscriber.CallbackUrl))
            {
                throw new GeneralException(
                    $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {notificatie.CorrelationId}: Abonnement with id '{abonnementId}' has no valid callback url"
                );
            }

            SetBatchIdOrDefault(batchId);
            SetCorrelationId(notificatie);

            // Notify subscriber on channel....
            context.WriteLineColored(
                ConsoleTextColor.Yellow,
                $"Try to deliver notification to subscriber '{subscriber.CallbackUrl}' on channel '{notificatie.Kanaal}'."
            );

            var result = await _notificationSender.SendAsync(notificatie, subscriber.CallbackUrl, subscriber.Auth);
            if (!result.Success)
            {
                context.WriteLineColored(
                    ConsoleTextColor.Red,
                    $"Could not deliver notification to subscriber '{subscriber.CallbackUrl}' on channel '{notificatie.Kanaal}'. Statuscode={result.StatusCode}"
                );

                throw new NotDeliveredException(
                    $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {notificatie.CorrelationId}: Could not deliver notificatie to subscriber '{notificatie.Rsin}', channel '{notificatie.Kanaal}', endpoint '{subscriber.CallbackUrl}'"
                );
            }

            context.WriteLineColored(
                ConsoleTextColor.Yellow,
                $"Successfully delivered notification to subscriber '{subscriber.CallbackUrl}' on channel '{notificatie.Kanaal}'. Statuscode={result.StatusCode}"
            );
        }
    }

    private void SetCorrelationId(SubscriberNotificatie notificatie)
    {
        _correlationIdAccessor.SetCorrelationId(notificatie.CorrelationId.ToString());
    }

    private void SetBatchIdOrDefault(Guid? batchId)
    {
        _batchIdAccessor.Id = batchId.HasValue ? batchId.ToString() : null;
    }

    private IDisposable GetLoggingScope(string rsin, Guid correlationId, Guid? batchId)
    {
        if (batchId.HasValue)
        {
            return _logger.BeginScope(
                new Dictionary<string, object>
                {
                    ["CorrelationId"] = correlationId,
                    ["RSIN"] = rsin,
                    ["BatchId"] = batchId,
                }
            );
        }
        else
        {
            return _logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId, ["RSIN"] = rsin });
        }
    }
}
