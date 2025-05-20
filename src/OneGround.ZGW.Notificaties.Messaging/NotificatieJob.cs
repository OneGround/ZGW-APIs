using Hangfire;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace OneGround.ZGW.Notificaties.Messaging;

public interface INotificatieJob
{
    Task ReQueueNotificatieAsync(SubscriberNotificatie notificatie, TimeSpan? next);
}

[DisableConcurrentExecution(10)]
[AutomaticRetry(Attempts = 5, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
public class NotificatieJob : INotificatieJob
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<NotificatieJob> _logger;

    public NotificatieJob(IPublishEndpoint publishEndpoint, ILogger<NotificatieJob> logger)
    {
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public Task ReQueueNotificatieAsync(SubscriberNotificatie notificatie, TimeSpan? next)
    {
        using (GetLoggingScope(notificatie.Rsin, notificatie.CorrelationId))
        {
            _logger.LogInformation(
                "{NotificatieJob}: Re-queue on channel '{Kanaal}' subscriber '{ChannelUrl}'",
                nameof(NotificatieJob),
                notificatie.Kanaal,
                notificatie.ChannelUrl
            );

            // Adjust the RescheduledAt and Next NextScheduled til we reached all appsetting.ScheduledRetries, eg. [ "00:05:00", "00:15:00", "02:00:00", "1.00:00:00" ]
            notificatie.RescheduledAt = DateTime.Now;
            notificatie.NextScheduled = next;

            return _publishEndpoint.Publish<INotifySubscriber>(notificatie, cancellationToken: CancellationToken.None);
        }
    }

    private IDisposable GetLoggingScope(string rsin, Guid correlationId)
    {
        return _logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId, ["RSIN"] = rsin });
    }
}
