using System;
using AutoFixture;
using MapsterMapper;
using OneGround.ZGW.Common.DataModel;
using OneGround.ZGW.Documenten.Contracts.v1;
using OneGround.ZGW.Documenten.Contracts.v1.Queries;
using OneGround.ZGW.Documenten.Contracts.v1.Requests;
using OneGround.ZGW.Documenten.DataModel;
using OneGround.ZGW.Documenten.Web.Models.v1;
using Xunit;

namespace OneGround.ZGW.Documenten.WebApi.UnitTests.MappingTests;

public class RequestToDomainProfileTests : IDisposable
{
    private readonly OmitOnRecursionFixture _fixture = new OmitOnRecursionFixture();
    private readonly DrcMapperTestHost _host = new DrcMapperTestHost();
    private readonly IMapper _mapper;

    public RequestToDomainProfileTests()
    {
        _mapper = _host.Mapper;
    }

    public void Dispose() => _host.Dispose();

    [Fact]
    public void GetAllEnkelvoudigInformatieObjectenQueryParameters_Maps_To_GetAllEnkelvoudiginformatieobjectenFilter()
    {
        // Setup
        _fixture.Customize<GetAllEnkelvoudigInformatieObjectenQueryParameters>(c =>
            c.With(p => p.Identificatie, "DOC-2020-0000001").With(p => p.Bronorganisatie, "999990561")
        );
        var value = _fixture.Create<GetAllEnkelvoudigInformatieObjectenQueryParameters>();

        // Act
        var result = _mapper.Map<GetAllEnkelvoudigInformatieObjectenFilter>(value);

        // Assert
        Assert.Equal(value.Identificatie, result.Identificatie);
        Assert.Equal(value.Bronorganisatie, result.Bronorganisatie);
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
