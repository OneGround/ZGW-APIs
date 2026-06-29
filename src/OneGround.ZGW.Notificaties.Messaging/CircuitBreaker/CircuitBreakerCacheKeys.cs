namespace OneGround.ZGW.Notificaties.Messaging.CircuitBreaker;

public static class CircuitBreakerCacheKeys
{
    public const string SubscriberPrefix = "ZGW:NRC:CircuitBreaker:subscriber:";
    public const string FailingSincePrefix = "ZGW:NRC:CircuitBreaker:failing-since:";

    public static string ForSubscriber(Guid abonnementId) => $"{SubscriberPrefix}{abonnementId}";

    public static string ForFailingSince(Guid abonnementId) => $"{FailingSincePrefix}{abonnementId}";
}
