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
    // Official RvIG test BSN, reused here purely as a safe, non-real 9-digit placeholder for
    // Bronorganisatie -- never assigned to a real person or organisation.
    private const string TestBronorganisatie = "999993653";

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
    public void EnkelvoudigInformatieObject_Maps_To_GetResponseDto_via_AfterMapping_with_DI_resolved_Inhoud()
    {
        // Covers the MapLatestEnkelvoudigInformatieObjectVersieResponse port: EnkelvoudigInformatieObjectGetResponseDto
        // is populated entirely from src.LatestEnkelvoudigInformatieObjectVersie (and its own LatestInformatieObject)
        // inside .AfterMapping, since every one of these members is .Ignore()-d in the main config.
        var latestVersion = new EnkelvoudigInformatieObjectVersie
        {
            Id = Guid.NewGuid(),
            Versie = 3,
            Bronorganisatie = TestBronorganisatie,
            Identificatie = "DOC-0001",
            Bestandsomvang = 4096,
            BeginRegistratie = new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Utc),
            CreatieDatum = new DateOnly(2024, 1, 1),
            Titel = "Titel-1",
            Vertrouwelijkheidaanduiding = VertrouwelijkheidAanduiding.openbaar,
            Auteur = "Auteur-1",
            Status = Status.definitief,
            Formaat = "application/pdf",
            Taal = "dut",
            Bestandsnaam = "bestand.pdf",
            Link = "https://example.test/link",
            Beschrijving = "Beschrijving-1",
            OntvangstDatum = new DateOnly(2024, 1, 3),
            VerzendDatum = new DateOnly(2024, 1, 4),
            Ondertekening_Datum = new DateOnly(2024, 1, 5),
            Ondertekening_Soort = Soort.digitaal,
            Integriteit_Algoritme = Algoritme.sha_256,
            Integriteit_Datum = new DateOnly(2024, 1, 6),
            Integriteit_Waarde = "abc123",
        };

        var value = new EnkelvoudigInformatieObject
        {
            Id = Guid.NewGuid(),
            InformatieObjectType = "https://example.test/informatieobjecttypen/1",
            IndicatieGebruiksrecht = true,
            Locked = true,
            EnkelvoudigInformatieObjectVersies = [latestVersion],
            LatestEnkelvoudigInformatieObjectVersie = latestVersion,
        };

        // Establish bidirectional relationships (as the repository would when loading the aggregate)
        latestVersion.InformatieObject = value;
        latestVersion.LatestInformatieObject = value;

        // Pin a mock return value for THIS specific version instance, distinguishable from the host's
        // default prefixing stub: this proves MapLatestVersieToGetResponse actually calls
        // uriService.GetUri(latestVersion), not that Inhoud coincidentally matches a resolved Url.
        _host.UriService.Setup(s => s.GetUri(latestVersion)).Returns("MOCKED-INHOUD-URL");

        var result = _mapper.Map<EnkelvoudigInformatieObjectGetResponseDto>(value);

        Assert.Equal(DrcMapperTestHost.Resolved(value), result.Url);
        Assert.Equal(latestVersion.Versie, result.Versie);
        Assert.Equal(latestVersion.Bronorganisatie, result.Bronorganisatie);
        Assert.Equal(latestVersion.Identificatie, result.Identificatie);
        Assert.Equal(latestVersion.Bestandsomvang, result.Bestandsomvang);
        Assert.Equal(
            latestVersion.BeginRegistratie.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture),
            result.BeginRegistratie
        );
        Assert.Equal(latestVersion.CreatieDatum.Value.ToString("yyyy-MM-dd"), result.CreatieDatum);
        Assert.Equal(latestVersion.Titel, result.Titel);
        Assert.Equal(latestVersion.Vertrouwelijkheidaanduiding.Value.ToString(), result.Vertrouwelijkheidaanduiding);
        Assert.Equal(latestVersion.Auteur, result.Auteur);
        Assert.Equal(latestVersion.Status.Value.ToString(), result.Status);
        Assert.Equal(latestVersion.Formaat, result.Formaat);
        Assert.Equal(latestVersion.Taal, result.Taal);
        Assert.Equal(latestVersion.Bestandsnaam, result.Bestandsnaam);
        Assert.Equal(latestVersion.Link, result.Link);
        Assert.Equal(latestVersion.Beschrijving, result.Beschrijving);
        Assert.Equal(latestVersion.OntvangstDatum.Value.ToString("yyyy-MM-dd"), result.OntvangstDatum);
        Assert.Equal(latestVersion.VerzendDatum.Value.ToString("yyyy-MM-dd"), result.VerzendDatum);
        Assert.Equal(latestVersion.Ondertekening_Datum.Value.ToString("yyyy-MM-dd"), result.Ondertekening.Datum);
        Assert.Equal(latestVersion.Ondertekening_Soort.Value.ToString(), result.Ondertekening.Soort);
        Assert.Equal(latestVersion.Integriteit_Algoritme.ToString(), result.Integriteit.Algoritme);
        Assert.Equal(latestVersion.Integriteit_Datum.Value.ToString("yyyy-MM-dd"), result.Integriteit.Datum);
        Assert.Equal(latestVersion.Integriteit_Waarde, result.Integriteit.Waarde);
        Assert.Equal(value.InformatieObjectType, result.InformatieObjectType);
        Assert.Equal(value.IndicatieGebruiksrecht, result.IndicatieGebruiksrecht);
        Assert.Equal(value.Locked, result.Locked);

        // The DI-resolved value -- proves the ported IMappingAction's uriService.GetUri(latestVersion)
        // call actually ran through MapContext.Current.GetService<IEntityUriService>().
        Assert.Equal("MOCKED-INHOUD-URL", result.Inhoud);
    }

    [Fact]
    public void EnkelvoudigInformatieObject_Maps_To_UpdateRequestDto_via_AfterMapping_without_DI()
    {
        // Covers the MapLatestEnkelvoudigInformatieObjectVersieRequest port: this AfterMapping has no DI
        // dependency at all, and (unlike the GetResponseDto port above) copies Inhoud verbatim from the
        // domain field rather than resolving it through IEntityUriService.
        var latestVersion = new EnkelvoudigInformatieObjectVersie
        {
            Id = Guid.NewGuid(),
            Versie = 5,
            Bronorganisatie = TestBronorganisatie,
            Identificatie = "DOC-0002",
            CreatieDatum = new DateOnly(2024, 2, 1),
            Titel = "Titel-2",
            Vertrouwelijkheidaanduiding = VertrouwelijkheidAanduiding.intern,
            Auteur = "Auteur-2",
            Status = Status.in_bewerking,
            Formaat = "application/xml",
            Taal = "dut",
            Bestandsnaam = "bestand2.xml",
            Inhoud = @"202401\11111111111111111111111111111111.xml",
            Link = "https://example.test/link2",
            Beschrijving = "Beschrijving-2",
            OntvangstDatum = new DateOnly(2024, 2, 2),
            VerzendDatum = new DateOnly(2024, 2, 3),
        };

        var value = new EnkelvoudigInformatieObject
        {
            Id = Guid.NewGuid(),
            InformatieObjectType = "https://example.test/informatieobjecttypen/2",
            IndicatieGebruiksrecht = false,
            Lock = "existing-lock-token",
            EnkelvoudigInformatieObjectVersies = [latestVersion],
            LatestEnkelvoudigInformatieObjectVersie = latestVersion,
        };

        latestVersion.InformatieObject = value;
        latestVersion.LatestInformatieObject = value;

        var result = _mapper.Map<EnkelvoudigInformatieObjectUpdateRequestDto>(value);

        Assert.Equal(latestVersion.Bronorganisatie, result.Bronorganisatie);
        Assert.Equal(latestVersion.Identificatie, result.Identificatie);
        Assert.Equal(latestVersion.CreatieDatum.Value.ToString("yyyy-MM-dd"), result.CreatieDatum);
        Assert.Equal(latestVersion.Titel, result.Titel);
        Assert.Equal(latestVersion.Vertrouwelijkheidaanduiding.Value.ToString(), result.Vertrouwelijkheidaanduiding);
        Assert.Equal(latestVersion.Auteur, result.Auteur);
        Assert.Equal(latestVersion.Status.Value.ToString(), result.Status);
        Assert.Equal(latestVersion.Formaat, result.Formaat);
        Assert.Equal(latestVersion.Taal, result.Taal);
        Assert.Equal(latestVersion.Bestandsnaam, result.Bestandsnaam);
        // Copied straight from the domain field -- NOT resolved through IEntityUriService (contrast with
        // the GetResponseDto port above).
        Assert.Equal(latestVersion.Inhoud, result.Inhoud);
        Assert.Equal(latestVersion.Link, result.Link);
        Assert.Equal(latestVersion.Beschrijving, result.Beschrijving);
        Assert.Equal(latestVersion.OntvangstDatum.Value.ToString("yyyy-MM-dd"), result.OntvangstDatum);
        Assert.Equal(latestVersion.VerzendDatum.Value.ToString("yyyy-MM-dd"), result.VerzendDatum);
        Assert.Equal(value.InformatieObjectType, result.InformatieObjectType);
        Assert.Equal(value.IndicatieGebruiksrecht, result.IndicatieGebruiksrecht);

        // Deliberately NOT merged: the caller must validate the Lock value from the incoming request,
        // not the value currently stored on the entity.
        Assert.Null(result.Lock);
    }

    [Fact]
    public void EnkelvoudigInformatieObjectVersie_Maps_To_CreateResponseDto_with_dates_url_and_optional_dtos()
    {
        var informatieObject = new EnkelvoudigInformatieObject
        {
            Id = Guid.NewGuid(),
            InformatieObjectType = "https://example.test/informatieobjecttypen/3",
            IndicatieGebruiksrecht = true,
            Locked = false,
        };

        var value = new EnkelvoudigInformatieObjectVersie
        {
            Id = Guid.NewGuid(),
            Versie = 1,
            CreatieDatum = new DateOnly(2024, 3, 1),
            OntvangstDatum = new DateOnly(2024, 3, 2),
            BeginRegistratie = new DateTime(2024, 3, 3, 4, 5, 6, DateTimeKind.Utc),
            VerzendDatum = new DateOnly(2024, 3, 4),
            Ondertekening_Datum = new DateOnly(2024, 3, 5),
            Ondertekening_Soort = Soort.analoog,
            Integriteit_Algoritme = Algoritme.md5,
            Integriteit_Datum = new DateOnly(2024, 3, 6),
            Integriteit_Waarde = "integriteit-waarde",
            InformatieObject = informatieObject,
        };

        var result = _mapper.Map<EnkelvoudigInformatieObjectCreateResponseDto>(value);

        Assert.Equal(DrcMapperTestHost.Resolved(informatieObject), result.Url);
        Assert.Equal(value.CreatieDatum.Value.ToString("yyyy-MM-dd"), result.CreatieDatum);
        Assert.Equal(value.OntvangstDatum.Value.ToString("yyyy-MM-dd"), result.OntvangstDatum);
        Assert.Equal(
            value.BeginRegistratie.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture),
            result.BeginRegistratie
        );
        Assert.Equal(value.VerzendDatum.Value.ToString("yyyy-MM-dd"), result.VerzendDatum);
        // EnkelvoudigInformatieObjectVersieMapperHelper.CreateOptionalOndertekeningDto/CreateOptionalIntegriteitDto
        // (an already Mapster-agnostic, untouched helper) populated these from the version's fields.
        Assert.Equal(value.Ondertekening_Datum.Value.ToString("yyyy-MM-dd"), result.Ondertekening.Datum);
        Assert.Equal(value.Ondertekening_Soort.Value.ToString(), result.Ondertekening.Soort);
        Assert.Equal(value.Integriteit_Algoritme.ToString(), result.Integriteit.Algoritme);
        Assert.Equal(value.Integriteit_Datum.Value.ToString("yyyy-MM-dd"), result.Integriteit.Datum);
        Assert.Equal(value.Integriteit_Waarde, result.Integriteit.Waarde);
        Assert.Equal(informatieObject.IndicatieGebruiksrecht, result.IndicatieGebruiksrecht);
        Assert.Equal(informatieObject.Locked, result.Locked);
        Assert.Equal(DrcMapperTestHost.Resolved(value), result.Inhoud);
        Assert.Equal(informatieObject.InformatieObjectType, result.InformatieObjectType);
    }

    [Fact]
    public void EnkelvoudigInformatieObjectVersie_Maps_To_UpdateResponseDto_includes_Lock_and_default_optional_dtos_when_empty()
    {
        var informatieObject = new EnkelvoudigInformatieObject
        {
            Id = Guid.NewGuid(),
            InformatieObjectType = "https://example.test/informatieobjecttypen/4",
            IndicatieGebruiksrecht = false,
            Locked = true,
            Lock = "lock-token-4",
        };

        // Ondertekening/Integriteit fields left at their defaults -- CreateOptionalOndertekeningDto/
        // CreateOptionalIntegriteitDto(..., createDefaultWhenEmpty: true) must still return a non-null,
        // empty DTO (not null) for the Create/Update response maps.
        var value = new EnkelvoudigInformatieObjectVersie
        {
            Id = Guid.NewGuid(),
            Versie = 2,
            InformatieObject = informatieObject,
        };

        var result = _mapper.Map<EnkelvoudigInformatieObjectUpdateResponseDto>(value);

        Assert.Equal(DrcMapperTestHost.Resolved(informatieObject), result.Url);
        Assert.Equal(informatieObject.Lock, result.Lock);
        Assert.Equal(informatieObject.Locked, result.Locked);
        Assert.Equal(informatieObject.InformatieObjectType, result.InformatieObjectType);
        Assert.NotNull(result.Ondertekening);
        Assert.NotNull(result.Integriteit);
    }

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
