using OneGround.ZGW.Common.Messaging;

namespace OneGround.ZGW.Notificaties.Messaging.Jobs.Notificatie;

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
            // Note: Create a copy of the kenmerken dictionary to avoid sharing state between notifications
            Kenmerken = new Dictionary<string, string>(notificatie.Kenmerken),
            CorrelationId = notificatie.CorrelationId,
            Rsin = notificatie.Rsin,
        };
    }
}
