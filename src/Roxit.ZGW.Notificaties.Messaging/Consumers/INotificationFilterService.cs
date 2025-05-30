using Roxit.ZGW.Common.Messaging;
using Roxit.ZGW.Notificaties.DataModel;

namespace Roxit.ZGW.Notificaties.Messaging.Consumers;

public interface INotificationFilterService
{
    bool IsIgnored(ISendNotificaties notificatie, Abonnement abonnement, AbonnementKanaal kanaal);
}
