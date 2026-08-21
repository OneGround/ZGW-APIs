using System;
using System.Collections.Generic;
using System.Linq;
using Mapster;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Newtonsoft.Json.Linq;
using OneGround.ZGW.Common.Web.Extensions.ServiceCollection.ZGWApiExtensions;
using OneGround.ZGW.Common.Web.Mapping;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.DataAccess;
using OneGround.ZGW.Documenten.DataModel;
using OneGround.ZGW.Documenten.Web;
using Xunit;

namespace OneGround.ZGW.Documenten.WebApi.UnitTests.MappingTests;

/// <summary>
/// The mapping contracts DRC depends on OUTSIDE the Map calls its controllers make: the audit trail via
/// <see cref="IZgwMapper"/>, the PATCH merge via <see cref="IZgwRequestMerger"/>, and the expander. The
/// per-register tests build their own mapper and exercise none of these paths — they passed while all
/// three were broken.
/// </summary>
/// <remarks>
/// A regression here is silent, not loud: Mapster convention-maps instead of throwing, so an audit
/// record or a PATCH result comes back quietly wrong rather than erroring. Hence the adapter type is
/// asserted directly rather than inferred from a working map.
/// </remarks>
public class DrcMapperContractTests : IDisposable
{
    private readonly ServiceProvider _provider;
    private readonly IServiceScope _scope;
    private readonly IZgwMapper _zgwMapper;
    private readonly IZgwRequestMerger _zgwRequestMerger;

    public DrcMapperContractTests()
    {
        // Prefixing, never echoing -- see DrcMapperTestHost.BaseUrl. Shares that prefix so there is one
        // definition of "resolved" across the suite.
        var mockedUriService = new Mock<IEntityUriService>();
        mockedUriService.Setup(s => s.GetUri(It.IsAny<IUrlEntity>())).Returns<IUrlEntity>(DrcMapperTestHost.Resolved);

        var services = new ServiceCollection();
        services.AddSingleton(mockedUriService.Object);

        // Mirrors Startup exactly: same extensions, same order, same assembly, Mapster enabled.
        services.AddAutoMapper(typeof(Startup).Assembly);
        services.AddZgwMapster(typeof(Startup).Assembly, enable: true);

        _provider = services.BuildServiceProvider();
        _scope = _provider.CreateScope();
        _zgwMapper = _scope.ServiceProvider.GetRequiredService<IZgwMapper>();
        _zgwRequestMerger = _scope.ServiceProvider.GetRequiredService<IZgwRequestMerger>();
    }

    public void Dispose()
    {
        _scope.Dispose();
        _provider.Dispose();
    }

    [Fact]
    public void DRC_resolves_the_Mapster_backed_mapper()
    {
        // Startup sets Mapster on, so a regression to the AutoMapper adapter would map every shared
        // consumer against a configuration with no DRC profiles left in it.
        Assert.IsType<MapsterZgwMapper>(_zgwMapper);
    }

    /// <summary>
    /// Every entity → response-DTO pair the registers declare, mapped through the adapter
    /// <c>AuditTrailServiceBase.SetOld</c>/<c>SetNew</c> uses. Asserting the URL is ABSOLUTE is what
    /// gives it teeth: these DTOs all have a same-named <c>Url</c> that Mapster convention-copies from
    /// the entity's relative one, so an assertion satisfied by the entity's own <c>Url</c> would pass
    /// with the register's resolver rule deleted. Discovered rather than listed, so v1.7's DTOs — the
    /// ones the audit trail was silently convention-mapping — are covered without anyone extending a
    /// list.
    /// </summary>
    /// <remarks>
    /// Absoluteness, not equality to <c>Resolved(entity)</c>: for an
    /// <see cref="EnkelvoudigInformatieObjectVersie"/> source every register deliberately resolves the
    /// PARENT document's url (<c>ResolveUrl(src.InformatieObject)</c>), matching the shipping AutoMapper
    /// profiles, because a document's canonical url is the object's and not the versie's download link.
    /// Only the <see cref="DrcMapperTestHost.BaseUrl"/> prefix is common to every pair, and it is also
    /// the whole of what a convention copy cannot produce — <c>IUrlEntity.Url</c> is relative.
    /// </remarks>
    [Theory]
    [MemberData(nameof(EntityToResponseDtoPairs))]
    public void AuditTrail_resolves_an_absolute_url_for_every_declared_response_dto(Type entityType, Type responseDtoType)
    {
        var entity = (IUrlEntity)BareEntity(entityType);

        var dto = MapThroughZgwMapper(responseDtoType, entity);

        var url = (string)responseDtoType.GetProperty("Url")!.GetValue(dto);

        Assert.StartsWith(DrcMapperTestHost.BaseUrl, url, StringComparison.Ordinal);
    }

    /// <summary>
    /// Every entity → request-DTO pair the registers declare, run through the real
    /// <see cref="IZgwRequestMerger"/> with an empty patch. A routing tripwire, not a value check —
    /// values are pinned by the register tests; this catches a merge resolved against a mapper that has
    /// no map for the pair, which throws at request time while every register-level fact stays green.
    /// </summary>
    [Theory]
    [MemberData(nameof(EntityToRequestDtoPairs))]
    public void RequestMerger_merges_an_empty_patch_for_every_declared_request_dto(Type entityType, Type requestDtoType)
    {
        var entity = BareEntity(entityType);

        var merged = MergeEmptyPatch(requestDtoType, entityType, entity);

        Assert.NotNull(merged);
        Assert.IsType(requestDtoType, merged);
    }

    /// <summary>
    /// A value-level PATCH fact for the shared merger both document mergers delegate to: the patched
    /// field comes from the JObject, while the untouched one can only come from the existing entity
    /// having been mapped in first — the step that needs the register.
    /// </summary>
    /// <remarks>
    /// <c>InformatieObject</c> is the untouched assertion because it is the only member of
    /// <c>GebruiksRechtRequestDto</c> that cannot convention-map: the entity member is an
    /// <see cref="EnkelvoudigInformatieObject"/> while the DTO member is a resolved absolute url string,
    /// so the value can only appear if the register's <c>MapsterUrlResolver</c> rule ran.
    /// </remarks>
    [Fact]
    public void RequestMerger_merges_a_PATCH_onto_an_existing_gebruiksrecht()
    {
        var existing = new GebruiksRecht
        {
            Id = Guid.NewGuid(),
            OmschrijvingVoorwaarden = "bestaande voorwaarden",
            InformatieObject = new EnkelvoudigInformatieObject { Id = Guid.NewGuid() },
        };
        var patch = new JObject { ["omschrijvingVoorwaarden"] = "gewijzigde voorwaarden" };

        var merged = _zgwRequestMerger.MergePartialUpdateToObjectRequest<Documenten.Contracts.v1.Requests.GebruiksRechtRequestDto, GebruiksRecht>(
            existing,
            patch
        );

        Assert.Equal("gewijzigde voorwaarden", merged.OmschrijvingVoorwaarden);
        Assert.Equal(DrcMapperTestHost.Resolved(existing.InformatieObject), merged.InformatieObject);
    }

    public static TheoryData<Type, Type> EntityToResponseDtoPairs() => DeclaredPairsEndingIn("ResponseDto", requireUrlOnDestination: true);

    public static TheoryData<Type, Type> EntityToRequestDtoPairs() => DeclaredPairsEndingIn("RequestDto", requireUrlOnDestination: false);

    /// <summary>
    /// Reads the pairs out of the config <c>AddZgwMapster</c> actually builds — the scanned, merged one
    /// that decides which definition of a pair survives.
    /// </summary>
    private static TheoryData<Type, Type> DeclaredPairsEndingIn(string destinationSuffix, bool requireUrlOnDestination)
    {
        var services = new ServiceCollection();
        services.AddZgwMapster(typeof(Startup).Assembly, enable: true);
        using var provider = services.BuildServiceProvider();
        var config = provider.GetRequiredService<TypeAdapterConfig>();

        var data = new TheoryData<Type, Type>();
        var pairs = config
            .RuleMap.Keys.Where(k =>
                typeof(IBaseEntity).IsAssignableFrom(k.Source)
                && k.Destination.Name.EndsWith(destinationSuffix, StringComparison.Ordinal)
                && (!requireUrlOnDestination || (typeof(IUrlEntity).IsAssignableFrom(k.Source) && k.Destination.GetProperty("Url") != null))
            )
            .OrderBy(k => k.Source.FullName)
            .ThenBy(k => k.Destination.FullName);

        foreach (var pair in pairs)
        {
            data.Add(pair.Source, pair.Destination);
        }

        // A filter that matched nothing would make the facts above vacuous rather than failing.
        Assert.NotEmpty(data);

        return data;
    }

    /// <summary>
    /// An entity with only <c>Id</c> set and every writable collection navigation initialised empty. The
    /// empty collections are load-bearing: the ported after-mapping blocks iterate BestandsDelen
    /// unguarded, so a null there is a NullReferenceException rather than a mapping failure.
    /// </summary>
    private static object BareEntity(Type entityType)
    {
        var entity = Activator.CreateInstance(entityType);

        foreach (var property in entityType.GetProperties().Where(p => p.CanWrite && p.CanRead))
        {
            if (property.Name == nameof(IBaseEntity.Id) && property.PropertyType == typeof(Guid))
            {
                property.SetValue(entity, Guid.NewGuid());
                continue;
            }

            if (!property.PropertyType.IsGenericType)
            {
                continue;
            }

            var definition = property.PropertyType.GetGenericTypeDefinition();
            if (definition == typeof(List<>) || definition == typeof(ICollection<>) || definition == typeof(IList<>))
            {
                property.SetValue(entity, Activator.CreateInstance(typeof(List<>).MakeGenericType(property.PropertyType.GetGenericArguments()[0])));
            }
        }

        LinkRequiredNavigations(entity);

        return entity;
    }

    /// <summary>
    /// Completes the one DRC relation no entity is ever persisted without: the document ↔ versie pair.
    /// Both halves' registers dereference the other half unguarded — and
    /// <c>EnkelvoudigInformatieObjectVersie.Url</c> throws <see cref="NullReferenceException"/> outright
    /// with neither navigation set — so a genuinely bare instance of either type fails on its own
    /// invariants rather than on anything these facts are about.
    /// </summary>
    private static void LinkRequiredNavigations(object entity)
    {
        switch (entity)
        {
            case EnkelvoudigInformatieObject informatieObject:
                // LatestInformatieObject, not InformatieObject: the ported MapLatestVersie... blocks read
                // the document's members back off the latest versie through that navigation.
                var versie = new EnkelvoudigInformatieObjectVersie { Id = Guid.NewGuid(), LatestInformatieObject = informatieObject };
                informatieObject.LatestEnkelvoudigInformatieObjectVersie = versie;
                informatieObject.EnkelvoudigInformatieObjectVersies.Add(versie);
                break;

            case EnkelvoudigInformatieObjectVersie versieEntity:
                versieEntity.InformatieObject = new EnkelvoudigInformatieObject { Id = Guid.NewGuid() };
                break;
        }
    }

    private object MapThroughZgwMapper(Type destinationType, object source) =>
        typeof(IZgwMapper).GetMethod(nameof(IZgwMapper.Map))!.MakeGenericMethod(destinationType).Invoke(_zgwMapper, [source]);

    private object MergeEmptyPatch(Type requestDtoType, Type entityType, object entity) =>
        typeof(IZgwRequestMerger)
            .GetMethod(nameof(IZgwRequestMerger.MergePartialUpdateToObjectRequest))!
            .MakeGenericMethod(requestDtoType, entityType)
            .Invoke(_zgwRequestMerger, [entity, new JObject()]);
}
