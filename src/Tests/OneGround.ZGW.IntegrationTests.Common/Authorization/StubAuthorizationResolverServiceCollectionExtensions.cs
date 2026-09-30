using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OneGround.ZGW.Common.Web.Authorization;

namespace OneGround.ZGW.IntegrationTests.Common.Authorization;

public static class StubAuthorizationResolverServiceCollectionExtensions
{
    /// <summary>
    /// Replaces the API's <see cref="IAuthorizationResolver"/> with <paramref name="resolver"/>. Call it from <c>ConfigureTestServices</c>.
    /// </summary>
    public static IServiceCollection AddStubAuthorizationResolver(this IServiceCollection services, StubAuthorizationResolver resolver)
    {
        services.RemoveAll<IAuthorizationResolver>();
        services.AddSingleton<IAuthorizationResolver>(resolver);

        return services;
    }
}
