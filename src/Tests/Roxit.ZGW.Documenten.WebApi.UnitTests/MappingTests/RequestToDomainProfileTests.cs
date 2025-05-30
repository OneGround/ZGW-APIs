using AutoFixture;
using AutoMapper;
using AutoMapper.Internal;
using Roxit.ZGW.Common.DataModel;
using Roxit.ZGW.Common.Web;
using Roxit.ZGW.Documenten.Contracts.v1;
using Roxit.ZGW.Documenten.Contracts.v1.Queries;
using Roxit.ZGW.Documenten.Contracts.v1.Requests;
using Roxit.ZGW.Documenten.DataModel;
using Roxit.ZGW.Documenten.Web.MappingProfiles.v1;
using Roxit.ZGW.Documenten.Web.Models.v1;
using Xunit;

namespace Roxit.ZGW.Documenten.WebApi.UnitTests.MappingTests;

public class RequestToDomainProfileTests
{
    private readonly OmitOnRecursionFixture _fixture = new OmitOnRecursionFixture();
    private readonly IMapper _mapper;

    public RequestToDomainProfileTests()
    {
        var configuration = new MapperConfiguration(config =>
        {
            config.AddProfile(new RequestToDomainProfile());
            config.Internal().Mappers.Insert(0, new NullableEnumMapper());
            config.ShouldMapMethod = (m => false);
        });

        configuration.AssertConfigurationIsValid();

        _mapper = configuration.CreateMapper();
    }

    [Fact]
    public void GetAllEnkelvoudigInformatieObjectenQueryParameters_Maps_To_GetAllEnkelvoudiginformatieobjectenFilter()
    {
        // Setup
        _fixture.Customize<GetAllEnkelvoudigInformatieObjectenQueryParameters>(c =>
            c.With(p => p.Identificatie, "DOC-2020-0000001").With(p => p.Bronorganisatie, "520087732")
        );
        var value = _fixture.Create<GetAllEnkelvoudigInformatieObjectenQueryParameters>();

        // Act
        var result = _mapper.Map<GetAllEnkelvoudigInformatieObjectenFilter>(value);

        // Assert
        Assert.Equal(value.Identificatie, result.Identificatie);
        Assert.Equal(value.Bronorganisatie, result.Bronorganisatie);
    }

    [Fact]
    public void EnkelvoudigInformatieObjectCreateRequestDto_Maps_To_EnkelvoudigInformatieObjectVersie()
    {
        // Setup
        _fixture.Customize<IntegriteitDto>(c =>
            c.With(p => p.Algoritme, Algoritme.crc_32.ToString()).With(p => p.Waarde, "123").With(p => p.Datum, "2020-11-17")
        );

        _fixture.Customize<OndertekeningDto>(c => c.With(p => p.Datum, "2020-11-18").With(p => p.Soort, Soort.digitaal.ToString()));

        _fixture.Customize<EnkelvoudigInformatieObjectCreateRequestDto>(c =>
            c.With(p => p.Identificatie, "DOC-2020-0000001")
                .With(p => p.Bronorganisatie, "520087732")
                .With(p => p.CreatieDatum, "2020-11-12")
                .With(p => p.Titel, "My document")
                .With(p => p.Vertrouwelijkheidaanduiding, _fixture.Create<VertrouwelijkheidAanduiding>().ToString())
                .With(p => p.Auteur, "somebody")
                .With(p => p.Status, _fixture.Create<Status>().ToString())
                .With(p => p.Formaat, "")
                .With(p => p.Taal, "eng")
                .With(p => p.Bestandsnaam, "document.pdf")
                .With(p => p.Inhoud, "TWFuIGlzIGRpc3Rpbmd1aXNoZWQsIG5vdCBvbmx5IGJ5IGhpcyByZWFzb24sIGJ1dCAuLi4=")
                .With(p => p.Link, "(no link)")
                .With(p => p.Beschrijving, "My description of the document")
                .With(p => p.OntvangstDatum, "2020-11-13")
                .With(p => p.VerzendDatum, "2020-11-14")
                .With(p => p.IndicatieGebruiksrecht, true)
                .With(p => p.Ondertekening, _fixture.Create<OndertekeningDto>)
                .With(p => p.Integriteit, _fixture.Create<IntegriteitDto>)
                .With(p => p.InformatieObjectType, "https://some-informatieobjecttype")
        );

        var value = _fixture.Create<EnkelvoudigInformatieObjectCreateRequestDto>();

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
        Assert.Equal(value.Inhoud, result.Inhoud);
        Assert.Equal(value.Link, result.Link);
        Assert.Equal(value.Beschrijving, result.Beschrijving);
        Assert.Equal(value.IndicatieGebruiksrecht, result.EnkelvoudigInformatieObject.IndicatieGebruiksrecht);
        Assert.Equal(value.OntvangstDatum, result.OntvangstDatum.Value.ToString("yyyy-MM-dd"));
        Assert.Equal(value.VerzendDatum, result.VerzendDatum.Value.ToString("yyyy-MM-dd"));
        Assert.Equal(value.Ondertekening.Datum, result.Ondertekening_Datum.Value.ToString("yyyy-MM-dd"));
        Assert.Equal(value.Ondertekening.Soort, result.Ondertekening_Soort.ToString());
        Assert.Equal(value.Ondertekening.Datum, result.Ondertekening_Datum.Value.ToString("yyyy-MM-dd"));
        Assert.Equal(value.Integriteit.Algoritme, result.Integriteit_Algoritme.ToString());
        Assert.Equal(value.Integriteit.Waarde, result.Integriteit_Waarde);
        Assert.Equal(value.Integriteit.Datum, result.Integriteit_Datum.Value.ToString("yyyy-MM-dd"));
        Assert.Equal(value.InformatieObjectType, result.EnkelvoudigInformatieObject.InformatieObjectType);
    }

    [Fact]
    public void EnkelvoudigInformatieObjectUpdateRequestDto_With_Lock_Maps_To_EnkelvoudigInformatieObjectVersie()
    {
        // Setup
        _fixture.Customize<IntegriteitDto>(c => c.With(p => p.Algoritme, Algoritme.crc_32.ToString()).With(p => p.Datum, "2020-11-17"));

        _fixture.Customize<OndertekeningDto>(c => c.With(p => p.Datum, "2020-11-18").With(p => p.Soort, Soort.digitaal.ToString()));

        _fixture.Customize<EnkelvoudigInformatieObjectUpdateRequestDto>(c =>
            c.With(p => p.Lock, "8494eecb2495447a8b29a8e31d10c4b4")
                .With(p => p.CreatieDatum, "2020-11-12")
                .With(p => p.Vertrouwelijkheidaanduiding, _fixture.Create<VertrouwelijkheidAanduiding>().ToString())
                .With(p => p.Status, _fixture.Create<Status>().ToString())
                .With(p => p.OntvangstDatum, "2020-11-13")
                .With(p => p.VerzendDatum, "2020-11-14")
        );

        var value = _fixture.Create<EnkelvoudigInformatieObjectUpdateRequestDto>();

        // Act
        var result = _mapper.Map<EnkelvoudigInformatieObjectVersie>(value);

        // Assert
        Assert.Equal(value.Lock, result.EnkelvoudigInformatieObject.Lock);
    }

    [Fact]
    public void GetGetAllObjectInformatieObjectenQueryParameters_Maps_To_GetAllObjectInformatieObjectenFilter()
    {
        // Setup
        _fixture.Customize<GetAllObjectInformatieObjectenQueryParameters>(c =>
            c.With(p => p.Object, "https://some-zaak").With(p => p.InformatieObject, "https://some-informatieobject")
        );
        var value = _fixture.Create<GetAllObjectInformatieObjectenQueryParameters>();

        // Act
        var result = _mapper.Map<GetAllObjectInformatieObjectenFilter>(value);

        // Assert
        Assert.Equal(value.Object, result.Object);
        Assert.Equal(value.InformatieObject, result.InformatieObject);
    }

    [Fact]
    public void ObjectInformatieObjectRequestDto_Maps_To_ObjectInformatieObject()
    {
        // Setup
        _fixture.Customize<ObjectInformatieObjectRequestDto>(c =>
            c.With(a => a.ObjectType, ObjectType.besluit.ToString()).Without(a => a.InformatieObject)
        );

        var value = _fixture.Create<ObjectInformatieObjectRequestDto>();

        // Act
        var result = _mapper.Map<ObjectInformatieObject>(value);

        // Assert
        Assert.Equal(value.Object, result.Object);
        Assert.Equal(value.ObjectType, result.ObjectType.ToString());
    }

    [Fact]
    public void GetGetAllGebruiksRechtenQueryParameters_Maps_To_GetAllGebruiksRechtenFilter()
    {
        // Setup
        _fixture.Customize<GetAllGebruiksRechtenQueryParameters>(c =>
            c.With(p => p.Startdatum__gt, "2020-11-13")
                .With(p => p.Startdatum__gte, "2020-11-14")
                .With(p => p.Startdatum__lt, "2020-11-15")
                .With(p => p.Startdatum__lte, "2020-11-16")
                .With(p => p.Einddatum__gt, "2020-11-17")
                .With(p => p.Einddatum__gte, "2020-11-18")
                .With(p => p.Einddatum__lt, "2020-11-19")
                .With(p => p.Einddatum__lte, "2020-11-20")
                .With(p => p.InformatieObject, "https://some-informatieobject")
        );
        var value = _fixture.Create<GetAllGebruiksRechtenQueryParameters>();

        // Act
        var result = _mapper.Map<GetAllGebruiksRechtenFilter>(value);

        // Assert
        Assert.Equal(value.Startdatum__gt, result.Startdatum__gt.Value.ToString("yyyy-MM-dd"));
        Assert.Equal(value.Startdatum__gte, result.Startdatum__gte.Value.ToString("yyyy-MM-dd"));
        Assert.Equal(value.Startdatum__lt, result.Startdatum__lt.Value.ToString("yyyy-MM-dd"));
        Assert.Equal(value.Startdatum__lte, result.Startdatum__lte.Value.ToString("yyyy-MM-dd"));
        Assert.Equal(value.Einddatum__gt, result.Einddatum__gt.Value.ToString("yyyy-MM-dd"));
        Assert.Equal(value.Einddatum__gte, result.Einddatum__gte.Value.ToString("yyyy-MM-dd"));
        Assert.Equal(value.Einddatum__lt, result.Einddatum__lt.Value.ToString("yyyy-MM-dd"));
        Assert.Equal(value.Einddatum__lte, result.Einddatum__lte.Value.ToString("yyyy-MM-dd"));
        Assert.Equal(value.InformatieObject, result.InformatieObject);
    }

    [Fact]
    public void GebruiksRechtRequestDto_Maps_To_GebruiksRecht()
    {
        // Setup
        _fixture.Customize<GebruiksRechtRequestDto>(c =>
            c.With(p => p.Startdatum, "2020-11-16").With(p => p.Einddatum, "2020-11-17").Without(p => p.InformatieObject)
        );

        var value = _fixture.Create<GebruiksRechtRequestDto>();

        // Act
        var result = _mapper.Map<GebruiksRecht>(value);

        // Assert
        Assert.Equal(value.OmschrijvingVoorwaarden, result.OmschrijvingVoorwaarden);
        Assert.Equal(value.Startdatum, result.Startdatum.ToLocalTime().ToString("yyyy-MM-dd"));
        Assert.Equal(value.Einddatum, result.Einddatum.Value.ToLocalTime().ToString("yyyy-MM-dd"));
    }
}
