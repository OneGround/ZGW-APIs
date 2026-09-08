using System;
using System.Collections.Generic;
using System.Linq;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using OneGround.ZGW.Common.Web.Extensions.ServiceCollection.ZGWApiExtensions;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.DataAccess;
using OneGround.ZGW.Zaken.Contracts.v1.Responses;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.Web;
using OneGround.ZGW.Zaken.Web.MappingProfiles.v1;
using Xunit;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.MappingTests;

public class ZrcMapsterWiringTests
{
    /// <summary>
    /// The only fact in this suite that varies the scanned assembly argument passed to
    /// <c>AddZgwMapster</c>, instead of going through the shared <see cref="ZrcMapperTestHost"/>, which
    /// always scans the same fixed assembly. That is what lets this fact detect a broken assembly scan:
    /// pointing the call at an assembly that declares no register reproduces the failure (see the observed
    /// failure mode below), which a host-based fact has no way to exercise.
    /// </summary>
    /// <remarks>
    /// Observed failure mode: pointing the seam at an assembly that declares no register — the data model
    /// assembly, <c>typeof(Zaak).Assembly</c> — fails on the url with
    /// <c>Assert.Equal() Failure: Strings differ</c>, expecting
    /// <c>"https://example.test/resolved-via-di"</c> and getting the entity's own relative
    /// <c>"/zaken/&lt;guid&gt;"</c>. That relative value is Mapster's same-name convention copy: with no
    /// register in scope the map still succeeds, quietly, on convention alone. Which is exactly why the mock
    /// returns a literal no convention copy can produce — a mock echoing <c>e.Url</c> would leave this fact
    /// green with the registers undiscovered.
    /// </remarks>
    [Fact]
    public void AddZgwMapster_discovers_ZRC_registers_and_runs_the_url_resolvers_through_DI()
    {
        var mockedUriService = new Mock<IEntityUriService>();
        mockedUriService.Setup(s => s.GetUri(It.IsAny<IUrlEntity>())).Returns("https://example.test/resolved-via-di");

        var services = new ServiceCollection();
        services.AddSingleton(mockedUriService.Object);
        services.AddZgwMapster(typeof(DomainToResponseRegister).Assembly);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

        var source = new Zaak { Id = Guid.NewGuid(), ZaakEigenschappen = [new ZaakEigenschap { Id = Guid.NewGuid() }] };
        var result = mapper.Map<ZaakResponseDto>(source);

        // The mocked literal is distinguishable from any same-name convention copy of the source's own
        // Url, so this only passes if DomainToResponseRegister was discovered by config.Scan AND
        // MapsterUrlResolver.ResolveUrl/ResolveUrls both resolved IEntityUriService through DI.
        Assert.Equal("https://example.test/resolved-via-di", result.Url);
        Assert.Equal(new[] { "https://example.test/resolved-via-di" }, result.Eigenschappen);
        mockedUriService.Verify(s => s.GetUri(It.IsAny<IUrlEntity>()), Times.AtLeastOnce());
    }

    /// <summary>
    /// The shape every <c>GetAllAsync</c> uses — <c>Map&lt;List&lt;TResponseDto&gt;&gt;(pageResult)</c> — which
    /// no other fact in the suite exercises. It differs from a single-object root in that
    /// <c>MapsterUrlResolver</c> reads <c>MapContext.Current</c>, only present on the <c>ServiceMapper</c>
    /// path, from inside per-element mapping. Asserts the per-element RESOLVED url, not the count: a count
    /// survives a broken resolver, and the host's mock PREFIXES the entity's relative <c>Url</c> so a
    /// same-named convention copy of that relative path fails the assertion.
    /// </summary>
    /// <remarks>
    /// Observed failure mode: deleting the
    /// <c>.Map(dest =&gt; dest.Url, src =&gt; MapsterUrlResolver.ResolveUrl(src))</c> line from the
    /// <c>Zaak -&gt; ZaakResponseDto</c> config fails on the first element with
    /// <c>Assert.Equal() Failure: Strings differ</c> — expected <c>"https://zrc.test/zaken/&lt;guid&gt;"</c>,
    /// actual <c>"/zaken/&lt;guid&gt;"</c>. The actual value is the entity's own relative <c>Url</c>, so the
    /// per-element assertion is what distinguishes a resolved url from the convention copy; a count
    /// assertion over the same result would have stayed green.
    /// </remarks>
    [Fact]
    public void A_List_collection_root_resolves_urls_for_every_element()
    {
        using var host = new ZrcMapperTestHost();

        var first = new Zaak { Id = Guid.NewGuid() };
        var second = new Zaak { Id = Guid.NewGuid() };

        var result = host.Mapper.Map<List<ZaakResponseDto>>(new List<Zaak> { first, second });

        Assert.Equal(ZrcMapperTestHost.Resolved(first), result[0].Url);
        Assert.Equal(ZrcMapperTestHost.Resolved(second), result[1].Url);
    }

    /// <summary>
    /// The constraint that forces every collection root in this service to name a MATERIALISED destination
    /// (<c>List&lt;T&gt;</c>) rather than <c>IEnumerable&lt;T&gt;</c>. An <c>IEnumerable&lt;T&gt;</c>
    /// destination root makes Mapster return a lazy <c>Select</c> projection; the scoped mapper establishes
    /// its ambient map context only for the duration of the <c>Map()</c> call and tears it down on return, so
    /// the per-element rules run later — on the caller's enumeration — with no context, and every rule that
    /// resolves a url through it throws. Materialising inside the mapping scope is what avoids that, which is
    /// why the fact above passes and this one asserts a throw.
    /// </summary>
    /// <remarks>
    /// Deliberately asserted rather than merely commented: only a NON-EMPTY source reaches the per-element
    /// rules, so an empty page looks perfectly healthy and no fixture returning one can catch this. Two GET
    /// endpoints shipped with an <c>IEnumerable</c> root and returned 500 on any non-empty result until this
    /// pair of facts surfaced it. If a future Mapster release materialises enumerable roots, this fact fails —
    /// that failure is the signal to revisit the constraint, not a reason to delete the fact.
    /// <para>
    /// Observed failure mode: this one needs no mutation to have been seen failing. It asserts the throw
    /// directly, and it was written from two shipped GET endpoints that returned 500 on exactly this shape —
    /// the fact is the reproduction. Empirically bounded while writing it: <c>List&lt;T&gt;</c>,
    /// <c>IList&lt;T&gt;</c> and <c>ICollection&lt;T&gt;</c> destination roots all materialise eagerly inside
    /// the mapping scope; only <c>IEnumerable&lt;T&gt;</c> defers.
    /// </para>
    /// </remarks>
    [Fact]
    public void An_IEnumerable_collection_root_loses_the_ambient_context_and_throws()
    {
        using var host = new ZrcMapperTestHost();

        var first = new ZaakBesluit { Id = Guid.NewGuid(), Besluit = "https://example.test/besluiten/1" };
        var second = new ZaakBesluit { Id = Guid.NewGuid(), Besluit = "https://example.test/besluiten/2" };
        var source = new List<ZaakBesluit> { first, second };

        // Map() itself does NOT throw - it only builds the lazy projection. The throw lands on enumeration,
        // which is exactly what makes this shape look healthy until real rows flow through it.
        var lazy = host.Mapper.Map<IEnumerable<ZaakBesluitResponseDto>>(source);

        var exception = Assert.Throws<InvalidOperationException>(() => lazy.ToList());
        Assert.Contains("ServiceAdapter", exception.Message);

        // The same pair through a materialised root resolves every element, proving the pair itself is sound
        // and the root shape is the whole difference.
        var materialised = host.Mapper.Map<List<ZaakBesluitResponseDto>>(source);
        Assert.Equal(ZrcMapperTestHost.Resolved(first), materialised[0].Url);
        Assert.Equal(ZrcMapperTestHost.Resolved(second), materialised[1].Url);
    }

    /// <summary>
    /// Every register's type pairs must survive into the shared config. <c>AddZgwMapster</c> scans all of
    /// this service's registers into ONE <see cref="TypeAdapterConfig"/>, and Mapster's <c>NewConfig</c>
    /// REPLACES an existing pair rather than merging into it, so a second declaration of a pair discards the
    /// first one's rules entirely.
    /// </summary>
    /// <remarks>
    /// Easy to do by accident: a later version's register may import an earlier version's contracts
    /// namespace, so an unqualified type name resolves to the earlier type and two declarations that read
    /// as version-specific in source are one pair. Scan order then picks a winner silently, and no
    /// per-register test can see it.
    /// <para>
    /// Observed failure mode: these registers really did carry duplicate declarations, and this fact named
    /// both colliding pairs together with every owning register —
    /// <c>NetTopologySuite.Geometries.Geometry -&gt; NetTopologySuite.Geometries.Geometry</c> (two registers)
    /// and <c>Contracts.v1._2.ObjectTypeOverigeDefinitieDto -&gt;
    /// DataModel.ZaakObject.ObjectTypeOverigeDefinitie</c>, which appeared TWICE because three registers
    /// declared it (every declaration past the first counts). Run it while both definitions still exist:
    /// once a pair is consolidated there is nothing left to observe.
    /// </para>
    /// </remarks>
    [Fact]
    public void No_register_silently_overwrites_another_registers_type_pair()
    {
        var registerTypes = typeof(Startup)
            .Assembly.GetTypes()
            .Where(t => typeof(IRegister).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface)
            .OrderBy(t => t.FullName)
            .ToList();
        Assert.NotEmpty(registerTypes);

        // Count each register's pairs in isolation, then compare against the merged config. Any shortfall
        // is a pair that one register overwrote for another.
        var owners = new Dictionary<string, string>();
        var duplicates = new List<string>();
        foreach (var registerType in registerTypes)
        {
            var isolated = new TypeAdapterConfig();
            ((IRegister)Activator.CreateInstance(registerType)).Register(isolated);

            foreach (var pair in isolated.RuleMap.Keys.Select(k => $"{k.Source.FullName} -> {k.Destination.FullName}"))
            {
                if (owners.TryGetValue(pair, out var firstOwner))
                {
                    duplicates.Add($"{pair}\n    declared by {firstOwner}\n    and by      {registerType.FullName}");
                }
                else
                {
                    owners[pair] = registerType.FullName;
                }
            }
        }

        Assert.True(
            duplicates.Count == 0,
            "These type pairs are declared by more than one IRegister. Mapster's NewConfig replaces, so "
                + "scan order decides which definition survives. Declare each pair exactly once:\n  "
                + string.Join("\n  ", duplicates)
        );
    }
}
