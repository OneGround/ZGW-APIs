namespace OneGround.ZGW.Notificaties.Messaging.Consumers;

public class NotDeliveredException : Exception
{
    public NotDeliveredException(string message)
        : base(message) { }
}

public class GeneralException : Exception
{
    public GeneralException(string message)
        : base(message) { }
}

public class SubscriptionBlockedException : Exception
{
    public SubscriptionBlockedException(string message)
        : base(message) { }
}
