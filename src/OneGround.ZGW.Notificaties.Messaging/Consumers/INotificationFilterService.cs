using OneGround.ZGW.Common.Messaging;
using OneGround.ZGW.Notificaties.DataModel;

namespace OneGround.ZGW.Notificaties.Messaging.Consumers;

public interface INotificationFilterService
{
    bool IsIgnored(ISendNotificaties notificatie, Abonnement abonnement, AbonnementKanaal kanaal);
}
