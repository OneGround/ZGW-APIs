using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Common.Caching;
using StackExchange.Redis;

namespace OneGround.ZGW.Common.Extensions;

public static class RedisCacheExtensions
{
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

    public static ConfigurationOptions GetRedisConfiguration(IServiceProvider sp)
    {
        var configuration = sp.GetRequiredService<IConfiguration>();
        var redisConnectionString = configuration.GetRequiredSection("Redis:ConnectionString");

        var redisOptions = ConfigurationOptions.Parse(redisConnectionString.Value);
        redisOptions.ClientName = "ZGW";

        return redisOptions;
    }

    public static IServiceCollection AddRedisCacheInvalidation(this IServiceCollection services)
    {
        return services.AddSingleton<ICacheInvalidator, CacheInvalidator>().AddRedisCache();
    }
}
