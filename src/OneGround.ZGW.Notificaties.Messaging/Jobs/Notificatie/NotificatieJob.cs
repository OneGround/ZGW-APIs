using Hangfire;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Notificaties.Messaging.Configuration;
using OneGround.ZGW.Notificaties.Messaging.Consumers;

namespace OneGround.ZGW.Notificaties.Messaging.Jobs.Notificatie;

public interface INotificatieJob
{
    Task ReQueueNotificatieAsync(SubscriberNotificatie notificatie);
}

[DisableConcurrentExecution(10)]
[AutomaticRetry(Attempts = 0, OnAttemptsExceeded = AttemptsExceededAction.Fail)] // Note: We are using our own retry-policy with incremental intervals so set Attempts explicit to 0
[Queue(Constants.NrcListenerQueue)]
public class NotificatieJob : INotificatieJob
{
    private readonly INotificationSender _notificationSender;
    private readonly HangfireConfiguration _hangfireConfiguration;
    private readonly NotificatiesEventBusConfiguration _eventbusConfiguration;
    private readonly ISendEndpointProvider _sendEndpointProvider;
    private readonly ILogger<NotificatieJob> _logger;

    public NotificatieJob(
        INotificationSender notificationSender,
        ISendEndpointProvider sendEndpointProvider,
        IConfiguration configuration,
        ILogger<NotificatieJob> logger
    )
    {
        _notificationSender = notificationSender;
        _sendEndpointProvider = sendEndpointProvider;

        _hangfireConfiguration = configuration.GetSection("Hangfire").Get<HangfireConfiguration>() ?? new HangfireConfiguration();
        _eventbusConfiguration =
            configuration.GetSection("Eventbus").Get<NotificatiesEventBusConfiguration>() ?? new NotificatiesEventBusConfiguration();

        _logger = logger;
    }

    public async Task ReQueueNotificatieAsync(SubscriberNotificatie notificatie)
    {
        ArgumentNullException.ThrowIfNull(nameof(notificatie));

        using (GetLoggingScope(notificatie.Rsin, notificatie.CorrelationId))
        {
            SubscriberResult result = null;
            try
            {
                // Notify subscriber on channel....
                bool maxRetries;

                result = await _notificationSender.SendAsync(notificatie, notificatie.ChannelUrl, notificatie.ChannelAuth);
                if (!result.Success)
                {
                    var (current, nextRetry) = GetCurrentAndNextRetry(notificatie);
                    if (current != null)
                    {
                        var nextAttempt = DateTime.Now.Add(current.Value);

                        // Adjust the RescheduledAt and Next NextScheduled till we reached all appsetting.ScheduledRetries, eg. [ "00:05:00", "00:15:00", "02:00:00", "1.00:00:00" ]
                        notificatie.RescheduledAt = DateTime.Now;
                        notificatie.NextScheduled = nextRetry;

                        var job = BackgroundJob.Schedule<NotificatieJob>(h => h.ReQueueNotificatieAsync(notificatie), nextAttempt);

                        var displayNextAttempt = nextAttempt.ToString("yyyy-MM-dd HH:mm:ss");
                        _logger.LogInformation(
                            "{NotificatieJob}: Hangfire job '{job}' re-scheduled on '{displayNextAttempt}' for subscriber '{Rsin}', channel '{Kanaal}', endpoint '{ChannelUrl}'",
                            nameof(NotificatieJob),
                            job,
                            displayNextAttempt,
                            notificatie.Rsin,
                            notificatie.Kanaal,
                            notificatie.ChannelUrl
                        );
                    }

                    maxRetries = current == null;

                    string error =
                        $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {notificatie.CorrelationId}: Could not deliver notificatie to subscriber '{notificatie.Rsin}', channel '{notificatie.Kanaal}', endpoint '{notificatie.ChannelUrl}'";
                    if (maxRetries)
                    {
                        error += ". Maximum retries exceeded";
                    }
                    throw new NotDeliveredException(error, notificatie.ChannelUrl, maxRetries);
                }
            }
            catch (NotDeliveredException ex)
            {
                if (ex.MaxRetriesExeeded)
                {
                    _logger.LogWarning(
                        "{NotificatieJob}: Failed to deliver notificatie to subscriber '{Rsin}', channel '{Kanaal}', endpoint '{ChannelUrl}'. Maximum retries exceeded. Notificatie send to deadletter queue with a TTL of {TTL} minutes.",
                        nameof(NotificatieJob),
                        notificatie.Rsin,
                        notificatie.Kanaal,
                        notificatie.ChannelUrl,
                        _eventbusConfiguration.NotDeliveredMessageTTL.TotalMinutes
                    );

                    //
                    // Maximum retries exeeded so post notificatie to dead-letter queue (dlq) message (with the configured TTL)

                    var sendEndpoint = await _sendEndpointProvider.GetSendEndpoint(
                        new Uri($"rabbitmq://{_eventbusConfiguration.HostName}/notificatie-subscriber-dlq")
                    );

                    await sendEndpoint.Send(notificatie, context => context.TimeToLive = _eventbusConfiguration.NotDeliveredMessageTTL);
                }
                else
                {
                    _logger.LogWarning(
                        "{NotificatieJob}: Failed to deliver notificatie to subscriber '{Rsin}', channel '{Kanaal}', endpoint '{ChannelUrl}'",
                        nameof(NotificatieJob),
                        notificatie.Rsin,
                        notificatie.Kanaal,
                        ex.ChannelUrl
                    );
                }
                throw; // Note: Mark job as Failed by throwing the exception
            }
        }
    }

    private IDisposable GetLoggingScope(string rsin, Guid correlationId)
    {
        return _logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId, ["RSIN"] = rsin });
    }

    private (TimeSpan? current, TimeSpan? next) GetCurrentAndNextRetry(SubscriberNotificatie notificatie)
    {
        if (!notificatie.NextScheduled.HasValue && notificatie.RescheduledAt.HasValue)
            return (null, null);

        var fromCurrent = _hangfireConfiguration
            .ScheduledRetries.SkipWhile(s => notificatie.NextScheduled.HasValue && s < notificatie.NextScheduled.Value)
            .ToList();

        var current = fromCurrent.FirstOrDefault();
        var next = fromCurrent.Skip(1).FirstOrDefault();

        return (current, next == default ? null : next);
    }
}
