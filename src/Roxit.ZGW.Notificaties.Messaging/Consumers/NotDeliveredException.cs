namespace Roxit.ZGW.Notificaties.Messaging.Consumers;

public class NotDeliveredException : Exception
{
    public NotDeliveredException(string message, string channelUrl, bool maxRetriesExceeded = false)
        : base(message)
    {
        ChannelUrl = channelUrl;
        MaxRetriesExeeded = maxRetriesExceeded;
    }

    public string ChannelUrl { get; }
    public bool MaxRetriesExeeded { get; }
}
