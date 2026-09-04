using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
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
/// Covers the mapping contracts DRC depends on OUTSIDE the Map calls its controllers make: the audit trail
/// via <see cref="IZgwMapper"/> and the PATCH merge via <see cref="IZgwRequestMerger"/>. The per-register
/// tests build their own mapper and exercise neither path -- they passed while both were broken. A
/// regression here is silent (Mapster convention-maps instead of throwing), so the adapter type is
/// asserted directly rather than inferred from a working map.
/// </summary>
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
        services.AddZgwMapster(typeof(Startup).Assembly);

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
        // Every shared consumer (the audit trail) maps through this adapter, and a missing or swapped
        // registration is silent because Mapster convention-maps instead of throwing.
        Assert.IsType<MapsterZgwMapper>(_zgwMapper);
    }

    /// <summary>
    /// Every entity → response-DTO pair the registers declare, mapped through the adapter
    /// <c>AuditTrailServiceBase.SetOld</c>/<c>SetNew</c> uses. Asserts the URL is ABSOLUTE rather than
    /// equal to a fixed value, because these DTOs all have a same-named <c>Url</c> that Mapster
    /// convention-copies from the entity's own relative one; only the absolute prefix is something a
    /// convention copy cannot produce. Pairs are discovered via reflection, not listed, so newly added
    /// DTOs are covered without anyone extending a list.
    /// </summary>
    /// <remarks>
    /// For an <see cref="EnkelvoudigInformatieObjectVersie"/> source, every register deliberately resolves
    /// the PARENT document's url (<c>ResolveUrl(src.InformatieObject)</c>), not the versie's own -- a
    /// document's canonical url is the object's, not the versie's download link.
    /// </remarks>
    [Theory]
    [MemberData(nameof(EntityToResponseDtoPairs))]
    public void AuditTrail_resolves_an_absolute_url_for_every_declared_response_dto(Type entityType, Type responseDtoType)
    {
        var entity = BareEntity(entityType);

        var dto = MapThroughZgwMapper(responseDtoType, entity);

        var url = (string)responseDtoType.GetProperty("Url")!.GetValue(dto);

        Assert.StartsWith(DrcMapperTestHost.BaseUrl, url, StringComparison.Ordinal);
    }

    /// <summary>
    /// Every entity → request-DTO pair the registers declare, run through the real
    /// <see cref="IZgwRequestMerger"/> with an empty patch. A routing tripwire, not a value check (values
    /// are pinned by the register tests) -- it catches a merge resolved against a mapper with no map for
    /// the pair, which would throw at request time while every register-level fact stays green.
    /// </summary>
    [Theory]
    [MemberData(nameof(EntityToRequestDtoPairs))]
    public void RequestMerger_merges_an_empty_patch_for_every_declared_request_dto(Type entityType, Type requestDtoType)
    {
        var entity = BareEntity(entityType);

        var merged = MergeEmptyPatch(requestDtoType, entityType, entity);

        // These two asserts can't fail by construction -- the absence of a throw IS the assertion this fact guards.
        Assert.NotNull(merged);
        Assert.IsType(requestDtoType, merged);
    }

    /// <summary>
    /// A value-level PATCH fact for the shared merger: the patched field comes from the JObject, while the
    /// untouched one (<c>InformatieObject</c>) can only come from the existing entity having been mapped in
    /// first via the register's <c>MapsterUrlResolver</c> rule -- it's the one member that can't convention-map,
    /// since the entity side is an <see cref="EnkelvoudigInformatieObject"/> but the DTO side is a resolved url string.
    /// </summary>
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

    /// <summary>Reads the pairs out of the scanned, merged config <c>AddZgwMapster</c> actually builds.</summary>
    private static TheoryData<Type, Type> DeclaredPairsEndingIn(string destinationSuffix, bool requireUrlOnDestination)
    {
        var services = new ServiceCollection();
        services.AddZgwMapster(typeof(Startup).Assembly);
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
    /// An entity with only <c>Id</c> set and every writable collection navigation initialised empty --
    /// needed because the ported after-mapping blocks iterate <c>BestandsDelen</c> unguarded, so a null
    /// there is a NullReferenceException rather than a mapping failure.
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
    /// Without it, <c>EnkelvoudigInformatieObjectVersie.Url</c> throws <see cref="NullReferenceException"/>
    /// on its own invariants, before the mapping logic these facts are about ever runs.
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

    // MergePartialUpdateToObjectRequest's afterMap parameter is optional, but MethodBase.Invoke does not
    // apply optional-parameter defaults - the argument array has to carry it explicitly.
    private object MergeEmptyPatch(Type requestDtoType, Type entityType, object entity) =>
        typeof(IZgwRequestMerger)
            .GetMethod(nameof(IZgwRequestMerger.MergePartialUpdateToObjectRequest))!
            .MakeGenericMethod(requestDtoType, entityType)
            .Invoke(_zgwRequestMerger, [entity, new JObject(), null]);
}
