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
using OneGround.ZGW.Documenten.Contracts.v1.Responses;
using OneGround.ZGW.Documenten.DataModel;
using OneGround.ZGW.Documenten.Web;
using OneGround.ZGW.Documenten.Web.MappingProfiles.v1;
using Xunit;

namespace OneGround.ZGW.Documenten.WebApi.UnitTests.MappingTests;

public class DrcMapsterWiringTests
{
    [Fact]
    public void AddZgwMapster_discovers_DRC_registers_and_runs_the_url_resolvers_through_DI()
    {
        var mockedUriService = new Mock<IEntityUriService>();
        mockedUriService.Setup(s => s.GetUri(It.IsAny<IUrlEntity>())).Returns("https://example.test/resolved-via-di");

        var services = new ServiceCollection();
        services.AddSingleton(mockedUriService.Object);
        services.AddZgwMapster(typeof(DomainToResponseRegister).Assembly, enable: true);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

        var latestInformatieObject = new EnkelvoudigInformatieObject { Id = Guid.NewGuid(), InformatieObjectType = "https://example.test/iot" };
        var latestVersion = new EnkelvoudigInformatieObjectVersie { Id = Guid.NewGuid(), LatestInformatieObject = latestInformatieObject };
        var source = new EnkelvoudigInformatieObject
        {
            Id = Guid.NewGuid(),
            InformatieObjectType = "https://example.test/iot",
            LatestEnkelvoudigInformatieObjectVersie = latestVersion,
        };

        var result = mapper.Map<EnkelvoudigInformatieObjectGetResponseDto>(source);

        // The mocked literal is distinguishable from any same-name convention copy of the source's own
        // Url, so this only passes if DomainToResponseRegister was discovered by config.Scan AND both
        // MapsterUrlResolver.ResolveUrl (for Url) and the .AfterMapping port's
        // MapContext.Current.GetService<IEntityUriService>() (for Inhoud) resolved through DI.
        Assert.Equal("https://example.test/resolved-via-di", result.Url);
        Assert.Equal("https://example.test/resolved-via-di", result.Inhoud);
        mockedUriService.Verify(s => s.GetUri(It.IsAny<IUrlEntity>()), Times.AtLeastOnce());
    }

    /// <summary>
    /// The shape every <c>GetAllAsync</c> uses — <c>Map&lt;List&lt;TResponseDto&gt;&gt;(pageResult)</c> —
    /// which no other fact in the suite exercises. It differs from a single-object root in that
    /// <c>MapsterUrlResolver</c> reads <c>MapContext.Current</c>, only present on the
    /// <c>ServiceMapper</c> path, from inside per-element <c>.AfterMapping</c> blocks. Asserts
    /// per-element URLs, not just the count: the count survives a broken resolver.
    /// </summary>
    [Fact]
    public void A_collection_root_resolves_urls_for_every_element()
    {
        using var host = new DrcMapperTestHost();

        var first = InformatieObjectWithLatestVersion();
        var second = InformatieObjectWithLatestVersion();

        var result = host.Mapper.Map<List<EnkelvoudigInformatieObjectGetResponseDto>>(new List<EnkelvoudigInformatieObject> { first, second });

        Assert.Equal(2, result.Count);
        Assert.Equal(DrcMapperTestHost.Resolved(first), result[0].Url);
        Assert.Equal(DrcMapperTestHost.Resolved(second), result[1].Url);
    }

    /// <summary>
    /// Every register's type pairs must survive into the shared config. <c>AddZgwMapster</c> scans all
    /// of DRC's registers into ONE <see cref="TypeAdapterConfig"/>, and Mapster's <c>NewConfig</c>
    /// REPLACES an existing pair rather than merging into it — unlike AutoMapper, where duplicate
    /// <c>CreateMap</c> calls for one TypePair accumulate onto the same TypeMap.
    /// </summary>
    /// <remarks>
    /// Easy to do by accident here because four API versions share several <c>Models.v1</c> destination
    /// types, so two declarations look version-specific in source while resolving to identical types.
    /// Scan order then picks a winner silently, and no per-register test can see it.
    /// </remarks>
    [Fact]
    public void No_register_silently_overwrites_another_registers_type_pair()
    {
        // Matches the assembly set AddZgwMapster itself scans (commonWebAssembly is currently free of
        // IRegister implementations, so this is not expected to add any types today, but a hand-rolled
        // scan here must not silently fall behind the production seam's set).
        var commonWebAssembly = typeof(MapsterServiceCollectionExtensions).Assembly;
        var registerTypes = new[] { typeof(Startup).Assembly, commonWebAssembly }
            .Distinct()
            .SelectMany(a => a.GetTypes())
            .Where(t => typeof(IRegister).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface)
            .OrderBy(t => t.FullName)
            .ToList();
        Assert.NotEmpty(registerTypes);

        // Builds one isolated TypeAdapterConfig per register and records which register first claimed
        // each type pair. A pair claimed by more than one register is a collision: Mapster's NewConfig
        // replaces rather than merges, so the merged production config would silently keep only the
        // last-scanned register's mapping for that pair.
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

    private static EnkelvoudigInformatieObject InformatieObjectWithLatestVersion()
    {
        var informatieObject = new EnkelvoudigInformatieObject
        {
            Id = Guid.NewGuid(),
            InformatieObjectType = "https://example.test/informatieobjecttypen/1",
        };

        informatieObject.LatestEnkelvoudigInformatieObjectVersie = new EnkelvoudigInformatieObjectVersie
        {
            Id = Guid.NewGuid(),
            LatestInformatieObject = informatieObject,
            BestandsDelen = [],
        };

        return informatieObject;
    }
}
