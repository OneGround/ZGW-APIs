using OneGround.ZGW.Common.Messaging.Configuration;

namespace OneGround.ZGW.Notificaties.Messaging.Configuration;

public class NotificatiesEventBusConfiguration : EventBusConfiguration
{
    public ushort ReceivePrefetchCount { get; set; } = 16;
    public TimeSpan NotDeliveredMessageTTL { get; set; } = TimeSpan.FromDays(7);
}
