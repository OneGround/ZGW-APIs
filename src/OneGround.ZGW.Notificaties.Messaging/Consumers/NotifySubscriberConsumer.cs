using Hangfire;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Common.Messaging;
using OneGround.ZGW.Notificaties.Messaging.Configuration;

namespace OneGround.ZGW.Notificaties.Messaging.Consumers;

public class NotifySubscriberConsumer : ConsumerBase<NotifySubscriberConsumer>, IConsumer<INotifySubscriber>
{
    private readonly INotificationSender _notificationSender;
    private readonly HangfireConfiguration _hangfireConfiguration;
    private readonly NotificatiesEventBusConfiguration _eventbusConfiguration;
    private readonly ISendEndpointProvider _sendEndpointProvider;

    public NotifySubscriberConsumer(
        ILogger<NotifySubscriberConsumer> logger,
        INotificationSender notificationSender,
        ISendEndpointProvider sendEndpointProvider,
        IConfiguration configuration
    )
        : base(logger)
    {
        _notificationSender = notificationSender;
        _sendEndpointProvider = sendEndpointProvider;

        _hangfireConfiguration = configuration.GetSection("Hangfire").Get<HangfireConfiguration>() ?? new HangfireConfiguration();
        _eventbusConfiguration =
            configuration.GetSection("Eventbus").Get<NotificatiesEventBusConfiguration>() ?? new NotificatiesEventBusConfiguration();
    }

    public async Task Consume(ConsumeContext<INotifySubscriber> context)
    {
        ArgumentNullException.ThrowIfNull(nameof(context.Message));

        using (GetLoggingScope(context.Message, context.Message.CorrelationId))
        {
            SubscriberResult result = null;
            try
            {
                // Notify subscriber on channel....
                var notificatie = context.Message;
                bool maxRetries;

                result = await _notificationSender.SendAsync(
                    notificatie,
                    context.Message.ChannelUrl,
                    context.Message.ChannelAuth,
                    context.CancellationToken
                );
                if (!result.Success)
                {
                    var (current, next) = GetCurrentAndNextRetry(context);
                    if (current != null)
                    {
                        var nextAttempt = DateTime.Now.Add(current.Value);

                        var job = BackgroundJob.Schedule<NotificatieJob>(
                            h => h.ReQueueNotificatieAsync(context.Message.ToInstance(), next),
                            nextAttempt
                        );

                        var displayNextAttempt = nextAttempt.ToString("yyyy-MM-dd HH:mm:ss");
                        Logger.LogInformation(
                            "{NotifySubscriberConsumer}: Job ({job}) re-schedule on {displayNextAttempt} on channel '{Kanaal}' subscriber '{url}'",
                            nameof(NotifySubscriberConsumer),
                            job,
                            displayNextAttempt,
                            context.Message.Kanaal,
                            context.Message.ChannelUrl
                        );
                    }

                    maxRetries = current == null;

                    throw new NotDeliveredException("Could not deliver message to subscriber.", context.Message.ChannelUrl, maxRetries);
                }

                await context.RespondAsync(result); // Note: Eventually we can use it synchronously and checking the result
            }
            catch (NotDeliveredException ex)
            {
                // Note: Do not throw any exceptions here.
                if (ex.MaxRetriesExeeded)
                {
                    Logger.LogWarning(
                        "{NotifySubscriberConsumer}: Failed on channel '{Kanaal}' subscriber '{ChannelUrl} maximum retries exceeded. Message send to deadletter queue with a TTL of {TTL} minutes.",
                        nameof(NotifySubscriberConsumer),
                        context.Message.Kanaal,
                        context.Message.ChannelUrl,
                        _eventbusConfiguration.NotDeliveredMessageTTL.TotalMinutes
                    );

                    //
                    // Post to notificatie to the dead-letter queue (dlq) message (with a TTL)

                    var sendEndpoint = await _sendEndpointProvider.GetSendEndpoint(
                        new Uri($"rabbitmq://{_eventbusConfiguration.HostName}/notificatie-subscriber-dlq")
                    );

                    await sendEndpoint.Send(context.Message, context => context.TimeToLive = _eventbusConfiguration.NotDeliveredMessageTTL);
                }
                else
                {
                    Logger.LogWarning(
                        "{NotifySubscriberConsumer}: Failed on channel '{Kanaal}' subscriber '{ChannelUrl}'.",
                        nameof(NotifySubscriberConsumer),
                        context.Message.Kanaal,
                        ex.ChannelUrl
                    );
                }
                await context.RespondAsync(result);
            }
        }
    }

    private (TimeSpan? current, TimeSpan? next) GetCurrentAndNextRetry(ConsumeContext<INotifySubscriber> context)
    {
        if (!context.Message.NextScheduled.HasValue && context.Message.RescheduledAt.HasValue)
            return (null, null);

        var fromCurrent = _hangfireConfiguration
            .ScheduledRetries.SkipWhile(s => context.Message.NextScheduled.HasValue && s < context.Message.NextScheduled.Value)
            .ToList();

        var current = fromCurrent.FirstOrDefault();
        var next = fromCurrent.Skip(1).FirstOrDefault();

        return (current, next == default ? null : next);
    }
}
