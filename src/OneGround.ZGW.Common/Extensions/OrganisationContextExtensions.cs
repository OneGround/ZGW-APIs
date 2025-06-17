using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Common.Authentication;

namespace OneGround.ZGW.Common.Extensions;

public static class OrganisationContextExtensions
{
    public static IServiceCollection AddOrganisationContext(this IServiceCollection services)
    {
        return services
            .AddSingleton<IOrganisationContextAccessor, OrganisationContextAccessor>()
            .AddSingleton<IOrganisationContextFactory, OrganisationContextFactory>();
    }
}
