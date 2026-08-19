using System;
using System.Collections.Generic;
using System.Linq;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using OneGround.ZGW.Catalogi.Contracts.v1.Responses;
using OneGround.ZGW.Catalogi.DataModel;
using OneGround.ZGW.Catalogi.Web;
using OneGround.ZGW.Catalogi.Web.MappingProfiles.v1;
using OneGround.ZGW.Common.Web.Extensions.ServiceCollection.ZGWApiExtensions;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.DataAccess;
using Xunit;

namespace OneGround.ZGW.Catalogi.WebApi.UnitTests.MappingTests;

public class ZtcMapsterWiringTests
{
    [Fact]
    public void AddZgwMapster_discovers_ZTC_registers_and_runs_the_url_resolvers_through_DI()
    {
        var mockedUriService = new Mock<IEntityUriService>();
        mockedUriService.Setup(s => s.GetUri(It.IsAny<IUrlEntity>())).Returns("https://example.test/resolved-via-di");

        var services = new ServiceCollection();
        services.AddSingleton(mockedUriService.Object);
        services.AddZgwMapster(typeof(DomainToResponseRegister).Assembly, enable: true);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

        var source = new ZaakType
        {
            Id = Guid.NewGuid(),
            StatusTypen = [new StatusType { Id = Guid.NewGuid() }],
            ZaakTypeGerelateerdeZaakTypen = [],
        };
        var result = mapper.Map<ZaakTypeResponseDto>(source);

        // The mocked literal is distinguishable from any same-name convention copy of the source's own
        // Url, so this only passes if DomainToResponseRegister was discovered by config.Scan AND
        // MapsterUrlResolver.ResolveUrl/ResolveUrls both resolved IEntityUriService through DI.
        Assert.Equal("https://example.test/resolved-via-di", result.Url);
        Assert.Equal(new[] { "https://example.test/resolved-via-di" }, result.StatusTypen);
        mockedUriService.Verify(s => s.GetUri(It.IsAny<IUrlEntity>()), Times.AtLeastOnce());
    }

    /// <summary>
    /// The shape every <c>GetAllAsync</c> uses — <c>Map&lt;List&lt;TResponseDto&gt;&gt;(pageResult)</c> —
    /// which no other fact in the suite exercises: all the rest map a single object.
    /// </summary>
    /// <remarks>
    /// Worth its own fact because the collection root differs from the single-object root in two ways
    /// this migration depends on. <c>MapsterUrlResolver</c> reads <c>MapContext.Current</c>, which exists
    /// only on the <c>ServiceMapper</c> path, from inside per-element <c>.AfterMapping</c> blocks; and
    /// <c>DestinationTransform.EmptyCollectionIfNull</c> applies to the root as well as to members. A
    /// regression that breaks only the collection root — a plain <c>Mapper</c> substituted for
    /// <c>ServiceMapper</c>, say — would otherwise leave the whole suite green while all 18 ZTC list
    /// endpoints return 500s or relative URLs.
    /// <para>
    /// Asserts per-element URL resolution rather than just the element count: the count survives a
    /// broken resolver, the resolved URL does not.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_collection_root_resolves_urls_for_every_element()
    {
        var mockedUriService = new Mock<IEntityUriService>();
        mockedUriService.Setup(s => s.GetUri(It.IsAny<IUrlEntity>())).Returns<IUrlEntity>(e => $"https://example.test{e.Url}");

        var services = new ServiceCollection();
        services.AddSingleton(mockedUriService.Object);
        services.AddZgwMapster(typeof(DomainToResponseRegister).Assembly, enable: true);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

        var first = new ZaakType { Id = Guid.NewGuid(), ZaakTypeGerelateerdeZaakTypen = [] };
        var second = new ZaakType { Id = Guid.NewGuid(), ZaakTypeGerelateerdeZaakTypen = [] };

        var result = mapper.Map<List<ZaakTypeResponseDto>>(new List<ZaakType> { first, second });

        Assert.Equal(2, result.Count);
        Assert.Equal($"https://example.test{first.Url}", result[0].Url);
        Assert.Equal($"https://example.test{second.Url}", result[1].Url);
    }

    /// <summary>
    /// Every register's type pairs must survive into the shared config. <c>AddZgwMapster</c> scans all
    /// of ZTC's registers into ONE <see cref="TypeAdapterConfig"/>, and Mapster's <c>NewConfig</c>
    /// REPLACES an existing pair rather than merging into it — unlike AutoMapper, where duplicate
    /// <c>CreateMap</c> calls for one TypePair accumulate onto the same TypeMap.
    /// </summary>
    /// <remarks>
    /// Two registers declaring the same CLR pair is easy to do by accident because the v1.3 contracts
    /// reuse several <c>Contracts.v1</c> DTOs (GerelateerdeZaaktypeDto, EigenschapSpecificatieDto), so
    /// the two declarations look version-specific in source while resolving to identical types.
    /// Assembly scan order then decides which definition wins and which is silently discarded, and no
    /// per-register test can see it: each builds its own isolated config and therefore exercises a
    /// definition that may not be the one the service resolves at runtime.
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

        // Count each register's pairs in isolation, then compare against the merged config. Any
        // shortfall is a pair that one register overwrote for another.
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
