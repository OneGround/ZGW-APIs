using OneGround.ZGW.Common.Messaging;
using OneGround.ZGW.Notificaties.DataModel;

namespace OneGround.ZGW.Notificaties.Messaging.Consumers;

public class NotificationFilterService : INotificationFilterService
{
    public bool IsIgnored(ISendNotificaties notificatie, Abonnement abonnement, AbonnementKanaal kanaal)
    {
        return false;
    }
}
