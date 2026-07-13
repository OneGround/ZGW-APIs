using System;
using Mapster;
using MapsterMapper;
using NetTopologySuite.Geometries;
using Newtonsoft.Json.Linq;
using OneGround.ZGW.Common.DataModel;
using OneGround.ZGW.Common.Web.Mapping.Mapster;
using OneGround.ZGW.Zaken.Contracts.v1;
using OneGround.ZGW.Zaken.Contracts.v1.Queries;
using OneGround.ZGW.Zaken.Contracts.v1.Requests;
using OneGround.ZGW.Zaken.Contracts.v1.Requests.ZaakObject;
using OneGround.ZGW.Zaken.Contracts.v1.Requests.ZaakRol;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.DataModel.ZaakObject;
using OneGround.ZGW.Zaken.DataModel.ZaakRol;
using OneGround.ZGW.Zaken.Web.MappingProfiles.v1;
using OneGround.ZGW.Zaken.Web.Models.v1;
using Xunit;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.MappingTests;

public class RequestToDomainProfileTests
{
    // Official RvIG test BSN value (elfproef-valid, never assigned to a real person/organization) -
    // reused here as a stand-in RSIN/Bronorganisatie/VerantwoordelijkeOrganisatie, which share the same
    // 9-digit elfproef structure.
    private const string TestRsin = "999993653";

    private readonly IMapper _mapper;

    public RequestToDomainProfileTests()
    {
        var config = new TypeAdapterConfig();
        config.RegisterNullableEnumRule();
        new RequestToDomainRegister().Register(config);
        config.Compile();
        _mapper = new Mapper(config);
    }

    [Fact]
    public void GetAllZakenQueryParameters_Maps_To_GetAllZakenFilter()
    {
        var source = new GetAllZakenQueryParameters
        {
            Identificatie = "ZK1",
            Bronorganisatie = TestRsin,
            Zaaktype = "https://example.test/zaaktypen/1",
            Archiefactiedatum = "2020-11-05",
            Archiefactiedatum__gt = "2020-11-06",
            Archiefactiedatum__lt = "2020-11-07",
            Startdatum = "2020-11-08",
            Startdatum__gt = "2020-11-09",
            Startdatum__gte = "2020-11-10",
            Startdatum__lt = "2020-11-11",
            Startdatum__lte = "2020-11-12",
            Archiefnominatie = ArchiefNominatie.vernietigen.ToString(),
            Archiefstatus = ArchiefStatus.overgedragen.ToString(),
            Archiefnominatie__in = $"{ArchiefNominatie.blijvend_bewaren}, {ArchiefNominatie.vernietigen}",
            Archiefstatus__in = $"{ArchiefStatus.nog_te_archiveren}, {ArchiefStatus.gearchiveerd}",
        };

        var result = _mapper.Map<GetAllZakenFilter>(source);

        Assert.Equal(new DateOnly(2020, 11, 5), result.Archiefactiedatum);
        Assert.Equal(new DateOnly(2020, 11, 6), result.Archiefactiedatum__gt);
        Assert.Equal(new DateOnly(2020, 11, 7), result.Archiefactiedatum__lt);
        Assert.Equal(ArchiefNominatie.vernietigen, result.Archiefnominatie);
        Assert.Equal([ArchiefNominatie.blijvend_bewaren, ArchiefNominatie.vernietigen], result.Archiefnominatie__in);
        Assert.Equal(ArchiefStatus.overgedragen, result.Archiefstatus);
        Assert.Equal([ArchiefStatus.nog_te_archiveren, ArchiefStatus.gearchiveerd], result.Archiefstatus__in);
        Assert.Equal(source.Bronorganisatie, result.Bronorganisatie);
        Assert.Equal(source.Identificatie, result.Identificatie);
        Assert.Equal(new DateOnly(2020, 11, 8), result.Startdatum);
        Assert.Equal(new DateOnly(2020, 11, 9), result.Startdatum__gt);
        Assert.Equal(new DateOnly(2020, 11, 10), result.Startdatum__gte);
        Assert.Equal(new DateOnly(2020, 11, 11), result.Startdatum__lt);
        Assert.Equal(new DateOnly(2020, 11, 12), result.Startdatum__lte);
        Assert.Equal(source.Zaaktype, result.Zaaktype);
    }

    [Fact]
    public void ZaakRequestDto_Maps_To_Zaak()
    {
        var point = new Point(23.45, 53.20);
        var source = new ZaakRequestDto
        {
            Identificatie = "ZK1",
            Bronorganisatie = TestRsin,
            Omschrijving = "omschrijving",
            Toelichting = "toelichting",
            Zaaktype = "https://example.test/zaaktypen/1/",
            Registratiedatum = "2020-11-06",
            VerantwoordelijkeOrganisatie = TestRsin,
            Startdatum = "2020-11-07",
            EinddatumGepland = "2020-11-08",
            UiterlijkeEinddatumAfdoening = "2020-11-09",
            Publicatiedatum = "2020-11-10",
            Communicatiekanaal = "communicatiekanaal",
            ProductenOfDiensten = ["https://example.test/producten/1"],
            Vertrouwelijkheidaanduiding = VertrouwelijkheidAanduiding.openbaar.ToString(),
            Betalingsindicatie = BetalingsIndicatie.geheel.ToString(),
            LaatsteBetaaldatum = "2020-11-11T12:13:14Z",
            Zaakgeometrie = point,
            Verlenging = new ZaakVerlengingDto { Duur = "P365D", Reden = "reden" },
            Opschorting = new ZaakOpschortingDto { Indicatie = true, Reden = "opschorting reden" },
            Selectielijstklasse = "selectielijstklasse",
            RelevanteAndereZaken = [new RelevanteAndereZaakDto { Url = "https://example.test/zaken/2", AardRelatie = "vervolg" }],
            Kenmerken = [new ZaakKenmerkDto { Bron = "bron", Kenmerk = "kenmerk" }],
            Archiefnominatie = ArchiefNominatie.blijvend_bewaren.ToString(),
            Archiefstatus = ArchiefStatus.nog_te_archiveren.ToString(),
            Archiefactiedatum = "2020-11-12",
        };

        var result = _mapper.Map<Zaak>(source);

        Assert.Equal(source.Identificatie, result.Identificatie);
        Assert.Equal(source.Bronorganisatie, result.Bronorganisatie);
        Assert.Equal(source.Omschrijving, result.Omschrijving);
        Assert.Equal(source.Toelichting, result.Toelichting);
        Assert.Equal("https://example.test/zaaktypen/1", result.Zaaktype);
        Assert.Equal(new DateOnly(2020, 11, 6), result.Registratiedatum);
        Assert.Equal(source.VerantwoordelijkeOrganisatie, result.VerantwoordelijkeOrganisatie);
        Assert.Equal(new DateOnly(2020, 11, 7), result.Startdatum);
        Assert.Equal(new DateOnly(2020, 11, 8), result.EinddatumGepland);
        Assert.Equal(new DateOnly(2020, 11, 9), result.UiterlijkeEinddatumAfdoening);
        Assert.Equal(new DateOnly(2020, 11, 10), result.Publicatiedatum);
        Assert.Equal(source.Communicatiekanaal, result.Communicatiekanaal);
        Assert.Equal(source.ProductenOfDiensten, result.ProductenOfDiensten);
        Assert.Equal(VertrouwelijkheidAanduiding.openbaar, result.VertrouwelijkheidAanduiding);
        Assert.Equal(BetalingsIndicatie.geheel, result.BetalingsIndicatie);
        Assert.Equal(new DateTime(2020, 11, 11, 12, 13, 14, DateTimeKind.Utc), result.LaatsteBetaaldatum);
        Assert.Equal(point, result.Zaakgeometrie);
        Assert.NotNull(result.Verlenging);
        Assert.Equal(source.Verlenging.Duur, result.Verlenging.Duur.ToString());
        Assert.Equal(source.Verlenging.Reden, result.Verlenging.Reden);
        Assert.NotNull(result.Opschorting);
        Assert.Equal(source.Opschorting.Indicatie, result.Opschorting.Indicatie);
        Assert.Equal(source.Opschorting.Reden, result.Opschorting.Reden);
        Assert.Equal(source.Selectielijstklasse, result.Selectielijstklasse);
        Assert.NotNull(result.RelevanteAndereZaken);
        Assert.Single(result.RelevanteAndereZaken);
        Assert.Equal("vervolg", result.RelevanteAndereZaken[0].AardRelatie);
        Assert.NotNull(result.Kenmerken);
        Assert.Single(result.Kenmerken);
        Assert.Equal("bron", result.Kenmerken[0].Bron);
        Assert.Equal("kenmerk", result.Kenmerken[0].Kenmerk);
        Assert.Equal(new DateOnly(2020, 11, 12), result.Archiefactiedatum);
        Assert.Equal(ArchiefNominatie.blijvend_bewaren, result.Archiefnominatie);
        Assert.Equal(ArchiefStatus.nog_te_archiveren, result.Archiefstatus);
    }

    [Fact]
    public void RelevanteAndereZaakDto_Maps_To_RelevanteAndereZaak()
    {
        var source = new RelevanteAndereZaakDto { Url = "https://example.test/zaken/2", AardRelatie = "vervolg" };

        var result = _mapper.Map<RelevanteAndereZaak>(source);

        Assert.Equal(source.AardRelatie, result.AardRelatie);
    }

    [Fact]
    public void ZaakKenmerkDto_Maps_To_ZaakKenmerk()
    {
        var source = new ZaakKenmerkDto { Bron = "bron", Kenmerk = "kenmerk" };

        var result = _mapper.Map<ZaakKenmerk>(source);

        Assert.Equal(source.Bron, result.Bron);
        Assert.Equal(source.Kenmerk, result.Kenmerk);
    }

    [Fact]
    public void GetAllZaakStatussenQueryParameters_Maps_To_GetAllZaakStatussenFilter()
    {
        var source = new GetAllZaakStatussenQueryParameters
        {
            Zaak = "https://example.test/zaken/1",
            StatusType = "https://example.test/statustypen/1",
        };

        var result = _mapper.Map<GetAllZaakStatussenFilter>(source);

        Assert.Equal(source.StatusType, result.StatusType);
        Assert.Equal(source.Zaak, result.Zaak);
    }

    [Fact]
    public void ZaakStatusRequestDto_Maps_To_ZaakStatus()
    {
        var source = new ZaakStatusRequestDto
        {
            Zaak = "https://example.test/zaken/1",
            StatusType = "https://example.test/statustypen/1",
            DatumStatusGezet = "2020-11-06T12:13:14Z",
            StatusToelichting = "toelichting",
        };

        var result = _mapper.Map<ZaakStatus>(source);

        Assert.Equal(new DateTime(2020, 11, 6, 12, 13, 14, DateTimeKind.Utc), result.DatumStatusGezet);
        Assert.Equal(source.StatusToelichting, result.StatusToelichting);
        Assert.Equal(source.StatusType, result.StatusType);
    }

    [Fact]
    public void GetAllZaakObjectenQueryParameters_Maps_To_GetAllZaakObjectenFilter()
    {
        var source = new GetAllZaakObjectenQueryParameters
        {
            Zaak = "https://example.test/zaken/1",
            Object = "https://example.test/objects/1",
            ObjectType = ObjectType.gemeentelijke_openbare_ruimte.ToString(),
        };

        var result = _mapper.Map<GetAllZaakObjectenFilter>(source);

        Assert.Equal(source.Object, result.Object);
        Assert.Equal(ObjectType.gemeentelijke_openbare_ruimte, result.ObjectType);
        Assert.Equal(source.Zaak, result.Zaak);
    }

    [Fact]
    public void ZaakObjectRequestDto_without_derived_data_Maps_To_ZaakObject_with_null_nested_objects()
    {
        var source = new ZaakObjectRequestDto
        {
            Object = "https://example.test/objects/1",
            ObjectType = ObjectType.gemeentelijke_openbare_ruimte.ToString(),
            ObjectTypeOverige = "overige",
            RelatieOmschrijving = "relatie omschrijving",
        };

        var result = _mapper.Map<ZaakObject>(source);

        Assert.Equal(source.Object, result.Object);
        Assert.Equal(ObjectType.gemeentelijke_openbare_ruimte, result.ObjectType);
        Assert.Equal(source.ObjectTypeOverige, result.ObjectTypeOverige);
        Assert.Equal(source.RelatieOmschrijving, result.RelatieOmschrijving);
        Assert.Null(result.Adres);
        Assert.Null(result.Buurt);
        Assert.Null(result.Pand);
        Assert.Null(result.KadastraleOnroerendeZaak);
        Assert.Null(result.Gemeente);
        Assert.Null(result.TerreinGebouwdObject);
        Assert.Null(result.Overige);
        Assert.Null(result.WozWaardeObject);
    }

    [Fact]
    public void ZaakObjectRequestDto_base_typed_reference_holding_AdresZaakObjectRequestDto_dispatches_to_Adres_mapping()
    {
        // The whole point of this test: `request` is declared and passed around as the BASE type
        // ZaakObjectRequestDto, but at runtime holds an AdresZaakObjectRequestDto instance (this mirrors
        // how a controller receives it after a custom JSON converter resolves the concrete subtype).
        // AutoMapper needed .IncludeAllDerived() to dispatch on the runtime type here; Mapster's
        // IMapper.Map<TDestination>(object source) does this by default, dispatching on source.GetType()
        // and using the AdresZaakObjectRequestDto->ZaakObject config registered further down. If that
        // dispatch stopped working, result.Adres below would be null (only the base config would run).
        ZaakObjectRequestDto request = new AdresZaakObjectRequestDto
        {
            Object = "https://example.test/objects/1",
            ObjectType = ObjectType.adres.ToString(),
            RelatieOmschrijving = "relatie omschrijving",
            ObjectIdentificatie = new AdresZaakObjectDto
            {
                Huisletter = "A",
                Huisnummer = 12,
                HuisnummerToevoeging = "bis",
                GorOpenbareRuimteNaam = "Teststraat",
                Identificatie = "ID1",
                WplWoonplaatsNaam = "Teststad",
                Postcode = "1234AB",
            },
        };

        var result = _mapper.Map<ZaakObject>(request);

        Assert.Equal(request.Object, result.Object);
        Assert.Equal(ObjectType.adres, result.ObjectType);
        Assert.Equal(request.RelatieOmschrijving, result.RelatieOmschrijving);
        Assert.NotNull(result.Adres);
        Assert.Equal("A", result.Adres.Huisletter);
        Assert.Equal(12, result.Adres.Huisnummer);
        Assert.Equal("bis", result.Adres.HuisnummerToevoeging);
        Assert.Equal("Teststraat", result.Adres.GorOpenbareRuimteNaam);
        Assert.Equal("ID1", result.Adres.Identificatie);
        Assert.Equal("Teststad", result.Adres.WplWoonplaatsNaam);
        Assert.Equal("1234AB", result.Adres.Postcode);
    }

    [Fact]
    public void ZaakRolRequestDto_base_typed_reference_holding_NatuurlijkPersoonZaakRolRequestDto_dispatches_to_NatuurlijkPersoon_mapping()
    {
        // Lower-priority twin of the ZaakObject dispatch test above: same base-typed-reference-holding-
        // derived-instance shape, proving the dropped .IncludeAllDerived() on ZaakRolRequestDto->ZaakRol
        // isn't needed either.
        ZaakRolRequestDto request = new NatuurlijkPersoonZaakRolRequestDto
        {
            Zaak = "https://example.test/zaken/1",
            Betrokkene = "https://example.test/betrokkenen/1",
            BetrokkeneType = BetrokkeneType.natuurlijk_persoon.ToString(),
            RolType = "https://example.test/roltypen/1",
            RolToelichting = "toelichting",
            BetrokkeneIdentificatie = new NatuurlijkPersoonZaakRolDto { InpBsn = "999993653", Geslachtsnaam = "Jansen" },
        };

        var result = _mapper.Map<ZaakRol>(request);

        Assert.Equal(request.Betrokkene, result.Betrokkene);
        Assert.Equal(BetrokkeneType.natuurlijk_persoon, result.BetrokkeneType);
        Assert.NotNull(result.NatuurlijkPersoon);
        Assert.Equal("999993653", result.NatuurlijkPersoon.InpBsnEncrypted);
        Assert.Equal("Jansen", result.NatuurlijkPersoon.Geslachtsnaam);
    }

    [Fact]
    public void AdresZaakObjectDto_Maps_To_AdresZaakObject()
    {
        var source = new AdresZaakObjectDto
        {
            GorOpenbareRuimteNaam = "Teststraat",
            Huisletter = "A",
            Huisnummer = 1,
            HuisnummerToevoeging = "bis",
            Identificatie = "ID1",
            Postcode = "1234AB",
            WplWoonplaatsNaam = "Teststad",
        };

        var result = _mapper.Map<AdresZaakObject>(source);

        Assert.Equal(source.GorOpenbareRuimteNaam, result.GorOpenbareRuimteNaam);
        Assert.Equal(source.Huisletter, result.Huisletter);
        Assert.Equal(source.Huisnummer, result.Huisnummer);
        Assert.Equal(source.HuisnummerToevoeging, result.HuisnummerToevoeging);
        Assert.Equal(source.Identificatie, result.Identificatie);
        Assert.Equal(source.Postcode, result.Postcode);
        Assert.Equal(source.WplWoonplaatsNaam, result.WplWoonplaatsNaam);
    }

    [Fact]
    public void BuurtZaakObjectDto_Maps_To_BuurtZaakObject()
    {
        var source = new BuurtZaakObjectDto
        {
            BuurtCode = "BC1",
            BuurtNaam = "Buurtnaam",
            GemGemeenteCode = "GC1",
            WykWijkCode = "WC1",
        };

        var result = _mapper.Map<BuurtZaakObject>(source);

        Assert.Equal(source.BuurtCode, result.BuurtCode);
        Assert.Equal(source.BuurtNaam, result.BuurtNaam);
        Assert.Equal(source.GemGemeenteCode, result.GemGemeenteCode);
        Assert.Equal(source.WykWijkCode, result.WykWijkCode);
    }

    [Fact]
    public void PandZaakObjectDto_Maps_To_PandZaakObject()
    {
        var source = new PandZaakObjectDto { Identificatie = "ID1" };

        var result = _mapper.Map<PandZaakObject>(source);

        Assert.Equal(source.Identificatie, result.Identificatie);
    }

    [Fact]
    public void KadastraleOnroerendeZaakObjectDto_Maps_To_KadastraleOnroerendeZaakObject()
    {
        var source = new KadastraleOnroerendeZaakObjectDto { KadastraleAanduiding = "aanduiding", KadastraleIdentificatie = "ID1" };

        var result = _mapper.Map<KadastraleOnroerendeZaakObject>(source);

        Assert.Equal(source.KadastraleAanduiding, result.KadastraleAanduiding);
        Assert.Equal(source.KadastraleIdentificatie, result.KadastraleIdentificatie);
    }

    [Fact]
    public void GemeenteZaakObjectDto_Maps_To_GemeenteZaakObject()
    {
        var source = new GemeenteZaakObjectDto { GemeenteCode = "GC1", GemeenteNaam = "Gemeentenaam" };

        var result = _mapper.Map<GemeenteZaakObject>(source);

        Assert.Equal(source.GemeenteCode, result.GemeenteCode);
        Assert.Equal(source.GemeenteNaam, result.GemeenteNaam);
    }

    [Fact]
    public void TerreinGebouwdObjectZaakObjectDto_Maps_To_TerreinGebouwdObjectZaakObject()
    {
        var source = new TerreinGebouwdObjectZaakObjectDto
        {
            Identificatie = "ID1",
            AdresAanduidingGrp = new AdresAanduidingGrpDto
            {
                AoaHuisletter = "A",
                AoaHuisnummer = 1,
                AoaHuisnummertoevoeging = "bis",
                AoaPostcode = "1234AB",
                GorOpenbareRuimteNaam = "Teststraat",
                NumIdentificatie = "NUM1",
                OaoIdentificatie = "OAO1",
                OgoLocatieAanduiding = "OGO1",
                WplWoonplaatsNaam = "Teststad",
            },
        };

        var result = _mapper.Map<TerreinGebouwdObjectZaakObject>(source);

        Assert.Equal(source.Identificatie, result.Identificatie);
        Assert.Equal(source.AdresAanduidingGrp.AoaHuisletter, result.AdresAanduidingGrp_AoaHuisletter);
        Assert.Equal(source.AdresAanduidingGrp.AoaHuisnummer, result.AdresAanduidingGrp_AoaHuisnummer);
        Assert.Equal(source.AdresAanduidingGrp.AoaHuisnummertoevoeging, result.AdresAanduidingGrp_AoaHuisnummertoevoeging);
        Assert.Equal(source.AdresAanduidingGrp.AoaPostcode, result.AdresAanduidingGrp_AoaPostcode);
        Assert.Equal(source.AdresAanduidingGrp.GorOpenbareRuimteNaam, result.AdresAanduidingGrp_GorOpenbareRuimteNaam);
        Assert.Equal(source.AdresAanduidingGrp.NumIdentificatie, result.AdresAanduidingGrp_NumIdentificatie);
        Assert.Equal(source.AdresAanduidingGrp.OaoIdentificatie, result.AdresAanduidingGrp_OaoIdentificatie);
        Assert.Equal(source.AdresAanduidingGrp.OgoLocatieAanduiding, result.AdresAanduidingGrp_OgoLocatieAanduiding);
        Assert.Equal(source.AdresAanduidingGrp.WplWoonplaatsNaam, result.AdresAanduidingGrp_WplWoonplaatsNaam);
    }

    [Fact]
    public void OverigeZaakObjectDto_Maps_To_OverigeZaakObject()
    {
        var source = new OverigeZaakObjectDto { OverigeData = JToken.Parse("""{"foo":"bar"}""") };

        var result = _mapper.Map<OverigeZaakObject>(source);

        Assert.Equal(source.OverigeData.ToString(Newtonsoft.Json.Formatting.None), result.OverigeData);
    }

    [Fact]
    public void WozWaardeZaakObjectDto_Maps_To_WozWaardeZaakObject()
    {
        var source = new WozWaardeZaakObjectDto { WaardePeildatum = "2020-01-01" };

        var result = _mapper.Map<WozWaardeZaakObject>(source);

        Assert.Equal(source.WaardePeildatum, result.WaardePeildatum);
    }

    [Fact]
    public void WozObjectDto_Maps_To_WozObject()
    {
        var source = new WozObjectDto { WozObjectNummer = "WOZ1" };

        var result = _mapper.Map<WozObject>(source);

        Assert.Equal(source.WozObjectNummer, result.WozObjectNummer);
    }

    [Fact]
    public void AanduidingWozObjectDto_Maps_To_AanduidingWozObject()
    {
        var source = new AanduidingWozObjectDto
        {
            AoaHuisletter = "A",
            AoaHuisnummer = 1,
            AoaHuisnummerToevoeging = "bis",
            AoaIdentificatie = "AOA1",
            AoaPostcode = "1234AB",
            GorOpenbareRuimteNaam = "Teststraat",
            LocatieOmschrijving = "locatie",
            WplWoonplaatsNaam = "Teststad",
        };

        var result = _mapper.Map<AanduidingWozObject>(source);

        Assert.Equal(source.AoaHuisletter, result.AoaHuisletter);
        Assert.Equal(source.AoaHuisnummer, result.AoaHuisnummer);
        Assert.Equal(source.AoaHuisnummerToevoeging, result.AoaHuisnummerToevoeging);
        Assert.Equal(source.AoaIdentificatie, result.AoaIdentificatie);
        Assert.Equal(source.AoaPostcode, result.AoaPostcode);
        Assert.Equal(source.GorOpenbareRuimteNaam, result.GorOpenbareRuimteNaam);
        Assert.Equal(source.LocatieOmschrijving, result.LocatieOmschrijving);
        Assert.Equal(source.WplWoonplaatsNaam, result.WplWoonplaatsNaam);
    }

    [Fact]
    public void OverigeZaakObjectRequestDto_MapsWith_CreateOverigeZaakObject()
    {
        var source = new OverigeZaakObjectRequestDto
        {
            ObjectIdentificatie = new OverigeZaakObjectDto { OverigeData = JToken.Parse("""{"foo":"bar","n":3}""") },
        };

        var result = _mapper.Map<OverigeZaakObject>(source);

        // The factory JSON-serializes the JToken via Newtonsoft's JsonConvert - not just ToString() on the
        // token - so this asserts the actual serialized content, not merely that OverigeData is non-null.
        Assert.Equal("{\"foo\":\"bar\",\"n\":3}", result.OverigeData);
    }

    [Fact]
    public void TerreinGebouwdObjectZaakObjectRequestDto_MapsWith_CreateTerreinGebouwdObjectZaakObject()
    {
        var source = new TerreinGebouwdObjectZaakObjectRequestDto
        {
            ObjectIdentificatie = new TerreinGebouwdObjectZaakObjectDto
            {
                Identificatie = "ID1",
                AdresAanduidingGrp = new AdresAanduidingGrpDto
                {
                    AoaHuisletter = "A",
                    AoaHuisnummer = 1,
                    AoaHuisnummertoevoeging = "bis",
                    AoaPostcode = "1234AB",
                    GorOpenbareRuimteNaam = "Teststraat",
                    NumIdentificatie = "NUM1",
                    OaoIdentificatie = "OAO1",
                    OgoLocatieAanduiding = "OGO1",
                    WplWoonplaatsNaam = "Teststad",
                },
            },
        };

        var result = _mapper.Map<TerreinGebouwdObjectZaakObject>(source);

        Assert.Equal("ID1", result.Identificatie);
        Assert.Equal("A", result.AdresAanduidingGrp_AoaHuisletter);
        Assert.Equal(1, result.AdresAanduidingGrp_AoaHuisnummer);
        Assert.Equal("bis", result.AdresAanduidingGrp_AoaHuisnummertoevoeging);
        Assert.Equal("1234AB", result.AdresAanduidingGrp_AoaPostcode);
        Assert.Equal("Teststraat", result.AdresAanduidingGrp_GorOpenbareRuimteNaam);
        Assert.Equal("NUM1", result.AdresAanduidingGrp_NumIdentificatie);
        Assert.Equal("OAO1", result.AdresAanduidingGrp_OaoIdentificatie);
        Assert.Equal("OGO1", result.AdresAanduidingGrp_OgoLocatieAanduiding);
        Assert.Equal("Teststad", result.AdresAanduidingGrp_WplWoonplaatsNaam);
    }

    [Fact]
    public void TerreinGebouwdObjectZaakObjectRequestDto_with_null_AdresAanduidingGrp_does_not_throw()
    {
        var source = new TerreinGebouwdObjectZaakObjectRequestDto
        {
            ObjectIdentificatie = new TerreinGebouwdObjectZaakObjectDto { Identificatie = "ID1", AdresAanduidingGrp = null },
        };

        var result = _mapper.Map<TerreinGebouwdObjectZaakObject>(source);

        Assert.Equal("ID1", result.Identificatie);
        Assert.Null(result.AdresAanduidingGrp_AoaHuisletter);
        Assert.Equal(0, result.AdresAanduidingGrp_AoaHuisnummer);
        Assert.Null(result.AdresAanduidingGrp_AoaHuisnummertoevoeging);
        Assert.Null(result.AdresAanduidingGrp_AoaPostcode);
        Assert.Null(result.AdresAanduidingGrp_GorOpenbareRuimteNaam);
        Assert.Null(result.AdresAanduidingGrp_NumIdentificatie);
        Assert.Null(result.AdresAanduidingGrp_OaoIdentificatie);
        Assert.Null(result.AdresAanduidingGrp_OgoLocatieAanduiding);
        Assert.Null(result.AdresAanduidingGrp_WplWoonplaatsNaam);
    }

    [Fact]
    public void GetAllZaakInformatieObjectenQueryParameters_Maps_To_GetAllZaakInformatieObjectenFilter()
    {
        var source = new GetAllZaakInformatieObjectenQueryParameters
        {
            Zaak = "https://example.test/zaken/1",
            InformatieObject = "https://example.test/informatieobjecten/1",
        };

        var result = _mapper.Map<GetAllZaakInformatieObjectenFilter>(source);

        Assert.Equal(source.InformatieObject, result.InformatieObject);
        Assert.Equal(source.Zaak, result.Zaak);
    }

    [Fact]
    public void ZaakInformatieObjectRequestDto_Maps_To_ZaakInformatieObject()
    {
        var source = new ZaakInformatieObjectRequestDto
        {
            Zaak = "https://example.test/zaken/1",
            InformatieObject = "https://example.test/informatieobjecten/1",
            Beschrijving = "beschrijving",
            Titel = "titel",
        };

        var result = _mapper.Map<ZaakInformatieObject>(source);

        Assert.Equal(source.Beschrijving, result.Beschrijving);
        Assert.Equal(source.InformatieObject, result.InformatieObject);
        Assert.Equal(source.Titel, result.Titel);
    }

    [Fact]
    public void GetAllZaakRollenQueryParameters_Maps_To_GetAllZaakRollenFilter()
    {
        var source = new GetAllZaakRollenQueryParameters
        {
            Zaak = "https://example.test/zaken/1",
            Betrokkene = "https://example.test/betrokkenen/1",
            BetrokkeneType = BetrokkeneType.niet_natuurlijk_persoon.ToString(),
            BetrokkeneIdentificatie__natuurlijkPersoon__inpBsn = "999993653",
            BetrokkeneIdentificatie__natuurlijkPersoon__anpIdentificatie = "ANP1",
            BetrokkeneIdentificatie__natuurlijkPersoon__inpA_nummer = "A1",
            BetrokkeneIdentificatie__nietNatuurlijkPersoon__innNnpId = "NNP1",
            BetrokkeneIdentificatie__nietNatuurlijkPersoon__annIdentificatie = "ANN1",
            BetrokkeneIdentificatie__vestiging__vestigingsNummer = "VN1",
            BetrokkeneIdentificatie__organisatorischeEenheid__identificatie = "OE1",
            BetrokkeneIdentificatie__medewerker__identificatie = "MW1",
            RolType = "https://example.test/roltypen/1",
            Omschrijving = "omschrijving",
            OmschrijvingGeneriek = OmschrijvingGeneriek.belanghebbende.ToString(),
        };

        var result = _mapper.Map<GetAllZaakRollenFilter>(source);

        Assert.Equal(source.Betrokkene, result.Betrokkene);
        Assert.Equal(BetrokkeneType.niet_natuurlijk_persoon, result.BetrokkeneType);
        Assert.Equal(source.BetrokkeneIdentificatie__medewerker__identificatie, result.MedewerkerIdentificatie);
        Assert.Equal(source.BetrokkeneIdentificatie__natuurlijkPersoon__anpIdentificatie, result.NatuurlijkPersoonAnpIdentificatie);
        Assert.Equal(source.BetrokkeneIdentificatie__natuurlijkPersoon__inpA_nummer, result.NatuurlijkPersoonInpANummer);
        Assert.Equal(source.BetrokkeneIdentificatie__natuurlijkPersoon__inpBsn, result.NatuurlijkPersoonInpBsn);
        Assert.Equal(source.BetrokkeneIdentificatie__nietNatuurlijkPersoon__annIdentificatie, result.NietNatuurlijkPersoonAnnIdentificatie);
        Assert.Equal(source.BetrokkeneIdentificatie__nietNatuurlijkPersoon__innNnpId, result.NietNatuurlijkPersoonInnNnpId);
        Assert.Equal(source.Omschrijving, result.Omschrijving);
        Assert.Equal(OmschrijvingGeneriek.belanghebbende, result.OmschrijvingGeneriek);
        Assert.Equal(source.BetrokkeneIdentificatie__organisatorischeEenheid__identificatie, result.OrganisatorischeEenheidIdentificatie);
        Assert.Equal(source.RolType, result.RolType);
        Assert.Equal(source.BetrokkeneIdentificatie__vestiging__vestigingsNummer, result.VestigingNummer);
        Assert.Equal(source.Zaak, result.Zaak);
    }

    [Fact]
    public void ZaakRolRequestDto_Maps_To_ZaakRol()
    {
        var source = new ZaakRolRequestDto
        {
            Zaak = "https://example.test/zaken/1",
            Betrokkene = "https://example.test/betrokkenen/1",
            BetrokkeneType = BetrokkeneType.natuurlijk_persoon.ToString(),
            RolType = "https://example.test/roltypen/1",
            RolToelichting = "roltoelichting",
            IndicatieMachtiging = IndicatieMachtiging.gemachtigde.ToString(),
        };

        var result = _mapper.Map<ZaakRol>(source);

        Assert.Equal(source.Betrokkene, result.Betrokkene);
        Assert.Equal(BetrokkeneType.natuurlijk_persoon, result.BetrokkeneType);
        Assert.Equal(source.RolType, result.RolType);
        Assert.Equal(source.RolToelichting, result.Roltoelichting);
        Assert.Equal(IndicatieMachtiging.gemachtigde, result.IndicatieMachtiging);
    }

    [Fact]
    public void VerblijfsadresDto_Maps_To_Verblijfsadres()
    {
        var source = new VerblijfsadresDto
        {
            AoaIdentificatie = "AOA1",
            WplWoonplaatsNaam = "Teststad",
            GorOpenbareRuimteNaam = "Teststraat",
            AoaPostcode = "1234AB",
            AoaHuisnummer = 1,
            AoaHuisletter = "A",
            AoaHuisnummertoevoeging = "bis",
            InpLocatiebeschrijving = "beschrijving",
        };

        var result = _mapper.Map<Verblijfsadres>(source);

        Assert.Equal(source.AoaIdentificatie, result.AoaIdentificatie);
        Assert.Equal(source.WplWoonplaatsNaam, result.WplWoonplaatsNaam);
        Assert.Equal(source.GorOpenbareRuimteNaam, result.GorOpenbareRuimteNaam);
        Assert.Equal(source.AoaPostcode, result.AoaPostcode);
        Assert.Equal(source.AoaHuisnummer, result.AoaHuisnummer);
        Assert.Equal(source.AoaHuisletter, result.AoaHuisletter);
        Assert.Equal(source.AoaHuisnummertoevoeging, result.AoaHuisnummertoevoeging);
        Assert.Equal(source.InpLocatiebeschrijving, result.InpLocatiebeschrijving);
    }

    [Fact]
    public void SubVerblijfBuitenlandDto_Maps_To_SubVerblijfBuitenland()
    {
        var source = new SubVerblijfBuitenlandDto
        {
            LndLandcode = "NL",
            LndLandnaam = "Nederland",
            SubAdresBuitenland1 = "adres1",
            SubAdresBuitenland2 = "adres2",
            SubAdresBuitenland3 = "adres3",
        };

        var result = _mapper.Map<SubVerblijfBuitenland>(source);

        Assert.Equal(source.LndLandcode, result.LndLandcode);
        Assert.Equal(source.LndLandnaam, result.LndLandnaam);
        Assert.Equal(source.SubAdresBuitenland1, result.SubAdresBuitenland1);
        Assert.Equal(source.SubAdresBuitenland2, result.SubAdresBuitenland2);
        Assert.Equal(source.SubAdresBuitenland3, result.SubAdresBuitenland3);
    }

    [Fact]
    public void NatuurlijkPersoonZaakRolDto_Maps_To_NatuurlijkPersoonZaakRol()
    {
        var source = new NatuurlijkPersoonZaakRolDto
        {
            InpBsn = "999993653",
            AnpIdentificatie = "ANP1",
            InpANummer = "A1",
            Geslachtsnaam = "Jansen",
            VoorvoegselGeslachtsnaam = "van",
            Voorletters = "J.",
            Voornamen = "Jan",
            Geslachtsaanduiding = Geslachtsaanduiding.m.ToString(),
            Geboortedatum = "2020-11-04",
        };

        var result = _mapper.Map<NatuurlijkPersoonZaakRol>(source);

        Assert.Null(result.InpBsnHash);
        Assert.Null(result.InpBsnHashKeyVersion);
        Assert.Equal(source.InpBsn, result.InpBsnEncrypted);
        Assert.Equal(source.AnpIdentificatie, result.AnpIdentificatie);
        Assert.Equal(source.InpANummer, result.InpANummer);
        Assert.Equal(source.Geslachtsnaam, result.Geslachtsnaam);
        Assert.Equal(source.VoorvoegselGeslachtsnaam, result.VoorvoegselGeslachtsnaam);
        Assert.Equal(source.Voorletters, result.Voorletters);
        Assert.Equal(source.Voornamen, result.Voornamen);
        Assert.Equal(Geslachtsaanduiding.m, result.Geslachtsaanduiding);
        Assert.Equal(new DateTime(2020, 11, 4), result.Geboortedatum);
    }

    [Fact]
    public void NietNatuurlijkPersoonZaakRolDto_Maps_To_NietNatuurlijkPersoonZaakRol()
    {
        var source = new NietNatuurlijkPersoonZaakRolDto
        {
            InnNnpId = "NNP1",
            AnnIdentificatie = "ANN1",
            StatutaireNaam = "Naam BV",
            InnRechtsvorm = InnRechtsvorm.besloten_vennootschap.ToString(),
            Bezoekadres = "Bezoekadres 1",
        };

        var result = _mapper.Map<NietNatuurlijkPersoonZaakRol>(source);

        Assert.Equal(source.InnNnpId, result.InnNnpId);
        Assert.Equal(source.AnnIdentificatie, result.AnnIdentificatie);
        Assert.Equal(source.StatutaireNaam, result.StatutaireNaam);
        Assert.Equal(InnRechtsvorm.besloten_vennootschap, result.InnRechtsvorm);
        Assert.Equal(source.Bezoekadres, result.Bezoekadres);
    }

    [Fact]
    public void MedewerkerZaakRolDto_Maps_To_MedewerkerZaakRol()
    {
        var source = new MedewerkerZaakRolDto
        {
            Identificatie = "MW1",
            Achternaam = "Achternaam",
            Voorletters = "V.",
            VoorvoegselAchternaam = "van",
        };

        var result = _mapper.Map<MedewerkerZaakRol>(source);

        Assert.Equal(source.Identificatie, result.Identificatie);
        Assert.Equal(source.Achternaam, result.Achternaam);
        Assert.Equal(source.Voorletters, result.Voorletters);
        Assert.Equal(source.VoorvoegselAchternaam, result.VoorvoegselAchternaam);
    }

    [Fact]
    public void VestigingZaakRolDto_Maps_To_VestigingZaakRol()
    {
        var source = new VestigingZaakRolDto { VestigingsNummer = "VN1", Handelsnaam = ["Naam 1", "Naam 2"] };

        var result = _mapper.Map<VestigingZaakRol>(source);

        Assert.Equal(source.VestigingsNummer, result.VestigingsNummer);
        Assert.Equal(source.Handelsnaam.Length, result.Handelsnaam.Count);
        Assert.All(source.Handelsnaam, c => Assert.Contains(c, result.Handelsnaam));
    }

    [Fact]
    public void OrganisatorischeEenheidZaakRolDto_Maps_To_OrganisatorischeEenheidZaakRol()
    {
        var source = new OrganisatorischeEenheidZaakRolDto
        {
            Identificatie = "OE1",
            Naam = "Naam",
            IsGehuisvestIn = "https://example.test/vestigingen/1",
        };

        var result = _mapper.Map<OrganisatorischeEenheidZaakRol>(source);

        Assert.Equal(source.Identificatie, result.Identificatie);
        Assert.Equal(source.Naam, result.Naam);
        Assert.Equal(source.IsGehuisvestIn, result.IsGehuisvestIn);
    }

    [Fact]
    public void ZaakResultaatRequestDto_Maps_To_ZaakResultaat()
    {
        var source = new ZaakResultaatRequestDto
        {
            Zaak = "https://example.test/zaken/1",
            ResultaatType = "https://example.test/resultaattypen/1",
            Toelichting = "toelichting",
        };

        var result = _mapper.Map<ZaakResultaat>(source);

        Assert.Equal(source.Toelichting, result.Toelichting);
        Assert.Equal(source.ResultaatType, result.ResultaatType);
    }

    [Fact]
    public void GetAllZaakResultatenQueryParameters_Maps_To_GetAllZaakResultatenFilter()
    {
        var source = new GetAllZaakResultatenQueryParameters
        {
            Zaak = "https://example.test/zaken/1",
            ResultaatType = "https://example.test/resultaattypen/1",
        };

        var result = _mapper.Map<GetAllZaakResultatenFilter>(source);

        Assert.Equal(source.ResultaatType, result.ResultaatType);
        Assert.Equal(source.Zaak, result.Zaak);
    }

    [Fact]
    public void ZaakEigenschapRequestDto_Maps_To_ZaakEigenschap()
    {
        var source = new ZaakEigenschapRequestDto
        {
            Zaak = "https://example.test/zaken/9337ba82-999a-4440-aa02-2b7b0b6c33f6",
            Eigenschap = "https://example.test/eigenschappen/1",
            Waarde = "waarde",
        };

        var result = _mapper.Map<ZaakEigenschap>(source);

        Assert.Equal(source.Waarde, result.Waarde);
        Assert.Equal(source.Eigenschap, result.Eigenschap);
        Assert.Equal(new Guid("9337ba82-999a-4440-aa02-2b7b0b6c33f6"), result.ZaakId);
    }

    [Fact]
    public void ZaakEigenschapRequestDto_with_unparseable_zaak_url_throws()
    {
        var source = new ZaakEigenschapRequestDto
        {
            Zaak = "https://example.test/zaken/not-a-guid",
            Eigenschap = "e",
            Waarde = "w",
        };

        Assert.Throws<InvalidOperationException>(() => _mapper.Map<ZaakEigenschap>(source));
    }

    [Fact]
    public void ZaakBesluitRequestDto_Maps_To_ZaakBesluit()
    {
        var source = new ZaakBesluitRequestDto { Besluit = "https://example.test/besluiten/1" };

        var result = _mapper.Map<ZaakBesluit>(source);

        Assert.Equal(source.Besluit, result.Besluit);
    }

    [Fact]
    public void GetAllKlantContactenQueryParameters_Maps_To_GetAllKlantContactenFilter()
    {
        var source = new GetAllKlantContactenQueryParameters { Zaak = "https://example.test/zaken/1" };

        var result = _mapper.Map<GetAllKlantContactenFilter>(source);

        Assert.Equal(source.Zaak, result.Zaak);
    }

    [Fact]
    public void KlantContactRequestDto_Maps_To_KlantContact()
    {
        var source = new KlantContactRequestDto
        {
            Zaak = "https://example.test/zaken/1",
            Identificatie = "KC1",
            DatumTijd = "2020-11-05 12:59:01",
            Kanaal = "kanaal",
            Onderwerp = "onderwerp",
            Toelichting = "toelichting",
        };

        var result = _mapper.Map<KlantContact>(source);

        Assert.Equal(source.Identificatie, result.Identificatie);
        Assert.Equal(new DateTime(2020, 11, 5, 12, 59, 1), result.DatumTijd);
        Assert.Equal(source.Kanaal, result.Kanaal);
        Assert.Equal(source.Onderwerp, result.Onderwerp);
        Assert.Equal(source.Toelichting, result.Toelichting);
    }
}
