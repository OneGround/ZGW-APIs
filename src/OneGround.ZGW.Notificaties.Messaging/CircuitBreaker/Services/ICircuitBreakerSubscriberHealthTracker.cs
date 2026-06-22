using OneGround.ZGW.Notificaties.Messaging.CircuitBreaker.Models;
using StackExchange.Redis;

namespace OneGround.ZGW.Notificaties.Messaging.CircuitBreaker.Services;

public interface ICircuitBreakerSubscriberHealthTracker
{
    /// <summary>
    /// Retrieves all cache key-value pairs from the underlying Redis store.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a collection of tuples, each
    /// consisting of a Redis key and its associated string value. The collection is empty if no cache entries are
    /// present.
    /// </returns>
    Task<IDictionary<RedisKey, CircuitBreakerSubscriberHealthState>> GetAllUnhealthyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes all unhealthy items from the collection.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <param name="onlyTheseRedisKeys">An optional list of specific Redis keys to clear.</param>
    /// <returns>The number of cleared unhealthy subscribers.</returns>
    Task<int> ClearAllUnhealthyAsync(CancellationToken cancellationToken = default, params string[] onlyTheseRedisKeys);

    /// <summary>
    /// Checks if a subscriber endpoint is healthy and available to receive notifications.
    /// </summary>
    /// <param name="abonnementId">The subscription (abonnement) to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the subscriber is healthy (circuit closed), false if unhealthy (circuit open).</returns>
    Task<bool> IsHealthyAsync(Guid abonnementId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a subscriber as unhealthy after a failure, incrementing the failure count.
    /// Opens the circuit if failure threshold is reached.
    /// </summary>
    /// <param name="abonnementId">The subscription (abonnement) that failed.</param>
    /// <param name="url">The subscriber URL (display only, kept in log messages).</param>
    /// <param name="errorMessage">Optional error message from the failure.</param>
    /// <param name="statusCode">Optional HTTP status code from the failure.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task MarkUnhealthyAsync(
        Guid abonnementId,
        string url,
        string? errorMessage = null,
        int? statusCode = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Marks a subscriber as healthy, resetting the failure count and closing the circuit.
    /// </summary>
    /// <param name="abonnementId">The subscription (abonnement) that succeeded.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task MarkHealthyAsync(Guid abonnementId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current health state of a subscriber.
    /// </summary>
    /// <param name="abonnementId">The subscription (abonnement) to query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The health state, or null if no state is tracked.</returns>
    Task<CircuitBreakerSubscriberHealthState?> GetHealthStateAsync(Guid abonnementId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets the health state of a subscriber, clearing all failure history.
    /// </summary>
    /// <param name="abonnementId">The subscription (abonnement) to reset.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ResetHealthAsync(Guid abonnementId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records the start of a subscription's long-term failure window (set-once). Writes the
    /// failing-since marker only when it is absent; a no-op when it already exists so the window
    /// keeps accumulating across breaker open/half-open cycles.
    /// </summary>
    /// <param name="abonnementId">The subscription (abonnement) that failed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task EnsureFailingSinceAsync(Guid abonnementId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears the failing-since marker (e.g. after a successful delivery resets the long-term window).
    /// </summary>
    /// <param name="abonnementId">The subscription (abonnement) that recovered.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ClearFailingSinceAsync(Guid abonnementId, CancellationToken cancellationToken = default);
}
