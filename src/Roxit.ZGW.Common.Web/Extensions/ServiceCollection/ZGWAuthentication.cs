using System;
using IdentityModel.AspNetCore.OAuth2Introspection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Roxit.ZGW.Common.CorrelationId;
using Roxit.ZGW.Common.Extensions;
using Roxit.ZGW.Common.Web.Authorization;

namespace Roxit.ZGW.Common.Web.Extensions.ServiceCollection;

public static class ZGWAuthenticationServiceCollectionExtensions
{
    public static void AddZGWAuthentication<TAuthorizationResolver>(this IServiceCollection services, IConfiguration configuration)
        where TAuthorizationResolver : class, IAuthorizationResolver
    {
        services.AddSingleton<IAuthorizationContextAccessor, AuthorizationContextAccessor>();
        services.AddScoped<IAuthorizationResolver, TAuthorizationResolver>();

        services.AddOrganisationContext();
        services.AddZGWSecretManager(configuration);

        services
            .AddAuthentication(OAuth2IntrospectionDefaults.AuthenticationScheme)
            .AddOAuth2Introspection(options =>
            {
                options.Authority = configuration.GetValue<string>("Auth:ZgwLegacyAuthProviderUrl");
                options.DiscoveryPolicy.ValidateEndpoints = false;
                options.DiscoveryPolicy.RequireKeySet = false;
                options.EnableCaching = true;
                options.CacheKeyPrefix = "ZGW:TokenIntrospections:";
                options.CacheDuration = TimeSpan.FromMinutes(5);
            });

        services.AddRedisCache();
        services.AddHttpClient(OAuth2IntrospectionDefaults.BackChannelHttpClientName).AddHttpMessageHandler<CorrelationIdHandler>();
    }
}
