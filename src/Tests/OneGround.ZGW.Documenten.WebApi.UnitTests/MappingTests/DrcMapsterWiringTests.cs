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

        // The mocked literal can't be confused with a same-name convention copy of the source's own Url, so this
        // only passes if both MapsterUrlResolver.ResolveUrl (Url) and the .AfterMapping port's DI-resolved
        // IEntityUriService (Inhoud) actually ran through DI.
        Assert.Equal("https://example.test/resolved-via-di", result.Url);
        Assert.Equal("https://example.test/resolved-via-di", result.Inhoud);
        mockedUriService.Verify(s => s.GetUri(It.IsAny<IUrlEntity>()), Times.AtLeastOnce());
    }

    /// <summary>
    /// Covers the collection-root shape every <c>GetAllAsync</c> uses (<c>Map&lt;List&lt;TResponseDto&gt;&gt;</c>),
    /// where <c>MapsterUrlResolver</c> reads <c>MapContext.Current</c> from inside per-element
    /// <c>.AfterMapping</c> blocks. Asserts per-element URLs, not just the count -- the count alone survives a broken resolver.
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
    /// <c>AddZgwMapster</c> scans all of DRC's registers into ONE <see cref="TypeAdapterConfig"/>, and Mapster's
    /// <c>NewConfig</c> REPLACES an existing pair rather than merging into it (unlike AutoMapper's <c>CreateMap</c>,
    /// which accumulates). Easy to trigger by accident: several versions share <c>Models.v1</c> destination types,
    /// so two declarations can look version-specific while resolving to the same pair, and scan order then
    /// silently picks a winner that no per-register test can see.
    /// </summary>
    [Fact]
    public void No_register_silently_overwrites_another_registers_type_pair()
    {
        // Matches the assembly set AddZgwMapster itself scans, so a hand-rolled scan here can't silently
        // fall behind the production seam's set.
        var commonWebAssembly = typeof(MapsterServiceCollectionExtensions).Assembly;
        var registerTypes = new[] { typeof(Startup).Assembly, commonWebAssembly }
            .Distinct()
            .SelectMany(a => a.GetTypes())
            .Where(t => typeof(IRegister).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface)
            .OrderBy(t => t.FullName)
            .ToList();
        Assert.NotEmpty(registerTypes);

        // Records which register first claims each type pair; a pair claimed by more than one is a collision.
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
