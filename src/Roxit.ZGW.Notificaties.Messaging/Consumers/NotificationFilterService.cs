using Roxit.ZGW.Common.Messaging;
using Roxit.ZGW.Notificaties.DataModel;

namespace Roxit.ZGW.Notificaties.Messaging.Consumers;

public class NotificationFilterService : INotificationFilterService
{
    public bool IsIgnored(ISendNotificaties notificatie, Abonnement abonnement, AbonnementKanaal kanaal)
    {
        return false;
    }
}
