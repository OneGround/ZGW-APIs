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

        // Defense-in-depth against unbounded recursion on a cyclic object graph (e.g. an EF Core
        // navigation-property loop). AutoMapper's parallel path in this seam has no equivalent
        // guard and remains exposed to the same class of risk — this only protects the Mapster
        // side. At this depth, Mapster returns a default value instead of recursing further,
        // rather than crashing the process with an uncatchable StackOverflowException. 200 is not
        // derived from any real domain-graph measurement — Phase 0 has no real profiles yet — it
        // is chosen only to clear the current synthetic 100-deep health test
        // (MapsterSeamHealthTests.Deeply_nested_acyclic_graph_maps_without_stack_overflow) with
        // headroom. Phase 1 should revisit this value once real mapping depths are known.
        config.Default.MaxDepth(200);

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
