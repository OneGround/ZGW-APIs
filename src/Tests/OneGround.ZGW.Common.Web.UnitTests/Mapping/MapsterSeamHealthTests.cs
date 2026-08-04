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

    // AuditTrailServiceBase.SetOld/SetNew pass the entity as IBaseEntity. A later step in this
    // migration routes the audit trail through Mapster, at which point the entire audit trail will
    // depend on Mapster resolving the map from source.GetType(), not the declared type. If it
    // resolved on IBaseEntity there would be no registered map and Weergave would come back null,
    // which is why this asserts the custom-mapped member rather than just "not null".
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
