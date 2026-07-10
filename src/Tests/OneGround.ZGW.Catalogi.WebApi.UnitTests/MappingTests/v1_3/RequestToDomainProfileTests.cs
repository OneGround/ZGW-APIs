using System;
using Mapster;
using MapsterMapper;
using OneGround.ZGW.Catalogi.Contracts.v1._3;
using OneGround.ZGW.Catalogi.Contracts.v1._3.Queries;
using OneGround.ZGW.Catalogi.Contracts.v1._3.Requests;
using OneGround.ZGW.Catalogi.DataModel;
using OneGround.ZGW.Catalogi.Web.MappingProfiles.v1._3;
using OneGround.ZGW.Catalogi.Web.Models.v1._3;
using OneGround.ZGW.Common.DataModel;
using OneGround.ZGW.Common.Web.Mapping.Mapster;
using Xunit;

namespace OneGround.ZGW.Catalogi.WebApi.UnitTests.MappingTests.v1_3;

public class RequestToDomainProfileTests
{
    // Official RvIG test BSN value (elfproef-valid, never assigned to a real person/organization) -
    // reused here as a stand-in RSIN, which shares the same 9-digit elfproef structure.
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
    public void ZaakTypeRequestDtoMapsToZaakType()
    {
        var source = new ZaakTypeRequestDto
        {
            Identificatie = "ZT1",
            Omschrijving = "omschrijving",
            OmschrijvingGeneriek = "omschrijving generiek",
            VertrouwelijkheidAanduiding = VertrouwelijkheidAanduiding.openbaar.ToString(),
            Doel = "doel",
            Aanleiding = "aanleiding",
            IndicatieInternOfExtern = IndicatieInternOfExtern.intern.ToString(),
            HandelingInitiator = "handeling initiator",
            Onderwerp = "onderwerp",
            HandelingBehandelaar = "handeling behandelaar",
            Doorlooptijd = "P1Y",
            Servicenorm = "P35D",
            VerlengingsTermijn = "P1M",
            BeginGeldigheid = "2020-11-12",
            EindeGeldigheid = "2020-11-11",
            BeginObject = "2020-01-02",
            EindeObject = "2020-01-03",
            VersieDatum = "2020-11-13",
            BronCatalogus = new BronCatalogusDto
            {
                Url = "https://example.test/catalogussen/1",
                Domein = "DOM01",
                Rsin = TestRsin,
            },
            BronZaaktype = new BronZaaktypeDto
            {
                Url = "https://example.test/zaaktypen/1",
                Identificatie = "BRONZT1",
                Omschrijving = "bron omschrijving",
            },
            GerelateerdeZaakTypen =
            [
                new Catalogi.Contracts.v1.GerelateerdeZaaktypeDto
                {
                    ZaakType = "https://example.test/zaaktypen/2",
                    AardRelatie = AardRelatie.vervolg.ToString(),
                    Toelichting = "relatie toelichting",
                },
            ],
        };

        var result = _mapper.Map<ZaakType>(source);

        Assert.Equal(new DateOnly(2020, 11, 12), result.BeginGeldigheid);
        Assert.Equal(new DateOnly(2020, 11, 11), result.EindeGeldigheid);
        Assert.Equal(new DateOnly(2020, 1, 2), result.BeginObject);
        Assert.Equal(new DateOnly(2020, 1, 3), result.EindeObject);
        Assert.Equal(new DateOnly(2020, 11, 13), result.VersieDatum);
        Assert.Equal(source.Doorlooptijd, result.Doorlooptijd.ToString());
        Assert.Equal(source.Servicenorm, result.Servicenorm.ToString());
        Assert.Equal(source.VerlengingsTermijn, result.VerlengingsTermijn.ToString());

        Assert.NotNull(result.BronCatalogus);
        Assert.Equal(source.BronCatalogus.Domein, result.BronCatalogus.Domein);
        Assert.NotNull(result.BronZaaktype);
        Assert.Equal(source.BronZaaktype.Identificatie, result.BronZaaktype.Identificatie);

        Assert.NotNull(result.ZaakTypeGerelateerdeZaakTypen);
        var relation = Assert.Single(result.ZaakTypeGerelateerdeZaakTypen);
        Assert.Equal("https://example.test/zaaktypen/2", relation.GerelateerdeZaakTypeIdentificatie);

        // The three AfterMapping resets: without them these would be null (the Ignore leaves the
        // field at its default, and this isolated TypeAdapterConfig has none of the production
        // EmptyCollectionIfNull destination transform registered), not an empty list.
        Assert.NotNull(result.ZaakTypeBesluitTypen);
        Assert.Empty(result.ZaakTypeBesluitTypen);
        Assert.NotNull(result.ZaakTypeDeelZaakTypen);
        Assert.Empty(result.ZaakTypeDeelZaakTypen);
        Assert.NotNull(result.ZaakTypeInformatieObjectTypen);
        Assert.Empty(result.ZaakTypeInformatieObjectTypen);
    }

    [Fact]
    public void BronCatalogusDtoMapsToBronCatalogus()
    {
        var source = new BronCatalogusDto
        {
            Url = "https://example.test/catalogussen/1",
            Domein = "DOM01",
            Rsin = TestRsin,
        };

        var result = _mapper.Map<BronCatalogus>(source);

        Assert.Equal(source.Url, result.Url);
        Assert.Equal(source.Domein, result.Domein);
        Assert.Equal(source.Rsin, result.Rsin);
    }

    [Fact]
    public void BronZaaktypeDtoMapsToBronZaaktype()
    {
        var source = new BronZaaktypeDto
        {
            Url = "https://example.test/zaaktypen/1",
            Identificatie = "BRONZT1",
            Omschrijving = "bron omschrijving",
        };

        var result = _mapper.Map<BronZaaktype>(source);

        Assert.Equal(source.Url, result.Url);
        Assert.Equal(source.Identificatie, result.Identificatie);
        Assert.Equal(source.Omschrijving, result.Omschrijving);
    }

    [Fact]
    public void GerelateerdeZaaktypeDtoMapsToZaakTypeGerelateerdeZaakType()
    {
        // Catalogi.Contracts.v1.GerelateerdeZaaktypeDto is the namespace-disambiguated source type
        // (v1._3 Contracts has no GerelateerdeZaaktypeDto of its own).
        var source = new Catalogi.Contracts.v1.GerelateerdeZaaktypeDto
        {
            ZaakType = "https://example.test/zaaktypen/2",
            AardRelatie = AardRelatie.bijdrage.ToString(),
            Toelichting = "relatie toelichting",
        };

        var result = _mapper.Map<ZaakTypeGerelateerdeZaakType>(source);

        Assert.Equal(AardRelatie.bijdrage, result.AardRelatie);
        Assert.Equal(source.Toelichting, result.Toelichting);
        Assert.Equal(source.ZaakType, result.GerelateerdeZaakTypeIdentificatie);
    }

    [Fact]
    public void GetAllStatusTypenQueryParameters_with_valid_date_maps_DatumGeldigheid()
    {
        var source = new GetAllStatusTypenQueryParameters { DatumGeldigheid = "2024-03-15" };

        var result = _mapper.Map<GetAllStatusTypenFilter>(source);

        Assert.Equal(new DateOnly(2024, 3, 15), result.DatumGeldigheid);
    }

    [Fact]
    public void GetAllStatusTypenQueryParameters_with_unparseable_date_maps_DatumGeldigheid_to_null()
    {
        var source = new GetAllStatusTypenQueryParameters { DatumGeldigheid = "not-a-date" };

        var result = _mapper.Map<GetAllStatusTypenFilter>(source);

        Assert.Null(result.DatumGeldigheid);
    }

    [Fact]
    public void StatusTypeRequestDtoMapsToStatusType()
    {
        var source = new StatusTypeRequestDto
        {
            Omschrijving = "omschrijving",
            OmschrijvingGeneriek = "omschrijving generiek",
            StatusTekst = "status tekst",
            VolgNummer = 1,
            Doorlooptijd = "P2D",
            BeginGeldigheid = "2020-11-12",
            EindeGeldigheid = "2020-11-13",
            BeginObject = "2020-01-02",
            EindeObject = "2020-01-03",
            CheckListItemStatustypes =
            [
                new CheckListItemStatusTypeDto
                {
                    ItemNaam = "item",
                    Toelichting = "toelichting",
                    Vraagstelling = "vraag",
                    Verplicht = true,
                },
            ],
        };

        var result = _mapper.Map<StatusType>(source);

        Assert.Equal(source.Doorlooptijd, result.Doorlooptijd.ToString());
        Assert.Equal(new DateOnly(2020, 11, 12), result.BeginGeldigheid);
        Assert.Equal(new DateOnly(2020, 11, 13), result.EindeGeldigheid);
        Assert.Equal(new DateOnly(2020, 1, 2), result.BeginObject);
        Assert.Equal(new DateOnly(2020, 1, 3), result.EindeObject);

        Assert.NotNull(result.CheckListItemStatustypes);
        var item = Assert.Single(result.CheckListItemStatustypes);
        Assert.Equal("item", item.ItemNaam);
        Assert.True(item.Verplicht);
    }

    [Fact]
    public void CheckListItemStatusTypeDtoMapsToCheckListItemStatusType()
    {
        var source = new CheckListItemStatusTypeDto
        {
            ItemNaam = "item",
            Toelichting = "toelichting",
            Vraagstelling = "vraag",
            Verplicht = true,
        };

        var result = _mapper.Map<CheckListItemStatusType>(source);

        Assert.Equal(source.ItemNaam, result.ItemNaam);
        Assert.Equal(source.Toelichting, result.Toelichting);
        Assert.Equal(source.Vraagstelling, result.Vraagstelling);
        Assert.Equal(source.Verplicht, result.Verplicht);
    }

    [Fact]
    public void GetAllRolTypenQueryParameters_with_valid_date_maps_DatumGeldigheid()
    {
        var source = new GetAllRolTypenQueryParameters { DatumGeldigheid = "2024-03-15" };

        var result = _mapper.Map<GetAllRolTypenFilter>(source);

        Assert.Equal(new DateOnly(2024, 3, 15), result.DatumGeldigheid);
    }

    [Fact]
    public void GetAllRolTypenQueryParameters_with_unparseable_date_maps_DatumGeldigheid_to_null()
    {
        var source = new GetAllRolTypenQueryParameters { DatumGeldigheid = "not-a-date" };

        var result = _mapper.Map<GetAllRolTypenFilter>(source);

        Assert.Null(result.DatumGeldigheid);
    }

    [Fact]
    public void RolTypeRequestDtoMapsToRolType()
    {
        var source = new RolTypeRequestDto
        {
            Omschrijving = "omschrijving",
            OmschrijvingGeneriek = OneGround.ZGW.Common.DataModel.OmschrijvingGeneriek.behandelaar.ToString(),
            BeginGeldigheid = "2020-11-12",
            EindeGeldigheid = "2020-11-13",
            BeginObject = "2020-01-02",
            EindeObject = "2020-01-03",
        };

        var result = _mapper.Map<RolType>(source);

        Assert.Equal(source.Omschrijving, result.Omschrijving);
        Assert.Equal(OneGround.ZGW.Common.DataModel.OmschrijvingGeneriek.behandelaar, result.OmschrijvingGeneriek);
        Assert.Equal(new DateOnly(2020, 11, 12), result.BeginGeldigheid);
        Assert.Equal(new DateOnly(2020, 11, 13), result.EindeGeldigheid);
        Assert.Equal(new DateOnly(2020, 1, 2), result.BeginObject);
        Assert.Equal(new DateOnly(2020, 1, 3), result.EindeObject);
    }

    [Fact]
    public void GetAllZaakTypeInformatieObjectTypenQueryParametersMapsToFilter()
    {
        var source = new GetAllZaakTypeInformatieObjectTypenQueryParameters
        {
            ZaakType = "https://example.test/zaaktypen/1",
            InformatieObjectType = "https://example.test/informatieobjecttypen/1",
            Richting = Richting.uitgaand.ToString(),
            Status = ConceptStatus.concept.ToString(),
        };

        var result = _mapper.Map<GetAllZaakTypeInformatieObjectTypenFilter>(source);

        Assert.Equal(source.ZaakType, result.ZaakType);
        Assert.Equal(source.InformatieObjectType, result.InformatieObjectType);
        Assert.Equal(Richting.uitgaand, result.Richting);
        Assert.Equal(ConceptStatus.concept, result.Status);
    }

    [Fact]
    public void ZaakTypeInformatieObjectTypeRequestDtoMapsToZaakTypeInformatieObjectType()
    {
        var source = new ZaakTypeInformatieObjectTypeRequestDto
        {
            ZaakType = "https://example.test/zaaktypen/1",
            InformatieObjectType = "https://example.test/informatieobjecttypen/1",
            VolgNummer = 3,
            Richting = Richting.inkomend.ToString(),
        };

        var result = _mapper.Map<ZaakTypeInformatieObjectType>(source);

        Assert.Equal(source.VolgNummer, result.VolgNummer);
        Assert.Equal(Richting.inkomend, result.Richting);
        Assert.Equal(source.InformatieObjectType, result.InformatieObjectTypeOmschrijving);
    }

    [Fact]
    public void CatalogusRequestDtoMapsToCatalogus()
    {
        var source = new CatalogusRequestDto
        {
            Domein = "DOM01",
            Rsin = TestRsin,
            ContactpersoonBeheerNaam = "Jan",
            ContactpersoonBeheerTelefoonnummer = "0101234567",
            ContactpersoonBeheerEmailadres = "jan@example.test",
            Naam = "Catalogus naam",
            Versie = "1",
            BegindatumVersie = "2021-05-04",
        };

        var result = _mapper.Map<Catalogus>(source);

        Assert.Equal(source.Domein, result.Domein);
        Assert.Equal(source.Rsin, result.Rsin);
        Assert.Equal(source.ContactpersoonBeheerNaam, result.ContactpersoonBeheerNaam);
        Assert.Equal(source.ContactpersoonBeheerTelefoonnummer, result.ContactpersoonBeheerTelefoonnummer);
        Assert.Equal(source.ContactpersoonBeheerEmailadres, result.ContactpersoonBeheerEmailadres);
        Assert.Equal(new DateOnly(2021, 5, 4), result.BegindatumVersie);
    }

    [Fact]
    public void CatalogusRequestDto_with_unparseable_BegindatumVersie_maps_to_null()
    {
        var source = new CatalogusRequestDto { BegindatumVersie = "not-a-date" };

        var result = _mapper.Map<Catalogus>(source);

        Assert.Null(result.BegindatumVersie);
    }

    [Fact]
    public void ResultaatTypeRequestDtoMapsToResultaatType()
    {
        var source = new ResultaatTypeRequestDto
        {
            Omschrijving = "omschrijving",
            ResultaatTypeOmschrijving = "resultaattype omschrijving",
            SelectieLijstKlasse = "selectielijstklasse",
            Toelichting = "toelichting",
            ArchiefNominatie = ArchiefNominatie.blijvend_bewaren.ToString(),
            ArchiefActieTermijn = "P1Y",
            ProcesTermijn = "P2Y",
            BeginGeldigheid = "2020-11-12",
            EindeGeldigheid = "2020-11-13",
            BeginObject = "2020-01-02",
            EindeObject = "2020-01-03",
        };

        var result = _mapper.Map<ResultaatType>(source);

        Assert.Equal(ArchiefNominatie.blijvend_bewaren, result.ArchiefNominatie);
        Assert.Equal(source.ArchiefActieTermijn, result.ArchiefActieTermijn.ToString());
        Assert.Equal(source.ProcesTermijn, result.ProcesTermijn.ToString());
        Assert.Equal(new DateOnly(2020, 11, 12), result.BeginGeldigheid);
        Assert.Equal(new DateOnly(2020, 11, 13), result.EindeGeldigheid);
        Assert.Equal(new DateOnly(2020, 1, 2), result.BeginObject);
        Assert.Equal(new DateOnly(2020, 1, 3), result.EindeObject);
    }

    [Fact]
    public void ResultaatTypeRequestDto_with_empty_ArchiefNominatie_maps_to_null()
    {
        var source = new ResultaatTypeRequestDto
        {
            ArchiefNominatie = string.Empty,
            ArchiefActieTermijn = "P1Y",
            ProcesTermijn = "P1Y",
        };

        var result = _mapper.Map<ResultaatType>(source);

        Assert.Null(result.ArchiefNominatie);
    }

    [Fact]
    public void GetAllResultaatTypenQueryParameters_with_valid_date_maps_DatumGeldigheid()
    {
        var source = new GetAllResultaatTypenQueryParameters { DatumGeldigheid = "2024-03-15" };

        var result = _mapper.Map<GetAllResultaatTypenFilter>(source);

        Assert.Equal(new DateOnly(2024, 3, 15), result.DatumGeldigheid);
    }

    [Fact]
    public void InformatieObjectTypeRequestDtoMapsToInformatieObjectType()
    {
        var source = new InformatieObjectTypeRequestDto
        {
            Omschrijving = "omschrijving",
            VertrouwelijkheidAanduiding = VertrouwelijkheidAanduiding.confidentieel.ToString(),
            BeginGeldigheid = "2020-11-12",
            EindeGeldigheid = "2020-11-13",
            BeginObject = "2020-01-02",
            EindeObject = "2020-01-03",
        };

        var result = _mapper.Map<InformatieObjectType>(source);

        Assert.Equal(source.Omschrijving, result.Omschrijving);
        Assert.Equal(new DateOnly(2020, 11, 12), result.BeginGeldigheid);
        Assert.Equal(new DateOnly(2020, 11, 13), result.EindeGeldigheid);
        Assert.Equal(new DateOnly(2020, 1, 2), result.BeginObject);
        Assert.Equal(new DateOnly(2020, 1, 3), result.EindeObject);
    }

    [Fact]
    public void InformatieObjectTypeRequestDto_with_unparseable_BeginObject_throws()
    {
        // InformatieObjectType.BeginObject/EindeObject go through ProfileHelper.DateFromStringOptional
        // (throws on a malformed non-empty string), unlike ZaakType/ZaakObjectType's BeginObject/EindeObject
        // which go through TryDateFromStringOptional (silently returns null instead). Swapping the helper
        // here would make this test pass when it should throw.
        var source = new InformatieObjectTypeRequestDto { BeginGeldigheid = "2020-11-12", BeginObject = "not-a-date" };

        // "not-a-date" is 10 characters, so it passes DateFromStringOptional's length guard and fails
        // during DateOnly.Parse itself (FormatException), rather than the InvalidOperationException
        // DateFromStringOptional raises for a wrong-length string.
        Assert.Throws<FormatException>(() => _mapper.Map<InformatieObjectType>(source));
    }

    [Fact]
    public void EigenschapRequestDtoMapsToEigenschap()
    {
        var source = new EigenschapRequestDto
        {
            Naam = "naam",
            Definitie = "definitie",
            Toelichting = "toelichting",
            BeginGeldigheid = "2020-11-12",
            EindeGeldigheid = "2020-11-13",
            BeginObject = "2020-01-02",
            EindeObject = "2020-01-03",
            Specificatie = new Catalogi.Contracts.v1.EigenschapSpecificatieDto
            {
                Groep = "groep",
                Formaat = Formaat.tekst.ToString(),
                Lengte = "10",
                Kardinaliteit = "1",
            },
        };

        var result = _mapper.Map<Eigenschap>(source);

        Assert.Equal(source.Naam, result.Naam);
        Assert.Equal(source.Definitie, result.Definitie);
        Assert.Equal(new DateOnly(2020, 11, 12), result.BeginGeldigheid);
        Assert.Equal(new DateOnly(2020, 11, 13), result.EindeGeldigheid);
        Assert.Equal(new DateOnly(2020, 1, 2), result.BeginObject);
        Assert.Equal(new DateOnly(2020, 1, 3), result.EindeObject);
        Assert.NotNull(result.Specificatie);
    }

    [Fact]
    public void EigenschapSpecificatieDtoMapsToEigenschapSpecificatie()
    {
        // Catalogi.Contracts.v1.EigenschapSpecificatieDto is the namespace-disambiguated source type
        // (v1._3 Contracts has no EigenschapSpecificatieDto of its own).
        var source = new Catalogi.Contracts.v1.EigenschapSpecificatieDto
        {
            Groep = "groep",
            Formaat = Formaat.getal.ToString(),
            Lengte = "10",
            Kardinaliteit = "1",
            Waardenverzameling = ["a", "b"],
        };

        var result = _mapper.Map<EigenschapSpecificatie>(source);

        Assert.Equal(source.Groep, result.Groep);
        Assert.Equal(Formaat.getal, result.Formaat);
        Assert.Equal(source.Lengte, result.Lengte);
        Assert.Equal(source.Kardinaliteit, result.Kardinaliteit);
        Assert.Equal(source.Waardenverzameling, result.Waardenverzameling);
    }

    [Fact]
    public void GetAllEigenschappenQueryParameters_with_valid_date_maps_DatumGeldigheid()
    {
        var source = new GetAllEigenschappenQueryParameters { DatumGeldigheid = "2024-03-15" };

        var result = _mapper.Map<GetAllEigenschappenFilter>(source);

        Assert.Equal(new DateOnly(2024, 3, 15), result.DatumGeldigheid);
    }

    [Fact]
    public void BesluitTypeRequestDtoMapsToBesluitType()
    {
        var source = new BesluitTypeRequestDto
        {
            Omschrijving = "omschrijving",
            OmschrijvingGeneriek = "omschrijving generiek",
            BesluitCategorie = "categorie",
            ReactieTermijn = "P1Y",
            PublicatieTermijn = "P2Y",
            Toelichting = "toelichting",
            BeginGeldigheid = "2020-11-12",
            EindeGeldigheid = "2020-11-13",
            BeginObject = "2020-01-02",
            EindeObject = "2020-01-03",
        };

        var result = _mapper.Map<BesluitType>(source);

        Assert.Equal(source.Omschrijving, result.Omschrijving);
        Assert.Equal(source.OmschrijvingGeneriek, result.OmschrijvingGeneriek);
        Assert.Equal(source.ReactieTermijn, result.ReactieTermijn.ToString());
        Assert.Equal(source.PublicatieTermijn, result.PublicatieTermijn.ToString());
        Assert.Equal(new DateOnly(2020, 11, 12), result.BeginGeldigheid);
        Assert.Equal(new DateOnly(2020, 11, 13), result.EindeGeldigheid);
        Assert.Equal(new DateOnly(2020, 1, 2), result.BeginObject);
        Assert.Equal(new DateOnly(2020, 1, 3), result.EindeObject);

        // The two AfterMapping resets: without them these would be null (see the analogous
        // ZaakType comment above for why null, not empty, is what a deleted AfterMapping produces here).
        Assert.NotNull(result.BesluitTypeZaakTypen);
        Assert.Empty(result.BesluitTypeZaakTypen);
        Assert.NotNull(result.BesluitTypeInformatieObjectTypen);
        Assert.Empty(result.BesluitTypeInformatieObjectTypen);
    }

    [Fact]
    public void BesluitTypeRequestDto_with_unparseable_BeginObject_throws()
    {
        // BesluitType.BeginObject/EindeObject go through ProfileHelper.DateFromStringOptional (throws on
        // a malformed non-empty string), the same helper as InformatieObjectType but different from
        // ZaakType/ZaakObjectType's TryDateFromStringOptional (silently null) for the same-named fields.
        var source = new BesluitTypeRequestDto
        {
            BeginGeldigheid = "2020-11-12",
            ReactieTermijn = "P1Y",
            PublicatieTermijn = "P1Y",
            BeginObject = "not-a-date",
        };

        // Same reasoning as the InformatieObjectType case above: a 10-character malformed string fails
        // in DateOnly.Parse (FormatException) rather than in DateFromStringOptional's length guard.
        Assert.Throws<FormatException>(() => _mapper.Map<BesluitType>(source));
    }

    [Fact]
    public void GetAllBesluitTypenQueryParameters_with_valid_date_maps_DatumGeldigheid()
    {
        var source = new GetAllBesluitTypenQueryParameters { DatumGeldigheid = "2024-03-15" };

        var result = _mapper.Map<GetAllBesluitTypenFilter>(source);

        Assert.Equal(new DateOnly(2024, 3, 15), result.DatumGeldigheid);
    }

    [Fact]
    public void GetAllZaakObjectTypenQueryParametersMapsToFilter()
    {
        var source = new GetAllZaakObjectTypenQueryParameters
        {
            AnderObjectType = "true",
            DatumBeginGeldigheid = "2020-11-12",
            DatumEindeGeldigheid = "2020-11-13",
            DatumGeldigheid = "2020-11-14",
            ObjectType = "objecttype",
            RelatieOmschrijving = "relatie omschrijving",
            ZaakType = "https://example.test/zaaktypen/1",
        };

        var result = _mapper.Map<GetAllZaakObjectTypenFilter>(source);

        Assert.True(result.AnderObjectType);
        Assert.Equal(new DateOnly(2020, 11, 12), result.DatumBeginGeldigheid);
        Assert.Equal(new DateOnly(2020, 11, 13), result.DatumEindeGeldigheid);
        Assert.Equal(new DateOnly(2020, 11, 14), result.DatumGeldigheid);
    }

    [Fact]
    public void GetAllZaakObjectTypenQueryParameters_with_null_AnderObjectType_maps_to_null()
    {
        var source = new GetAllZaakObjectTypenQueryParameters { AnderObjectType = null };

        var result = _mapper.Map<GetAllZaakObjectTypenFilter>(source);

        Assert.Null(result.AnderObjectType);
    }

    [Fact]
    public void ZaakObjectTypeRequestDtoMapsToZaakObjectType()
    {
        var source = new ZaakObjectTypeRequestDto
        {
            AnderObjectType = true,
            ObjectType = "objecttype",
            RelatieOmschrijving = "relatie omschrijving",
            BeginGeldigheid = "2020-11-12",
            EindeGeldigheid = "2020-11-13",
            BeginObject = "2020-01-02",
            EindeObject = "2020-01-03",
        };

        var result = _mapper.Map<ZaakObjectType>(source);

        Assert.Equal(source.ObjectType, result.ObjectType);
        Assert.Equal(source.RelatieOmschrijving, result.RelatieOmschrijving);
        Assert.Equal(new DateOnly(2020, 11, 12), result.BeginGeldigheid);
        Assert.Equal(new DateOnly(2020, 11, 13), result.EindeGeldigheid);
        Assert.Equal(new DateOnly(2020, 1, 2), result.BeginObject);
        Assert.Equal(new DateOnly(2020, 1, 3), result.EindeObject);
    }

    [Fact]
    public void ZaakObjectTypeRequestDto_with_unparseable_BeginObject_maps_to_null()
    {
        // ZaakObjectType.BeginObject/EindeObject go through TryDateFromStringOptional (silently null on
        // a malformed string), unlike BeginGeldigheid/EindeGeldigheid on the same DTO which go through
        // DateFromStringOptional (throws) — and unlike InformatieObjectType/BesluitType's BeginObject
        // which also throws. Mixing these up in either direction would flip this test's outcome.
        var source = new ZaakObjectTypeRequestDto { BeginGeldigheid = "2020-11-12", BeginObject = "not-a-date" };

        var result = _mapper.Map<ZaakObjectType>(source);

        Assert.Null(result.BeginObject);
    }
}
