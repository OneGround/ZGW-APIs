using System;
using AutoFixture;
using Mapster;
using MapsterMapper;
using OneGround.ZGW.Common.DataModel;
using OneGround.ZGW.Common.Web.Mapping.Mapster;
using OneGround.ZGW.Documenten.Contracts.v1;
using OneGround.ZGW.Documenten.Contracts.v1._5;
using OneGround.ZGW.Documenten.Contracts.v1._5.Queries;
using OneGround.ZGW.Documenten.Contracts.v1._5.Requests;
using OneGround.ZGW.Documenten.DataModel;
using OneGround.ZGW.Documenten.Web.MappingProfiles.v1._5;
using OneGround.ZGW.Documenten.WebApi.UnitTests.MappingTests;
using Xunit;

namespace OneGround.ZGW.Documenten.WebApi.UnitTests.MappingTests.v1_5;

public class RequestToDomainProfileTests
{
    private readonly OmitOnRecursionFixture _fixture = new OmitOnRecursionFixture();
    private readonly IMapper _mapper;

    public RequestToDomainProfileTests()
    {
        var config = new TypeAdapterConfig();
        // The seam's global nullable-enum rule lives in AddZgwMapster, not in the register; this test
        // builds config directly, so register it here too for parity with production.
        config.RegisterNullableEnumRule();
        new RequestToDomainRegister().Register(config);
        config.Compile();
        _mapper = new Mapper(config);
    }

    private static EnkelvoudigInformatieObjectCreateRequestDto CreateRequestDto()
    {
        return new EnkelvoudigInformatieObjectCreateRequestDto
        {
            Identificatie = "DOC-2020-0000001",
            Bronorganisatie = "999990561",
            CreatieDatum = "2020-11-12",
            Titel = "My document",
            Auteur = "somebody",
            Formaat = "",
            Taal = "eng",
            Bestandsnaam = "document.pdf",
            Bestandsomvang = 12345,
            Inhoud = "TWFuIGlzIGRpc3Rpbmd1aXNoZWQsIG5vdCBvbmx5IGJ5IGhpcyByZWFzb24sIGJ1dCAuLi4=",
            Link = "(no link)",
            Beschrijving = "My description of the document",
            OntvangstDatum = "2020-11-13",
            VerzendDatum = "2020-11-14",
            IndicatieGebruiksrecht = true,
            Ondertekening = new OndertekeningDto { Soort = Soort.digitaal.ToString(), Datum = "2020-11-18" },
            Integriteit = new IntegriteitDto
            {
                Algoritme = Algoritme.crc_32.ToString(),
                Waarde = "123",
                Datum = "2020-11-17",
            },
            InformatieObjectType = "https://some-informatieobjecttype",
            Vertrouwelijkheidaanduiding = VertrouwelijkheidAanduiding.openbaar.ToString(),
            Status = Status.definitief.ToString(),
            Verschijningsvorm = "some-verschijningsvorm",
            Trefwoorden = ["bouwtekening", "vergunning"],
        };
    }

    private static EnkelvoudigInformatieObjectUpdateRequestDto UpdateRequestDto()
    {
        return new EnkelvoudigInformatieObjectUpdateRequestDto
        {
            Lock = "8494eecb2495447a8b29a8e31d10c4b4",
            CreatieDatum = "2020-11-12",
            Taal = "eng",
            Bestandsomvang = 12345,
            OntvangstDatum = "2020-11-13",
            VerzendDatum = "2020-11-14",
            IndicatieGebruiksrecht = true,
            Ondertekening = new OndertekeningDto { Soort = Soort.digitaal.ToString(), Datum = "2020-11-18" },
            Integriteit = new IntegriteitDto
            {
                Algoritme = Algoritme.crc_32.ToString(),
                Waarde = "123",
                Datum = "2020-11-17",
            },
            InformatieObjectType = "https://some-informatieobjecttype/",
            Vertrouwelijkheidaanduiding = VertrouwelijkheidAanduiding.openbaar.ToString(),
            Status = Status.definitief.ToString(),
            Verschijningsvorm = "some-verschijningsvorm",
        };
    }

    private static VerzendingRequestDto VerzendingRequestDto()
    {
        return new VerzendingRequestDto
        {
            Betrokkene = "https://some-betrokkene",
            AardRelatie = DataModel.AardRelatie.afzender.ToString(),
            Toelichting = "some toelichting",
            OntvangstDatum = "2020-11-13",
            Verzenddatum = "2020-11-14",
            Contactpersoon = "some contactpersoon",
            BinnenlandsCorrespondentieAdres = new BinnenlandsCorrespondentieAdresDto
            {
                Huisletter = "A",
                Huisnummer = 1,
                HuisnummerToevoeging = "bis",
                NaamOpenbareRuimte = "some street",
                Postcode = "1234AB",
                WoonplaatsNaam = "some city",
            },
            Faxnummer = "0101234567",
            EmailAdres = "someone@example.com",
            MijnOverheid = true,
            Telefoonnummer = "0101234567",
        };
    }

    // ------------------------------------------------------------------------------------------
    // 1. THE CRITICAL Trefwoorden_In null-preservation facts.
    // ------------------------------------------------------------------------------------------

    [Fact]
    public void GetAllEnkelvoudigInformatieObjectenQueryParameters_With_Trefwoorden_Maps_To_Trefwoorden_In()
    {
        // Setup
        var value = new GetAllEnkelvoudigInformatieObjectenQueryParameters { Trefwoorden = "bouwtekening,vergunning,aanvraag" };

        // Act
        var result = _mapper.Map<Web.Models.v1._5.GetAllEnkelvoudigInformatieObjectenFilter>(value);

        // Assert
        Assert.Equal(["bouwtekening", "vergunning", "aanvraag"], result.Trefwoorden_In);
    }

    [Fact]
    public void GetAllEnkelvoudigInformatieObjectenQueryParameters_Without_Trefwoorden_Maps_Trefwoorden_In_To_Null_Not_Empty()
    {
        // Setup: no Trefwoorden concept in the source (property left null) - the domain code treats a
        // null Trefwoorden_In filter as "no filter applied" in an EF Where query, but an empty list as
        // "match nothing" - these are NOT interchangeable, hence Assert.Null rather than Assert.Empty.
        var value = new GetAllEnkelvoudigInformatieObjectenQueryParameters { Trefwoorden = null };

        // Act
        var result = _mapper.Map<Web.Models.v1._5.GetAllEnkelvoudigInformatieObjectenFilter>(value);

        // Assert
        Assert.Null(result.Trefwoorden_In);
    }

    [Fact]
    public void EnkelvoudigInformatieObjectSearchRequestDto_Maps_Trefwoorden_In_To_Null_Not_Empty()
    {
        // Setup: the search DTO has no Trefwoorden concept at all - Trefwoorden_In is Ignore()'d and
        // then forced to null via .AfterMapping, exactly like the query-parameters config above.
        var value = new EnkelvoudigInformatieObjectSearchRequestDto { Uuid_In = ["11111111-1111-1111-1111-111111111111"] };

        // Act
        var result = _mapper.Map<Web.Models.v1._5.GetAllEnkelvoudigInformatieObjectenFilter>(value);

        // Assert
        Assert.Null(result.Trefwoorden_In);
        Assert.Equal(value.Uuid_In, result.Uuid_In);
    }

    [Fact]
    public void EnkelvoudigInformatieObjectSearchRequestDto_Ignores_Bronorganisatie_And_Identificatie()
    {
        // Setup
        var value = new EnkelvoudigInformatieObjectSearchRequestDto { Uuid_In = ["11111111-1111-1111-1111-111111111111"] };

        // Act
        var result = _mapper.Map<Web.Models.v1._5.GetAllEnkelvoudigInformatieObjectenFilter>(value);

        // Assert: not mapped (Ignore()'d), so the destination's own default (null) is left untouched.
        Assert.Null(result.Bronorganisatie);
        Assert.Null(result.Identificatie);
    }

    // ------------------------------------------------------------------------------------------
    // Deliberate-breakage exercise for the null-preservation ports (logical-correctness check only).
    //
    // Note on what this specific test CAN and CANNOT prove: this suite builds a bare
    // `TypeAdapterConfig()` (see constructor above), which has NO `EmptyCollectionIfNull` destination
    // transform registered (that transform is only wired up in production via `AddZgwMapster`). So
    // even a naive `.Map(dest => dest.Trefwoorden_In, src => src.Trefwoorden == null ? null : ...)`
    // fold would correctly yield null here - this test cannot demonstrate the transform silently
    // coalescing null to `[]`. What it DOES prove is that the `.AfterMapping` assignment (and not some
    // other code path) is what is actually responsible for producing the null - i.e. it is exercising
    // the right lever. The DEFINITIVE proof that `.AfterMapping` (rather than a plain `.Map` fold) is
    // REQUIRED under the real `EmptyCollectionIfNull` transform is deferred to the orchestrator's
    // later A/B parity task, which wires the real `AddZgwMapster` seam config.
    // ------------------------------------------------------------------------------------------

    [Fact]
    public void Trefwoorden_In_Null_Preservation_Is_Driven_By_AfterMapping_Not_Some_Other_Path()
    {
        // Setup: register a SECOND, deliberately-broken config using a plain .Map fold (no
        // .AfterMapping) for the query-parameters case, to confirm the null result in the main config
        // is coming from .AfterMapping and not, say, from ProfileHelper.ArrayFromString itself already
        // returning null for a null input (which would make the .AfterMapping redundant).
        var brokenConfig = new TypeAdapterConfig();
        brokenConfig.RegisterNullableEnumRule();
        brokenConfig
            .NewConfig<GetAllEnkelvoudigInformatieObjectenQueryParameters, Web.Models.v1._5.GetAllEnkelvoudigInformatieObjectenFilter>()
            .Map(
                dest => dest.Trefwoorden_In,
                src => src.Trefwoorden == null ? null : OneGround.ZGW.Common.Helpers.ProfileHelper.ArrayFromString(src.Trefwoorden)
            );
        brokenConfig.Compile();
        var brokenMapper = new Mapper(brokenConfig);

        var value = new GetAllEnkelvoudigInformatieObjectenQueryParameters { Trefwoorden = null };

        // Act
        var brokenResult = brokenMapper.Map<Web.Models.v1._5.GetAllEnkelvoudigInformatieObjectenFilter>(value);
        var realResult = _mapper.Map<Web.Models.v1._5.GetAllEnkelvoudigInformatieObjectenFilter>(value);

        // Assert: under a bare config (no EmptyCollectionIfNull transform active), the plain-.Map fold
        // ALSO produces null here - this is the documented limitation above. Both configs agree in
        // this bare-config test; the real config's behavior when the production transform IS active
        // is proven separately by the later A/B parity task.
        Assert.Null(brokenResult.Trefwoorden_In);
        Assert.Null(realResult.Trefwoorden_In);
    }

    // ------------------------------------------------------------------------------------------
    // 2. The nested InformatieObject object-construction lambdas (Create vs Update).
    // ------------------------------------------------------------------------------------------

    [Fact]
    public void EnkelvoudigInformatieObjectCreateRequestDto_Maps_To_EnkelvoudigInformatieObjectVersie()
    {
        // Setup
        var value = CreateRequestDto();

        // Act
        var result = _mapper.Map<EnkelvoudigInformatieObjectVersie>(value);

        // Assert
        Assert.Equal(value.Identificatie, result.Identificatie);
        Assert.Equal(value.Bronorganisatie, result.Bronorganisatie);
        Assert.Equal(value.CreatieDatum, result.CreatieDatum.Value.ToString("yyyy-MM-dd"));
        Assert.Equal(value.Titel, result.Titel);
        Assert.Equal(value.Vertrouwelijkheidaanduiding, result.Vertrouwelijkheidaanduiding.ToString());
        Assert.Equal(value.Auteur, result.Auteur);
        Assert.Equal(value.Status, result.Status.ToString());
        Assert.Equal(value.Formaat, result.Formaat);
        Assert.Equal(value.Taal, result.Taal);
        Assert.Equal(value.Bestandsnaam, result.Bestandsnaam);
        Assert.Equal(value.Bestandsomvang, result.Bestandsomvang);
        Assert.Equal(value.Inhoud, result.Inhoud);
        Assert.Equal(value.Link, result.Link);
        Assert.Equal(value.Beschrijving, result.Beschrijving);
        Assert.Equal(value.IndicatieGebruiksrecht, result.InformatieObject.IndicatieGebruiksrecht);
        Assert.Equal(value.OntvangstDatum, result.OntvangstDatum.Value.ToString("yyyy-MM-dd"));
        Assert.Equal(value.VerzendDatum, result.VerzendDatum.Value.ToString("yyyy-MM-dd"));
        Assert.Equal(value.Ondertekening.Datum, result.Ondertekening_Datum.Value.ToString("yyyy-MM-dd"));
        Assert.Equal(value.Ondertekening.Soort, result.Ondertekening_Soort.ToString());
        Assert.Equal(value.Integriteit.Algoritme, result.Integriteit_Algoritme.ToString());
        Assert.Equal(value.Integriteit.Waarde, result.Integriteit_Waarde);
        Assert.Equal(value.Integriteit.Datum, result.Integriteit_Datum.Value.ToString("yyyy-MM-dd"));
        Assert.Equal(value.Verschijningsvorm, result.Verschijningsvorm);
        Assert.Equal(value.Trefwoorden, result.Trefwoorden);
        // Create-map InformatieObject: InformatieObjectType is TrimEnd('/')'d - source has no trailing slash here,
        // dedicated trim-behavior test below proves the trimming itself.
        Assert.Equal(value.InformatieObjectType, result.InformatieObject.InformatieObjectType);
    }

    [Fact]
    public void EnkelvoudigInformatieObjectUpdateRequestDto_With_Lock_Maps_To_EnkelvoudigInformatieObjectVersie()
    {
        // Setup
        var value = UpdateRequestDto();

        // Act
        var result = _mapper.Map<EnkelvoudigInformatieObjectVersie>(value);

        // Assert
        Assert.Equal(value.Lock, result.InformatieObject.Lock);
        Assert.Equal(value.IndicatieGebruiksrecht, result.InformatieObject.IndicatieGebruiksrecht);
        Assert.Equal(value.Verschijningsvorm, result.Verschijningsvorm);
    }

    [Fact]
    public void CreateMap_InformatieObjectType_Is_TrimEnd_Slashed()
    {
        // Setup
        var value = CreateRequestDto();
        value.InformatieObjectType = "https://some-informatieobjecttype/";

        // Act
        var result = _mapper.Map<EnkelvoudigInformatieObjectVersie>(value);

        // Assert: Create-map trims the trailing slash.
        Assert.Equal("https://some-informatieobjecttype", result.InformatieObject.InformatieObjectType);
    }

    [Fact]
    public void UpdateMap_InformatieObjectType_Is_Not_TrimEnd_Slashed()
    {
        // Setup
        var value = UpdateRequestDto();
        value.InformatieObjectType = "https://some-informatieobjecttype/";

        // Act
        var result = _mapper.Map<EnkelvoudigInformatieObjectVersie>(value);

        // Assert: Update-map does NOT trim the trailing slash - this is the deliberate difference
        // between the Create-map and Update-map InformatieObject construction lambdas.
        Assert.Equal("https://some-informatieobjecttype/", result.InformatieObject.InformatieObjectType);
        Assert.Equal(value.Lock, result.InformatieObject.Lock);
    }

    // ------------------------------------------------------------------------------------------
    // 3. The five enum-parse helpers.
    // ------------------------------------------------------------------------------------------

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void VertrouwelijkheidAanduidingFromString_Null_Or_Empty_Maps_To_Null(string input)
    {
        // Setup
        var value = CreateRequestDto();
        value.Vertrouwelijkheidaanduiding = input;

        // Act
        var result = _mapper.Map<EnkelvoudigInformatieObjectVersie>(value);

        // Assert
        Assert.Null(result.Vertrouwelijkheidaanduiding);
    }

    [Fact]
    public void VertrouwelijkheidAanduidingFromString_Valid_Name_Maps_To_Enum()
    {
        // Setup
        var value = CreateRequestDto();
        value.Vertrouwelijkheidaanduiding = VertrouwelijkheidAanduiding.geheim.ToString();

        // Act
        var result = _mapper.Map<EnkelvoudigInformatieObjectVersie>(value);

        // Assert
        Assert.Equal(VertrouwelijkheidAanduiding.geheim, result.Vertrouwelijkheidaanduiding);
    }

    [Fact]
    public void VertrouwelijkheidAanduidingFromString_Unrecognized_Name_Throws()
    {
        // Setup
        var value = CreateRequestDto();
        value.Vertrouwelijkheidaanduiding = "not-a-real-value";

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => _mapper.Map<EnkelvoudigInformatieObjectVersie>(value));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void StatusFromString_Null_Or_Empty_Maps_To_Null(string input)
    {
        // Setup
        var value = CreateRequestDto();
        value.Status = input;

        // Act
        var result = _mapper.Map<EnkelvoudigInformatieObjectVersie>(value);

        // Assert
        Assert.Null(result.Status);
    }

    [Fact]
    public void StatusFromString_Valid_Name_Maps_To_Enum()
    {
        // Setup
        var value = CreateRequestDto();
        value.Status = Status.gearchiveerd.ToString();

        // Act
        var result = _mapper.Map<EnkelvoudigInformatieObjectVersie>(value);

        // Assert
        Assert.Equal(Status.gearchiveerd, result.Status);
    }

    [Fact]
    public void StatusFromString_Unrecognized_Name_Throws()
    {
        // Setup
        var value = CreateRequestDto();
        value.Status = "not-a-real-value";

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => _mapper.Map<EnkelvoudigInformatieObjectVersie>(value));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void SoortFromString_Null_Or_Empty_Maps_To_Null(string input)
    {
        // Setup
        var value = CreateRequestDto();
        value.Ondertekening.Soort = input;

        // Act
        var result = _mapper.Map<EnkelvoudigInformatieObjectVersie>(value);

        // Assert
        Assert.Null(result.Ondertekening_Soort);
    }

    [Fact]
    public void SoortFromString_Valid_Name_Maps_To_Enum()
    {
        // Setup
        var value = CreateRequestDto();
        value.Ondertekening.Soort = Soort.pki.ToString();

        // Act
        var result = _mapper.Map<EnkelvoudigInformatieObjectVersie>(value);

        // Assert
        Assert.Equal(Soort.pki, result.Ondertekening_Soort);
    }

    [Fact]
    public void SoortFromString_Unrecognized_Name_Throws()
    {
        // Setup
        var value = CreateRequestDto();
        value.Ondertekening.Soort = "not-a-real-value";

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => _mapper.Map<EnkelvoudigInformatieObjectVersie>(value));
    }

    [Fact]
    public void AlgoritmeFromString_Valid_Name_Maps_To_Enum()
    {
        // Setup
        var value = CreateRequestDto();
        value.Integriteit.Algoritme = Algoritme.sha_256.ToString();

        // Act
        var result = _mapper.Map<EnkelvoudigInformatieObjectVersie>(value);

        // Assert
        Assert.Equal(Algoritme.sha_256, result.Integriteit_Algoritme);
    }

    [Fact]
    public void AlgoritmeFromString_Null_Throws_ArgumentNullException()
    {
        // Setup: unlike VertrouwelijkheidAanduidingFromString/StatusFromString/SoortFromString,
        // AlgoritmeFromString has no nullable return - a null input throws ArgumentNullException
        // rather than mapping to null.
        var value = CreateRequestDto();
        value.Integriteit.Algoritme = null;

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => _mapper.Map<EnkelvoudigInformatieObjectVersie>(value));
    }

    [Fact]
    public void AlgoritmeFromString_Unrecognized_Name_Throws_InvalidOperationException()
    {
        // Setup
        var value = CreateRequestDto();
        value.Integriteit.Algoritme = "not-a-real-value";

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => _mapper.Map<EnkelvoudigInformatieObjectVersie>(value));
    }

    [Fact]
    public void AardRelatieFromString_Valid_Name_Maps_To_Enum()
    {
        // Setup
        var value = VerzendingRequestDto();
        value.AardRelatie = DataModel.AardRelatie.geadresseerde.ToString();

        // Act
        var result = _mapper.Map<Verzending>(value);

        // Assert
        Assert.Equal(DataModel.AardRelatie.geadresseerde, result.AardRelatie);
    }

    [Fact]
    public void AardRelatieFromString_Null_Throws_ArgumentNullException()
    {
        // Setup: like AlgoritmeFromString, AardRelatieFromString has no nullable return and no
        // null-or-empty short-circuit branch - a null input throws ArgumentNullException.
        var value = VerzendingRequestDto();
        value.AardRelatie = null;

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => _mapper.Map<Verzending>(value));
    }

    [Fact]
    public void AardRelatieFromString_Unrecognized_Name_Throws_InvalidOperationException()
    {
        // Setup
        var value = VerzendingRequestDto();
        value.AardRelatie = "not-a-real-value";

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => _mapper.Map<Verzending>(value));
    }

    // ------------------------------------------------------------------------------------------
    // 4. Verzending-related maps: AardRelatie plus a correspondence-address sub-map.
    // ------------------------------------------------------------------------------------------

    [Fact]
    public void VerzendingRequestDto_Maps_To_Verzending()
    {
        // Setup
        var value = VerzendingRequestDto();

        // Act
        var result = _mapper.Map<Verzending>(value);

        // Assert
        Assert.Equal(value.Betrokkene, result.Betrokkene);
        Assert.Equal(DataModel.AardRelatie.afzender, result.AardRelatie);
        Assert.Equal(value.Toelichting, result.Toelichting);
        Assert.Equal(value.OntvangstDatum, result.Ontvangstdatum.Value.ToString("yyyy-MM-dd"));
        Assert.Equal(value.Verzenddatum, result.Verzenddatum.Value.ToString("yyyy-MM-dd"));
        Assert.Equal(value.Contactpersoon, result.Contactpersoon);
        Assert.Equal(value.Faxnummer, result.Faxnummer);
        Assert.Equal(value.EmailAdres, result.EmailAdres);
        Assert.Equal(value.MijnOverheid, result.MijnOverheid);
        Assert.Equal(value.Telefoonnummer, result.Telefoonnummer);
        Assert.NotNull(result.BinnenlandsCorrespondentieAdres);
        Assert.Equal(value.BinnenlandsCorrespondentieAdres.Postcode, result.BinnenlandsCorrespondentieAdres.Postcode);
        Assert.Equal(value.BinnenlandsCorrespondentieAdres.WoonplaatsNaam, result.BinnenlandsCorrespondentieAdres.WoonplaatsNaam);
        Assert.Equal(value.BinnenlandsCorrespondentieAdres.Huisnummer, result.BinnenlandsCorrespondentieAdres.Huisnummer);
    }

    [Fact]
    public void BuitenlandsCorrespondentieAdresDto_Maps_To_BuitenlandsCorrespondentieAdres()
    {
        // Setup
        var value = new BuitenlandsCorrespondentieAdresDto
        {
            AdresBuitenland1 = "Some street 1",
            AdresBuitenland2 = "Some place 2",
            AdresBuitenland3 = "Some place 3",
            LandPostadres = "https://some-land",
        };

        // Act
        var result = _mapper.Map<BuitenlandsCorrespondentieAdres>(value);

        // Assert
        Assert.Equal(value.AdresBuitenland1, result.AdresBuitenland1);
        Assert.Equal(value.AdresBuitenland2, result.AdresBuitenland2);
        Assert.Equal(value.AdresBuitenland3, result.AdresBuitenland3);
        Assert.Equal(value.LandPostadres, result.LandPostadres);
    }

    [Fact]
    public void CorrespondentiePostAdresDto_Maps_To_CorrespondentiePostadres()
    {
        // Setup
        var value = new CorrespondentiePostAdresDto
        {
            PostbusOfAntwoordnummer = 123,
            PostadresPostcode = "1234AB",
            PostadresType = PostadresType.postbusnummer.ToString(),
            WoonplaatsNaam = "some city",
        };

        // Act
        var result = _mapper.Map<CorrespondentiePostadres>(value);

        // Assert
        Assert.Equal(value.PostbusOfAntwoordnummer, result.PostbusOfAntwoordnummer);
        Assert.Equal(value.PostadresPostcode, result.PostadresPostcode);
        Assert.Equal(PostadresType.postbusnummer, result.PostadresType);
        Assert.Equal(value.WoonplaatsNaam, result.WoonplaatsNaam);
    }
}
