using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace OneGround.ZGW.IntegrationTests.Common.Authentication;

public static class TestAuthenticationServiceCollectionExtensions
{
    /// <summary>
    /// Makes <see cref="TestAuthenticationHandler"/> the default scheme; call it from <c>ConfigureTestServices</c>, after the API's own schemes.
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
