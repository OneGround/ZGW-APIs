using System.Linq;
using System.Reflection;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OneGround.ZGW.Common.Web.Mapping;
using OneGround.ZGW.Common.Web.Mapping.Mapster;
using OneGround.ZGW.Common.Web.Services;

namespace OneGround.ZGW.Common.Web.Extensions.ServiceCollection.ZGWApiExtensions;

public static class MapsterServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Mapster mapping seam (a scanned <see cref="TypeAdapterConfig"/> plus a scoped
    /// <c>MapsterMapper.IMapper</c>) for a service. Opt-in per service: when <paramref name="enable"/> is
    /// <c>false</c> (the default) nothing is registered and the service keeps using AutoMapper only, so a
    /// service adopts Mapster by explicitly enabling it. Nullable-enum string conversion (see
    /// <see cref="NullableEnumMapsterRegistration.RegisterNullableEnumRule"/>) is registered globally here — it
    /// applies to every Nullable&lt;enum&gt; in every assembly automatically, with no per-service registration
    /// needed. <paramref name="additionalAssemblies"/> is only relevant to <c>IRegister</c> discovery
    /// (<c>config.Scan</c>) below, not to enum handling.
    /// </summary>
    public static IServiceCollection AddZgwMapster(
        this IServiceCollection services,
        Assembly callingAssembly,
        bool enable = false,
        params Assembly[] additionalAssemblies
    )
    {
        if (!enable)
        {
            return services;
        }

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

        // Parity with AutoMapper's default (AllowNullCollections = false): a null source collection
        // maps to an empty destination collection rather than null. Applied globally so every
        // service's collection members behave like the AutoMapper baseline without per-register
        // null-coalescing. See MapsterSeamHealthTests.Null_source_collection_maps_to_empty_not_null.
        config.Default.AddDestinationTransform(DestinationTransform.EmptyCollectionIfNull);

        // Parity with AutoMapper's default member matching, which is case-insensitive (a
        // framework-global default). Mapster's default (NameMatchingStrategy.Exact) is
        // case-sensitive and would silently drop members whose source/destination names differ
        // only by casing. IgnoreCase reproduces AutoMapper exactly without over-matching
        // genuinely-different names (verified empirically against both mappers). Because
        // AutoMapper is case-insensitive everywhere, this can only make services strictly closer
        // to the AutoMapper baseline — it can never introduce a divergence. Note: Flexible does
        // NOT achieve this (it also drops case-only mismatches) — do not substitute it. See
        // MapsterSeamHealthTests.Member_names_differing_only_by_case_still_map.
        config.Default.NameMatchingStrategy(NameMatchingStrategy.IgnoreCase);

        // Note: Mapster only maps properties/fields, not methods, so AutoMapper's
        // ShouldMapMethod = _ => false has no equivalent here (methods are never mapped).

        // Parity with NullableEnumMapper: empty/null string maps to null for Nullable<enum>; an
        // unrecognized name also maps to null (not an exception). This is a single global rule —
        // no per-service or per-assembly registration is needed; it covers every Nullable<enum> in
        // every assembly automatically (present or future), mirroring AutoMapper's original global
        // TypePair-based behavior.
        config.RegisterNullableEnumRule();

        // Discover IRegister mapping definitions in the service assembly and Common.Web.
        config.Scan(assemblies);

        services.AddSingleton(config);
        services.AddScoped<IMapper, ServiceMapper>();

        // Replaces the AutoMapper-backed default registered by AddAutoMapper. Relies on AddZGWApi
        // calling AddAutoMapper first; if those two calls were ever reordered this Replace would be
        // overwritten and a migrated service would silently fall back to AutoMapper.
        services.Replace(ServiceDescriptor.Scoped<IZgwMapper, MapsterZgwMapper>());

        // Registered only when Mapster is enabled, never unconditionally: a service that hasn't
        // enabled Mapster must fail to resolve this, not silently get a merger backed by an empty config.
        services.AddScoped<IZgwRequestMerger, ZgwRequestMerger>();

        return services;
    }
}
