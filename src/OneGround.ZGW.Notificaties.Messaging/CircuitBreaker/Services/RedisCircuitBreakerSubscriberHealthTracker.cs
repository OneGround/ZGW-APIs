using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OneGround.ZGW.Notificaties.Messaging.CircuitBreaker.Models;
using OneGround.ZGW.Notificaties.Messaging.CircuitBreaker.Options;

namespace OneGround.ZGW.Notificaties.Messaging.CircuitBreaker.Services;

public class RedisCircuitBreakerSubscriberHealthTracker(
    IDistributedCache cache,
    ILogger<RedisCircuitBreakerSubscriberHealthTracker> logger,
    IOptions<CircuitBreakerOptions> settings
) : ICircuitBreakerSubscriberHealthTracker
{
    private readonly CircuitBreakerOptions _options = settings.Value;

    private const string CacheKeyPrefix = "ZGW:NRC:CircuitBreaker:subscriber:";

    public async Task<bool> IsHealthyAsync(string url, CancellationToken cancellationToken = default)
    {
        try
        {
            var state = await GetHealthStateAsync(url, cancellationToken);

            if (state == null)
            {
                // No health state tracked - consider healthy
                return true;
            }

            var isHealthy = !state.IsCircuitOpen;

            if (!isHealthy)
            {
                logger.LogWarning(
                    "Subscriber health check failed: Circuit is OPEN for '{Url}'. "
                        + "Blocked until {BlockedUntil}. Consecutive failures: {ConsecutiveFailures}",
                    url,
                    state.BlockedUntil,
                    state.ConsecutiveFailures
                );
            }

            return isHealthy;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking subscriber health for '{Url}'. Treating as healthy (fail-open).", url);
            // Fail-open: if we can't check health, allow the notification through
            return true;
        }
    }

    public async Task MarkUnhealthyAsync(
        string url,
        string? errorMessage = null,
        int? statusCode = null,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var state = await GetHealthStateAsync(url, cancellationToken) ?? new CircuitBreakerSubscriberHealthState { Url = url };

            state.ConsecutiveFailures++;
            state.LastFailureAt = DateTime.UtcNow;
            state.LastErrorMessage = errorMessage;
            state.LastStatusCode = statusCode;

            if (state.FirstFailureAt == null)
            {
                state.FirstFailureAt = DateTime.UtcNow;
            }

            // Open circuit if failure threshold is reached
            if (state.ConsecutiveFailures >= _options.FailureThreshold && state.BlockedUntil == null)
            {
                state.BlockedUntil = DateTime.UtcNow.Add(_options.BreakDuration);

                logger.LogWarning(
                    "Circuit breaker OPENED for subscriber '{Url}'. "
                        + "Failure threshold reached: {ConsecutiveFailures}/{FailureThreshold}. "
                        + "Blocked until {BlockedUntil}. Last error: {ErrorMessage}",
                    url,
                    state.ConsecutiveFailures,
                    _options.FailureThreshold,
                    state.BlockedUntil,
                    errorMessage
                );
            }
            else
            {
                logger.LogInformation(
                    "Subscriber '{Url}' marked as unhealthy. Consecutive failures: {ConsecutiveFailures}/{FailureThreshold}. "
                        + "Last error: {ErrorMessage}",
                    url,
                    state.ConsecutiveFailures,
                    _options.FailureThreshold,
                    errorMessage
                );
            }

            await SaveHealthStateAsync(state, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error marking subscriber '{Url}' as unhealthy. Health tracking may be inconsistent.", url);
        }
    }

    public async Task MarkHealthyAsync(string url, CancellationToken cancellationToken = default)
    {
        try
        {
            var state = await GetHealthStateAsync(url, cancellationToken);
            if (state == null)
            {
                // Already healthy, nothing to do
                return;
            }

            var wasCircuitOpen = state.IsCircuitOpen;
            var previousFailures = state.ConsecutiveFailures;

            // Remove the health state (healthy = no tracking needed)
            await ResetHealthAsync(url, cancellationToken);

            if (wasCircuitOpen)
            {
                logger.LogInformation(
                    "Circuit breaker CLOSED for subscriber '{Url}'. " + "Subscriber recovered after {ConsecutiveFailures} failures.",
                    url,
                    previousFailures
                );
            }
            else if (previousFailures > 0)
            {
                logger.LogInformation("Subscriber '{Url}' marked as healthy. Failure count reset from {PreviousFailures}.", url, previousFailures);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error marking subscriber '{Url}' as healthy. Health tracking may be inconsistent.", url);
        }
    }

    public async Task<CircuitBreakerSubscriberHealthState?> GetHealthStateAsync(string url, CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = GetCacheKey(url);
            var cachedData = await cache.GetStringAsync(cacheKey, cancellationToken);

            if (string.IsNullOrEmpty(cachedData))
            {
                return null;
            }

            return JsonSerializer.Deserialize<CircuitBreakerSubscriberHealthState>(cachedData);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving health state for subscriber '{Url}'.", url);
            return null;
        }
    }

    public async Task ResetHealthAsync(string url, CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = GetCacheKey(url);
            await cache.RemoveAsync(cacheKey, cancellationToken);

            logger.LogInformation("Health state reset for subscriber '{Url}'.", url);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error resetting health state for subscriber '{Url}'.", url);
        }
    }

    private async Task SaveHealthStateAsync(CircuitBreakerSubscriberHealthState state, CancellationToken cancellationToken)
    {
        var cacheKey = GetCacheKey(state.Url);
        var serialized = JsonSerializer.Serialize(state);

        var options = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_options.CacheExpirationMinutes) };

        await cache.SetStringAsync(cacheKey, serialized, options, cancellationToken);
    }

    private static string GetCacheKey(string url)
    {
        // Use SHA256 hash of URL to create a safe, consistent cache key
        var urlBytes = Encoding.UTF8.GetBytes(url);
        var hashBytes = SHA256.HashData(urlBytes);
        var hashString = Convert.ToBase64String(hashBytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');

        return $"{CacheKeyPrefix}{hashString}";
    }
}
