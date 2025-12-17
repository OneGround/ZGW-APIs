using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Notificaties.Messaging.CircuitBreaker.Options;
using OneGround.ZGW.Notificaties.Messaging.CircuitBreaker.Services;

namespace OneGround.ZGW.Notificaties.Messaging.CircuitBreaker.Extensions;

public static class CircuitBreakerServiceCollectionExtensions
{
    public static IServiceCollection AddCircuitBreaker(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<CircuitBreakerOptions>()
            .Bind(configuration.GetSection(CircuitBreakerOptions.CircuitBreaker))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // var redisConnectionString = configuration["Redis:ConnectionString"];
        // if (!string.IsNullOrEmpty(redisConnectionString))
        // {
        //     services.AddStackExchangeRedisCache(options =>
        //     {
        //         options.Configuration = redisConnectionString;
        //         options.InstanceName = "NRC:";
        //     });
        // }
        // else
        // {
        //     // Fallback to in-memory cache for local development without Redis
        //     services.AddDistributedMemoryCache();
        // }

        services.AddSingleton<ICircuitBreakerSubscriberHealthTracker, RedisCircuitBreakerSubscriberHealthTracker>();
        // services.AddSingleton<CircuitBreakerSubscriberHealthJobFilter>();

        return services;
    }
}
