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
    /// <c>MapsterMapper.IMapper</c>) for a service. Nullable-enum string conversion (see
    /// <see cref="NullableEnumMapsterRegistration.RegisterNullableEnumRule"/>) is registered globally here — it
    /// applies to every Nullable&lt;enum&gt; in every assembly automatically, with no per-service registration
    /// needed. <paramref name="additionalAssemblies"/> is only relevant to <c>IRegister</c> discovery
    /// (<c>config.Scan</c>) below, not to enum handling.
    /// </summary>
    public static IServiceCollection AddZgwMapster(this IServiceCollection services, Assembly callingAssembly, params Assembly[] additionalAssemblies)
    {
        var commonWebAssembly = typeof(MapsterServiceCollectionExtensions).Assembly;
        var assemblies = new[] { callingAssembly, commonWebAssembly }.Concat(additionalAssemblies).Distinct().ToArray();

        var config = new TypeAdapterConfig();

        // Defense-in-depth against unbounded recursion on a cyclic object graph (e.g. an EF Core
        // navigation-property loop). At this depth, Mapster returns a default value instead of
        // recursing further, rather than crashing the process with an uncatchable
        // StackOverflowException.
        // Measured 2026-09-04 across the richest service's 166 registered pairs: the deepest destination
        // TYPE graph is 8 levels. 200 is therefore ~25x headroom, and deliberately not tightened to fit
        // that number — a self-referential entity (deel-zaaktypen chains, relevanteAndereZaken) nests by
        // DATA at runtime, which the type graph does not bound. The failure mode of a cap that is too low
        // is silent: Mapster returns a default value for the truncated member instead of erroring, so
        // trading headroom for earlier cycle detection would risk quietly dropping real data. Also clears
        // the synthetic 100-deep health test
        // (MapsterSeamHealthTests.Deeply_nested_acyclic_graph_maps_without_stack_overflow).
        config.Default.MaxDepth(200);

        // Parity with AutoMapper's default (AllowNullCollections = false): a null source collection
        // maps to an empty destination collection rather than null. Applied globally so every
        // service's collection members behave like the AutoMapper baseline without per-register
        // null-coalescing. See MapsterSeamHealthTests.Null_source_collection_maps_to_empty_not_null.
        //
        // SCOPE: destination MEMBERS only, never the destination ROOT. A destination transform runs
        // as part of mapping a member, so `Map<List<T>>(null)` still returns null here where
        // AutoMapper returned an empty list — the one place this parity does NOT hold. Callers that
        // dereference the result of a collection-root Map (e.g. `.ForEach(...)`) must not rely on it
        // being non-null; every such caller in the repo today feeds it a materialised EF list.
        // Pinned by MapsterSeamHealthTests.Null_source_collection_ROOT_maps_to_null_unlike_a_member.
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

        services.AddScoped<IRequestMerger, RequestMerger>();

        return services;
    }
}
