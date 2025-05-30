namespace Roxit.ZGW.Notificaties.Messaging;

public class SubscriberNotificatie : INotifySubscriber
{
    public string Rsin { get; set; }
    public Guid CorrelationId { get; set; }

    public string Kanaal { get; set; }
    public string HoofdObject { get; set; }
    public string Resource { get; set; }
    public string ResourceUrl { get; set; }
    public string Actie { get; set; }
    public IDictionary<string, string> Kenmerken { get; set; }

    public string ChannelUrl { get; set; }
    public string ChannelAuth { get; set; }
    public DateTime CreationTime { get; set; }
    public DateTime? RescheduledAt { get; set; }
    public TimeSpan? NextScheduled { get; set; }

    public static SubscriberNotificatie From(INotifySubscriber notifySubscriber)
    {
        return new SubscriberNotificatie
        {
            Rsin = notifySubscriber.Rsin,
            CorrelationId = notifySubscriber.CorrelationId,

            Kanaal = notifySubscriber.Kanaal,
            HoofdObject = notifySubscriber.HoofdObject,
            Resource = notifySubscriber.Resource,
            ResourceUrl = notifySubscriber.ResourceUrl,
            Actie = notifySubscriber.Actie,
            Kenmerken = notifySubscriber.Kenmerken,

            ChannelUrl = notifySubscriber.ChannelUrl,
            ChannelAuth = notifySubscriber.ChannelAuth,
            CreationTime = notifySubscriber.CreationTime,
            RescheduledAt = notifySubscriber.RescheduledAt,
        };
    }
}
