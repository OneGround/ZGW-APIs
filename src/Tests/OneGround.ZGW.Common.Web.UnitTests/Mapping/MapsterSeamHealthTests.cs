using System;
using System.Collections.Generic;
using System.Linq;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using OneGround.ZGW.Common.Web.Extensions.ServiceCollection.ZGWApiExtensions;
using OneGround.ZGW.Common.Web.Mapping.Mapster;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.DataAccess;
using Xunit;

namespace OneGround.ZGW.Common.Web.UnitTests.Mapping;

public class MapsterSeamHealthTests
{
    private sealed class NodeSource
    {
        public int Value { get; set; }
        public NodeSource Child { get; set; }
    }

    private sealed class NodeDto
    {
        public int Value { get; set; }
        public NodeDto Child { get; set; }
    }

    [Fact]
    public void AddZgwMapster_configuration_compiles_without_error()
    {
        var services = new ServiceCollection();
        services.AddZgwMapster(typeof(MapsterSeamHealthTests).Assembly, enable: true);
        using var provider = services.BuildServiceProvider();

        var config = provider.GetRequiredService<TypeAdapterConfig>();

        var exception = Record.Exception(() => config.Compile());

        Assert.Null(exception);
    }

    [Fact]
    public void Deeply_nested_acyclic_graph_maps_without_stack_overflow()
    {
        var services = new ServiceCollection();
        services.AddZgwMapster(typeof(MapsterSeamHealthTests).Assembly, enable: true);
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

        NodeSource head = null;
        for (var i = 0; i < 100; i++)
        {
            head = new NodeSource { Value = i, Child = head };
        }

        var result = mapper.Map<NodeDto>(head);

        var depth = 0;
        for (var node = result; node != null; node = node.Child)
        {
            depth++;
        }

        Assert.Equal(100, depth);
    }

    private sealed class CyclicNode
    {
        public int Value { get; set; }
        public CyclicNode Self { get; set; }
    }

    [Fact]
    public void Cyclic_self_referencing_graph_terminates_without_crashing()
    {
        var services = new ServiceCollection();
        services.AddZgwMapster(typeof(MapsterSeamHealthTests).Assembly, enable: true);
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

        var cyclic = new CyclicNode { Value = 1 };
        cyclic.Self = cyclic; // genuine cycle: object references itself

        var result = mapper.Map<CyclicNode>(cyclic);

        Assert.NotNull(result);
        Assert.Equal(1, result.Value);

        // Walk Self until it terminates (null) or we exceed a generous bound — must NOT infinitely recurse/hang.
        var depth = 0;
        var node = result;
        while (node?.Self != null && depth < 1000)
        {
            node = node.Self;
            depth++;
        }

        // Mapster's MaxDepth(200) substitutes a default (null) value once the recursion counter
        // reaches the cap, so a self-referencing chain terminates at exactly MaxDepth - 1 levels.
        // Asserting a tight range (not just "< 1000") pins the cap as the actual termination
        // cause, not merely "it eventually stopped for some reason."
        Assert.InRange(depth, 190, 199);
    }

    private enum SeamHealthColour
    {
        Red,
        Green,
    }

    private sealed class ColourHolder
    {
        public SeamHealthColour Colour { get; set; }
    }

    private sealed class ColourDto
    {
        public string Colour { get; set; }
    }

    // Mapster defaults to the enum NAME (not the numeric ordinal) for enum<->string conversion,
    // matching AutoMapper's implicit behavior. This is pinned centrally here — via the real
    // AddZgwMapster seam — because per-service mapping registers (e.g. AC's DomainToResponseRegister)
    // rely on this default without an explicit member map. If a future change to AddZgwMapster
    // (e.g. a NumericEnumMappingBehavior setting) alters this default, this test catches it.
    [Fact]
    public void Enum_to_string_conversion_produces_the_name_not_the_numeric_value()
    {
        var services = new ServiceCollection();
        services.AddZgwMapster(typeof(MapsterSeamHealthTests).Assembly, enable: true);
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

        var result = mapper.Map<ColourDto>(new ColourHolder { Colour = SeamHealthColour.Green });

        Assert.Equal(nameof(SeamHealthColour.Green), result.Colour);
    }

    private sealed class NestedItem
    {
        public string Code { get; set; }
    }

    private sealed class NestedItemDto
    {
        public string Code { get; set; }
        public string Description { get; set; }
    }

    // Pitfall found during Phase 1/AC migration (DomainToResponseRegister): a bare x.Adapt<T>() call
    // inside a register's Map lambda resolves against Mapster's ambient TypeAdapterConfig.GlobalSettings,
    // NOT the local TypeAdapterConfig being built by that register's own Register(TypeAdapterConfig
    // config) method. This is silent — no compile error, no runtime exception — so a nested/collection
    // member relying on a custom mapping rule (e.g. a computed field) quietly gets the WRONG value once
    // real data flows through. Pinned here because this exact pattern (mapping a collection property via
    // .Select(x => x.Adapt<T>())) is natural to write and will recur across every remaining Phase 1
    // service unless documented as a known fact of the seam. The fix is to pass the local config
    // explicitly: x.Adapt<TSource, TDest>(config).
    [Fact]
    public void Bare_Adapt_call_does_not_see_a_locally_built_configs_custom_rule()
    {
        var config = new TypeAdapterConfig();
        config.NewConfig<NestedItem, NestedItemDto>().Map(dest => dest.Description, src => $"computed-{src.Code}");
        config.Compile();

        var item = new NestedItem { Code = "A1" };

        var viaBareAdapt = item.Adapt<NestedItemDto>();
        var viaExplicitConfig = item.Adapt<NestedItem, NestedItemDto>(config);

        // The danger: the bare call silently loses the custom rule (Description stays null).
        Assert.Null(viaBareAdapt.Description);
        // The fix: passing the local config explicitly picks up the custom rule correctly.
        Assert.Equal("computed-A1", viaExplicitConfig.Description);
    }

    private sealed class CollectionSource
    {
        public List<string> Items { get; set; }
    }

    private sealed class CollectionDto
    {
        public List<string> Items { get; set; }
    }

    // Pins the global EmptyCollectionIfNull transform added in AddZgwMapster: a null source collection
    // must map to an empty (non-null) destination collection, matching AutoMapper's AllowNullCollections
    // = false default. Every service relies on this without per-register null-coalescing.
    [Fact]
    public void Null_source_collection_maps_to_empty_not_null()
    {
        var services = new ServiceCollection();
        services.AddZgwMapster(typeof(MapsterSeamHealthTests).Assembly, enable: true);
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

        var result = mapper.Map<CollectionDto>(new CollectionSource { Items = null });

        Assert.NotNull(result.Items);
        Assert.Empty(result.Items);
    }

    private enum SeamHealthNullableColour
    {
        Red,
        Green,
    }

    private sealed class NullableColourHolder
    {
        public string Colour { get; set; }
    }

    private sealed class NullableColourDto
    {
        public SeamHealthNullableColour? Colour { get; set; }
    }

    // Proves the global nullable-enum rule (registered once inside AddZgwMapster) requires zero
    // per-assembly registration: callingAssembly here is deliberately unrelated to where
    // SeamHealthNullableColour/NullableColourHolder/NullableColourDto are declared (this test
    // assembly), yet string->Nullable<enum> conversion still works correctly for all three cases.
    [Fact]
    public void Nullable_enum_conversion_requires_no_assembly_registration()
    {
        var services = new ServiceCollection();
        services.AddZgwMapster(typeof(Xunit.Assert).Assembly, enable: true); // deliberately unrelated assembly
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

        var valid = mapper.Map<NullableColourDto>(new NullableColourHolder { Colour = "Green" });
        var empty = mapper.Map<NullableColourDto>(new NullableColourHolder { Colour = "" });
        var unknown = mapper.Map<NullableColourDto>(new NullableColourHolder { Colour = "Purple" });

        Assert.Equal(SeamHealthNullableColour.Green, valid.Colour);
        Assert.Null(empty.Colour);
        Assert.Null(unknown.Colour);
    }

    private enum SeamHealthNonNullableColour
    {
        Red,
        Green,
    }

    private sealed class NonNullableColourHolder
    {
        public string Colour { get; set; }
    }

    private sealed class NonNullableColourDto
    {
        public SeamHealthNonNullableColour Colour { get; set; }
    }

    // Confirms the global nullable-enum rule (which matches only Nullable<enum> destinations) does NOT
    // affect non-nullable string->enum conversion, which must keep using Mapster's own default behavior
    // (e.g. AutorisatieResponseDto.Component / AutorisatieRequestDto.Component in AC are non-nullable
    // enums mapped from string — this pins that they remain unaffected by RegisterNullableEnumRule).
    [Fact]
    public void Non_nullable_enum_conversion_is_unaffected_by_the_nullable_enum_rule()
    {
        var services = new ServiceCollection();
        services.AddZgwMapster(typeof(MapsterSeamHealthTests).Assembly, enable: true);
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

        var result = mapper.Map<NonNullableColourDto>(new NonNullableColourHolder { Colour = "Green" });

        Assert.Equal(SeamHealthNonNullableColour.Green, result.Colour);
    }

    private sealed class MapToTargetColourHolder
    {
        public string Colour { get; set; }
    }

    private sealed class MapToTargetColourDto
    {
        public SeamHealthNullableColour? Colour { get; set; }
    }

    // Proves the MapToTarget gap fix: mapping ONTO an existing destination object (as opposed to
    // creating a new one) is a distinct Mapster code path (MapType.MapToTarget) driven by
    // Settings.ConverterToTargetFactory, not Settings.ConverterFactory. Before this fix, an empty
    // source string mapped onto an existing object with a non-null nullable-enum property fell
    // through to Mapster's own default handling and silently produced the enum's zero-value member
    // instead of null — reproducing the exact bug this whole mechanism exists to prevent. This test
    // goes through the real AddZgwMapster seam (not a bespoke TypeAdapterConfig), matching the EF
    // Core update-in-place pattern (mapper.Map(source, existingEntity)) used across the codebase.
    [Fact]
    public void MapToTarget_onto_existing_object_maps_empty_string_to_null_not_zero_value()
    {
        var services = new ServiceCollection();
        services.AddZgwMapster(typeof(MapsterSeamHealthTests).Assembly, enable: true);
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

        var existing = new MapToTargetColourDto { Colour = SeamHealthNullableColour.Green };

        var result = mapper.Map(new MapToTargetColourHolder { Colour = "" }, existing);

        Assert.Same(existing, result);
        Assert.Null(result.Colour);
    }

    private sealed class CaseProbeSource
    {
        public string Procestermijn { get; set; }
        public bool Burgerzaken { get; set; }
    }

    private sealed class CaseProbeDto
    {
        public string ProcesTermijn { get; set; }
        public bool BurgerZaken { get; set; }
    }

    // Regression: AutoMapper's default member matching is case-insensitive (a framework-global
    // default). Mapster's default (Exact) is case-sensitive and would silently drop members whose
    // source/destination names differ only by casing (e.g. RL's Resultaat.Procestermijn ->
    // ProcesTermijn). AddZgwMapster sets NameMatchingStrategy.IgnoreCase globally to reproduce
    // AutoMapper. IgnoreCase matches AutoMapper exactly without over-matching genuinely-different
    // names (verified empirically against both mappers). Flexible does NOT fix this — do not use it.
    [Fact]
    public void Member_names_differing_only_by_case_still_map()
    {
        var services = new ServiceCollection();
        services.AddZgwMapster(typeof(MapsterSeamHealthTests).Assembly, enable: true);
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

        var result = mapper.Map<CaseProbeDto>(new CaseProbeSource { Procestermijn = "value", Burgerzaken = true });

        Assert.Equal("value", result.ProcesTermijn);
        Assert.True(result.BurgerZaken);
    }

    private sealed class UrlProbeEntity : IUrlEntity
    {
        public string Url { get; set; }
    }

    private sealed class UrlsProbeSource
    {
        public IEnumerable<IUrlEntity> Items { get; set; }
    }

    private sealed class UrlsProbeDto
    {
        public IEnumerable<string> Items { get; set; }
    }

    public sealed class UrlsProbeRegister : IRegister
    {
        public void Register(TypeAdapterConfig config) =>
            config.NewConfig<UrlsProbeSource, UrlsProbeDto>().Map(d => d.Items, s => MapsterUrlResolver.ResolveUrls(s.Items));
    }

    // Pins the shared MapsterUrlResolver.ResolveUrls collection-URL helper (analogous to
    // ResolveUrl for single entities) through the real AddZgwMapster DI seam, including
    // IRegister discovery via config.Scan — the pattern every ZTC register that maps a
    // MemberUrlsResolver-shaped collection property will rely on.
    [Fact]
    public void ResolveUrls_maps_a_collection_of_entities_to_their_urls_via_DI()
    {
        var services = new ServiceCollection();
        var uriService = new Mock<IEntityUriService>();
        uriService.Setup(s => s.GetUri(It.IsAny<IUrlEntity>())).Returns<IUrlEntity>(e => $"https://example.test/{e.Url}");
        services.AddSingleton(uriService.Object);
        services.AddZgwMapster(typeof(MapsterSeamHealthTests).Assembly, enable: true);
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

        var src = new UrlsProbeSource
        {
            Items = new IUrlEntity[]
            {
                new UrlProbeEntity { Url = "a" },
                new UrlProbeEntity { Url = "b" },
            },
        };
        var result = mapper.Map<UrlsProbeDto>(src);

        Assert.Equal(new[] { "https://example.test/a", "https://example.test/b" }, result.Items);
    }

    private sealed class RootShapeProbeEntity : IUrlEntity
    {
        public string Url { get; set; }
    }

    private sealed class RootShapeProbeDto
    {
        public string Url { get; set; }
    }

    // A per-element resolver: each element's Url member calls MapsterUrlResolver.ResolveUrl,
    // which reads the DI-registered IEntityUriService off the ambient MapContext.Current. This
    // is the shape every real register uses for a single-entity->url member (e.g. Url on a
    // response DTO); the point of the facts below is what happens when the SOURCE being mapped
    // is a collection and the DESTINATION ROOT (not a member) is a collection type.
    public sealed class RootShapeProbeRegister : IRegister
    {
        public void Register(TypeAdapterConfig config) =>
            config.NewConfig<RootShapeProbeEntity, RootShapeProbeDto>().Map(d => d.Url, s => MapsterUrlResolver.ResolveUrl(s));
    }

    // ServiceMapper (registered scoped by AddZgwMapster) stashes the request's IServiceProvider on
    // MapContext.Current only for the duration of a single Map() call, then tears it down when
    // Map() returns. MapsterUrlResolver.ResolveUrl reads that ambient context to resolve
    // IEntityUriService. When the DESTINATION ROOT is List<T> (or IList<T>/ICollection<T> — see
    // below), Mapster materializes the whole list eagerly, inside Map(), while the context is
    // still live, so every element's resolver call succeeds before Map() returns. Asserting the
    // resolved urls (not just Count) matters: a broken resolver that silently returned null per
    // element would still leave the count intact.
    [Fact]
    public void List_destination_root_resolves_every_elements_url()
    {
        var services = new ServiceCollection();
        var uriService = new Mock<IEntityUriService>();
        uriService.Setup(s => s.GetUri(It.IsAny<IUrlEntity>())).Returns<IUrlEntity>(e => $"https://example.test/{e.Url}");
        services.AddSingleton(uriService.Object);
        services.AddZgwMapster(typeof(MapsterSeamHealthTests).Assembly, enable: true);
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

        var src = new List<RootShapeProbeEntity>
        {
            new() { Url = "a" },
            new() { Url = "b" },
        };

        var result = mapper.Map<List<RootShapeProbeDto>>(src);

        Assert.Equal(new[] { "https://example.test/a", "https://example.test/b" }, result.Select(d => d.Url));
    }

    // The bug this pins: when the DESTINATION ROOT is IEnumerable<T> (as opposed to List<T>,
    // IList<T> or ICollection<T> — all of which materialize eagerly, see the facts below), Mapster
    // returns a LAZY Select projection instead of a materialized collection. Map() itself returns
    // successfully — no element has been projected yet. The per-element resolver calls only run
    // when the CALLER enumerates the result, which in production happens after the request's
    // ServiceMapper.Map() call (and its ambient MapContext.Current) has already gone out of scope,
    // so IEntityUriService can no longer be resolved. This fails ONLY for a non-empty source: an
    // empty result enumerates zero elements and never touches the resolver, so it looks healthy in
    // testing and in any production request that happens to return no rows.
    //
    // The two assertions below are deliberately split so the test proves ORDERING, not just that
    // something somewhere throws: Map() must complete without throwing (matching what happens in
    // production, where the scope looks fine until enumeration), and only the subsequent
    // enumeration — performed immediately, still inside this DI scope, because the ambient context
    // is torn down when Map() returns, not when the scope is disposed — must throw.
    //
    // If a future Mapster release starts materializing IEnumerable<T> destination roots eagerly,
    // this fact will fail (the ToList() call will no longer throw). That failure is the signal to
    // revisit this constraint — e.g. re-check whether IEnumerable<T> can be dropped from the list of
    // unsafe destination roots documented at the seam — not a reason to delete or weaken the fact.
    [Fact]
    public void IEnumerable_destination_root_defers_the_per_element_resolver_and_throws_on_enumeration()
    {
        var services = new ServiceCollection();
        var uriService = new Mock<IEntityUriService>();
        uriService.Setup(s => s.GetUri(It.IsAny<IUrlEntity>())).Returns<IUrlEntity>(e => $"https://example.test/{e.Url}");
        services.AddSingleton(uriService.Object);
        services.AddZgwMapster(typeof(MapsterSeamHealthTests).Assembly, enable: true);
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

        var src = new List<RootShapeProbeEntity>
        {
            new() { Url = "a" },
            new() { Url = "b" },
        };

        // Map() itself must succeed — the destination root is IEnumerable<T>, so this only builds
        // the lazy projection and never touches the resolver.
        var result = mapper.Map<IEnumerable<RootShapeProbeDto>>(src);

        // Enumerating — still inside the same DI scope, immediately after Map() returns — is what
        // actually runs the per-element resolver, and it now runs after ServiceMapper has already
        // torn down MapContext.Current for this Map() call.
        var exception = Record.Exception(() => result.ToList());

        Assert.NotNull(exception);
        Assert.Contains("ServiceAdapter", exception.Message);
    }

    // Pins the empirically-established bound: List<T> (covered above), IList<T> and ICollection<T>
    // destination roots are ALL materialized eagerly, same as List<T> — only IEnumerable<T> defers.
    // This exists so a future reader does not over-generalize the fact above to "every
    // interface-typed destination root is unsafe" and start wrapping IList<T>/ICollection<T> return
    // types in defensive ToList() calls that they don't need.
    [Fact]
    public void IList_and_ICollection_destination_roots_are_materialized_and_resolve_every_elements_url()
    {
        var services = new ServiceCollection();
        var uriService = new Mock<IEntityUriService>();
        uriService.Setup(s => s.GetUri(It.IsAny<IUrlEntity>())).Returns<IUrlEntity>(e => $"https://example.test/{e.Url}");
        services.AddSingleton(uriService.Object);
        services.AddZgwMapster(typeof(MapsterSeamHealthTests).Assembly, enable: true);
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

        var src = new List<RootShapeProbeEntity>
        {
            new() { Url = "a" },
            new() { Url = "b" },
        };
        var expected = new[] { "https://example.test/a", "https://example.test/b" };

        IList<RootShapeProbeDto> listResult = mapper.Map<IList<RootShapeProbeDto>>(src);
        ICollection<RootShapeProbeDto> collectionResult = mapper.Map<ICollection<RootShapeProbeDto>>(src);

        Assert.Equal(expected, listResult.Select(d => d.Url));
        Assert.Equal(expected, collectionResult.Select(d => d.Url));
    }

    private sealed class InterfaceTypedEntity : IBaseEntity
    {
        public Guid Id { get; set; }
        public string Naam { get; set; }
    }

    private sealed class InterfaceTypedEntityDto
    {
        public string Naam { get; set; }
        public string Weergave { get; set; }
    }

    // AuditTrailServiceBase.SetOld/SetNew pass the entity as IBaseEntity, so the entire audit trail
    // depends on Mapster resolving the map from source.GetType(), not the declared type. If it
    // resolved on IBaseEntity there would be no registered map and Weergave would come back null,
    // which is why this asserts the custom-mapped member rather than just "not null".
    // The BOUNDARY of the parity pinned by Null_source_collection_maps_to_empty_not_null above, asserted
    // as a contrast in one fact because the member case on its own reads as if the parity were global.
    // EmptyCollectionIfNull is a destination-MEMBER transform, so it never runs for a destination ROOT:
    // mapper.Map<List<T>>(null) returns null, where AutoMapper returned an empty list (measured against
    // AutoMapper 14.0.0). This is the one documented place the AllowNullCollections parity does not hold,
    // and it is why a caller must not dereference the result of a collection-root Map — every GetAll in
    // the repo feeds it a materialised EF list, which is what keeps that safe today rather than luck.
    [Fact]
    public void Null_source_collection_ROOT_maps_to_null_unlike_a_member()
    {
        var services = new ServiceCollection();
        services.AddZgwMapster(typeof(MapsterSeamHealthTests).Assembly, enable: true);
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

        object nullSource = null;
        var root = mapper.Map<List<NestedItemDto>>(nullSource);
        var member = mapper.Map<CollectionDto>(new CollectionSource { Items = null });

        Assert.Null(root);
        Assert.NotNull(member.Items);
        Assert.Empty(member.Items);
    }

    private sealed class GuardProbeInner
    {
        public string Value { get; set; }
    }

    private sealed class GuardProbeSource
    {
        public string Text { get; set; }
        public GuardProbeInner Inner { get; set; }
    }

    private sealed class GuardProbeDto
    {
        public string ViaArgument { get; set; }
        public string ViaReceiver { get; set; }
        public string ViaPlainChain { get; set; }
    }

    /// <summary>Stands in for a raw parse (JToken.Parse, PeriodPattern.Parse, Guid.Parse): rejects null.</summary>
    private static string Required(string value) => value ?? throw new ArgumentNullException(nameof(value));

    // A plain member chain is null-guarded by BOTH mappers — the shape that needs no ternary. Pinned as
    // the counterpart to the two facts below so the line between "guarded" and "not guarded" is on record
    // rather than re-derived per service: it is the presence of a method call, not the depth of the chain.
    [Fact]
    public void A_plain_source_member_chain_is_null_guarded_like_AutoMapper()
    {
        var config = new TypeAdapterConfig();
        config
            .NewConfig<GuardProbeSource, GuardProbeDto>()
            .Map(d => d.ViaPlainChain, s => s.Inner.Value)
            .Ignore(d => d.ViaArgument)
            .Ignore(d => d.ViaReceiver);
        var mapper = new Mapper(config);

        var result = mapper.Map<GuardProbeDto>(new GuardProbeSource { Inner = null });

        Assert.Null(result.ViaPlainChain);
    }

    // DIVERGENCE from AutoMapper, deliberately left unguarded — do not "fix" this by adding ternaries
    // across the registers. AutoMapper rewrote a MapFrom expression with null-propagation and
    // short-circuited the WHOLE expression to default when any source member in it was null, so the callee
    // was never invoked; Mapster invokes it and passes the null in. Measured on AutoMapper 14.0.0 /
    // Mapster 10.0.11: `MapFrom(s => Required(s.Text))` with Text null → AutoMapper null, Mapster throws.
    //
    // Whether that is a defect at a given site is TWO factors, not one: the callee's null behaviour AND
    // whether the field is optional in that service's validator. OneGroundFluentValidationActionFilter is
    // a global pre-action filter, so a missing REQUIRED field answers 400 before any controller Map runs —
    // which is what makes the remaining raw-parse sites safe. Guarding blindly would instead clear a
    // genuinely optional field. Audit new sites with: grep 'Parse(src\.' and 'src\.\w+\.\w+\('.
    [Fact]
    public void A_source_member_passed_as_a_method_ARGUMENT_is_not_null_guarded()
    {
        var config = new TypeAdapterConfig();
        config
            .NewConfig<GuardProbeSource, GuardProbeDto>()
            .Map(d => d.ViaArgument, s => Required(s.Text))
            .Ignore(d => d.ViaReceiver)
            .Ignore(d => d.ViaPlainChain);
        var mapper = new Mapper(config);

        Assert.Throws<ArgumentNullException>(() => mapper.Map<GuardProbeDto>(new GuardProbeSource { Text = null }));
    }

    // The other half of the same divergence, and the half that was missed once: AutoMapper's
    // null-propagation covered the method RECEIVER too, not just arguments. `MapFrom(s => s.Text.TrimEnd())`
    // with Text null returned null on AutoMapper and throws NullReferenceException on Mapster. Recorded
    // separately because "arguments" alone reads as if `src.A.Method()` were parity, which it is not for a
    // reference-typed A. (For a value type or Nullable<T> receiver neither mapper throws.)
    [Fact]
    public void A_source_member_used_as_a_method_RECEIVER_is_not_null_guarded_either()
    {
        var config = new TypeAdapterConfig();
        config
            .NewConfig<GuardProbeSource, GuardProbeDto>()
            .Map(d => d.ViaReceiver, s => s.Text.TrimEnd('/'))
            .Ignore(d => d.ViaArgument)
            .Ignore(d => d.ViaPlainChain);
        var mapper = new Mapper(config);

        Assert.Throws<NullReferenceException>(() => mapper.Map<GuardProbeDto>(new GuardProbeSource { Text = null }));
    }

    [Fact]
    public void Map_of_an_interface_typed_source_resolves_on_the_runtime_type()
    {
        var config = new TypeAdapterConfig();
        config.NewConfig<InterfaceTypedEntity, InterfaceTypedEntityDto>().Map(dest => dest.Weergave, src => "van-de-concrete-map");
        config.Compile();
        var mapper = new Mapper(config);

        IBaseEntity entity = new InterfaceTypedEntity { Id = Guid.NewGuid(), Naam = "naam" };

        var result = mapper.Map<InterfaceTypedEntityDto>(entity);

        Assert.Equal("van-de-concrete-map", result.Weergave);
        Assert.Equal("naam", result.Naam);
    }
}
