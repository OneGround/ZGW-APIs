using System.ComponentModel.DataAnnotations;

namespace OneGround.ZGW.Notificaties.Messaging.CircuitBreaker.Options;

public class CircuitBreakerOptions
{
    public const string CircuitBreaker = "CircuitBreaker";

    /// <summary>
    /// Number of consecutive failures before opening the circuit and blocking the subscriber.
    /// Default: 3 failures.
    /// </summary>
    [Range(1, 20)]
    public int FailureThreshold { get; set; } = 3;

    /// <summary>
    /// Duration to keep the circuit open (subscriber blocked) after a failure threshold is reached.
    /// Default: 5 minutes.
    /// </summary>
    [Required]
    public TimeSpan BreakDuration { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Duration to cache subscriber health state in Redis before automatic expiration and recovery.
    /// Must exceed BreakDuration so an open circuit survives until its half-open transition.
    /// Default: 10 minutes.
    /// </summary>
    [Range(1, 60)]
    public int CacheExpirationMinutes { get; set; } = 10;

    /// <summary>
    /// Continuous-failure duration after which the subscription is permanently blocked in the database.
    /// The subscription can be unblocked again via the management API.
    /// Default: 7 days.
    /// </summary>
    [Required]
    public TimeSpan BlockSubscriptionAfter { get; set; } = TimeSpan.FromDays(7);

    /// <summary>
    /// Time-to-live of the failing-since marker that tracks the long-term failure window. Must
    /// outlive the longest gap between delivery attempts (retry schedule goes up to 1 day) so the
    /// window keeps accumulating across breaker open/half-open cycles.
    /// </summary>
    public TimeSpan FailingSinceMarkerExpiration => BlockSubscriptionAfter + TimeSpan.FromDays(1);
}
