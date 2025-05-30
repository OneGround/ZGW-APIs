using System.Linq;
using AutoFixture;
using AutoMapper;
using AutoMapper.Internal;
using NetTopologySuite.Geometries;
using Roxit.ZGW.Common.DataModel;
using Roxit.ZGW.Common.Web;
using Roxit.ZGW.Zaken.Contracts.v1;
using Roxit.ZGW.Zaken.Contracts.v1.Queries;
using Roxit.ZGW.Zaken.Contracts.v1.Requests;
using Roxit.ZGW.Zaken.Contracts.v1.Requests.ZaakObject;
using Roxit.ZGW.Zaken.Contracts.v1.Requests.ZaakRol;
using Roxit.ZGW.Zaken.DataModel;
using Roxit.ZGW.Zaken.DataModel.ZaakObject;
using Roxit.ZGW.Zaken.DataModel.ZaakRol;
using Roxit.ZGW.Zaken.Web.MappingProfiles.v1;
using Roxit.ZGW.Zaken.Web.Models.v1;
using Xunit;

namespace Roxit.ZGW.Zaken.WebApi.UnitTests.MappingTests;

public class RequestToDomainProfileTests
{
    private readonly AutoMapperFixture _fixture = new AutoMapperFixture();
    private readonly IMapper _mapper;

    public RequestToDomainProfileTests()
    {
        var configuration = new MapperConfiguration(config =>
        {
            config.AddProfile(new RequestToDomainProfile());
            config.Internal().Mappers.Insert(0, new NullableEnumMapper());
        });

        // Important: if tests starts failing, that means that mappings are missing Ignore() or MapFrom()
        // for members which does not map automatically by name
        configuration.AssertConfigurationIsValid();

        _mapper = configuration.CreateMapper();
    }

    [Fact]
    public void GetAllZakenQueryParameters_Maps_To_GetAllZakenFilter()
    {
        _fixture.Customize<GetAllZakenQueryParameters>(c =>
            c.With(p => p.Archiefactiedatum, "2020-11-05")
                .With(p => p.Archiefactiedatum__gt, "2020-11-06")
                .With(p => p.Archiefactiedatum__lt, "2020-11-07")
                .With(p => p.Startdatum, "2020-11-08")
                .With(p => p.Startdatum__gt, "2020-11-09")
                .With(p => p.Startdatum__gte, "2020-11-10")
                .With(p => p.Startdatum__lt, "2020-11-11")
                .With(p => p.Startdatum__lte, "2020-11-12")
                .With(p => p.Archiefnominatie, ArchiefNominatie.vernietigen.ToString())
                .With(p => p.Archiefstatus, ArchiefStatus.overgedragen.ToString())
                .With(p => p.Archiefnominatie__in, $"{ArchiefNominatie.blijvend_bewaren}, {ArchiefNominatie.vernietigen}")
                .With(p => p.Archiefstatus__in, $"{ArchiefStatus.nog_te_archiveren}, {ArchiefStatus.gearchiveerd}")
        );
        var value = _fixture.Create<GetAllZakenQueryParameters>();
        var result = _mapper.Map<GetAllZakenFilter>(value);

        Assert.Equal(value.Archiefactiedatum, result.Archiefactiedatum.Value.ToString("yyyy-MM-dd"));
        Assert.Equal(value.Archiefactiedatum__gt, result.Archiefactiedatum__gt.Value.ToString("yyyy-MM-dd"));
        Assert.Equal(value.Archiefactiedatum__lt, result.Archiefactiedatum__lt.Value.ToString("yyyy-MM-dd"));
        Assert.Equal(value.Archiefnominatie, result.Archiefnominatie.Value.ToString());
        Assert.Equal(new[] { ArchiefNominatie.blijvend_bewaren, ArchiefNominatie.vernietigen }, result.Archiefnominatie__in);
        Assert.Equal(value.Archiefstatus, result.Archiefstatus.Value.ToString());
        Assert.Equal(new[] { ArchiefStatus.nog_te_archiveren, ArchiefStatus.gearchiveerd }, result.Archiefstatus__in);
        Assert.Equal(value.Bronorganisatie, result.Bronorganisatie);
        Assert.Equal(value.Identificatie, result.Identificatie);
        Assert.Equal(value.Startdatum, result.Startdatum.Value.ToString("yyyy-MM-dd"));
        Assert.Equal(value.Startdatum__gt, result.Startdatum__gt.Value.ToString("yyyy-MM-dd"));
        Assert.Equal(value.Startdatum__gte, result.Startdatum__gte.Value.ToString("yyyy-MM-dd"));
        Assert.Equal(value.Startdatum__lt, result.Startdatum__lt.Value.ToString("yyyy-MM-dd"));
        Assert.Equal(value.Startdatum__lte, result.Startdatum__lte.Value.ToString("yyyy-MM-dd"));
        Assert.Equal(value.Zaaktype, result.Zaaktype);
    }

    [Fact]
    public void ZaakRequestDto_Maps_To_Zaak()
    {
        static Point createPoint() => new Point(23.45, 53.20);

        var point = new Point(23.45, 53.20);
        _fixture.Customize<ZaakRequestDto>(c =>
            c.With(p => p.Zaakgeometrie, createPoint())
                .With(p => p.Registratiedatum, "2020-11-06")
                .With(p => p.Startdatum, "2020-11-07")
                .With(p => p.EinddatumGepland, "2020-11-08")
                .With(p => p.UiterlijkeEinddatumAfdoening, "2020-11-09")
                .With(p => p.Publicatiedatum, "2020-11-10")
                .With(p => p.LaatsteBetaaldatum, "2020-11-11T12:13:14Z")
                .With(p => p.Archiefactiedatum, "2020-11-12")
                .With(p => p.Verlenging, new ZaakVerlengingDto { Duur = "P365D", Reden = _fixture.Create<string>() })
                .With(p => p.Vertrouwelijkheidaanduiding, _fixture.Create<VertrouwelijkheidAanduiding>().ToString())
                .With(p => p.Archiefnominatie, _fixture.Create<ArchiefNominatie>().ToString())
                .With(p => p.Archiefstatus, _fixture.Create<ArchiefStatus>().ToString())
                .With(p => p.Betalingsindicatie, _fixture.Create<BetalingsIndicatie>().ToString())
        );
        var value = _fixture.Create<ZaakRequestDto>();
        var result = _mapper.Map<Zaak>(value);

        Assert.Equal(value.Identificatie, result.Identificatie);
        Assert.Equal(value.Bronorganisatie, result.Bronorganisatie);
        Assert.Equal(value.Omschrijving, result.Omschrijving);
        Assert.Equal(value.Toelichting, result.Toelichting);
        Assert.Equal(value.Zaaktype, result.Zaaktype);
        Assert.Equal(value.Registratiedatum, result.Registratiedatum.Value.ToString("yyyy-MM-dd"));
        Assert.Equal(value.VerantwoordelijkeOrganisatie, result.VerantwoordelijkeOrganisatie);
        Assert.Equal(value.Startdatum, result.Startdatum.ToString("yyyy-MM-dd"));
        Assert.Equal(value.EinddatumGepland, result.EinddatumGepland.Value.ToString("yyyy-MM-dd"));
        Assert.Equal(value.UiterlijkeEinddatumAfdoening, result.UiterlijkeEinddatumAfdoening.Value.ToString("yyyy-MM-dd"));
        Assert.Equal(value.Publicatiedatum, result.Publicatiedatum.Value.ToString("yyyy-MM-dd"));
        Assert.Equal(value.Communicatiekanaal, result.Communicatiekanaal);
        Assert.Equal(value.ProductenOfDiensten, result.ProductenOfDiensten);
        Assert.Equal(value.Vertrouwelijkheidaanduiding, result.VertrouwelijkheidAanduiding.ToString());
        Assert.Equal(value.Betalingsindicatie, result.BetalingsIndicatie.ToString());
        Assert.Equal(value.LaatsteBetaaldatum, result.LaatsteBetaaldatum.Value.ToString("yyyy-MM-ddThh:mm:ssZ"));
        Assert.Equal(createPoint(), result.Zaakgeometrie);
        Assert.Equal(value.Verlenging.Duur, result.Verlenging.Duur.ToString());
        Assert.Equal(value.Verlenging.Reden, result.Verlenging.Reden);
        Assert.Equal(value.Opschorting.Indicatie, result.Opschorting.Indicatie);
        Assert.Equal(value.Opschorting.Reden, result.Opschorting.Reden);
        Assert.Equal(value.Selectielijstklasse, result.Selectielijstklasse);
        Assert.All(value.RelevanteAndereZaken, c => Assert.NotNull(result.RelevanteAndereZaken.SingleOrDefault(s => s.AardRelatie == c.AardRelatie)));
        Assert.All(value.Kenmerken, c => Assert.NotNull(result.Kenmerken.SingleOrDefault(s => s.Bron == c.Bron && s.Kenmerk == c.Kenmerk)));
        Assert.Equal(value.Archiefactiedatum, result.Archiefactiedatum.Value.ToString("yyyy-MM-dd"));
        Assert.Equal(value.Archiefnominatie, result.Archiefnominatie.Value.ToString());
        Assert.Equal(value.Archiefstatus, result.Archiefstatus.ToString());
    }

    [Fact]
    public void RelevanteAndereZaakDto_Maps_To_RelevanteAndereZaak()
    {
        var value = _fixture.Create<RelevanteAndereZaakDto>();
        var result = _mapper.Map<RelevanteAndereZaak>(value);

        Assert.Equal(value.AardRelatie, result.AardRelatie);
    }

    [Fact]
    public void ZaakKenmerkDto_Maps_To_ZaakKenmerk()
    {
        var value = _fixture.Create<ZaakKenmerkDto>();
        var result = _mapper.Map<ZaakKenmerk>(value);

        Assert.Equal(value.Bron, result.Bron);
        Assert.Equal(value.Kenmerk, result.Kenmerk);
    }

    [Fact]
    public void GetAllZaakStatussenQueryParameters_Maps_To_GetAllZaakStatussenFilter()
    {
        var value = _fixture.Create<GetAllZaakStatussenQueryParameters>();
        var result = _mapper.Map<GetAllZaakStatussenFilter>(value);

        Assert.Equal(value.StatusType, result.StatusType);
        Assert.Equal(value.Zaak, result.Zaak);
    }

    [Fact]
    public void ZaakStatusRequestDto_Maps_To_ZaakStatus()
    {
        _fixture.Customize<ZaakStatusRequestDto>(c => c.With(p => p.DatumStatusGezet, "2020-11-06T12:13:14Z"));
        var value = _fixture.Create<ZaakStatusRequestDto>();
        var result = _mapper.Map<ZaakStatus>(value);

        Assert.Equal(value.DatumStatusGezet, result.DatumStatusGezet.ToString("yyyy-MM-ddThh:mm:ssZ"));
        Assert.Equal(value.StatusToelichting, result.StatusToelichting);
        Assert.Equal(value.StatusType, result.StatusType);
    }

    [Fact]
    public void GetAllZaakObjectenQueryParameters_Maps_To_GetAllZaakObjectenFilter()
    {
        _fixture.Customize<GetAllZaakObjectenQueryParameters>(c => c.With(p => p.ObjectType, _fixture.Create<ObjectType>().ToString()));
        var value = _fixture.Create<GetAllZaakObjectenQueryParameters>();
        var result = _mapper.Map<GetAllZaakObjectenFilter>(value);

        Assert.Equal(value.Object, result.Object);
        Assert.Equal(value.ObjectType, result.ObjectType.ToString());
        Assert.Equal(value.Zaak, result.Zaak);
    }

    [Fact]
    public void ZaakObjectRequestDto_Maps_To_ZaakObject()
    {
        _fixture.Customize<ZaakObjectRequestDto>(c => c.With(p => p.ObjectType, ObjectType.gemeentelijke_openbare_ruimte.ToString()));
        var value = _fixture.Create<ZaakObjectRequestDto>();
        var result = _mapper.Map<ZaakObject>(value);

        Assert.Equal(value.Object, result.Object);
        Assert.Equal(value.ObjectType, result.ObjectType.ToString());
        Assert.Equal(value.ObjectTypeOverige, result.ObjectTypeOverige);
        Assert.Equal(value.RelatieOmschrijving, result.RelatieOmschrijving);
        Assert.Null(result.Adres);
        Assert.Null(result.Gemeente);
        Assert.Null(result.Overige);
        Assert.Null(result.TerreinGebouwdObject);
    }

    [Fact]
    public void AdresZaakObjectDto_Maps_To_AdresZaakObject()
    {
        var value = _fixture.Create<AdresZaakObjectDto>();
        var result = _mapper.Map<AdresZaakObject>(value);

        Assert.Equal(value.GorOpenbareRuimteNaam, result.GorOpenbareRuimteNaam);
        Assert.Equal(value.Huisletter, result.Huisletter);
        Assert.Equal(value.Huisnummer, result.Huisnummer);
        Assert.Equal(value.HuisnummerToevoeging, result.HuisnummerToevoeging);
        Assert.Equal(value.Identificatie, result.Identificatie);
        Assert.Equal(value.Postcode, result.Postcode);
        Assert.Equal(value.WplWoonplaatsNaam, result.WplWoonplaatsNaam);
    }

    [Fact]
    public void BuurtZaakObjectDto_Maps_To_BuurtZaakObject()
    {
        var value = _fixture.Create<BuurtZaakObjectDto>();
        var result = _mapper.Map<BuurtZaakObject>(value);

        Assert.Equal(value.BuurtCode, result.BuurtCode);
        Assert.Equal(value.BuurtNaam, result.BuurtNaam);
        Assert.Equal(value.GemGemeenteCode, result.GemGemeenteCode);
        Assert.Equal(value.WykWijkCode, result.WykWijkCode);
    }

    [Fact]
    public void PandZaakObjectDto_Maps_To_PandZaakObject()
    {
        var value = _fixture.Create<PandZaakObjectDto>();
        var result = _mapper.Map<PandZaakObject>(value);

        Assert.Equal(value.Identificatie, result.Identificatie);
    }

    [Fact]
    public void KadastraleOnroerendeZaakObjectDto_Maps_To_KadastraleOnroerendeZaakObject()
    {
        var value = _fixture.Create<KadastraleOnroerendeZaakObjectDto>();
        var result = _mapper.Map<KadastraleOnroerendeZaakObject>(value);

        Assert.Equal(value.KadastraleAanduiding, result.KadastraleAanduiding);
        Assert.Equal(value.KadastraleIdentificatie, result.KadastraleIdentificatie);
    }

    [Fact]
    public void GemeenteZaakObjectDto_Maps_To_GemeenteZaakObject()
    {
        var value = _fixture.Create<GemeenteZaakObjectDto>();
        var result = _mapper.Map<GemeenteZaakObject>(value);

        Assert.Equal(value.GemeenteCode, result.GemeenteCode);
        Assert.Equal(value.GemeenteNaam, result.GemeenteNaam);
    }

    [Fact]
    public void TerreinGebouwdObjectZaakObjectDto_Maps_To_TerreinGebouwdObjectZaakObject()
    {
        var value = _fixture.Create<TerreinGebouwdObjectZaakObjectDto>();
        var result = _mapper.Map<TerreinGebouwdObjectZaakObject>(value);

        Assert.Equal(value.Identificatie, result.Identificatie);
        Assert.Equal(value.AdresAanduidingGrp.AoaHuisletter, result.AdresAanduidingGrp_AoaHuisletter);
        Assert.Equal(value.AdresAanduidingGrp.AoaHuisnummer, result.AdresAanduidingGrp_AoaHuisnummer);
        Assert.Equal(value.AdresAanduidingGrp.AoaHuisnummertoevoeging, result.AdresAanduidingGrp_AoaHuisnummertoevoeging);
        Assert.Equal(value.AdresAanduidingGrp.AoaPostcode, result.AdresAanduidingGrp_AoaPostcode);
        Assert.Equal(value.AdresAanduidingGrp.GorOpenbareRuimteNaam, result.AdresAanduidingGrp_GorOpenbareRuimteNaam);
        Assert.Equal(value.AdresAanduidingGrp.NumIdentificatie, result.AdresAanduidingGrp_NumIdentificatie);
        Assert.Equal(value.AdresAanduidingGrp.OaoIdentificatie, result.AdresAanduidingGrp_OaoIdentificatie);
        Assert.Equal(value.AdresAanduidingGrp.OgoLocatieAanduiding, result.AdresAanduidingGrp_OgoLocatieAanduiding);
        Assert.Equal(value.AdresAanduidingGrp.WplWoonplaatsNaam, result.AdresAanduidingGrp_WplWoonplaatsNaam);
    }

    [Fact]
    public void OverigeZaakObjectDto_Maps_To_OverigeZaakObject()
    {
        var value = _fixture.Create<OverigeZaakObjectDto>();
        var result = _mapper.Map<OverigeZaakObject>(value);

        Assert.Equal(value.OverigeData, result.OverigeData);
    }

    [Fact]
    public void WozWaardeZaakObjectDto_Maps_To_WozWaardeZaakObject()
    {
        var value = _fixture.Create<WozWaardeZaakObjectDto>();
        var result = _mapper.Map<WozWaardeZaakObject>(value);

        Assert.Equal(value.WaardePeildatum, result.WaardePeildatum);
    }

    [Fact]
    public void WozObjectDto_Maps_To_WozObject()
    {
        var value = _fixture.Create<WozObjectDto>();
        var result = _mapper.Map<WozObject>(value);

        Assert.Equal(value.WozObjectNummer, result.WozObjectNummer);
    }

    [Fact]
    public void AanduidingWozObjectDto_Maps_To_AanduidingWozObject()
    {
        var value = _fixture.Create<AanduidingWozObjectDto>();
        var result = _mapper.Map<AanduidingWozObject>(value);

        Assert.Equal(value.AoaHuisletter, result.AoaHuisletter);
        Assert.Equal(value.AoaHuisnummer, result.AoaHuisnummer);
        Assert.Equal(value.AoaHuisnummerToevoeging, result.AoaHuisnummerToevoeging);
        Assert.Equal(value.AoaIdentificatie, result.AoaIdentificatie);
        Assert.Equal(value.AoaPostcode, result.AoaPostcode);
        Assert.Equal(value.GorOpenbareRuimteNaam, result.GorOpenbareRuimteNaam);
        Assert.Equal(value.LocatieOmschrijving, result.LocatieOmschrijving);
        Assert.Equal(value.WplWoonplaatsNaam, result.WplWoonplaatsNaam);
    }

    [Fact]
    public void GetAllZaakInformatieObjectenQueryParameters_Maps_To_GetAllZaakInformatieObjectenFilter()
    {
        var value = _fixture.Create<GetAllZaakInformatieObjectenQueryParameters>();
        var result = _mapper.Map<GetAllZaakInformatieObjectenFilter>(value);

        Assert.Equal(value.InformatieObject, result.InformatieObject);
        Assert.Equal(value.Zaak, result.Zaak);
    }

    [Fact]
    public void ZaakInformatieObjectRequestDto_Maps_To_ZaakInformatieObject()
    {
        var value = _fixture.Create<ZaakInformatieObjectRequestDto>();
        var result = _mapper.Map<ZaakInformatieObject>(value);

        Assert.Equal(value.Beschrijving, result.Beschrijving);
        Assert.Equal(value.InformatieObject, result.InformatieObject);
        Assert.Equal(value.Titel, result.Titel);
    }

    [Fact]
    public void GetAllZaakRollenQueryParameters_Maps_To_GetAllZaakRollenFilter()
    {
        _fixture.Customize<GetAllZaakRollenQueryParameters>(c =>
            c.With(p => p.BetrokkeneType, BetrokkeneType.niet_natuurlijk_persoon.ToString())
                .With(p => p.OmschrijvingGeneriek, OmschrijvingGeneriek.belanghebbende.ToString())
        );
        var value = _fixture.Create<GetAllZaakRollenQueryParameters>();
        var result = _mapper.Map<GetAllZaakRollenFilter>(value);

        Assert.Equal(value.Betrokkene, result.Betrokkene);
        Assert.Equal(value.BetrokkeneType, result.BetrokkeneType.ToString());
        Assert.Equal(value.BetrokkeneIdentificatie__medewerker__identificatie, result.MedewerkerIdentificatie);
        Assert.Equal(value.BetrokkeneIdentificatie__natuurlijkPersoon__anpIdentificatie, result.NatuurlijkPersoonAnpIdentificatie);
        Assert.Equal(value.BetrokkeneIdentificatie__natuurlijkPersoon__inpA_nummer, result.NatuurlijkPersoonInpANummer);
        Assert.Equal(value.BetrokkeneIdentificatie__natuurlijkPersoon__inpBsn, result.NatuurlijkPersoonInpBsn);
        Assert.Equal(value.BetrokkeneIdentificatie__nietNatuurlijkPersoon__annIdentificatie, result.NietNatuurlijkPersoonAnnIdentificatie);
        Assert.Equal(value.BetrokkeneIdentificatie__nietNatuurlijkPersoon__innNnpId, result.NietNatuurlijkPersoonInnNnpId);
        Assert.Equal(value.Omschrijving, result.Omschrijving);
        Assert.Equal(value.OmschrijvingGeneriek, result.OmschrijvingGeneriek.ToString());
        Assert.Equal(value.BetrokkeneIdentificatie__organisatorischeEenheid__identificatie, result.OrganisatorischeEenheidIdentificatie);
        Assert.Equal(value.RolType, result.RolType);
        Assert.Equal(value.BetrokkeneIdentificatie__vestiging__vestigingsNummer, result.VestigingNummer);
        Assert.Equal(value.Zaak, result.Zaak);
    }

    [Fact]
    public void ZaakRolRequestDto_Maps_To_ZaakRol()
    {
        _fixture.Customize<ZaakRolRequestDto>(c =>
            c.With(p => p.BetrokkeneType, _fixture.Create<BetrokkeneType>().ToString())
                .With(p => p.IndicatieMachtiging, _fixture.Create<IndicatieMachtiging>().ToString())
        );
        var value = _fixture.Create<ZaakRolRequestDto>();
        var result = _mapper.Map<ZaakRol>(value);

        Assert.Equal(value.Betrokkene, result.Betrokkene);
        Assert.Equal(value.BetrokkeneType, result.BetrokkeneType.ToString());
        Assert.Equal(value.RolType, result.RolType);
        Assert.Equal(value.RolToelichting, result.Roltoelichting);
        Assert.Equal(value.IndicatieMachtiging, result.IndicatieMachtiging.ToString());
    }

    [Fact]
    public void VerblijfsadresDto_Maps_To_Verblijfsadres()
    {
        var value = _fixture.Create<VerblijfsadresDto>();
        var result = _mapper.Map<Verblijfsadres>(value);

        Assert.Equal(value.AoaIdentificatie, result.AoaIdentificatie);
        Assert.Equal(value.WplWoonplaatsNaam, result.WplWoonplaatsNaam);
        Assert.Equal(value.GorOpenbareRuimteNaam, result.GorOpenbareRuimteNaam);
        Assert.Equal(value.AoaPostcode, result.AoaPostcode);
        Assert.Equal(value.AoaHuisnummer, result.AoaHuisnummer);
        Assert.Equal(value.AoaHuisletter, result.AoaHuisletter);
        Assert.Equal(value.AoaHuisnummertoevoeging, result.AoaHuisnummertoevoeging);
        Assert.Equal(value.InpLocatiebeschrijving, result.InpLocatiebeschrijving);
    }

    [Fact]
    public void SubVerblijfBuitenlandDto_Maps_To_SubVerblijfBuitenland()
    {
        var value = _fixture.Create<SubVerblijfBuitenlandDto>();
        var result = _mapper.Map<SubVerblijfBuitenland>(value);

        Assert.Equal(value.LndLandcode, result.LndLandcode);
        Assert.Equal(value.LndLandnaam, result.LndLandnaam);
        Assert.Equal(value.SubAdresBuitenland1, result.SubAdresBuitenland1);
        Assert.Equal(value.SubAdresBuitenland2, result.SubAdresBuitenland2);
        Assert.Equal(value.SubAdresBuitenland3, result.SubAdresBuitenland3);
    }

    [Fact]
    public void NatuurlijkPersoonZaakRolDto_Maps_To_NatuurlijkPersoonZaakRol()
    {
        _fixture.Customize<NatuurlijkPersoonZaakRolDto>(c =>
            c.With(p => p.Geboortedatum, "2020-11-04").With(p => p.Geslachtsaanduiding, _fixture.Create<Geslachtsaanduiding>().ToString())
        );
        var value = _fixture.Create<NatuurlijkPersoonZaakRolDto>();
        var result = _mapper.Map<NatuurlijkPersoonZaakRol>(value);

        Assert.Equal(value.InpBsn, result.InpBsn);
        Assert.Equal(value.AnpIdentificatie, result.AnpIdentificatie);
        Assert.Equal(value.InpANummer, result.InpANummer);
        Assert.Equal(value.Geslachtsnaam, result.Geslachtsnaam);
        Assert.Equal(value.VoorvoegselGeslachtsnaam, result.VoorvoegselGeslachtsnaam);
        Assert.Equal(value.Voorletters, result.Voorletters);
        Assert.Equal(value.Voornamen, result.Voornamen);
        Assert.Equal(value.Geslachtsaanduiding, result.Geslachtsaanduiding.ToString());
        Assert.Equal(value.Geboortedatum, result.Geboortedatum.Value.ToString("yyyy-MM-dd"));
    }

    [Fact]
    public void NietNatuurlijkPersoonZaakRolDto_Maps_To_NietNatuurlijkPersoonZaakRol()
    {
        _fixture.Customize<NietNatuurlijkPersoonZaakRolDto>(c => c.With(p => p.InnRechtsvorm, _fixture.Create<InnRechtsvorm>().ToString()));
        var value = _fixture.Create<NietNatuurlijkPersoonZaakRolDto>();
        var result = _mapper.Map<NietNatuurlijkPersoonZaakRol>(value);

        Assert.Equal(value.InnNnpId, result.InnNnpId);
        Assert.Equal(value.AnnIdentificatie, result.AnnIdentificatie);
        Assert.Equal(value.StatutaireNaam, result.StatutaireNaam);
        Assert.Equal(value.InnRechtsvorm, result.InnRechtsvorm.ToString());
        Assert.Equal(value.Bezoekadres, result.Bezoekadres);
    }

    [Fact]
    public void MedewerkerZaakRolDto_Maps_To_MedewerkerZaakRol()
    {
        var value = _fixture.Create<MedewerkerZaakRolDto>();
        var result = _mapper.Map<MedewerkerZaakRol>(value);

        Assert.Equal(value.Identificatie, result.Identificatie);
        Assert.Equal(value.Achternaam, result.Achternaam);
        Assert.Equal(value.Voorletters, result.Voorletters);
        Assert.Equal(value.VoorvoegselAchternaam, result.VoorvoegselAchternaam);
    }

    [Fact]
    public void VestigingZaakRolDto_Maps_To_VestigingZaakRol()
    {
        var value = _fixture.Create<VestigingZaakRolDto>();
        var result = _mapper.Map<VestigingZaakRol>(value);

        Assert.Equal(value.VestigingsNummer, result.VestigingsNummer);
        Assert.Equal(value.Handelsnaam.Length, result.Handelsnaam.Count);
        Assert.All(value.Handelsnaam, c => Assert.Contains(c, result.Handelsnaam));
    }

    [Fact]
    public void OrganisatorischeEenheidZaakRolDto_Maps_To_OrganisatorischeEenheidZaakRol()
    {
        var value = _fixture.Create<OrganisatorischeEenheidZaakRolDto>();
        var result = _mapper.Map<OrganisatorischeEenheidZaakRol>(value);

        Assert.Equal(value.Identificatie, result.Identificatie);
        Assert.Equal(value.Naam, result.Naam);
        Assert.Equal(value.IsGehuisvestIn, result.IsGehuisvestIn);
    }

    [Fact]
    public void ZaakResultaatRequestDto_Maps_To_ZaakResultaat()
    {
        var value = _fixture.Create<ZaakResultaatRequestDto>();
        var result = _mapper.Map<ZaakResultaat>(value);

        Assert.Equal(value.Toelichting, result.Toelichting);
        Assert.Equal(value.ResultaatType, result.ResultaatType);
    }

    [Fact]
    public void GetAllZaakResultatenQueryParameters_Maps_To_GetAllZaakResultatenFilter()
    {
        var value = _fixture.Create<GetAllZaakResultatenQueryParameters>();
        var result = _mapper.Map<GetAllZaakResultatenFilter>(value);

        Assert.Equal(value.ResultaatType, result.ResultaatType);
        Assert.Equal(value.Zaak, result.Zaak);
    }

    [Fact]
    public void ZaakEigenschapRequestDto_Maps_To_ZaakEigenschap()
    {
        _fixture.Customize<ZaakEigenschapRequestDto>(c => c.With(p => p.Zaak, "/zaken/9337ba82-999a-4440-aa02-2b7b0b6c33f6"));

        var value = _fixture.Create<ZaakEigenschapRequestDto>();
        var result = _mapper.Map<ZaakEigenschap>(value);

        Assert.Equal(value.Waarde, result.Waarde);
    }

    [Fact]
    public void ZaakBesluitRequestDto_Maps_To_ZaakBesluit()
    {
        var value = _fixture.Create<ZaakBesluitRequestDto>();
        var result = _mapper.Map<ZaakBesluit>(value);

        Assert.Equal(value.Besluit, result.Besluit);
    }

    [Fact]
    public void GetAllKlantContactenQueryParameters_Maps_To_GetAllKlantContactenFilter()
    {
        var value = _fixture.Create<GetAllKlantContactenQueryParameters>();
        var result = _mapper.Map<GetAllKlantContactenFilter>(value);

        Assert.Equal(value.Zaak, result.Zaak);
    }

    [Fact]
    public void KlantContactRequestDto_Maps_To_KlantContact()
    {
        _fixture.Customize<KlantContactRequestDto>(c => c.With(p => p.DatumTijd, "2020-11-05 12:59:01"));

        var value = _fixture.Create<KlantContactRequestDto>();
        var result = _mapper.Map<KlantContact>(value);

        Assert.Equal(value.Identificatie, result.Identificatie);
        Assert.Equal(value.DatumTijd, result.DatumTijd.ToString("yyyy-MM-dd HH:mm:ss"));
        Assert.Equal(value.Kanaal, result.Kanaal);
        Assert.Equal(value.Onderwerp, result.Onderwerp);
        Assert.Equal(value.Toelichting, result.Toelichting);
    }
}
