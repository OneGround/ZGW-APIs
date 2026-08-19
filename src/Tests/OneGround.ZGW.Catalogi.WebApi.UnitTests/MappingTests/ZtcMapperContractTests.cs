using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Newtonsoft.Json.Linq;
using OneGround.ZGW.Catalogi.DataModel;
using OneGround.ZGW.Catalogi.Web;
using OneGround.ZGW.Common.DataModel;
using OneGround.ZGW.Common.Web.Extensions.ServiceCollection.ZGWApiExtensions;
using OneGround.ZGW.Common.Web.Mapping;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.DataAccess;
using Xunit;
using ZaakTypeRequestDtoV1 = OneGround.ZGW.Catalogi.Contracts.v1.Requests.ZaakTypeRequestDto;
using ZaakTypeRequestDtoV13 = OneGround.ZGW.Catalogi.Contracts.v1._3.Requests.ZaakTypeRequestDto;
using ZaakTypeResponseDtoV1 = OneGround.ZGW.Catalogi.Contracts.v1.Responses.ZaakTypeResponseDto;
using ZaakTypeResponseDtoV13 = OneGround.ZGW.Catalogi.Contracts.v1._3.Responses.ZaakTypeResponseDto;

namespace OneGround.ZGW.Catalogi.WebApi.UnitTests.MappingTests;

/// <summary>
/// The two mapping contracts ZTC depends on outside the Map calls its controllers make themselves: the
/// audit trail (<see cref="IZgwMapper"/>) and the PATCH merge (<see cref="IZgwRequestMerger"/>). The
/// register tests resolve <c>MapsterMapper.IMapper</c> directly and so exercise neither adapter.
/// </summary>
/// <remarks>
/// ZTC's AutoMapper profiles are deleted, so both consumers must resolve the Mapster-backed adapters.
/// A regression here is silent, not loud: Mapster convention-maps instead of throwing, so an audit
/// record or a PATCH result comes back quietly wrong rather than as an exception. That is why the
/// adapter type is asserted directly rather than inferred from a working map, and why the controller
/// constructors are checked by reflection - a controller left on AutoMapper's
/// <see cref="IRequestMerger"/> keeps compiling and every mapping fact below keeps passing, because
/// they resolve the correct merger themselves.
/// </remarks>
public class ZtcMapperContractTests : IDisposable
{
    private static readonly Guid GerelateerdZaakTypeId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid DeelZaakTypeId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private readonly ServiceProvider _provider;
    private readonly IServiceScope _scope;
    private readonly IZgwMapper _zgwMapper;
    private readonly IZgwRequestMerger _zgwRequestMerger;

    public ZtcMapperContractTests()
    {
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
    public void ZTC_resolves_the_Mapster_backed_mapper()
    {
        // ZTC has no AutoMapper profiles left, so a regression to that adapter would map every shared
        // consumer against an empty configuration.
        Assert.IsType<MapsterZgwMapper>(_zgwMapper);
    }

    [Fact]
    public void AuditTrail_maps_an_existing_ZaakType_to_its_v1_response_dto()
    {
        // AuditTrailServiceBase.SetOld/SetNew go through IZgwMapper with exactly these DTOs, and every
        // mutating ZTC endpoint writes an audit record that way, so a dropped register turns every
        // POST/PUT/PATCH/DELETE audit entry into a convention-mapped approximation.
        var existing = ExistingZaakType();

        var dto = _zgwMapper.Map<ZaakTypeResponseDtoV1>(existing);

        Assert.Equal(existing.Url, dto.Url);
        Assert.Equal("ZAAKTYPE-001", dto.Identificatie);
        Assert.Equal(existing.Catalogus.Url, dto.Catalogus);
        AssertGerelateerdeZaakTypenSurvived(dto.GerelateerdeZaakTypen.Select(g => (g.AardRelatie, g.Toelichting, g.ZaakType)));
    }

    [Fact]
    public void AuditTrail_maps_an_existing_ZaakType_to_its_v1_3_response_dto()
    {
        var existing = ExistingZaakType();

        var dto = _zgwMapper.Map<ZaakTypeResponseDtoV13>(existing);

        Assert.Equal(existing.Url, dto.Url);
        Assert.Equal("ZAAKTYPE-001", dto.Identificatie);
        Assert.Equal(existing.Catalogus.Url, dto.Catalogus);
        AssertGerelateerdeZaakTypenSurvived(dto.GerelateerdeZaakTypen.Select(g => (g.AardRelatie, g.Toelichting, g.ZaakType)));
    }

    [Fact]
    public void RequestMerger_can_merge_a_PATCH_onto_an_existing_ZaakType_v1()
    {
        var existing = ExistingZaakType();
        var patch = new JObject { ["omschrijving"] = "gewijzigde omschrijving" };

        var merged = _zgwRequestMerger.MergePartialUpdateToObjectRequest<ZaakTypeRequestDtoV1, ZaakType>(existing, patch);

        // The untouched fields can only come from the existing entity having been mapped in first -
        // the step that needs the register.
        Assert.Equal("gewijzigde omschrijving", merged.Omschrijving);
        Assert.Equal("ZAAKTYPE-001", merged.Identificatie);
        Assert.Equal(existing.Catalogus.Url, merged.Catalogus);
        AssertGerelateerdeZaakTypenSurvived(merged.GerelateerdeZaakTypen.Select(g => (g.AardRelatie, g.Toelichting, g.ZaakType)));
        AssertRelationUrlsSurvived(merged.DeelZaakTypen, merged.BesluitTypen, existing);
    }

    [Fact]
    public void RequestMerger_can_merge_a_PATCH_onto_an_existing_ZaakType_v1_3()
    {
        var existing = ExistingZaakType();
        var patch = new JObject { ["omschrijving"] = "gewijzigde omschrijving" };

        var merged = _zgwRequestMerger.MergePartialUpdateToObjectRequest<ZaakTypeRequestDtoV13, ZaakType>(existing, patch);

        Assert.Equal("gewijzigde omschrijving", merged.Omschrijving);
        Assert.Equal("ZAAKTYPE-001", merged.Identificatie);
        Assert.Equal(existing.Catalogus.Url, merged.Catalogus);
        // A PATCH that does not mention a v1.3-only field must not silently reset it.
        Assert.Equal("Team Vergunningen", merged.Verantwoordelijke);

        // v1.3 identifies related types by their denormalized identificatie/omschrijving rather than by
        // URL (the v1 request map and both response maps use URLs) - asserted on the values the v1
        // facts deliberately do not use, so the two registers cannot satisfy each other's expectations.
        var relation = Assert.Single(merged.GerelateerdeZaakTypen);
        Assert.Equal(AardRelatie.vervolg.ToString(), relation.AardRelatie);
        Assert.Equal("volgt op de aanvraag", relation.Toelichting);
        Assert.Equal("ZAAKTYPE-GERELATEERD", relation.ZaakType);
        Assert.Equal(["ZAAKTYPE-DEEL"], merged.DeelZaakTypen);
        Assert.Equal(["besluittype-omschrijving"], merged.BesluitTypen);
    }

    /// <summary>
    /// Every ZTC controller that runs a PATCH must take <see cref="IZgwRequestMerger"/>, not only the
    /// AutoMapper <see cref="IRequestMerger"/> that <c>ZGWControllerBase</c> still requires. Asserted
    /// structurally because it cannot be observed from a mapping test: the merge facts above resolve
    /// the Mapster merger themselves and stay green while a controller merges through the AutoMapper
    /// one against an empty configuration.
    /// </summary>
    [Theory]
    [MemberData(nameof(PatchingControllerTypes))]
    public void Every_patching_controller_takes_the_Mapster_request_merger(Type controllerType)
    {
        var parameterTypes = controllerType.GetConstructors().Single().GetParameters().Select(p => p.ParameterType);

        Assert.Contains(typeof(IZgwRequestMerger), parameterTypes);
    }

    /// <summary>
    /// Discovered rather than listed, so a controller that gains a PATCH later is covered without
    /// anyone remembering to extend a list. Which merger field a method body uses is not visible by
    /// reflection, so "patching" is taken from the action name ZTC controllers use for it.
    /// </summary>
    public static TheoryData<Type> PatchingControllerTypes()
    {
        var controllers = typeof(Startup)
            .Assembly.GetTypes()
            .Where(t => t.Name.EndsWith("Controller") && t.GetMethod("PartialUpdateAsync") != null)
            .OrderBy(t => t.FullName);

        var data = new TheoryData<Type>();
        foreach (var controller in controllers)
        {
            data.Add(controller);
        }

        // Guards the discovery itself: a filter that silently matched nothing would make the fact
        // above vacuous rather than failing.
        Assert.NotEmpty(data);

        return data;
    }

    /// <summary>
    /// This collection is produced by an <c>.AfterMapping</c> block rather than by convention, so it is
    /// the first thing to disappear if the register is dropped - and it comes back as an empty list
    /// rather than an error, which on a PATCH means the update handler removes every relation the
    /// ZAAKTYPE had and answers 200.
    /// </summary>
    private static void AssertGerelateerdeZaakTypenSurvived(
        IEnumerable<(string AardRelatie, string Toelichting, string ZaakType)> gerelateerdeZaakTypen
    )
    {
        var item = Assert.Single(gerelateerdeZaakTypen);
        Assert.Equal(AardRelatie.vervolg.ToString(), item.AardRelatie);
        Assert.Equal("volgt op de aanvraag", item.Toelichting);
        Assert.Equal($"/zaaktypen/{GerelateerdZaakTypeId}", item.ZaakType);
    }

    private static void AssertRelationUrlsSurvived(IEnumerable<string> deelZaakTypen, IEnumerable<string> besluitTypen, ZaakType existing)
    {
        Assert.Equal([$"/zaaktypen/{DeelZaakTypeId}"], deelZaakTypen);
        Assert.Equal([existing.ZaakTypeBesluitTypen.Single().BesluitType.Url], besluitTypen);
    }

    private static ZaakType ExistingZaakType() =>
        new ZaakType
        {
            Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            Identificatie = "ZAAKTYPE-001",
            Omschrijving = "bestaande omschrijving",
            Verantwoordelijke = "Team Vergunningen",
            BeginGeldigheid = new DateOnly(2026, 1, 1),
            VersieDatum = new DateOnly(2026, 1, 1),
            Catalogus = new Catalogus { Id = Guid.Parse("44444444-4444-4444-4444-444444444444") },
            ZaakTypeGerelateerdeZaakTypen =
            [
                new ZaakTypeGerelateerdeZaakType
                {
                    AardRelatie = AardRelatie.vervolg,
                    Toelichting = "volgt op de aanvraag",
                    GerelateerdeZaakType = new ZaakType { Id = GerelateerdZaakTypeId },
                    GerelateerdeZaakTypeIdentificatie = "ZAAKTYPE-GERELATEERD",
                },
            ],
            ZaakTypeDeelZaakTypen =
            [
                new ZaakTypeDeelZaakType
                {
                    DeelZaakType = new ZaakType { Id = DeelZaakTypeId },
                    DeelZaakTypeIdentificatie = "ZAAKTYPE-DEEL",
                },
            ],
            ZaakTypeBesluitTypen =
            [
                new ZaakTypeBesluitType
                {
                    BesluitType = new BesluitType { Id = Guid.Parse("55555555-5555-5555-5555-555555555555") },
                    BesluitTypeOmschrijving = "besluittype-omschrijving",
                },
            ],
        };
}
