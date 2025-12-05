using System;
using System.Collections.Generic;
using System.Linq;
using AutoFixture;
using AutoMapper;
using Moq;
using NetTopologySuite.Geometries;
using OneGround.ZGW.Common.Web.Mapping.ValueResolvers;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.DataAccess;
using OneGround.ZGW.Zaken.Contracts.v1;
using OneGround.ZGW.Zaken.Contracts.v1.Requests;
using OneGround.ZGW.Zaken.Contracts.v1.Responses;
using OneGround.ZGW.Zaken.Contracts.v1.Responses.ZaakObject;
using OneGround.ZGW.Zaken.Contracts.v1.Responses.ZaakRol;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.DataModel.ZaakObject;
using OneGround.ZGW.Zaken.DataModel.ZaakRol;
using OneGround.ZGW.Zaken.Web.MappingProfiles.v1;
using Xunit;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.MappingTests;

public class DomainToResponseProfileTests
{
    private readonly AutoMapperFixture _fixture = new AutoMapperFixture();
    private readonly Mock<IEntityUriService> _mockedUriService = new Mock<IEntityUriService>();
    private readonly IMapper _mapper;

    public DomainToResponseProfileTests()
    {
        _fixture.Register<DateOnly>(() => DateOnly.FromDateTime(DateTime.UtcNow));

        var configuration = new MapperConfiguration(config =>
        {
            config.AddProfile(new DomainToResponseProfile());
            config.ShouldMapMethod = (m => false);
        });

        // Important: if tests starts failing, that means that mappings are missing Ignore() or MapFrom()
        // for members which does not map automatically by name
        configuration.AssertConfigurationIsValid();

        _mockedUriService.Setup(s => s.GetUri(It.IsAny<IUrlEntity>())).Returns<IUrlEntity>(e => e.Url);

        _mapper = configuration.CreateMapper(t =>
        {
            if (t == typeof(UrlResolver))
            {
                return new UrlResolver(_mockedUriService.Object);
            }
            if (t == typeof(MemberUrlResolver))
            {
                return new MemberUrlResolver(_mockedUriService.Object);
            }
            if (t == typeof(MemberUrlsResolver))
            {
                return new MemberUrlsResolver(_mockedUriService.Object);
            }
            throw new NotImplementedException($"Mapper is missing the service: {t})");
        });
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
        Assert.Equal(value.LaatsteBetaaldatum?.ToString("yyyy-MM-ddTHH:mm:ssZ"), result.LaatsteBetaaldatum);
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
        Assert.Equal(value.InpBsn, result.InpBsn);
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

    [Fact]
    public void OverigeZaakObject_Maps_To_OverigeZaakObjectDto()
    {
        var value = _fixture.Create<OverigeZaakObject>();
        var result = _mapper.Map<OverigeZaakObjectDto>(value);

        Assert.Equal(value.OverigeData, result.OverigeData);
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
        _fixture.Customize<ZaakInformatieObject>(c => c.With(p => p.RegistratieDatum, DateTime.UtcNow));

        var value = _fixture.Create<ZaakInformatieObject>();
        var result = _mapper.Map<ZaakInformatieObjectResponseDto>(value);

        Assert.Equal(value.Beschrijving, result.Beschrijving);
        Assert.Equal(value.InformatieObject, result.InformatieObject);
        Assert.Equal(value.RegistratieDatum.ToString("yyyy-MM-ddTHH:mm:ssZ"), result.RegistratieDatum);
        Assert.Equal(value.Titel, result.Titel);
        Assert.Equal(value.Url, result.Url);
        Assert.Equal(value.Id.ToString(), result.Uuid);
        Assert.Equal(value.Zaak.Url, result.Zaak);
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
}
