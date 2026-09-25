using System;
using System.Globalization;
using AutoFixture;
using MapsterMapper;
using Newtonsoft.Json.Linq;
using OneGround.ZGW.Common.Contracts.v1.AuditTrail;
using OneGround.ZGW.Common.DataModel;
using OneGround.ZGW.Common.Helpers;
using OneGround.ZGW.DataAccess.AuditTrail;
using OneGround.ZGW.Documenten.Contracts.v1.Requests;
using OneGround.ZGW.Documenten.Contracts.v1.Responses;
using OneGround.ZGW.Documenten.DataModel;
using Xunit;

namespace OneGround.ZGW.Documenten.WebApi.UnitTests.MappingTests;

public class DomainToResponseProfileTests : IDisposable
{
    private readonly OmitOnRecursionFixture _fixture = new OmitOnRecursionFixture();
    private readonly DrcMapperTestHost _host = new DrcMapperTestHost();
    private readonly IMapper _mapper;

    public DomainToResponseProfileTests()
    {
        _fixture.Register<DateOnly>(() => DateOnly.FromDateTime(DateTime.UtcNow));
        _fixture.Register<DateTime>(() => DateTime.UtcNow);

        _mapper = _host.Mapper;
    }

    public void Dispose() => _host.Dispose();

    [Fact]
    public void ObjectInformatieObject_Maps_To_ObjectInformatieObjectResponseDto()
    {
        _fixture.Customize<ObjectInformatieObjectResponseDto>(c => c.With(a => a.ObjectType, ObjectType.besluit.ToString()));

        var value = _fixture.Create<ObjectInformatieObject>();

        var result = _mapper.Map<ObjectInformatieObjectResponseDto>(value);

        Assert.Equal(DrcMapperTestHost.Resolved(value), result.Url);
        Assert.Equal(value.Object, result.Object);
        Assert.Equal(value.ObjectType.ToString(), result.ObjectType);
        Assert.Equal(DrcMapperTestHost.Resolved(value.InformatieObject), result.InformatieObject);
    }

    [Fact]
    public void GebruiksRecht_Maps_To_GebruiksRechtResponseDto()
    {
        var value = _fixture.Create<GebruiksRecht>();

        var result = _mapper.Map<GebruiksRechtResponseDto>(value);

        Assert.Equal(DrcMapperTestHost.Resolved(value), result.Url);
        Assert.Equal(DrcMapperTestHost.Resolved(value.InformatieObject), result.InformatieObject);
        Assert.Equal(value.OmschrijvingVoorwaarden, result.OmschrijvingVoorwaarden);
        Assert.Equal(value.Startdatum.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture), result.Startdatum);
        Assert.Equal(value.Einddatum.Value.ToString("yyyy-MM-ddTHH:mm:ssZ"), result.Einddatum);
    }

    [Fact]
    public void GebruiksRecht_Maps_To_GebruiksRechtRequestDto_for_PATCH_merge()
    {
        var value = _fixture.Create<GebruiksRecht>();

        var result = _mapper.Map<GebruiksRechtRequestDto>(value);

        Assert.Equal(DrcMapperTestHost.Resolved(value.InformatieObject), result.InformatieObject);
        Assert.Equal(value.OmschrijvingVoorwaarden, result.OmschrijvingVoorwaarden);
        Assert.Equal(value.Startdatum.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture), result.Startdatum);
        Assert.Equal(value.Einddatum.Value.ToString("yyyy-MM-ddTHH:mm:ssZ"), result.Einddatum);
    }

    [Fact]
    public void AuditTrailRegel_Maps_Wijzigingen_Json_Shape()
    {
        // Pin Oud/Nieuw to valid JSON explicitly. This exercises ConvertWijzigingenToDto's real
        // JsonConvert.DeserializeObject call -- a broken port would either throw during mapping or
        // leave Wijzigingen.Oud/.Nieuw null.
        var value = new AuditTrailRegel
        {
            Id = Guid.NewGuid(),
            Bron = "DRC",
            ApplicatieId = "app-1",
            ApplicatieWeergave = "App 1",
            GebruikersId = "user-1",
            GebruikersWeergave = "User 1",
            Actie = "update",
            ActieWeergave = "Update",
            HoofdObject = "/enkelvoudiginformatieobjecten/1",
            Resource = "enkelvoudiginformatieobject",
            ResourceUrl = "/enkelvoudiginformatieobjecten/1",
            Toelichting = "toelichting",
            ResourceWeergave = "Resource 1",
            AanmaakDatum = new DateTime(2024, 4, 1, 12, 0, 0, DateTimeKind.Utc),
            Oud = "{\"naam\":\"oud-waarde\"}",
            Nieuw = "{\"naam\":\"nieuw-waarde\"}",
        };

        var result = _mapper.Map<AuditTrailRegelDto>(value);

        Assert.Equal(value.Id.ToString(), result.Uuid);
        Assert.Equal(ProfileHelper.StringDateFromDateTime(value.AanmaakDatum, true), result.AanmaakDatum);
        Assert.NotNull(result.Wijzigingen);
        Assert.IsType<JObject>(result.Wijzigingen.Oud);
        Assert.IsType<JObject>(result.Wijzigingen.Nieuw);
        Assert.Equal("oud-waarde", ((JObject)result.Wijzigingen.Oud)["naam"]!.ToString());
        Assert.Equal("nieuw-waarde", ((JObject)result.Wijzigingen.Nieuw)["naam"]!.ToString());
    }

    [Fact]
    public void AuditTrailRegel_Maps_Wijzigingen_To_Null_When_Oud_And_Nieuw_Are_Empty()
    {
        var value = new AuditTrailRegel
        {
            Id = Guid.NewGuid(),
            Bron = "DRC",
            ApplicatieId = "app-1",
            ApplicatieWeergave = "App 1",
            GebruikersId = "user-1",
            GebruikersWeergave = "User 1",
            Actie = "create",
            ActieWeergave = "Create",
            HoofdObject = "/enkelvoudiginformatieobjecten/1",
            Resource = "enkelvoudiginformatieobject",
            ResourceUrl = "/enkelvoudiginformatieobjecten/1",
            Toelichting = "toelichting",
            ResourceWeergave = "Resource 1",
            AanmaakDatum = new DateTime(2024, 4, 2, 12, 0, 0, DateTimeKind.Utc),
            Oud = null,
            Nieuw = "",
        };

        var result = _mapper.Map<AuditTrailRegelDto>(value);

        Assert.NotNull(result.Wijzigingen);
        Assert.Null(result.Wijzigingen.Oud);
        Assert.Null(result.Wijzigingen.Nieuw);
    }
}
