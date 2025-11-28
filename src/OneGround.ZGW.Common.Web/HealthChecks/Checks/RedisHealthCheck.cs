using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace OneGround.ZGW.Common.Web.HealthChecks.Checks;

public class RedisHealthCheck(IConnectionMultiplexer redis) : IHealthCheck
{
    public const string HealthCheckName = "Redis";

    private readonly IConnectionMultiplexer _redis = redis ?? throw new ArgumentNullException(nameof(redis));

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_redis.IsConnected)
            {
                return HealthCheckResult.Unhealthy("Redis connection is not established");
            }

            var database = _redis.GetDatabase();
            var pingResult = await database.PingAsync();

            if (pingResult.TotalMilliseconds > 1000)
            {
                return HealthCheckResult.Degraded(
                    $"Redis is responding slowly: {pingResult.TotalMilliseconds:F2}ms",
                    data: new Dictionary<string, object>
                    {
                        { "ping_ms", pingResult.TotalMilliseconds },
                        { "endpoints", string.Join(", ", _redis.GetEndPoints().Select(e => e.ToString())) },
                    }
                );
            }

            return HealthCheckResult.Healthy(
                $"Redis is healthy (ping: {pingResult.TotalMilliseconds:F2}ms)",
                data: new Dictionary<string, object>
                {
                    { "ping_ms", pingResult.TotalMilliseconds },
                    { "endpoints", string.Join(", ", _redis.GetEndPoints().Select(e => e.ToString())) },
                }
            );
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                "Redis health check failed",
                exception: ex,
                data: new Dictionary<string, object> { { "error", ex.Message } }
            );
        }
    }
}
