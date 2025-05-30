namespace Roxit.ZGW.Notificaties.Messaging;

public static class INotifySubscriberExtensionMethod
{
    public static SubscriberNotificatie ToInstance(this INotifySubscriber notifySubscriber)
    {
        return SubscriberNotificatie.From(notifySubscriber);
    }
}
