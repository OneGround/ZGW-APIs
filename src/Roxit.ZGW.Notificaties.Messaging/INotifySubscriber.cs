using Roxit.ZGW.Common.Messaging;

namespace Roxit.ZGW.Notificaties.Messaging;

public interface INotifySubscriber : INotificatie
{
    string ChannelUrl { get; }
    string ChannelAuth { get; }
    DateTime CreationTime { get; }
    DateTime? RescheduledAt { get; }
    TimeSpan? NextScheduled { get; }
}
