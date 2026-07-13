using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AutoFixture;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NetTopologySuite.Geometries;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OneGround.ZGW.Common.Contracts.v1.AuditTrail;
using OneGround.ZGW.Common.Helpers;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.DataAccess;
using OneGround.ZGW.DataAccess.AuditTrail;
using OneGround.ZGW.Zaken.Contracts.v1;
using OneGround.ZGW.Zaken.Contracts.v1.Requests;
using OneGround.ZGW.Zaken.Contracts.v1.Requests.ZaakObject;
using OneGround.ZGW.Zaken.Contracts.v1.Responses;
using OneGround.ZGW.Zaken.Contracts.v1.Responses.ZaakObject;
using OneGround.ZGW.Zaken.Contracts.v1.Responses.ZaakRol;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.DataModel.ZaakObject;
using OneGround.ZGW.Zaken.DataModel.ZaakRol;
using OneGround.ZGW.Zaken.Web.MappingProfiles.v1;
using Xunit;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.MappingTests;

public class DomainToResponseProfileTests : IDisposable
{
    private readonly AutoMapperFixture _fixture = new AutoMapperFixture();
    private readonly Mock<IEntityUriService> _mockedUriService = new Mock<IEntityUriService>();
    private readonly ServiceProvider _provider;
    private readonly IServiceScope _scope;
    private readonly IMapper _mapper;

    public DomainToResponseProfileTests()
    {
        _fixture.Register<DateOnly>(() => DateOnly.FromDateTime(DateTime.UtcNow));
        _mockedUriService.Setup(s => s.GetUri(It.IsAny<IUrlEntity>())).Returns<IUrlEntity>(e => e.Url);

        var config = new TypeAdapterConfig();
        new DomainToResponseRegister().Register(config);
        config.Compile();

        var services = new ServiceCollection();
        services.AddSingleton(_mockedUriService.Object);
        services.AddSingleton(config);
        services.AddScoped<IMapper, ServiceMapper>();
        _provider = services.BuildServiceProvider();
        _scope = _provider.CreateScope();
        _mapper = _scope.ServiceProvider.GetRequiredService<IMapper>();
    }

    public void Dispose()
    {
        _scope.Dispose();
        _provider.Dispose();
    }

    [Fact]
    public void ZaakEigenschap_Maps_To_ZaakEigenschapResponseDto()
    {
        var value = _fixture.Create<ZaakEigenschap>();
        var result = _mapper.Map<ZaakEigenschapResponseDto>(value);

        Assert.Equal(value.Eigenschap, result.Eigenschap);
        Assert.Equal(value.Waarde, result.Waarde);
        Assert.Equal(value.Zaak.Url, result.Zaak);
        Assert.Equal(value.Naam, result.Naam);
        Assert.Equal(value.Url, result.Url);
        Assert.Equal(value.Id.ToString(), result.Uuid);
    }

    [Fact]
    public void ZaakStatus_Maps_To_ZaakStatusResponseDto()
    {
        _fixture.Customize<ZaakStatus>(c => c.With(p => p.DatumStatusGezet, DateTime.UtcNow));

        var value = _fixture.Create<ZaakStatus>();
        var result = _mapper.Map<ZaakStatusResponseDto>(value);

        Assert.Equal(value.Zaak.Url, result.Zaak);
        Assert.Equal(value.StatusType, result.StatusType);
        Assert.Equal(value.DatumStatusGezet.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ"), result.DatumStatusGezet);
        Assert.Equal(value.StatusToelichting, result.StatusToelichting);
        Assert.Equal(value.Url, result.Url);
        Assert.Equal(value.Id.ToString(), result.Uuid);
    }

    [Fact]
    public void Zaak_Maps_To_ZaakRequestDto()
    {
        var value = _fixture.Create<Zaak>();
        var result = _mapper.Map<ZaakRequestDto>(value);

        Assert.Equal(value.Opschorting.Indicatie, result.Opschorting.Indicatie);
        Assert.Equal(value.Opschorting.Reden, result.Opschorting.Reden);
        Assert.Equal(value.Verlenging.Duur.ToString(), result.Verlenging.Duur);
        Assert.Equal(value.Verlenging.Reden, result.Verlenging.Reden);
        Assert.Equal(value.Archiefnominatie.ToString(), result.Archiefnominatie);
        Assert.Equal(value.Archiefstatus.ToString(), result.Archiefstatus);
        Assert.Equal(value.BetalingsIndicatie.ToString(), result.Betalingsindicatie);
        Assert.Equal(value.VertrouwelijkheidAanduiding.ToString(), result.Vertrouwelijkheidaanduiding);
    }

    [Fact]
    public void Zaak_Maps_To_ZaakResponseDto()
    {
        var value = _fixture.Create<Zaak>();
        // due to recursive objects this needs to be manually set,
        // because OmitOnRecursionFixture does not fill this automatically
        value.Deelzaken = [new Zaak { }];
        var result = _mapper.Map<ZaakResponseDto>(value);

        Assert.Equal(value.Id.ToString(), result.Uuid);
        Assert.All(value.Deelzaken, c => Assert.Contains(c.Url, result.Deelzaken));
        Assert.Equal(value.Opschorting.Indicatie, result.Opschorting.Indicatie);
        Assert.Equal(value.Opschorting.Reden, result.Opschorting.Reden);
        Assert.Equal(value.Verlenging.Duur.ToString(), result.Verlenging.Duur);
        Assert.Equal(value.Verlenging.Reden, result.Verlenging.Reden);
        Assert.Equal(value.Registratiedatum.Value.ToString("yyyy-MM-dd"), result.Registratiedatum);
        Assert.Equal(value.Startdatum.ToString("yyyy-MM-dd"), result.Startdatum);
        Assert.Equal(value.Einddatum?.ToString("yyyy-MM-dd"), result.Einddatum);
        Assert.Equal(value.EinddatumGepland?.ToString("yyyy-MM-dd"), result.EinddatumGepland);
        Assert.Equal(value.UiterlijkeEinddatumAfdoening?.ToString("yyyy-MM-dd"), result.UiterlijkeEinddatumAfdoening);
        Assert.Equal(value.Publicatiedatum?.ToString("yyyy-MM-dd"), result.Publicatiedatum);
        Assert.Equal(
            value.LaatsteBetaaldatum?.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture),
            result.LaatsteBetaaldatum
        );
        Assert.Equal(value.Archiefactiedatum?.ToString("yyyy-MM-dd"), result.Archiefactiedatum);
        Assert.Equal(value.Archiefnominatie.ToString(), result.Archiefnominatie);
        Assert.Equal(value.Archiefstatus.ToString(), result.Archiefstatus);
        Assert.Equal(value.BetalingsIndicatie.ToString(), result.Betalingsindicatie);
        Assert.Equal(value.VertrouwelijkheidAanduiding.ToString(), result.Vertrouwelijkheidaanduiding);
        Assert.All(value.ZaakEigenschappen, c => Assert.Contains(c.Url, result.Eigenschappen));
        Assert.Equal(value.ZaakStatussen.OrderByDescending(s => s.DatumStatusGezet).FirstOrDefault().Url, result.Status);
        Assert.Equal(value.Resultaat.Url, result.Resultaat);

        // common ZaakResponseDto and ZaakRequestDto fields
        Assert.Equal(value.Identificatie, result.Identificatie);
        Assert.Equal(value.Bronorganisatie, result.Bronorganisatie);
        Assert.Equal(value.Omschrijving, result.Omschrijving);
        Assert.Equal(value.Toelichting, result.Toelichting);
        Assert.Equal(value.Zaaktype, result.Zaaktype);
        Assert.Equal(value.VerantwoordelijkeOrganisatie, result.VerantwoordelijkeOrganisatie);
        Assert.Equal(value.Communicatiekanaal, result.Communicatiekanaal);
        Assert.Equal(value.ProductenOfDiensten, result.ProductenOfDiensten);
        Assert.Equal(value.Selectielijstklasse, result.Selectielijstklasse);
    }

    [Fact]
    public void Zaak_Maps_To_ZaakResponseDto_With_Zaakgeometrie_Point()
    {
        var point = new Point(52.1326, 5.2913);
        _fixture.Customize<Zaak>(c => c.With(p => p.Zaakgeometrie, point));
        var value = _fixture.Create<Zaak>();
        var result = _mapper.Map<ZaakResponseDto>(value);

        Assert.Equal("Point", result.Zaakgeometrie.GeometryType);
        Assert.Collection(result.Zaakgeometrie.Coordinates, c => Assert.Equal(c, point.Coordinate));
    }

    [Fact]
    public void Zaak_Maps_To_ZaakResponseDto_With_Zaakgeometrie_LineString()
    {
        var coordinates = new[] { new Coordinate(52.1326, 5.2913), new Coordinate(55.1694, 23.8813) };
        var linestring = new LineString(coordinates);
        _fixture.Customize<Zaak>(c => c.With(p => p.Zaakgeometrie, linestring));
        var value = _fixture.Create<Zaak>();
        var result = _mapper.Map<ZaakResponseDto>(value);

        Assert.Equal("LineString", result.Zaakgeometrie.GeometryType);
        Assert.Collection(result.Zaakgeometrie.Coordinates, c => Assert.Equal(c, coordinates[0]), c => Assert.Equal(c, coordinates[1]));
    }

    [Fact]
    public void Zaak_Maps_To_ZaakResponseDto_With_Recursive_Hoofzaak_Url()
    {
        _fixture.Customize<Zaak>(c => c.Without(p => p.Zaakgeometrie).With(p => p.Hoofdzaak, new Zaak { }));
        var value = _fixture.Create<Zaak>();
        var result = _mapper.Map<ZaakResponseDto>(value);

        Assert.Equal(value.Hoofdzaak.Url, result.Hoofdzaak);
    }

    [Fact]
    public void Zaak_with_null_ZaakStatussen_maps_Status_to_null()
    {
        // The Status PreCondition: unlike the general EmptyCollectionIfNull destination transform
        // (which applies to collection-typed members), dest.Status is a plain string (scalar), so a
        // null ZaakStatussen navigation must fold to a null Status, not throw or fall back to some
        // default. Verified by deliberate breakage: temporarily change the null-check in
        // DomainToResponseRegister's Zaak->ZaakResponseDto Status map to unconditionally call
        // MapsterUrlResolver.ResolveUrl(src.ZaakStatussen.OrderByDescending(...).FirstOrDefault())
        // and this test throws a NullReferenceException instead of passing.
        _fixture.Customize<Zaak>(c => c.Without(p => p.Zaakgeometrie).Without(p => p.ZaakStatussen));
        var value = _fixture.Create<Zaak>();

        var result = _mapper.Map<ZaakResponseDto>(value);

        Assert.Null(result.Status);
    }

    [Fact]
    public void Zaak_with_multiple_ZaakStatussen_maps_Status_to_latest_by_DatumStatusGezet()
    {
        // Confirms the PreCondition's OrderByDescending(s => s.DatumStatusGezet).FirstOrDefault()
        // picks the LATEST status, not merely "any" status. Verified by deliberate breakage: change
        // OrderByDescending to OrderBy (or drop the ordering) in DomainToResponseRegister and
        // re-run - this test then asserts against the oldest status's URL and fails.
        var zaak = new Zaak { Id = Guid.NewGuid() };
        var oldest = new ZaakStatus
        {
            Id = Guid.NewGuid(),
            Zaak = zaak,
            DatumStatusGezet = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        };
        var latest = new ZaakStatus
        {
            Id = Guid.NewGuid(),
            Zaak = zaak,
            DatumStatusGezet = new DateTime(2023, 6, 15, 0, 0, 0, DateTimeKind.Utc),
        };
        var middle = new ZaakStatus
        {
            Id = Guid.NewGuid(),
            Zaak = zaak,
            DatumStatusGezet = new DateTime(2021, 3, 3, 0, 0, 0, DateTimeKind.Utc),
        };
        zaak.ZaakStatussen = [oldest, latest, middle];

        var result = _mapper.Map<ZaakResponseDto>(zaak);

        Assert.Equal(latest.Url, result.Status);
        Assert.NotEqual(oldest.Url, result.Status);
    }

    [Fact]
    public void ZaakKenmerk_Maps_To_ZaakKenmerkDto()
    {
        var value = _fixture.Create<ZaakKenmerk>();
        var result = _mapper.Map<ZaakKenmerkDto>(value);

        Assert.Equal(value.Bron, result.Bron);
        Assert.Equal(value.Kenmerk, result.Kenmerk);
    }

    [Fact]
    public void RelevanteAndereZaak_Maps_To_RelevanteAndereZaakDto()
    {
        var value = _fixture.Create<RelevanteAndereZaak>();
        var result = _mapper.Map<RelevanteAndereZaakDto>(value);

        Assert.Equal(value.AardRelatie, result.AardRelatie);
        Assert.Equal(value.Url, result.Url);
    }

    [Fact]
    public void ZaakRol_Maps_To_ZaakRolResponseDto()
    {
        _fixture.Customize<ZaakRol>(c => c.With(p => p.Registratiedatum, DateTime.UtcNow));

        var value = _fixture.Create<ZaakRol>();
        var result = _mapper.Map<ZaakRolResponseDto>(value);

        Assert.Equal(value.Url, result.Url);
        Assert.Equal(value.Id.ToString(), result.Uuid);
        Assert.Equal(value.Registratiedatum.ToString("yyyy-MM-ddTHH:mm:ssZ"), result.Registratiedatum);
        Assert.Equal(value.Omschrijving, result.Omschrijving);
        Assert.Equal(value.OmschrijvingGeneriek.ToString(), result.OmschrijvingGeneriek);
    }

    [Fact]
    public void Verblijfsadres_Maps_To_VerblijfsadresDto()
    {
        var value = _fixture.Create<Verblijfsadres>();
        var result = _mapper.Map<VerblijfsadresDto>(value);

        Assert.Equal(value.AoaHuisletter, result.AoaHuisletter);
        Assert.Equal(value.AoaHuisnummer, result.AoaHuisnummer);
        Assert.Equal(value.AoaHuisnummertoevoeging.ToString(), result.AoaHuisnummertoevoeging);
        Assert.Equal(value.AoaIdentificatie, result.AoaIdentificatie);
        Assert.Equal(value.AoaPostcode, result.AoaPostcode);
        Assert.Equal(value.GorOpenbareRuimteNaam, result.GorOpenbareRuimteNaam);
        Assert.Equal(value.InpLocatiebeschrijving, result.InpLocatiebeschrijving);
        Assert.Equal(value.WplWoonplaatsNaam, result.WplWoonplaatsNaam);
    }

    [Fact]
    public void SubVerblijfBuitenland_Maps_To_SubVerblijfBuitenlandDto()
    {
        var value = _fixture.Create<SubVerblijfBuitenland>();
        var result = _mapper.Map<SubVerblijfBuitenlandDto>(value);

        Assert.Equal(value.LndLandcode, result.LndLandcode);
        Assert.Equal(value.LndLandnaam, result.LndLandnaam);
        Assert.Equal(value.SubAdresBuitenland1, result.SubAdresBuitenland1);
        Assert.Equal(value.SubAdresBuitenland2, result.SubAdresBuitenland2);
        Assert.Equal(value.SubAdresBuitenland3, result.SubAdresBuitenland3);
    }

    [Fact]
    public void NatuurlijkPersoonZaakRol_Maps_To_NatuurlijkPersoonZaakRolDto()
    {
        _fixture.Customize<NatuurlijkPersoonZaakRol>(c => c.With(p => p.Geboortedatum, DateTime.UtcNow));

        var value = _fixture.Create<NatuurlijkPersoonZaakRol>();
        var result = _mapper.Map<NatuurlijkPersoonZaakRolDto>(value);

        Assert.Equal(value.AnpIdentificatie, result.AnpIdentificatie);
        Assert.True(value.Geboortedatum.HasValue);
        Assert.Equal(value.Geboortedatum.Value.ToString("yyyy-MM-ddTHH:mm:ssZ"), result.Geboortedatum);
        Assert.Equal(value.Geslachtsaanduiding.ToString(), result.Geslachtsaanduiding);
        Assert.Equal(value.Geslachtsnaam, result.Geslachtsnaam);
        Assert.Equal(value.InpANummer, result.InpANummer);
        Assert.Equal(value.InpBsnEncrypted, result.InpBsn);
        Assert.Equal(value.Voorletters, result.Voorletters);
        Assert.Equal(value.Voornamen, result.Voornamen);
        Assert.Equal(value.VoorvoegselGeslachtsnaam, result.VoorvoegselGeslachtsnaam);
        Assert.NotNull(result.Verblijfsadres);
        Assert.NotNull(result.SubVerblijfBuitenland);
    }

    [Fact]
    public void NietNatuurlijkPersoonZaakRol_Maps_To_NietNatuurlijkPersoonZaakRolDto()
    {
        _fixture.Customize<NietNatuurlijkPersoonZaakRol>(c => c.With(p => p.InnRechtsvorm, _fixture.Create<InnRechtsvorm>()));
        var value = _fixture.Create<NietNatuurlijkPersoonZaakRol>();
        var result = _mapper.Map<NietNatuurlijkPersoonZaakRolDto>(value);

        Assert.Equal(value.AnnIdentificatie, result.AnnIdentificatie);
        Assert.Equal(value.Bezoekadres, result.Bezoekadres);
        Assert.Equal(value.InnNnpId, result.InnNnpId);
        Assert.Equal(value.InnRechtsvorm.ToString(), result.InnRechtsvorm);
        Assert.Equal(value.StatutaireNaam, result.StatutaireNaam);
        Assert.NotNull(result.SubVerblijfBuitenland);
    }

    [Fact]
    public void VestigingZaakRol_Maps_To_VestigingZaakRolDto()
    {
        var value = _fixture.Create<VestigingZaakRol>();
        var result = _mapper.Map<VestigingZaakRolDto>(value);

        Assert.Equal(value.Handelsnaam, result.Handelsnaam);
        Assert.Equal(value.VestigingsNummer, result.VestigingsNummer);
        Assert.NotNull(result.SubVerblijfBuitenland);
        Assert.NotNull(result.Verblijfsadres);
    }

    [Fact]
    public void OrganisatorischeEenheidZaakRol_Maps_To_OrganisatorischeEenheidZaakRolDto()
    {
        var value = _fixture.Create<OrganisatorischeEenheidZaakRol>();
        var result = _mapper.Map<OrganisatorischeEenheidZaakRolDto>(value);

        Assert.Equal(value.Identificatie, result.Identificatie);
        Assert.Equal(value.Naam, result.Naam);
        Assert.Equal(value.IsGehuisvestIn, result.IsGehuisvestIn);
    }

    [Fact]
    public void MedewerkerZaakRol_Maps_To_MedewerkerZaakRolDto()
    {
        var value = _fixture.Create<MedewerkerZaakRol>();
        var result = _mapper.Map<MedewerkerZaakRolDto>(value);

        Assert.Equal(value.Achternaam, result.Achternaam);
        Assert.Equal(value.Identificatie, result.Identificatie);
        Assert.Equal(value.Voorletters, result.Voorletters);
        Assert.Equal(value.VoorvoegselAchternaam, result.VoorvoegselAchternaam);
    }

    [Fact]
    public void ZaakObject_Maps_To_ZaakObjectResponseDto()
    {
        var obj = _fixture.Create<ZaakObject>();
        var result = _mapper.Map<ZaakObjectResponseDto>(obj);

        Assert.Equal(obj.Object, result.Object);
        Assert.Equal(obj.ObjectType.ToString(), result.ObjectType);
        Assert.Equal(obj.ObjectTypeOverige, result.ObjectTypeOverige);
        Assert.Equal(obj.RelatieOmschrijving, result.RelatieOmschrijving);
        Assert.Equal(obj.Url, result.Url);
        Assert.Equal(obj.Id, result.Uuid);
        Assert.Equal(obj.Zaak.Url, result.Zaak);
    }

    [Fact]
    public void ZaakObject_with_ObjectType_adres_Maps_To_AdresZaakObjectResponseDto_via_local_config()
    {
        // The Shape-B ConstructUsing factory (CreateZaakObjectResponseDto) recursively adapts the
        // nested Adres entity via source.Adres.Adapt<AdresZaakObjectDto>(config), passing the LOCAL
        // config explicitly rather than calling a bare .Adapt<T>() (which would resolve against
        // Mapster's ambient TypeAdapterConfig.GlobalSettings instead). This asserts the derived
        // response type is selected and its ObjectIdentificatie is actually populated by that nested
        // map, proving the recursive call reached the local config's AdresZaakObject->AdresZaakObjectDto
        // rule (not merely returning a default/empty instance).
        var zaak = new Zaak { Id = Guid.NewGuid() };
        var adres = new AdresZaakObject
        {
            Id = Guid.NewGuid(),
            Identificatie = "adres-identificatie",
            WplWoonplaatsNaam = "Enschede",
            GorOpenbareRuimteNaam = "Hoofdstraat",
            Huisnummer = 42,
        };
        var source = new ZaakObject
        {
            Id = Guid.NewGuid(),
            Zaak = zaak,
            ObjectType = ObjectType.adres,
            Adres = adres,
        };

        var result = _mapper.Map<ZaakObjectResponseDto>(source);

        var adresResult = Assert.IsType<AdresZaakObjectResponseDto>(result);
        Assert.NotNull(adresResult.ObjectIdentificatie);
        Assert.Equal(adres.Identificatie, adresResult.ObjectIdentificatie.Identificatie);
        Assert.Equal(adres.WplWoonplaatsNaam, adresResult.ObjectIdentificatie.WplWoonplaatsNaam);
        Assert.Equal(adres.GorOpenbareRuimteNaam, adresResult.ObjectIdentificatie.GorOpenbareRuimteNaam);
        Assert.Equal(adres.Huisnummer, adresResult.ObjectIdentificatie.Huisnummer);
    }

    [Fact]
    public void ZaakObject_with_ObjectType_overige_Maps_ObjectIdentificatie_via_local_JToken_rule()
    {
        // Critical proof that the recursive Adapt call inside the factory uses THIS local config, not
        // GlobalSettings: OverigeZaakObject->OverigeZaakObjectDto has a local-only rule
        // (.Map(dest.OverigeData, src => JToken.Parse(src.OverigeData))) converting the raw JSON
        // string column into a JToken. If the factory's nested call fell back to GlobalSettings
        // (which has no knowledge of this rule), OverigeData would map wrong (e.g. stay a raw string,
        // fail to convert, or throw) instead of a correctly-parsed JToken.
        var zaak = new Zaak { Id = Guid.NewGuid() };
        var overige = new OverigeZaakObject { Id = Guid.NewGuid(), OverigeData = "{\"key\":\"value\",\"count\":3}" };
        var source = new ZaakObject
        {
            Id = Guid.NewGuid(),
            Zaak = zaak,
            ObjectType = ObjectType.overige,
            Overige = overige,
        };

        var result = _mapper.Map<ZaakObjectResponseDto>(source);

        var overigeResult = Assert.IsType<OverigeZaakObjectResponseDto>(result);
        Assert.NotNull(overigeResult.ObjectIdentificatie);
        Assert.Equal(JToken.Parse(overige.OverigeData), overigeResult.ObjectIdentificatie.OverigeData);
        Assert.Equal("value", overigeResult.ObjectIdentificatie.OverigeData["key"].ToString());
    }

    [Fact]
    public void ZaakRol_with_BetrokkeneType_natuurlijk_persoon_Maps_BetrokkeneIdentificatie_via_local_config()
    {
        // Mirrors the ZaakObject factory tests above for the other Shape-B ConstructUsing factory
        // (CreateZaakRolResponseDto). NatuurlijkPersoonZaakRol->NatuurlijkPersoonZaakRolDto has its own
        // local-only rule mapping InpBsn from InpBsnEncrypted (not a same-name convention match) -
        // asserting it here proves the nested source.NatuurlijkPersoon.Adapt<T>(config) call resolved
        // against the local config rather than GlobalSettings.
        var zaak = new Zaak { Id = Guid.NewGuid() };
        var natuurlijkPersoon = new NatuurlijkPersoonZaakRol
        {
            Id = Guid.NewGuid(),
            InpBsnEncrypted = "123456789",
            Geslachtsnaam = "Jansen",
        };
        var source = new ZaakRol
        {
            Id = Guid.NewGuid(),
            Zaak = zaak,
            BetrokkeneType = BetrokkeneType.natuurlijk_persoon,
            NatuurlijkPersoon = natuurlijkPersoon,
            Roltoelichting = "toelichting",
            Omschrijving = "omschrijving",
        };

        var result = _mapper.Map<ZaakRolResponseDto>(source);

        var natuurlijkPersoonResult = Assert.IsType<NatuurlijkPersoonZaakRolResponseDto>(result);
        Assert.NotNull(natuurlijkPersoonResult.BetrokkeneIdentificatie);
        Assert.Equal(natuurlijkPersoon.InpBsnEncrypted, natuurlijkPersoonResult.BetrokkeneIdentificatie.InpBsn);
        Assert.Equal(natuurlijkPersoon.Geslachtsnaam, natuurlijkPersoonResult.BetrokkeneIdentificatie.Geslachtsnaam);
    }

    [Fact]
    public void AdresZaakObject_Maps_To_AdresZaakObjectDto()
    {
        var value = _fixture.Create<AdresZaakObject>();
        var result = _mapper.Map<AdresZaakObjectDto>(value);

        Assert.Equal(value.GorOpenbareRuimteNaam, result.GorOpenbareRuimteNaam);
        Assert.Equal(value.Huisletter, result.Huisletter);
        Assert.Equal(value.Huisnummer, result.Huisnummer);
        Assert.Equal(value.HuisnummerToevoeging, result.HuisnummerToevoeging);
        Assert.Equal(value.Identificatie, result.Identificatie);
        Assert.Equal(value.Postcode, result.Postcode);
        Assert.Equal(value.WplWoonplaatsNaam, result.WplWoonplaatsNaam);
    }

    [Fact]
    public void AdresZaakObject_Maps_To_AdresZaakObjectRequestDto_with_ObjectIdentificatie_via_local_config()
    {
        // One of the 8 PATCH-merge maps: AdresZaakObject -> AdresZaakObjectRequestDto assigns
        // ObjectIdentificatie via src.Adapt<AdresZaakObjectDto>(config), threading the local config
        // explicitly (the AutoMapper source used an implicit whole-entity MapFrom(src => src), which
        // does not compile/translate directly under Mapster). Asserts the nested DTO is populated, not
        // silently null/empty.
        var value = _fixture.Create<AdresZaakObject>();

        var result = _mapper.Map<AdresZaakObjectRequestDto>(value);

        Assert.NotNull(result.ObjectIdentificatie);
        Assert.Equal(value.Identificatie, result.ObjectIdentificatie.Identificatie);
        Assert.Equal(value.WplWoonplaatsNaam, result.ObjectIdentificatie.WplWoonplaatsNaam);
        Assert.Equal(value.GorOpenbareRuimteNaam, result.ObjectIdentificatie.GorOpenbareRuimteNaam);
        Assert.Equal(value.Huisnummer, result.ObjectIdentificatie.Huisnummer);
    }

    [Fact]
    public void OverigeZaakObject_Maps_To_OverigeZaakObjectRequestDto_with_ObjectIdentificatie_via_local_config()
    {
        // Second of the 8 PATCH-merge maps, and the more discriminating of the two: OverigeZaakObject's
        // nested map relies on the local JToken.Parse(...) rule (same one exercised by the response-side
        // factory test above) - a fallback to GlobalSettings here would produce a wrong/empty OverigeData.
        var value = _fixture.Create<OverigeZaakObject>();
        value.OverigeData = "{\"foo\":\"bar\"}";

        var result = _mapper.Map<OverigeZaakObjectRequestDto>(value);

        Assert.NotNull(result.ObjectIdentificatie);
        Assert.Equal(JToken.Parse(value.OverigeData), result.ObjectIdentificatie.OverigeData);
        Assert.Equal("bar", result.ObjectIdentificatie.OverigeData["foo"].ToString());
    }

    [Fact]
    public void BuurtZaakObject_Maps_To_BuurtZaakObjectDto()
    {
        var value = _fixture.Create<BuurtZaakObject>();
        var result = _mapper.Map<BuurtZaakObjectDto>(value);

        Assert.Equal(value.BuurtCode, result.BuurtCode);
        Assert.Equal(value.BuurtNaam, result.BuurtNaam);
        Assert.Equal(value.GemGemeenteCode, result.GemGemeenteCode);
        Assert.Equal(value.WykWijkCode, result.WykWijkCode);
    }

    [Fact]
    public void PandZaakObject_Maps_To_PandZaakObjectDto()
    {
        var value = _fixture.Create<PandZaakObject>();
        var result = _mapper.Map<PandZaakObjectDto>(value);

        Assert.Equal(value.Identificatie, result.Identificatie);
    }

    [Fact]
    public void KadastraleOnroerendeZaakObject_Maps_To_KadastraleOnroerendeZaakObjectDto()
    {
        var value = _fixture.Create<KadastraleOnroerendeZaakObject>();
        var result = _mapper.Map<KadastraleOnroerendeZaakObjectDto>(value);

        Assert.Equal(value.KadastraleAanduiding, result.KadastraleAanduiding);
        Assert.Equal(value.KadastraleIdentificatie, result.KadastraleIdentificatie);
    }

    [Fact]
    public void GemeenteZaakObject_Maps_To_GemeenteZaakObjectDto()
    {
        var value = _fixture.Create<GemeenteZaakObject>();
        var result = _mapper.Map<GemeenteZaakObjectDto>(value);

        Assert.Equal(value.GemeenteCode, result.GemeenteCode);
        Assert.Equal(value.GemeenteNaam, result.GemeenteNaam);
    }

    [Theory]
    [MemberData(nameof(OverigeDataJsonValues))]
    public void OverigeZaakObject_Maps_To_OverigeZaakObjectDto(string jsonValue)
    {
        _fixture.Customize<OverigeZaakObject>(c => c.With(p => p.OverigeData, jsonValue));

        var value = _fixture.Create<OverigeZaakObject>();
        var result = _mapper.Map<OverigeZaakObjectDto>(value);

        Assert.Equal(JToken.Parse(jsonValue), result.OverigeData);
    }

    [Fact]
    public void TerreinGebouwdObjectZaakObject_Maps_To_TerreinGebouwdObjectZaakObjectDto()
    {
        var value = _fixture.Create<TerreinGebouwdObjectZaakObject>();
        var result = _mapper.Map<TerreinGebouwdObjectZaakObjectDto>(value);

        Assert.Equal(value.Identificatie, result.Identificatie);
        Assert.Equal(value.AdresAanduidingGrp_AoaHuisletter, result.AdresAanduidingGrp.AoaHuisletter);
        Assert.Equal(value.AdresAanduidingGrp_AoaHuisnummer, result.AdresAanduidingGrp.AoaHuisnummer);
        Assert.Equal(value.AdresAanduidingGrp_AoaHuisnummertoevoeging, result.AdresAanduidingGrp.AoaHuisnummertoevoeging);
        Assert.Equal(value.AdresAanduidingGrp_AoaPostcode, result.AdresAanduidingGrp.AoaPostcode);
        Assert.Equal(value.AdresAanduidingGrp_GorOpenbareRuimteNaam, result.AdresAanduidingGrp.GorOpenbareRuimteNaam);
        Assert.Equal(value.AdresAanduidingGrp_NumIdentificatie, result.AdresAanduidingGrp.NumIdentificatie);
        Assert.Equal(value.AdresAanduidingGrp_OaoIdentificatie, result.AdresAanduidingGrp.OaoIdentificatie);
        Assert.Equal(value.AdresAanduidingGrp_OgoLocatieAanduiding, result.AdresAanduidingGrp.OgoLocatieAanduiding);
        Assert.Equal(value.AdresAanduidingGrp_WplWoonplaatsNaam, result.AdresAanduidingGrp.WplWoonplaatsNaam);
    }

    [Fact]
    public void ZaakInformatieObject_Maps_To_ZaakInformatieObjectResponseDto()
    {
        _fixture.Customize<ZaakInformatieObject>(c =>
            c.With(p => p.RegistratieDatum, DateTime.UtcNow).With(p => p.AardRelatieWeergave, AardRelatieWeergave.hoort_bij_omgekeerd_kent)
        );

        var value = _fixture.Create<ZaakInformatieObject>();
        var result = _mapper.Map<ZaakInformatieObjectResponseDto>(value);

        Assert.Equal(value.Beschrijving, result.Beschrijving);
        Assert.Equal(value.InformatieObject, result.InformatieObject);
        Assert.Equal(value.RegistratieDatum.ToString("yyyy-MM-ddTHH:mm:ssZ"), result.RegistratieDatum);
        Assert.Equal(value.Titel, result.Titel);
        Assert.Equal(value.Url, result.Url);
        Assert.Equal(value.Id.ToString(), result.Uuid);
        Assert.Equal(value.Zaak.Url, result.Zaak);
        Assert.Equal("Hoort bij, omgekeerd: kent", result.AardRelatieWeergave);
    }

    [Fact]
    public void AardRelatieWeergave_hoort_bij_omgekeerd_kent_Maps_To_expected_string()
    {
        _fixture.Customize<ZaakInformatieObject>(c => c.With(p => p.AardRelatieWeergave, AardRelatieWeergave.hoort_bij_omgekeerd_kent));
        var value = _fixture.Create<ZaakInformatieObject>();

        var result = _mapper.Map<ZaakInformatieObjectResponseDto>(value);

        Assert.Equal("Hoort bij, omgekeerd: kent", result.AardRelatieWeergave);
    }

    [Fact]
    public void AardRelatieWeergave_legt_vast_omgekeerd_kan_vastgelegd_zijn_als_Maps_To_expected_string()
    {
        _fixture.Customize<ZaakInformatieObject>(c =>
            c.With(p => p.AardRelatieWeergave, AardRelatieWeergave.legt_vast_omgekeerd_kan_vastgelegd_zijn_als)
        );
        var value = _fixture.Create<ZaakInformatieObject>();

        var result = _mapper.Map<ZaakInformatieObjectResponseDto>(value);

        Assert.Equal("Legt vast, omgekeerd: kan vastgelegd zijn als", result.AardRelatieWeergave);
    }

    [Fact]
    public void ZaakResultaat_Maps_To_ZaakResultaatRequestDto()
    {
        var value = _fixture.Create<ZaakResultaat>();
        var result = _mapper.Map<ZaakResultaatRequestDto>(value);

        Assert.Equal(value.Zaak.Url, result.Zaak);
        Assert.Equal(value.ResultaatType, result.ResultaatType);
        Assert.Equal(value.Toelichting, result.Toelichting);
    }

    [Fact]
    public void ZaakBesluit_Maps_To_ZaakBesluitResponseDto()
    {
        var value = _fixture.Create<ZaakBesluit>();
        var result = _mapper.Map<ZaakBesluitResponseDto>(value);

        Assert.Equal(value.Besluit, result.Besluit);
        Assert.Equal(value.Url, result.Url);
        Assert.Equal(value.Id.ToString(), result.Uuid);
    }

    [Fact]
    public void KlantContact_Maps_To_KlantContactResponseDto()
    {
        _fixture.Customize<KlantContact>(c => c.With(p => p.DatumTijd, DateTime.UtcNow));

        var value = _fixture.Create<KlantContact>();
        var result = _mapper.Map<KlantContactResponseDto>(value);

        Assert.Equal(value.Identificatie, result.Identificatie);
        Assert.Equal(value.DatumTijd.ToString("yyyy-MM-ddTHH:mm:ssZ"), result.DatumTijd);
        Assert.Equal(value.Kanaal, result.Kanaal);
        Assert.Equal(value.Onderwerp, result.Onderwerp);
        Assert.Equal(value.Toelichting, result.Toelichting);
    }

    [Fact]
    public void AuditTrailRegel_Maps_To_AuditTrailRegelDto()
    {
        // AutoFixture's random strings for Oud/Nieuw won't deserialize as JSON, so pin them to
        // valid JSON explicitly. This exercises ConvertWijzigingenToDto for real (a broken port
        // would either throw during mapping or leave Wijzigingen.Oud/.Nieuw null).
        var value = _fixture
            .Build<AuditTrailRegel>()
            .With(a => a.Oud, "{\"naam\":\"oud-waarde\"}")
            .With(a => a.Nieuw, "{\"naam\":\"nieuw-waarde\"}")
            .Create();

        var result = _mapper.Map<AuditTrailRegelDto>(value);

        Assert.Equal(value.Id.ToString(), result.Uuid);
        Assert.Equal(ProfileHelper.StringDateFromDateTime(value.AanmaakDatum, true), result.AanmaakDatum);
        // dynamic access into the deserialized payload proves ConvertWijzigingenToDto actually ran
        // JsonConvert.DeserializeObject rather than just assigning the raw JSON string through -- the
        // latter would still be non-null (Assert.NotNull alone wouldn't catch it) but ".naam" wouldn't
        // resolve to the pinned value below.
        Assert.Equal("oud-waarde", ((dynamic)result.Wijzigingen.Oud).naam.ToString());
        Assert.Equal("nieuw-waarde", ((dynamic)result.Wijzigingen.Nieuw).naam.ToString());
    }

    [Fact]
    public void AuditTrailRegel_with_null_or_empty_Oud_and_Nieuw_Maps_Wijzigingen_fields_to_null()
    {
        var value = _fixture.Build<AuditTrailRegel>().With(a => a.Oud, (string)null).With(a => a.Nieuw, "").Create();

        var result = _mapper.Map<AuditTrailRegelDto>(value);

        Assert.NotNull(result.Wijzigingen);
        Assert.Null(result.Wijzigingen.Oud);
        Assert.Null(result.Wijzigingen.Nieuw);
    }

    public static IEnumerable<object[]> OverigeDataJsonValues =>
        [
            [
                JsonConvert.SerializeObject(
                    new
                    {
                        name = "Test",
                        value = 42,
                        nested = new { flag = true },
                    }
                ),
            ],
            [JsonConvert.SerializeObject(new object[] { "item1", 123, true, new { prop = "value" } })],
            [JsonConvert.SerializeObject(12345.67)],
            [JsonConvert.SerializeObject(true)],
            [JsonConvert.SerializeObject("some plain text value")],
        ];
}
