using OneGround.ZGW.Common.Messaging;

namespace OneGround.ZGW.Notificaties.Messaging;

public interface INotifySubscriber : INotificatie
{
    string ChannelUrl { get; }
    string ChannelAuth { get; }
    DateTime CreationTime { get; }
    DateTime? RescheduledAt { get; }
    TimeSpan? NextScheduled { get; }
}
