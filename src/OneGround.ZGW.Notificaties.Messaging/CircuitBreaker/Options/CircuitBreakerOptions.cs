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
    /// Default: 10 minutes.
    /// </summary>
    [Range(1, 60)]
    public int CacheExpirationMinutes { get; set; } = 10;
}
