using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OneGround.ZGW.Notificaties.Messaging.CircuitBreaker;
using OneGround.ZGW.Notificaties.Messaging.CircuitBreaker.Models;
using OneGround.ZGW.Notificaties.Messaging.CircuitBreaker.Options;
using StackExchange.Redis;

namespace OneGround.ZGW.Notificaties.Messaging.CircuitBreaker.Services;

public class RedisCircuitBreakerSubscriberHealthTracker(
    IDistributedCache cache,
    ILogger<RedisCircuitBreakerSubscriberHealthTracker> logger,
    IOptions<CircuitBreakerOptions> circuitBreakerOptions,
    IConnectionMultiplexer connectionMultiplexer
) : ICircuitBreakerSubscriberHealthTracker
{
    public async Task<IDictionary<RedisKey, CircuitBreakerSubscriberHealthState>> GetAllUnhealthyAsync(CancellationToken cancellationToken = default)
    {
        IDatabase db = connectionMultiplexer.GetDatabase();

        var endpoint = connectionMultiplexer.GetEndPoints()[0];
        IServer server = connectionMultiplexer.GetServer(endpoint);

        var results = new Dictionary<RedisKey, CircuitBreakerSubscriberHealthState>();

        foreach (var key in server.Keys(pattern: $"{CircuitBreakerCacheKeys.SubscriberPrefix}*"))
        {
            if (key == default(RedisKey))
            {
                continue;
            }

            RedisType type = await db.KeyTypeAsync(key);

            if (type == RedisType.Hash)
            {
                var cachedData = await cache.GetStringAsync(key!, cancellationToken);
                if (cachedData == null)
                {
                    continue;
                }

                var value = JsonSerializer.Deserialize<CircuitBreakerSubscriberHealthState>(cachedData!);
                if (value == null)
                {
                    continue;
                }

                results.Add(key, value!);
            }
        }
        return results;
    }

    public async Task<int> ClearAllUnhealthyAsync(CancellationToken cancellationToken = default, params string[] onlyTheseRedisKeys)
    {
        try
        {
            var unhealthySubscribers = await GetAllUnhealthyAsync(cancellationToken);

            if (!unhealthySubscribers.Any())
            {
                logger.LogInformation("No unhealthy subscribers found to clear.");
                return 0;
            }

            var dictOnlyTheseRedisKeys = onlyTheseRedisKeys.ToHashSet();

            var clearedCount = 0;
            foreach (var kvp in unhealthySubscribers)
            {
                var redisKey = kvp.Key.ToString();
                if (dictOnlyTheseRedisKeys.Count > 0 && !dictOnlyTheseRedisKeys.Contains(redisKey))
                {
                    continue;
                }
                await cache.RemoveAsync(redisKey, cancellationToken);
                clearedCount++;
            }

            logger.LogInformation("Cleared {ClearedCount} of {TotalCount} unhealthy subscribers.", clearedCount, unhealthySubscribers.Count);

            return clearedCount;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error clearing (all/selected) unhealthy subscribers.");
            throw;
        }
    }

    public async Task<bool> IsHealthyAsync(Guid abonnementId, CancellationToken cancellationToken = default)
    {
        try
        {
            var state = await GetHealthStateAsync(abonnementId, cancellationToken);

            if (state == null)
            {
                // No health state tracked - consider healthy
                return true;
            }

            // Check if circuit should transition from Open to Half-Open (monitoring)
            if (state.BlockedUntil.HasValue && DateTime.UtcNow >= state.BlockedUntil.Value)
            {
                // Transition to half-open: drop the breaker state and allow one trial attempt.
                await ResetHealthAsync(abonnementId, cancellationToken);

                logger.LogInformation(
                    "Circuit breaker transitioned to HALF-OPEN for subscription {AbonnementId} ('{Url}'). Monitoring for recovery.",
                    state.AbonnementId,
                    state.Url
                );

                return true; // Allow one attempt
            }

            var isHealthy = !state.IsCircuitOpen;

            if (!isHealthy)
            {
                logger.LogWarning(
                    "Subscriber health check failed: Circuit is OPEN for subscription {AbonnementId} ('{Url}'). "
                        + "Blocked until {BlockedUntil}. Consecutive failures: {ConsecutiveFailures}",
                    state.AbonnementId,
                    state.Url,
                    state.BlockedUntil,
                    state.ConsecutiveFailures
                );
            }

            return isHealthy;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking subscriber health for subscription {AbonnementId}. Treating as healthy (fail-open).", abonnementId);
            // Fail-open: if we can't check health, allow the notification through
            return true;
        }
    }

    public async Task MarkUnhealthyAsync(
        Guid abonnementId,
        string url,
        string? errorMessage = null,
        int? statusCode = null,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var state =
                await GetHealthStateAsync(abonnementId, cancellationToken)
                ?? new CircuitBreakerSubscriberHealthState { AbonnementId = abonnementId, Url = url };

            // Detect if we're failing during HALF-OPEN (monitoring) state:
            // State exists, BlockedUntil is null, and we have a low failure count
            var wasInHalfOpen =
                state.BlockedUntil == null
                && state.ConsecutiveFailures < circuitBreakerOptions.Value.FailureThreshold
                && state.LastFailureAt.HasValue;

            state.ConsecutiveFailures++;
            state.LastFailureAt = DateTime.UtcNow;
            state.LastErrorMessage = errorMessage;
            state.LastStatusCode = statusCode;

            if (state.FirstFailureAt == null)
            {
                state.FirstFailureAt = DateTime.UtcNow;
            }

            // Open circuit if failure threshold is reached
            if (state.ConsecutiveFailures >= circuitBreakerOptions.Value.FailureThreshold)
            {
                state.BlockedUntil = DateTime.UtcNow.Add(circuitBreakerOptions.Value.BreakDuration);

                if (wasInHalfOpen)
                {
                    logger.LogWarning(
                        "Circuit breaker RE-OPENED for subscription {AbonnementId} ('{Url}'). "
                            + "Failed again during monitoring. Consecutive failures: {ConsecutiveFailures}. "
                            + "Blocked until {BlockedUntil}. Last error: {ErrorMessage}",
                        abonnementId,
                        url,
                        state.ConsecutiveFailures,
                        state.BlockedUntil,
                        errorMessage
                    );
                }
                else
                {
                    logger.LogWarning(
                        "Circuit breaker OPENED for subscription {AbonnementId} ('{Url}'). "
                            + "Failure threshold reached: {ConsecutiveFailures}/{FailureThreshold}. "
                            + "Blocked until {BlockedUntil}. Last error: {ErrorMessage}",
                        abonnementId,
                        url,
                        state.ConsecutiveFailures,
                        circuitBreakerOptions.Value.FailureThreshold,
                        state.BlockedUntil,
                        errorMessage
                    );
                }
            }
            else
            {
                logger.LogInformation(
                    "Subscription {AbonnementId} ('{Url}') marked as unhealthy. Consecutive failures: {ConsecutiveFailures}/{FailureThreshold}. "
                        + "Last error: {ErrorMessage}",
                    abonnementId,
                    url,
                    state.ConsecutiveFailures,
                    circuitBreakerOptions.Value.FailureThreshold,
                    errorMessage
                );
            }

            await SaveHealthStateAsync(state, cancellationToken);
            await EnsureFailingSinceAsync(abonnementId, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error marking subscription {AbonnementId} as unhealthy. Health tracking may be inconsistent.", abonnementId);
        }
    }

    public async Task MarkHealthyAsync(Guid abonnementId, CancellationToken cancellationToken = default)
    {
        try
        {
            var state = await GetHealthStateAsync(abonnementId, cancellationToken);
            if (state == null)
            {
                await ClearFailingSinceAsync(abonnementId, cancellationToken);
                return;
            }

            var wasCircuitOpen = state.IsCircuitOpen;
            var previousFailures = state.ConsecutiveFailures;

            // Remove the health state (healthy = no tracking needed)
            await ResetHealthAsync(abonnementId, cancellationToken);
            await ClearFailingSinceAsync(abonnementId, cancellationToken);

            if (wasCircuitOpen)
            {
                logger.LogInformation(
                    "Circuit breaker CLOSED for subscription {AbonnementId}. " + "Subscriber recovered after {ConsecutiveFailures} failures.",
                    abonnementId,
                    previousFailures
                );
            }
            else if (previousFailures > 0)
            {
                logger.LogInformation(
                    "Subscription {AbonnementId} marked as healthy. Failure count reset from {PreviousFailures}.",
                    abonnementId,
                    previousFailures
                );
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error marking subscription {AbonnementId} as healthy. Health tracking may be inconsistent.", abonnementId);
        }
    }

    public async Task<CircuitBreakerSubscriberHealthState?> GetHealthStateAsync(Guid abonnementId, CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = GetCacheKey(abonnementId);
            var cachedData = await cache.GetStringAsync(cacheKey, cancellationToken);

            if (string.IsNullOrEmpty(cachedData))
            {
                return null;
            }

            return JsonSerializer.Deserialize<CircuitBreakerSubscriberHealthState>(cachedData);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving health state for subscription {AbonnementId}.", abonnementId);
            return null;
        }
    }

    public async Task ResetHealthAsync(Guid abonnementId, CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = GetCacheKey(abonnementId);
            await cache.RemoveAsync(cacheKey, cancellationToken);

            logger.LogInformation("Health state reset for subscription {AbonnementId}.", abonnementId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error resetting health state for subscription {AbonnementId}.", abonnementId);
        }
    }

    public async Task EnsureFailingSinceAsync(Guid abonnementId, CancellationToken cancellationToken = default)
    {
        var markerKey = CircuitBreakerCacheKeys.ForFailingSince(abonnementId);

        var existing = await cache.GetStringAsync(markerKey, cancellationToken);
        if (existing != null && DateTime.TryParse(existing, null, DateTimeStyles.RoundtripKind, out _))
        {
            // Set-once: the long-term failure window keeps accumulating across breaker open/half-open cycles.
            return;
        }

        var options = new DistributedCacheEntryOptions
        {
            // Must outlive the longest gap between delivery attempts (retry schedule goes up
            // to 1 day) so the window keeps accumulating across breaker cycles.
            AbsoluteExpirationRelativeToNow = circuitBreakerOptions.Value.FailingSinceMarkerExpiration,
        };
        await cache.SetStringAsync(markerKey, DateTime.UtcNow.ToString("O"), options, cancellationToken);
    }

    public async Task ClearFailingSinceAsync(Guid abonnementId, CancellationToken cancellationToken = default)
    {
        await cache.RemoveAsync(CircuitBreakerCacheKeys.ForFailingSince(abonnementId), cancellationToken);
    }

    private async Task SaveHealthStateAsync(CircuitBreakerSubscriberHealthState state, CancellationToken cancellationToken)
    {
        var cacheKey = GetCacheKey(state.AbonnementId);
        var serialized = JsonSerializer.Serialize(state);

        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(circuitBreakerOptions.Value.CacheExpirationMinutes),
        };

        await cache.SetStringAsync(cacheKey, serialized, options, cancellationToken);
    }

    private static string GetCacheKey(Guid abonnementId) => CircuitBreakerCacheKeys.ForSubscriber(abonnementId);
}
