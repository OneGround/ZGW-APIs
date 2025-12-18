namespace OneGround.ZGW.Notificaties.Messaging.CircuitBreaker.Models;

public class CircuitBreakerSubscriberHealthState
{
    /// <summary>
    /// The subscriber URL that this health state tracks.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Number of consecutive failures observed.
    /// </summary>
    public int ConsecutiveFailures { get; set; }

    /// <summary>
    /// Timestamp when the subscriber was first marked as unhealthy.
    /// </summary>
    public DateTime? FirstFailureAt { get; set; }

    /// <summary>
    /// Timestamp when the subscriber was last marked as unhealthy.
    /// </summary>
    public DateTime? LastFailureAt { get; set; }

    /// <summary>
    /// Timestamp until which the subscriber is blocked.
    /// Null if the subscriber is healthy.
    /// </summary>
    public DateTime? BlockedUntil { get; set; }

    /// <summary>
    /// Indicates whether the circuit is currently open (subscriber blocked).
    /// </summary>
    public bool IsCircuitOpen => BlockedUntil.HasValue && BlockedUntil.Value > DateTime.UtcNow;

    /// <summary>
    /// Last error message received from the subscriber.
    /// </summary>
    public string? LastErrorMessage { get; set; }

    /// <summary>
    /// Last HTTP status code received from the subscriber (if applicable).
    /// </summary>
    public int? LastStatusCode { get; set; }
}
