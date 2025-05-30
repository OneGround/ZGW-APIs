using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Roxit.ZGW.Common.Authentication;
using Roxit.ZGW.Common.Caching;
using Roxit.ZGW.Common.Configuration;
using Roxit.ZGW.Common.Services;
using StackExchange.Redis;

namespace Roxit.ZGW.Common.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddAppSettingsServiceEndpoints(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMemoryCache();
        services.AddSingleton<IServiceDiscovery, EndpointsAppSettingsServiceDiscovery>();
        services.Configure<EndpointConfiguration>(configuration.GetSection("Services"));
    }

    public static IServiceCollection AddZGWSecretManager(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ZgwServiceAccountConfiguration>(configuration.GetSection("ZgwServiceAccountCredentials"));
        return services.AddSingleton<ICachedZGWSecrets, CachedZGWSecrets>();
    }

    public static IServiceCollection AddRedisCacheInvalidation(this IServiceCollection services)
    {
        return services.AddSingleton<ICacheInvalidator, CacheInvalidator>().AddRedisCache();
    }

    public static IServiceCollection AddRedisCache(this IServiceCollection services)
    {
        services.AddSingleton(GetRedisConfiguration);
        services.AddSingleton<IConnectionMultiplexer>(sp => ConnectionMultiplexer.Connect(sp.GetRequiredService<ConfigurationOptions>()));
        services.AddStackExchangeRedisCache(_ => { });
        services
            .AddOptions<RedisCacheOptions>()
            .Configure<IServiceProvider>(
                (options, sp) =>
                {
                    var redisOptions = sp.GetRequiredService<ConfigurationOptions>();
                    var multiplexer = sp.GetRequiredService<IConnectionMultiplexer>();
                    options.ConfigurationOptions = redisOptions;
                    options.ConnectionMultiplexerFactory = () => Task.FromResult(multiplexer);
                }
            );

        return services;
    }

    public static IServiceCollection AddOrganisationContext(this IServiceCollection services)
    {
        return services
            .AddSingleton<IOrganisationContextAccessor, OrganisationContextAccessor>()
            .AddSingleton<IOrganisationContextFactory, OrganisationContextFactory>();
    }

    public static ConfigurationOptions GetRedisConfiguration(IServiceProvider sp)
    {
        var configuration = sp.GetRequiredService<IConfiguration>();
        var redisConnectionString = configuration.GetRequiredSection("Redis:ConnectionString");

        var redisOptions = ConfigurationOptions.Parse(redisConnectionString.Value);
        redisOptions.ClientName = "ZGW";

        return redisOptions;
    }
}
