using System;
using AutoFixture;
using AutoMapper;
using Mapster;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OneGround.ZGW.Besluiten.Contracts.v1.Requests;
using OneGround.ZGW.Besluiten.Contracts.v1.Responses;
using OneGround.ZGW.Besluiten.DataModel;
using OneGround.ZGW.Besluiten.Web;
using OneGround.ZGW.Common;
using OneGround.ZGW.Common.Web.Extensions.ServiceCollection.ZGWApiExtensions;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.DataAccess;
using Xunit;

namespace OneGround.ZGW.Besluiten.WebApi.UnitTests.MappingTests;

/// <summary>
/// Guards the mapping contracts BRC depends on OUTSIDE its controllers. The controllers were migrated
/// to Mapster, but three consumers still map BRC domain types through AutoMapper, and two of them live
/// in shared Common.Web where a BRC-only change cannot see them:
/// <list type="bullet">
///   <item><c>AuditTrailServiceBase.SetOld/SetNew&lt;TDto&gt;</c> — every mutating BRC handler.</item>
///   <item><c>RequestMerger.MergePartialUpdateToObjectRequest</c> — PATCH /besluiten/{id}.</item>
///   <item><c>BesluitInformatieObjectenExpander</c> — GET /besluiten?expand=besluitinformatieobjecten.</item>
/// </list>
/// The per-register tests in this folder build an isolated TypeAdapterConfig and therefore cannot see
/// these paths at all: they passed while all three were broken. These tests resolve the mappers the way
/// Startup does, so deleting a mapping definition that a non-controller consumer still needs fails here.
/// Until Common.Web itself is migrated, BRC has to keep serving both mappers, so the second half of this
/// class pins the two against each other: audit-trail JSON is written by AutoMapper while the API
/// response is produced by Mapster, and a divergence between them would be invisible everywhere else.
/// </summary>
public class BrcMapperContractTests : IDisposable
{
    private readonly AutoMapperFixture _fixture = new AutoMapperFixture();
    private readonly ServiceProvider _provider;
    private readonly IServiceScope _scope;
    private readonly IMapper _autoMapper;
    private readonly MapsterMapper.IMapper _mapsterMapper;

    public BrcMapperContractTests()
    {
        _fixture.Register<DateOnly>(() => DateOnly.FromDateTime(DateTime.UtcNow));

        var mockedUriService = new Mock<IEntityUriService>();
        mockedUriService.Setup(s => s.GetUri(It.IsAny<IUrlEntity>())).Returns<IUrlEntity>(e => e.Url);

        var services = new ServiceCollection();
        services.AddSingleton(mockedUriService.Object);

        // Both registrations mirror Startup exactly: same extension methods, same assembly, so profile
        // and IRegister discovery behave here the way they do in the running service.
        services.AddAutoMapper(typeof(Startup).Assembly);
        services.AddZgwMapster(typeof(Startup).Assembly, enable: true);

        _provider = services.BuildServiceProvider();
        _scope = _provider.CreateScope();
        _autoMapper = _scope.ServiceProvider.GetRequiredService<IMapper>();
        _mapsterMapper = _scope.ServiceProvider.GetRequiredService<MapsterMapper.IMapper>();
    }

    public void Dispose()
    {
        _scope.Dispose();
        _provider.Dispose();
    }

    // ---------------------------------------------------------------------------------------------
    // Contracts required by consumers that still map through AutoMapper
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void AuditTrail_can_map_Besluit_to_BesluitResponseDto()
    {
        // The exact call AuditTrailServiceBase.SetNew<TDto>/SetOld<TDto> makes, for the TDto used by
        // CreateBesluitCommandHandler and UpdateBesluitCommandHandler. Asserting a resolved Url rather
        // than merely "did not throw" also proves the AutoMapper UrlResolver still reaches
        // IEntityUriService through DI, which a map present but misconfigured would fail.
        var besluit = _fixture.Create<Besluit>();

        var dto = _autoMapper.Map<BesluitResponseDto>(besluit);

        Assert.Equal(besluit.Identificatie, dto.Identificatie);
        Assert.Equal(besluit.Url, dto.Url);
    }

    [Fact]
    public void AuditTrail_and_expander_can_map_BesluitInformatieObject_to_BesluitInformatieObjectResponseDto()
    {
        // One map, two consumers: CreateBesluitInformatieObjectCommandHandler /
        // DeleteBesluitInformatieObjectCommandHandler via SetNew/SetOld, and
        // BesluitInformatieObjectenExpander for _expand.
        var besluitInformatieObject = _fixture.Create<BesluitInformatieObject>();

        var dto = _autoMapper.Map<BesluitInformatieObjectResponseDto>(besluitInformatieObject);

        Assert.Equal(besluitInformatieObject.InformatieObject, dto.InformatieObject);
        Assert.Equal(besluitInformatieObject.Besluit.Url, dto.Besluit);
    }

    [Fact]
    public void RequestMerger_can_merge_a_PATCH_onto_an_existing_Besluit()
    {
        // Drives the real RequestMerger rather than asserting on a map in isolation, because that is
        // what PartialUpdateAsync calls and RequestMerger resolves AutoMapper's IMapper internally --
        // registering Besluit -> BesluitRequestDto on Mapster alone does not satisfy this path.
        var merger = new RequestMerger(_autoMapper);
        var existing = _fixture.Create<Besluit>();
        var patch = new JObject { ["toelichting"] = "gewijzigde toelichting" };

        var merged = merger.MergePartialUpdateToObjectRequest<BesluitRequestDto, Besluit>(existing, patch);

        // The patched field comes from the JObject; the untouched field can only come from the
        // existing entity having been mapped in first, which is the step that needs the map.
        Assert.Equal("gewijzigde toelichting", merged.Toelichting);
        Assert.Equal(existing.Identificatie, merged.Identificatie);
    }

    // ---------------------------------------------------------------------------------------------
    // Parity between the two mappers, for as long as BRC runs both
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void Besluit_to_BesluitResponseDto_is_identical_under_both_mappers()
    {
        var besluit = _fixture.Create<Besluit>();

        Assert.Equal(Serialize(_autoMapper.Map<BesluitResponseDto>(besluit)), Serialize(_mapsterMapper.Map<BesluitResponseDto>(besluit)));
    }

    [Fact]
    public void Besluit_to_BesluitRequestDto_is_identical_under_both_mappers()
    {
        var besluit = _fixture.Create<Besluit>();

        Assert.Equal(Serialize(_autoMapper.Map<BesluitRequestDto>(besluit)), Serialize(_mapsterMapper.Map<BesluitRequestDto>(besluit)));
    }

    [Fact]
    public void BesluitInformatieObject_to_BesluitInformatieObjectResponseDto_is_identical_under_both_mappers()
    {
        var besluitInformatieObject = _fixture.Create<BesluitInformatieObject>();

        Assert.Equal(
            Serialize(_autoMapper.Map<BesluitInformatieObjectResponseDto>(besluitInformatieObject)),
            Serialize(_mapsterMapper.Map<BesluitInformatieObjectResponseDto>(besluitInformatieObject))
        );
    }

    // Compares through the serializer the service actually emits with, so the assertion covers the
    // shape callers observe rather than reference identity.
    private static string Serialize(object value) => JsonConvert.SerializeObject(value, new ZGWJsonSerializerSettings());
}
