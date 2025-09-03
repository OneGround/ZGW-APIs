using System;
using System.Net.Http;
using Duende.AspNetCore.Authentication.OAuth2Introspection;
using Duende.IdentityModel.Client;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using OneGround.ZGW.Common.Authentication;
using OneGround.ZGW.Common.Configuration;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.CorrelationId;
using OneGround.ZGW.Common.Extensions;
using OneGround.ZGW.Common.Web.Authorization;
using Polly;

namespace OneGround.ZGW.Common.Web.Authentication;

public static class ZGWAuthenticationServiceCollectionExtensions
{
    public static void AddZgwAuthentication<TAuthorizationResolver>(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment hostEnvironment
    )
        where TAuthorizationResolver : class, IAuthorizationResolver
    {
        services.Configure<ZgwAuthConfiguration>(configuration.GetSection("Auth"));

        services.AddSingleton<IAuthorizationContextAccessor, AuthorizationContextAccessor>();
        services.AddScoped<IAuthorizationResolver, TAuthorizationResolver>();

        services.AddOrganisationContext();
        services.AddZGWSecretManager(configuration);

        services
            .AddAuthentication(ZgwAuthenticationConstants.PolicySelectorAuthenticationScheme)
            .AddJwtBearer(
                ZgwAuthenticationConstants.OAuth2AuthenticationScheme,
                options =>
                {
                    options.SaveToken = true;
                    options.RequireHttpsMetadata = !hostEnvironment.IsLocal();
                }
            )
            .AddOAuth2Introspection(
                ZgwAuthenticationConstants.ZgwTokenIntrospectionAuthenticationScheme,
                options =>
                {
                    options.IntrospectionEndpoint = configuration.GetValue<string>("Auth:ZgwTokenIntrospectionEndpoint");
                    options.EnableCaching = true;
                    options.CacheKeyPrefix = "ZGW:TokenIntrospections:";
                    options.CacheDuration = TimeSpan.FromMinutes(5);
                }
            )
            .AddPolicyScheme(
                ZgwAuthenticationConstants.PolicySelectorAuthenticationScheme,
                null,
                options =>
                {
                    options.ForwardDefaultSelector = AuthenticationSchemeSelector.SelectAuthenticationScheme;
                }
            );
        services
            .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<ZgwAuthConfiguration>>(
                (jwtBearerOptions, options) =>
                {
                    var authConfig = options.Value;

                    jwtBearerOptions.Authority = authConfig.Authority;
                    jwtBearerOptions.TokenValidationParameters.ValidIssuer = authConfig.ValidIssuer;
                    jwtBearerOptions.Audience = authConfig.ValidAudience;
                }
            );

        services.AddRedisCache();
    }

    public static void RegisterZgwTokenClient(this IServiceCollection services, IConfiguration configuration, IHostEnvironment hostEnvironment)
    {
        services.Configure<ZgwAuthConfiguration>(configuration.GetSection("Auth"));

        services.AddMemoryCache();
        services.AddSingleton<IZgwTokenCacheService, ZgwTokenCacheService>();

        services
            .AddHttpClient(
                ServiceRoleName.IDP,
                (provider, client) =>
                {
                    var authenticationOptions = provider.GetRequiredService<IOptions<ZgwAuthConfiguration>>();
                    client.BaseAddress = new Uri(authenticationOptions.Value.Authority);
                }
            )
            .AddHttpMessageHandler<CorrelationIdHandler>()
            .AddResilienceHandler(
                "token-resilience",
                builder =>
                {
                    builder.AddRetry(
                        new HttpRetryStrategyOptions
                        {
                            MaxRetryAttempts = 3,
                            BackoffType = DelayBackoffType.Exponential,
                            Delay = TimeSpan.FromSeconds(1),
                        }
                    );
                }
            );

        services.AddSingleton<IZgwAuthDiscoveryCache, ZgwAuthDiscoveryCache>(provider =>
        {
            var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
            var authenticationOptions = provider.GetRequiredService<IOptions<ZgwAuthConfiguration>>();
            var discoveryPolicy = new DiscoveryPolicy { RequireHttps = !hostEnvironment.IsLocal() };

            var discoveryCache = new DiscoveryCache(
                authenticationOptions.Value.Authority,
                () => httpClientFactory.CreateClient(ServiceRoleName.IDP),
                discoveryPolicy
            );

            return new ZgwAuthDiscoveryCache(discoveryCache);
        });

        services.AddTransient<IZgwTokenService, ZgwTokenService>();
    }
}
