using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace OneGround.ZGW.IntegrationTests.Common.Authentication;

public static class TestAuthenticationServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="TestAuthenticationHandler"/> and makes it the default scheme for authenticating, challenging and
    /// forbidding. Call it from <c>ConfigureTestServices</c>, so it runs after the API registered its own schemes.
    /// Only the authentication scheme is replaced: authorization (the <c>[Authorize]</c> attribute and the scope filters)
    /// still runs as it does in production.
    /// </summary>
    public static IServiceCollection AddTestAuthentication(this IServiceCollection services)
    {
        services
            .AddAuthentication()
            .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(TestAuthenticationDefaults.AuthenticationScheme, _ => { });

        services.PostConfigure<AuthenticationOptions>(options =>
        {
            options.DefaultScheme = TestAuthenticationDefaults.AuthenticationScheme;
            options.DefaultAuthenticateScheme = TestAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = TestAuthenticationDefaults.AuthenticationScheme;
            options.DefaultForbidScheme = TestAuthenticationDefaults.AuthenticationScheme;
        });

        return services;
    }
}
