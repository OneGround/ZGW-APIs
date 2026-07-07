using System.Linq;
using System.Reflection;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Common.Web.Mapping.Mapster;

namespace OneGround.ZGW.Common.Web.Extensions.ServiceCollection.ZGWApiExtensions;

public static class MapsterServiceCollectionExtensions
{
    /// <summary>
    /// Nullable-enum rule discovery (see <see cref="NullableEnumMapsterRegistration.RegisterNullableEnumRules"/>) is
    /// scoped to <paramref name="callingAssembly"/>, this assembly, and <paramref name="additionalAssemblies"/>.
    /// Unlike the AutoMapper original (which matched any string-&gt;Nullable&lt;enum&gt; TypePair regardless of
    /// declaring assembly), callers must pass any assembly containing enum properties they map into (e.g. a
    /// service's *.DataModel assembly) via <paramref name="additionalAssemblies"/>, or no rule will be registered
    /// for those types and the mapping will fail or fall through to Mapster's default string-to-enum behavior
    /// (which throws on unknown values, unlike this rule).
    /// </summary>
    public static IServiceCollection AddZgwMapster(this IServiceCollection services, Assembly callingAssembly, params Assembly[] additionalAssemblies)
    {
        var commonWebAssembly = typeof(MapsterServiceCollectionExtensions).Assembly;
        var assemblies = new[] { callingAssembly, commonWebAssembly }.Concat(additionalAssemblies).Distinct().ToArray();

        var config = new TypeAdapterConfig();

        // Note: Mapster only maps properties/fields, not methods, so AutoMapper's
        // ShouldMapMethod = _ => false has no equivalent here (methods are never mapped).

        // Parity with NullableEnumMapper: empty string maps to null for Nullable<enum>.
        // NOTE: nullable-enum rule discovery is scoped to `assemblies` above. Unlike the AutoMapper
        // original (which matched any string->Nullable<enum> TypePair regardless of declaring
        // assembly), callers must pass any assembly containing enum properties they map into
        // (e.g. a service's *.DataModel assembly) via `additionalAssemblies`, or no rule will be
        // registered for those types and the mapping will fail or fall through to Mapster's default
        // string-to-enum behavior (which throws on unknown values, unlike this rule).
        config.RegisterNullableEnumRules(assemblies);

        // Discover IRegister mapping definitions in the service assembly and Common.Web.
        config.Scan(assemblies);

        services.AddSingleton(config);
        services.AddScoped<IMapper, ServiceMapper>();

        return services;
    }
}
