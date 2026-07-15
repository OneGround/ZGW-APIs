using System;
using AutoFixture;
using Mapster;
using MapsterMapper;
using OneGround.ZGW.Common.DataModel;
using OneGround.ZGW.Common.Web.Mapping.Mapster;
using OneGround.ZGW.Documenten.Contracts.v1;
using OneGround.ZGW.Documenten.Contracts.v1._1.Requests;
using OneGround.ZGW.Documenten.Contracts.v1.Queries;
using OneGround.ZGW.Documenten.DataModel;
using OneGround.ZGW.Documenten.Web.MappingProfiles.v1._1;
using OneGround.ZGW.Documenten.WebApi.UnitTests.MappingTests;
using Xunit;

namespace OneGround.ZGW.Documenten.WebApi.UnitTests.MappingTests.v1_1;

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
        };
    }

    [Fact]
    public void GetAllEnkelvoudigInformatieObjectenQueryParameters_Maps_To_GetAllEnkelvoudiginformatieobjectenFilter()
    {
        // Setup
        _fixture.Customize<GetAllEnkelvoudigInformatieObjectenQueryParameters>(c =>
            c.With(p => p.Identificatie, "DOC-2020-0000001").With(p => p.Bronorganisatie, "999990561")
        );
        var value = _fixture.Create<GetAllEnkelvoudigInformatieObjectenQueryParameters>();

        // Act
        var result = _mapper.Map<Web.Models.v1.GetAllEnkelvoudigInformatieObjectenFilter>(value);

        // Assert
        Assert.Equal(value.Identificatie, result.Identificatie);
        Assert.Equal(value.Bronorganisatie, result.Bronorganisatie);
    }

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
        // Create-map InformatieObject: InformatieObjectType is TrimEnd('/')'d - source has no trailing slash here,
        // dedicated trim-behavior test below proves the trimming itself.
        Assert.Equal(value.InformatieObjectType, result.InformatieObject.InformatieObjectType);
    }

    [Fact]
    public void EnkelvoudigInformatieObjectCreateRequestDto_With_Null_Ondertekening_And_Integriteit_Maps_Without_Throwing()
    {
        // Ondertekening/Integriteit are optional on the wire (no [Required] attribute) -- a real request
        // omitting them must not NullReferenceException on the member-path access inside the register
        // (found via a genuine regression: AutoMapper's MapFrom auto-null-guards these paths, Mapster's
        // .Map lambdas do not). AlgoritmeFromString itself throws on a null argument by design, so the
        // register must skip calling it entirely rather than merely null-guard its argument.
        var value = CreateRequestDto();
        value.Ondertekening = null;
        value.Integriteit = null;

        var result = _mapper.Map<EnkelvoudigInformatieObjectVersie>(value);

        Assert.Null(result.Ondertekening_Datum);
        Assert.Null(result.Ondertekening_Soort);
        Assert.Equal(default, result.Integriteit_Algoritme);
        Assert.Null(result.Integriteit_Datum);
        Assert.Null(result.Integriteit_Waarde);
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
        // Setup: unlike the other three helpers, AlgoritmeFromString has no nullable return - a null
        // input throws ArgumentNullException rather than mapping to null.
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
}
