using System;
using System.Collections.Generic;
using System.Linq;
using AutoFixture;
using AutoMapper;
using Moq;
using Roxit.ZGW.Common.Web.Mapping.ValueResolvers;
using Roxit.ZGW.Common.Web.Services.UriServices;
using Roxit.ZGW.DataAccess;
using Roxit.ZGW.Documenten.Contracts.v1.Responses;
using Roxit.ZGW.Documenten.DataModel;
using Roxit.ZGW.Documenten.Web.MappingProfiles.v1;
using Xunit;

namespace Roxit.ZGW.Documenten.WebApi.UnitTests.MappingTests;

public class ResponseToDomainProfileTests
{
    private readonly OmitOnRecursionFixture _fixture = new OmitOnRecursionFixture();
    private readonly Mock<IEntityUriService> _mockedUriService = new Mock<IEntityUriService>();
    private readonly IMapper _mapper;

    public ResponseToDomainProfileTests()
    {
        _fixture.Register<DateOnly>(() => DateOnly.FromDateTime(DateTime.UtcNow));

        var configuration = new MapperConfiguration(config =>
        {
            config.AddProfile(new DomainToResponseProfile());
            config.ShouldMapMethod = (m => false);
        });

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
            if (t == typeof(MapLatestEnkelvoudigInformatieObjectVersieResponse))
            {
                return new MapLatestEnkelvoudigInformatieObjectVersieResponse(_mockedUriService.Object);
            }
            throw new NotImplementedException($"Mapper is missing the service: {t})");
        });
    }

    [Fact]
    public void EnkelvoudigInformatieObject_Maps_To_EnkelvoudiginformatieobjectGetResponse_with_Version()
    {
        // Setup
        _fixture.Customize<EnkelvoudigInformatieObjectVersie>(c => c.With(p => p.Versie, 1).Without(p => p.Inhoud));

        var enkelvoudigInformatieObjectVersies = new List<EnkelvoudigInformatieObjectVersie> { _fixture.Create<EnkelvoudigInformatieObjectVersie>() };

        _fixture.Customize<EnkelvoudigInformatieObject>(c => c.With(p => p.EnkelvoudigInformatieObjectVersies, enkelvoudigInformatieObjectVersies));

        var value = _fixture.Create<EnkelvoudigInformatieObject>();

        // Act
        var result = _mapper.Map<EnkelvoudigInformatieObjectGetResponseDto>(value);

        // Assert
        var latest = value.EnkelvoudigInformatieObjectVersies.First();

        Assert.Equal(value.Url, result.Url);
        Assert.Equal(latest.Versie, result.Versie);

        Assert.Equal(latest.BeginRegistratie, result.BeginRegistratie);
        Assert.Equal(latest.Bestandsomvang, result.Bestandsomvang);

        Assert.Equal(latest.Identificatie, result.Identificatie);
        Assert.Equal(latest.Bronorganisatie, result.Bronorganisatie);
        Assert.Equal(latest.CreatieDatum.Value.ToString("yyyy-MM-dd"), result.CreatieDatum);
        Assert.Equal(latest.Vertrouwelijkheidaanduiding.Value.ToString(), result.Vertrouwelijkheidaanduiding);
        Assert.Equal(latest.Auteur, result.Auteur);
        Assert.Equal(latest.Status.Value.ToString(), result.Status);
        Assert.Equal(latest.Formaat, result.Formaat);
        Assert.Equal(latest.Taal, result.Taal);
        Assert.Equal(latest.Bestandsnaam, result.Bestandsnaam);
        Assert.Equal(latest.Link, result.Link);
        Assert.Equal(latest.Beschrijving, result.Beschrijving);
        Assert.Equal(latest.OntvangstDatum.Value.ToString("yyyy-MM-dd"), result.OntvangstDatum);
        Assert.Equal(latest.VerzendDatum.Value.ToString("yyyy-MM-dd"), result.VerzendDatum);

        Assert.Equal(latest.Ondertekening_Datum.Value.ToString("yyyy-MM-dd"), result.Ondertekening.Datum);
        Assert.Equal(latest.Ondertekening_Soort.Value.ToString(), result.Ondertekening.Soort);

        Assert.Equal(latest.Integriteit_Algoritme.ToString(), result.Integriteit.Algoritme);
        Assert.Equal(latest.Integriteit_Datum.Value.ToString("yyyy-MM-dd"), result.Integriteit.Datum);
        Assert.Equal(latest.Integriteit_Waarde, result.Integriteit.Waarde);

        Assert.Equal(latest.EnkelvoudigInformatieObject.Locked, result.Locked);
        Assert.Equal(latest.EnkelvoudigInformatieObject.InformatieObjectType, result.InformatieObjectType);
    }

    [Fact]
    public void EnkelvoudigInformatieObject_Inhoud_Maps_To_EnkelvoudiginformatieobjectGetResponse_with_Download_Link()
    {
        // Setup
        _fixture.Customize<EnkelvoudigInformatieObjectVersie>(c => c.With(p => p.Versie, 1));

        var enkelvoudigInformatieObjectVersies = new List<EnkelvoudigInformatieObjectVersie>
        {
            new EnkelvoudigInformatieObjectVersie { Versie = 1, Inhoud = @"202011\cc66cb0b8c6d44f8a12d26ec83a930bb.jpg" },
        };

        _fixture.Customize<EnkelvoudigInformatieObject>(c =>
            c.With(p => p.Id, Guid.Parse("292de971-b3b6-4a25-a7d7-d3030f7b9bc5"))
                .With(p => p.EnkelvoudigInformatieObjectVersies, enkelvoudigInformatieObjectVersies)
        );

        var value = _fixture.Create<EnkelvoudigInformatieObject>();

        value.EnkelvoudigInformatieObjectVersies[0].EnkelvoudigInformatieObject = value;

        // Act
        var result = _mapper.Map<EnkelvoudigInformatieObjectGetResponseDto>(value);

        // Assert
        Assert.Equal("/enkelvoudiginformatieobjecten/292de971-b3b6-4a25-a7d7-d3030f7b9bc5/download?versie=1", result.Inhoud);
    }

    [Fact]
    public void EnkelvoudigInformatieObject_With_Multiple_Versions_Maps_To_EnkelvoudiginformatieobjectGetResponse_With_Latest_Version()
    {
        // Setup
        var enkelvoudigInformatieObjectVersies = new List<EnkelvoudigInformatieObjectVersie>
        {
            new EnkelvoudigInformatieObjectVersie
            {
                Versie = 1,
                Status = Status.in_bewerking,
                Inhoud = @"202011\cc66cb0b8c6d44f8a12d26ec83a930bb.jpg",
            },
            new EnkelvoudigInformatieObjectVersie
            {
                Versie = 3,
                Status = Status.definitief,
                Inhoud = @"202011\0386023f2ece4742ba6536e88b10fe8e.jpg",
            },
            new EnkelvoudigInformatieObjectVersie
            {
                Versie = 2,
                Status = Status.in_bewerking,
                Inhoud = @"202011\0386023f2ece4742ba6536e88b10fe8e.jpg",
            },
        };

        _fixture.Customize<EnkelvoudigInformatieObject>(c =>
            c.With(p => p.Id, Guid.Parse("292de971-b3b6-4a25-a7d7-d3030f7b9bc5"))
                .With(p => p.EnkelvoudigInformatieObjectVersies, enkelvoudigInformatieObjectVersies)
        );

        var value = _fixture.Create<EnkelvoudigInformatieObject>();

        value.EnkelvoudigInformatieObjectVersies.ForEach(e => e.EnkelvoudigInformatieObject = value);

        // Act
        var result = _mapper.Map<EnkelvoudigInformatieObjectGetResponseDto>(value);

        // Assert
        Assert.Equal(3, result.Versie);
    }

    [Fact]
    public void ObjectInformatieObject_Maps_To_ObjectInformatieObjectResponseDto()
    {
        // Setup
        _fixture.Customize<ObjectInformatieObjectResponseDto>(c => c.With(a => a.ObjectType, ObjectType.besluit.ToString()));

        var value = _fixture.Create<ObjectInformatieObject>();

        // Act
        var result = _mapper.Map<ObjectInformatieObjectResponseDto>(value);

        // Assert
        Assert.Equal(value.Object, result.Object);
        Assert.Equal(value.ObjectType.ToString(), result.ObjectType);
        Assert.Equal(value.InformatieObject.Url, result.InformatieObject);
    }

    [Fact]
    public void GebruiksRecht_Maps_To_GebruiksRechtResponseDto()
    {
        // Setup
        var value = _fixture.Create<GebruiksRecht>();

        // Act
        var result = _mapper.Map<GebruiksRechtResponseDto>(value);

        // Assert
        Assert.Equal(value.InformatieObject.Url, result.InformatieObject);
        Assert.Equal(value.OmschrijvingVoorwaarden, result.OmschrijvingVoorwaarden);
        Assert.Equal(value.Startdatum.ToString("yyyy-MM-ddTHH:mm:ssZ"), result.Startdatum);
        Assert.Equal(value.Einddatum.Value.ToString("yyyy-MM-ddTHH:mm:ssZ"), result.Einddatum);
    }
}
