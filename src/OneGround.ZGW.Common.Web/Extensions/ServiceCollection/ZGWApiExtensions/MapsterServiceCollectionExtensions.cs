using System.Reflection;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;

namespace OneGround.ZGW.Common.Web.Extensions.ServiceCollection.ZGWApiExtensions;

public static class MapsterServiceCollectionExtensions
{
    public static IServiceCollection AddZgwMapster(this IServiceCollection services, Assembly callingAssembly)
    {
        var commonWebAssembly = typeof(MapsterServiceCollectionExtensions).Assembly;
        var assemblies = new[] { callingAssembly, commonWebAssembly };

        var config = new TypeAdapterConfig();

        // Note: Mapster only maps properties/fields, not methods, so AutoMapper's
        // ShouldMapMethod = _ => false has no equivalent here (methods are never mapped).

        // Parity with NullableEnumMapper: empty string maps to null for Nullable<enum>.
        // TODO(Task 4): re-enable once RegisterNullableEnumRules exists
        // config.RegisterNullableEnumRules(assemblies);

        // Discover IRegister mapping definitions in the service assembly and Common.Web.
        config.Scan(assemblies);

        services.AddSingleton(config);
        services.AddScoped<IMapper, ServiceMapper>();

        return services;
    }
}
