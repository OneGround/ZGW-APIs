using System;
using AutoFixture;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Newtonsoft.Json.Linq;
using OneGround.ZGW.Besluiten.Contracts.v1.Requests;
using OneGround.ZGW.Besluiten.Contracts.v1.Responses;
using OneGround.ZGW.Besluiten.DataModel;
using OneGround.ZGW.Besluiten.Web;
using OneGround.ZGW.Common.Web.Extensions.ServiceCollection.ZGWApiExtensions;
using OneGround.ZGW.Common.Web.Mapping;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.DataAccess;
using Xunit;

namespace OneGround.ZGW.Besluiten.WebApi.UnitTests.MappingTests;

/// <summary>
/// Guards the mapping contracts BRC depends on OUTSIDE its controllers: the audit trail
/// (<c>AuditTrailServiceBase.SetOld/SetNew</c>) via <see cref="IZgwMapper"/>, the PATCH merge via
/// <see cref="IZgwRequestMerger"/>, and the <c>?expand=</c> expander. The per-register tests in this
/// folder build an isolated TypeAdapterConfig and cannot see any of these paths — they passed while all
/// three were broken. These resolve the real container the way Startup does.
/// </summary>
/// <remarks>
/// Mapster does not throw on a missing map, it convention-maps. So unlike the AutoMapper era, a deleted
/// register would produce a quietly wrong audit record rather than an exception — which is what makes
/// these tests load-bearing rather than a formality.
/// </remarks>
public class BrcMapperContractTests : IDisposable
{
    private readonly MappingTestFixture _fixture = new MappingTestFixture();
    private readonly ServiceProvider _provider;
    private readonly IServiceScope _scope;
    private readonly IZgwMapper _zgwMapper;
    private readonly IZgwRequestMerger _zgwRequestMerger;

    public BrcMapperContractTests()
    {
        _fixture.Register<DateOnly>(() => DateOnly.FromDateTime(DateTime.UtcNow));

        var mockedUriService = new Mock<IEntityUriService>();
        mockedUriService.Setup(s => s.GetUri(It.IsAny<IUrlEntity>())).Returns<IUrlEntity>(e => e.Url);

        var services = new ServiceCollection();
        services.AddSingleton(mockedUriService.Object);

        // Mirrors Startup exactly: same extensions, same order, same assembly, EnableMapster on.
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
    public void BRC_resolves_the_Mapster_backed_mapper()
    {
        // If this ever regresses to the AutoMapper adapter, BRC has no profiles left and every write
        // would fail — so assert the routing directly rather than inferring it from a successful map.
        Assert.IsType<MapsterZgwMapper>(_zgwMapper);
    }

    [Fact]
    public void AuditTrail_can_map_Besluit_to_BesluitResponseDto()
    {
        // The exact call AuditTrailServiceBase.SetNew<TDto>/SetOld<TDto> makes, for the TDto used by
        // CreateBesluitCommandHandler and UpdateBesluitCommandHandler. Asserting a resolved Url proves
        // MapsterUrlResolver still reaches IEntityUriService through DI.
        var besluit = _fixture.Create<Besluit>();

        var dto = _zgwMapper.Map<BesluitResponseDto>(besluit);

        Assert.Equal(besluit.Identificatie, dto.Identificatie);
        Assert.Equal(besluit.Url, dto.Url);
        // Besluit.Datum is a DateOnly but BesluitResponseDto.Datum is a string, so this can only pass
        // if the register's explicit .Map(dest => dest.Datum, ...) ran (a type mismatch defeats Mapster's
        // same-name convention fallback, unlike Url above which happens to convention-map to the same value).
        Assert.Equal(besluit.Datum.ToString("yyyy-MM-dd"), dto.Datum);
    }

    [Fact]
    public void AuditTrail_and_expander_can_map_BesluitInformatieObject_to_BesluitInformatieObjectResponseDto()
    {
        // One map, three consumers: the BIO create/delete handlers via SetNew/SetOld, and
        // BesluitInformatieObjectenExpander for _expand.
        var besluitInformatieObject = _fixture.Create<BesluitInformatieObject>();

        var dto = _zgwMapper.Map<BesluitInformatieObjectResponseDto>(besluitInformatieObject);

        Assert.Equal(besluitInformatieObject.InformatieObject, dto.InformatieObject);
        Assert.Equal(besluitInformatieObject.Besluit.Url, dto.Besluit);
    }

    [Fact]
    public void RequestMerger_can_merge_a_PATCH_onto_an_existing_Besluit()
    {
        var existing = _fixture.Create<Besluit>();
        var patch = new JObject { ["toelichting"] = "gewijzigde toelichting" };

        var merged = _zgwRequestMerger.MergePartialUpdateToObjectRequest<BesluitRequestDto, Besluit>(existing, patch);

        // The patched field comes from the JObject; the untouched field can only come from the existing
        // entity having been mapped in first, which is the step that needs the register.
        Assert.Equal("gewijzigde toelichting", merged.Toelichting);
        Assert.Equal(existing.Identificatie, merged.Identificatie);
    }
}
