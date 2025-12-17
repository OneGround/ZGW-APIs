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

        services.AddSingleton<ICircuitBreakerSubscriberHealthTracker, RedisCircuitBreakerSubscriberHealthTracker>();

        return services;
    }
}
