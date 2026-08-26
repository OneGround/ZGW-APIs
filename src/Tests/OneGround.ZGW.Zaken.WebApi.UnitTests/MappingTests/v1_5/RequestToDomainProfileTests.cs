using System;
using MapsterMapper;
using Newtonsoft.Json.Linq;
using OneGround.ZGW.Common.DataModel;
using OneGround.ZGW.Zaken.Contracts.v1;
using OneGround.ZGW.Zaken.Contracts.v1._5;
using OneGround.ZGW.Zaken.Contracts.v1._5.Queries;
using OneGround.ZGW.Zaken.Contracts.v1._5.Requests;
using OneGround.ZGW.Zaken.Contracts.v1._5.Requests.ZaakObject;
using OneGround.ZGW.Zaken.Contracts.v1._5.Requests.ZaakRol;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.DataModel.ZaakObject;
using OneGround.ZGW.Zaken.DataModel.ZaakRol;
using OneGround.ZGW.Zaken.Web.Models.v1._5;
using Xunit;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.MappingTests.v1_5;

public class RequestToDomainProfileTests : IDisposable
{
    // Official RvIG test BSN value (elfproef-valid, never assigned to a real person/organization) - reused
    // here as a stand-in RSIN/Bronorganisatie, which shares the same 9-digit elfproef structure.
    private const string TestRsin = "999993653";

    // Official KvK test-environment number for a fictitious company (Eenmanszaak).
    private const string TestKvkNummer = "69599084";

    private readonly ZrcMapperTestHost _host = new();
    private readonly IMapper _mapper;

    public RequestToDomainProfileTests()
    {
        _mapper = _host.Mapper;
    }

    public void Dispose() => _host.Dispose();

    [Fact]
    public void GetAllZakenQueryParameters_Maps_To_GetAllZakenFilter()
    {
        var source = new GetAllZakenQueryParameters
        {
            Identificatie = "ZK1",
            Bronorganisatie = TestRsin,
            Zaaktype = "https://example.test/zaaktypen/1",
            Archiefactiedatum = "2020-11-05",
            Startdatum = "2020-11-08",
            Archiefnominatie__in = $"{ArchiefNominatie.blijvend_bewaren}, {ArchiefNominatie.vernietigen}",
            Archiefstatus__in = $"{ArchiefStatus.nog_te_archiveren}, {ArchiefStatus.gearchiveerd}",
            Rol__betrokkeneType = BetrokkeneType.natuurlijk_persoon.ToString(),
            MaximaleVertrouwelijkheidaanduiding = VertrouwelijkheidAanduiding.openbaar.ToString(),
        };

        var result = _mapper.Map<GetAllZakenFilter>(source);

        Assert.Equal(source.Identificatie, result.Identificatie);
        Assert.Equal(source.Bronorganisatie, result.Bronorganisatie);
        Assert.Equal(source.Zaaktype, result.Zaaktype);
        Assert.Equal(new DateOnly(2020, 11, 5), result.Archiefactiedatum);
        Assert.Equal(new DateOnly(2020, 11, 8), result.Startdatum);
        Assert.Equal([ArchiefNominatie.blijvend_bewaren, ArchiefNominatie.vernietigen], result.Archiefnominatie__in);
        Assert.Equal([ArchiefStatus.nog_te_archiveren, ArchiefStatus.gearchiveerd], result.Archiefstatus__in);
        Assert.Empty(result.Uuid__in);
        Assert.Empty(result.Zaaktype__in);
        Assert.Equal(BetrokkeneType.natuurlijk_persoon, result.Rol__betrokkeneType);
        Assert.Equal(VertrouwelijkheidAanduiding.openbaar, result.MaximaleVertrouwelijkheidaanduiding);
    }

    [Fact]
    public void ZaakSearchRequestDto_with_null_arrays_Maps_To_GetAllZakenFilter_with_empty_arrays()
    {
        // An all-five-null source is the discriminating input for the five .AfterMapping ??= Array.Empty<T>()
        // folds on ZaakSearchRequestDto -> GetAllZakenFilter: a populated source exercises none of them. The
        // sibling fact below covers the populated direction.
        //
        // Under the shared configuration those five folds turn out to be redundant, not load-bearing: the
        // global EmptyCollectionIfNull destination transform already substitutes an empty collection for each
        // of these IList<T> members. Verified by deliberate breakage - disabling all five .AfterMapping calls
        // in RequestToDomainRegister leaves every assertion below passing. The folds are kept anyway (they are
        // what makes the pair correct under a configuration without that transform), so this fact pins the
        // OBSERVABLE contract - empty, never null - rather than which of the two mechanisms produced it.
        var source = new ZaakSearchRequestDto
        {
            Archiefnominatie__in = null,
            Archiefstatus__in = null,
            Bronorganisatie__in = null,
            Uuid__in = null,
            Zaaktype__in = null,
        };

        var result = _mapper.Map<GetAllZakenFilter>(source);

        Assert.NotNull(result.Archiefnominatie__in);
        Assert.Empty(result.Archiefnominatie__in);
        Assert.NotNull(result.Uuid__in);
        Assert.Empty(result.Uuid__in);
        Assert.NotNull(result.Archiefstatus__in);
        Assert.Empty(result.Archiefstatus__in);
        Assert.NotNull(result.Bronorganisatie__in);
        Assert.Empty(result.Bronorganisatie__in);
        Assert.NotNull(result.Zaaktype__in);
        Assert.Empty(result.Zaaktype__in);
    }

    [Fact]
    public void ZaakSearchRequestDto_with_populated_arrays_Maps_To_GetAllZakenFilter()
    {
        var source = new ZaakSearchRequestDto
        {
            Archiefnominatie__in = [ArchiefNominatie.vernietigen.ToString()],
            Archiefstatus__in = [ArchiefStatus.overgedragen.ToString()],
            Bronorganisatie__in = [TestRsin],
            Uuid__in = ["9337ba82-999a-4440-aa02-2b7b0b6c33f6"],
            Zaaktype__in = ["https://example.test/zaaktypen/1"],
        };

        var result = _mapper.Map<GetAllZakenFilter>(source);

        Assert.Equal([ArchiefNominatie.vernietigen], result.Archiefnominatie__in);
        Assert.Equal([ArchiefStatus.overgedragen], result.Archiefstatus__in);
        Assert.Equal([TestRsin], result.Bronorganisatie__in);
        Assert.Equal([new Guid("9337ba82-999a-4440-aa02-2b7b0b6c33f6")], result.Uuid__in);
        Assert.Equal(["https://example.test/zaaktypen/1"], result.Zaaktype__in);
    }

    [Fact]
    public void ZaakProcessobjectDto_Maps_To_ZaakProcessobject()
    {
        var source = new ZaakProcessobjectDto
        {
            Datumkenmerk = "datumkenmerk",
            Identificatie = "ID1",
            Objecttype = "objecttype",
            Registratie = "registratie",
        };

        var result = _mapper.Map<ZaakProcessobject>(source);

        Assert.Equal(source.Datumkenmerk, result.Datumkenmerk);
        Assert.Equal(source.Identificatie, result.Identificatie);
        Assert.Equal(source.Objecttype, result.Objecttype);
        Assert.Equal(source.Registratie, result.Registratie);
    }

    [Fact]
    public void ZaakRequestDto_Maps_To_Zaak()
    {
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
            Vertrouwelijkheidaanduiding = VertrouwelijkheidAanduiding.openbaar.ToString(),
            Betalingsindicatie = BetalingsIndicatie.geheel.ToString(),
            LaatsteBetaaldatum = "2020-11-11T12:13:14Z",
            Archiefnominatie = ArchiefNominatie.blijvend_bewaren.ToString(),
            Archiefstatus = ArchiefStatus.nog_te_archiveren.ToString(),
            Archiefactiedatum = "2020-11-12",
            OpdrachtgevendeOrganisatie = TestRsin,
            Processobjectaard = "processobjectaard",
            StartdatumBewaartermijn = "2020-11-13",
            Processobject = new ZaakProcessobjectDto
            {
                Datumkenmerk = "datumkenmerk",
                Identificatie = "ID1",
                Objecttype = "objecttype",
                Registratie = "registratie",
            },
        };

        var result = _mapper.Map<Zaak>(source);

        Assert.Equal(source.Identificatie, result.Identificatie);
        Assert.Equal(source.Bronorganisatie, result.Bronorganisatie);
        Assert.Equal(source.Omschrijving, result.Omschrijving);
        Assert.Equal(source.Toelichting, result.Toelichting);
        Assert.Equal("https://example.test/zaaktypen/1", result.Zaaktype);
        Assert.Equal(new DateOnly(2020, 11, 6), result.Registratiedatum);
        Assert.Equal(new DateOnly(2020, 11, 7), result.Startdatum);
        Assert.Equal(new DateOnly(2020, 11, 8), result.EinddatumGepland);
        Assert.Equal(new DateOnly(2020, 11, 9), result.UiterlijkeEinddatumAfdoening);
        Assert.Equal(new DateOnly(2020, 11, 10), result.Publicatiedatum);
        Assert.Equal(VertrouwelijkheidAanduiding.openbaar, result.VertrouwelijkheidAanduiding);
        Assert.Equal(BetalingsIndicatie.geheel, result.BetalingsIndicatie);
        Assert.Equal(new DateTime(2020, 11, 11, 12, 13, 14, DateTimeKind.Utc), result.LaatsteBetaaldatum);
        Assert.Equal(ArchiefNominatie.blijvend_bewaren, result.Archiefnominatie);
        Assert.Equal(ArchiefStatus.nog_te_archiveren, result.Archiefstatus);
        Assert.Equal(new DateOnly(2020, 11, 12), result.Archiefactiedatum);
        Assert.Equal(source.OpdrachtgevendeOrganisatie, result.OpdrachtgevendeOrganisatie);
        Assert.Equal(source.Processobjectaard, result.Processobjectaard);
        Assert.Equal(new DateOnly(2020, 11, 13), result.StartdatumBewaartermijn);
        Assert.NotNull(result.Processobject);
        Assert.Equal("ID1", result.Processobject.Identificatie);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void MapVertrouwelijkheidAanduiding_with_blank_input_Maps_To_nullvalue(string vertrouwelijkheidaanduiding)
    {
        // Discriminates the null-guard in RequestToDomainRegister.MapVertrouwelijkheidAanduiding: a bare
        // Enum.Parse<VertrouwelijkheidAanduiding>(vertrouwelijkheidaanduiding) with no guard throws on
        // null/empty/whitespace input instead of returning nullvalue. Verified by deliberate breakage:
        // temporarily replacing the helper body with `return Enum.Parse<VertrouwelijkheidAanduiding>(x);`
        // makes this test fail with an ArgumentException/ArgumentNullException instead of asserting nullvalue.
        var source = new ZaakRequestDto
        {
            Zaaktype = "https://example.test/zaaktypen/1",
            Startdatum = "2020-11-07",
            Vertrouwelijkheidaanduiding = vertrouwelijkheidaanduiding,
        };

        var result = _mapper.Map<Zaak>(source);

        Assert.Equal(VertrouwelijkheidAanduiding.nullvalue, result.VertrouwelijkheidAanduiding);
    }

    [Fact]
    public void MapVertrouwelijkheidAanduiding_with_valid_input_Maps_To_parsed_enum()
    {
        var source = new ZaakRequestDto
        {
            Zaaktype = "https://example.test/zaaktypen/1",
            Startdatum = "2020-11-07",
            Vertrouwelijkheidaanduiding = VertrouwelijkheidAanduiding.zeer_geheim.ToString(),
        };

        var result = _mapper.Map<Zaak>(source);

        Assert.Equal(VertrouwelijkheidAanduiding.zeer_geheim, result.VertrouwelijkheidAanduiding);
    }

    [Fact]
    public void GetAllZaakStatussenQueryParameters_Maps_To_GetAllZaakStatussenFilter()
    {
        var source = new GetAllZaakStatussenQueryParameters
        {
            Zaak = "https://example.test/zaken/1",
            StatusType = "https://example.test/statustypen/1",
            IndicatieLaatstGezetteStatus = "true",
        };

        var result = _mapper.Map<GetAllZaakStatussenFilter>(source);

        Assert.Equal(source.Zaak, result.Zaak);
        Assert.Equal(source.StatusType, result.StatusType);
        Assert.True(result.IndicatieLaatstGezetteStatus);
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
            GezetDoor = "https://example.test/rollen/1",
        };

        var result = _mapper.Map<ZaakStatus>(source);

        Assert.Equal(new DateTime(2020, 11, 6, 12, 13, 14, DateTimeKind.Utc), result.DatumStatusGezet);
        Assert.Equal(source.StatusToelichting, result.StatusToelichting);
        Assert.Equal(source.StatusType, result.StatusType);
        // New in v1.5: GezetDoor is settable directly from the request (v1.0 ignores it as audit-derived) -
        // discriminates that this register deliberately does NOT ignore it, unlike the v1 sibling.
        Assert.Equal(source.GezetDoor, result.GezetDoor);
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
        Assert.Null(result.Overige);
    }

    [Fact]
    public void ZaakObjectRequestDto_base_typed_reference_holding_AdresZaakObjectRequestDto_dispatches_to_Adres_mapping()
    {
        // The whole point of this test: `request` is declared and passed around as the BASE type
        // ZaakObjectRequestDto, but at runtime holds an AdresZaakObjectRequestDto instance (this mirrors how a
        // controller receives it after a custom JSON converter resolves the concrete subtype).
        // MapsterMapper.IMapper.Map<TDestination>(object source) dispatches on source.GetType() and uses the
        // AdresZaakObjectRequestDto->ZaakObject config registered further down in RequestToDomainRegister.cs.
        // If the base ZaakObjectRequestDto->ZaakObject config had an explicit Ignore/Map rule for Adres, that
        // rule would silently win over this derived rule for every source type in the hierarchy and
        // result.Adres would be null.
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
        // Same shape as the ZaakObject dispatch test above, for the ZaakRolRequestDto->ZaakRol hierarchy.
        // v1.5 is the version actually served in production going forward, so this gets the same priority
        // as the ZaakObject dispatch test.
        ZaakRolRequestDto request = new NatuurlijkPersoonZaakRolRequestDto
        {
            Zaak = "https://example.test/zaken/1",
            Betrokkene = "https://example.test/betrokkenen/1",
            BetrokkeneType = BetrokkeneType.natuurlijk_persoon.ToString(),
            RolType = "https://example.test/roltypen/1",
            RolToelichting = "toelichting",
            AfwijkendeNaamBetrokkene = "afwijkende naam",
            BetrokkeneIdentificatie = new NatuurlijkPersoonZaakRolDto { InpBsn = TestRsin, Geslachtsnaam = "Jansen" },
        };

        var result = _mapper.Map<ZaakRol>(request);

        Assert.Equal(request.Betrokkene, result.Betrokkene);
        Assert.Equal(BetrokkeneType.natuurlijk_persoon, result.BetrokkeneType);
        Assert.Equal(request.AfwijkendeNaamBetrokkene, result.AfwijkendeNaamBetrokkene);
        Assert.NotNull(result.NatuurlijkPersoon);
        Assert.Equal(TestRsin, result.NatuurlijkPersoon.InpBsnEncrypted);
        Assert.Equal("Jansen", result.NatuurlijkPersoon.Geslachtsnaam);
    }

    [Fact]
    public void OverigeZaakObjectRequestDto_MapsWith_CreateOverigeZaakObject()
    {
        // This version's factory calls source.ObjectIdentificatie.OverigeData.ToString(Formatting.None)
        // directly on the JToken - NOT JsonConvert.SerializeObject(...) like the v1 sibling's factory. Asserts
        // the actual serialized content (not just non-null) so a regression back to the sibling's behavior (or
        // any other change) would be caught, even though for this simple token the two approaches happen to
        // produce identical output.
        var source = new OverigeZaakObjectRequestDto
        {
            ObjectIdentificatie = new OverigeZaakObjectDto { OverigeData = JToken.Parse("""{"foo":"bar","n":3}""") },
        };

        var result = _mapper.Map<OverigeZaakObject>(source);

        Assert.Equal("{\"foo\":\"bar\",\"n\":3}", result.OverigeData);
    }

    [Fact]
    public void AdresZaakObjectRequestDto_MapsWith_CreateAdresZaakObject()
    {
        var source = new AdresZaakObjectRequestDto
        {
            ObjectIdentificatie = new AdresZaakObjectDto
            {
                Huisletter = "A",
                Huisnummer = 1,
                HuisnummerToevoeging = "bis",
                GorOpenbareRuimteNaam = "Teststraat",
                Identificatie = "ID1",
                WplWoonplaatsNaam = "Teststad",
                Postcode = "1234AB",
            },
        };

        var result = _mapper.Map<AdresZaakObject>(source);

        Assert.Equal("A", result.Huisletter);
        Assert.Equal(1, result.Huisnummer);
        Assert.Equal("bis", result.HuisnummerToevoeging);
        Assert.Equal("Teststraat", result.GorOpenbareRuimteNaam);
        Assert.Equal("ID1", result.Identificatie);
        Assert.Equal("Teststad", result.WplWoonplaatsNaam);
        Assert.Equal("1234AB", result.Postcode);
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
    }

    [Fact]
    public void ContactpersoonRolDto_Maps_To_ContactpersoonRol()
    {
        var source = new ContactpersoonRolDto
        {
            EmailAdres = "test@example.test",
            Functie = "functie",
            Telefoonnummer = "0612345678",
            Naam = "naam",
        };

        var result = _mapper.Map<ContactpersoonRol>(source);

        Assert.Equal(source.EmailAdres, result.EmailAdres);
        Assert.Equal(source.Functie, result.Functie);
        Assert.Equal(source.Telefoonnummer, result.Telefoonnummer);
        Assert.Equal(source.Naam, result.Naam);
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
            ContactpersoonRol = new ContactpersoonRolDto { Naam = "naam" },
        };

        var result = _mapper.Map<ZaakRol>(source);

        Assert.Equal(source.Betrokkene, result.Betrokkene);
        Assert.Equal(BetrokkeneType.natuurlijk_persoon, result.BetrokkeneType);
        Assert.Equal(source.RolType, result.RolType);
        Assert.Equal(source.RolToelichting, result.Roltoelichting);
        Assert.Equal(IndicatieMachtiging.gemachtigde, result.IndicatieMachtiging);
        Assert.NotNull(result.ContactpersoonRol);
        Assert.Equal("naam", result.ContactpersoonRol.Naam);
    }

    [Fact]
    public void NietNatuurlijkPersoonZaakRolRequestDto_Maps_To_ZaakRol()
    {
        ZaakRolRequestDto request = new NietNatuurlijkPersoonZaakRolRequestDto
        {
            Zaak = "https://example.test/zaken/1",
            Betrokkene = "https://example.test/betrokkenen/1",
            BetrokkeneType = BetrokkeneType.niet_natuurlijk_persoon.ToString(),
            RolType = "https://example.test/roltypen/1",
            BetrokkeneIdentificatie = new NietNatuurlijkPersoonZaakRolDto { InnNnpId = "NNP1", StatutaireNaam = "Naam BV" },
        };

        var result = _mapper.Map<ZaakRol>(request);

        Assert.NotNull(result.NietNatuurlijkPersoon);
        Assert.Equal("NNP1", result.NietNatuurlijkPersoon.InnNnpId);
        Assert.Equal("Naam BV", result.NietNatuurlijkPersoon.StatutaireNaam);
    }

    [Fact]
    public void VestigingZaakRolRequestDto_Maps_To_ZaakRol_including_new_KvkNummer_field()
    {
        // Discriminates that KvKNummer (v1.5's new field) round-trips to the domain's KvkNummer despite the
        // casing difference, via NameMatchingStrategy.IgnoreCase (set globally by AddZgwMapster through the
        // shared ZrcMapperTestHost, not by this class) - no register declares an explicit rule for this field.
        ZaakRolRequestDto request = new VestigingZaakRolRequestDto
        {
            Zaak = "https://example.test/zaken/1",
            Betrokkene = "https://example.test/betrokkenen/1",
            BetrokkeneType = BetrokkeneType.vestiging.ToString(),
            RolType = "https://example.test/roltypen/1",
            BetrokkeneIdentificatie = new OneGround.ZGW.Zaken.Contracts.v1._5.VestigingZaakRolDto
            {
                VestigingsNummer = "VN1",
                KvKNummer = TestKvkNummer,
            },
        };

        var result = _mapper.Map<ZaakRol>(request);

        Assert.NotNull(result.Vestiging);
        Assert.Equal("VN1", result.Vestiging.VestigingsNummer);
        Assert.Equal(TestKvkNummer, result.Vestiging.KvkNummer);
    }

    [Fact]
    public void OrganisatorischeEenheidZaakRolRequestDto_Maps_To_ZaakRol()
    {
        ZaakRolRequestDto request = new OrganisatorischeEenheidZaakRolRequestDto
        {
            Zaak = "https://example.test/zaken/1",
            Betrokkene = "https://example.test/betrokkenen/1",
            BetrokkeneType = BetrokkeneType.organisatorische_eenheid.ToString(),
            RolType = "https://example.test/roltypen/1",
            BetrokkeneIdentificatie = new OrganisatorischeEenheidZaakRolDto { Identificatie = "OE1", Naam = "Naam" },
        };

        var result = _mapper.Map<ZaakRol>(request);

        Assert.NotNull(result.OrganisatorischeEenheid);
        Assert.Equal("OE1", result.OrganisatorischeEenheid.Identificatie);
    }

    [Fact]
    public void MedewerkerZaakRolRequestDto_Maps_To_ZaakRol()
    {
        ZaakRolRequestDto request = new MedewerkerZaakRolRequestDto
        {
            Zaak = "https://example.test/zaken/1",
            Betrokkene = "https://example.test/betrokkenen/1",
            BetrokkeneType = BetrokkeneType.medewerker.ToString(),
            RolType = "https://example.test/roltypen/1",
            BetrokkeneIdentificatie = new MedewerkerZaakRolDto { Identificatie = "MW1" },
        };

        var result = _mapper.Map<ZaakRol>(request);

        Assert.NotNull(result.Medewerker);
        Assert.Equal("MW1", result.Medewerker.Identificatie);
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
            VernietigingsDatum = "2020-11-06T12:13:14Z",
        };

        var result = _mapper.Map<ZaakInformatieObject>(source);

        Assert.Equal(source.Beschrijving, result.Beschrijving);
        Assert.Equal(source.InformatieObject, result.InformatieObject);
        Assert.Equal(source.Titel, result.Titel);
        Assert.Equal(new DateTime(2020, 11, 6, 12, 13, 14, DateTimeKind.Utc), result.VernietigingsDatum);
    }

    [Fact]
    public void GetAllZaakVerzoekenQueryParameters_Maps_To_GetAllZaakVerzoekenFilter()
    {
        var source = new GetAllZaakVerzoekenQueryParameters { Zaak = "https://example.test/zaken/1", Verzoek = "https://example.test/verzoeken/1" };

        var result = _mapper.Map<GetAllZaakVerzoekenFilter>(source);

        Assert.Equal(source.Zaak, result.Zaak);
        Assert.Equal(source.Verzoek, result.Verzoek);
    }

    [Fact]
    public void ZaakVerzoekRequestDto_Maps_To_ZaakVerzoek()
    {
        var source = new ZaakVerzoekRequestDto { Zaak = "https://example.test/zaken/1", Verzoek = "https://example.test/verzoeken/1" };

        var result = _mapper.Map<ZaakVerzoek>(source);

        Assert.Equal(source.Verzoek, result.Verzoek);
    }

    [Fact]
    public void GetAllZaakContactmomentenQueryParameters_Maps_To_GetAllZaakContactmomentenFilter()
    {
        var source = new GetAllZaakContactmomentenQueryParameters
        {
            Zaak = "https://example.test/zaken/1",
            Contactmoment = "https://example.test/contactmomenten/1",
        };

        var result = _mapper.Map<GetAllZaakContactmomentenFilter>(source);

        Assert.Equal(source.Zaak, result.Zaak);
        Assert.Equal(source.Contactmoment, result.Contactmoment);
    }

    [Fact]
    public void ZaakContactmomentRequestDto_Maps_To_ZaakContactmoment()
    {
        var source = new ZaakContactmomentRequestDto
        {
            Zaak = "https://example.test/zaken/1",
            Contactmoment = "https://example.test/contactmomenten/1",
        };

        var result = _mapper.Map<ZaakContactmoment>(source);

        Assert.Equal(source.Contactmoment, result.Contactmoment);
    }
}
