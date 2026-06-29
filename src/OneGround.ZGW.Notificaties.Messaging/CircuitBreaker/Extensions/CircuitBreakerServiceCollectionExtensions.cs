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
            .Validate(
                o => o.BreakDuration > TimeSpan.Zero,
                $"{CircuitBreakerOptions.CircuitBreaker}: {nameof(CircuitBreakerOptions.BreakDuration)} must be greater than zero."
            )
            .Validate(
                o => TimeSpan.FromMinutes(o.CacheExpirationMinutes) > o.BreakDuration,
                $"{CircuitBreakerOptions.CircuitBreaker}: {nameof(CircuitBreakerOptions.CacheExpirationMinutes)} must exceed {nameof(CircuitBreakerOptions.BreakDuration)} so an open circuit survives until its half-open transition."
            )
            .Validate(
                o => o.BlockSubscriptionAfter > o.BreakDuration,
                $"{CircuitBreakerOptions.CircuitBreaker}: {nameof(CircuitBreakerOptions.BlockSubscriptionAfter)} must be greater than {nameof(CircuitBreakerOptions.BreakDuration)}."
            )
            .ValidateOnStart();

        services.AddSingleton<ICircuitBreakerSubscriberHealthTracker, RedisCircuitBreakerSubscriberHealthTracker>();

        return services;
    }
}
