using System;
using System.Globalization;
using MapsterMapper;
using OneGround.ZGW.Common.DataModel;
using OneGround.ZGW.Documenten.Contracts.v1._5;
using OneGround.ZGW.Documenten.Contracts.v1._5.Requests;
using OneGround.ZGW.Documenten.Contracts.v1._5.Responses;
using OneGround.ZGW.Documenten.DataModel;
using OneGround.ZGW.Documenten.WebApi.UnitTests.MappingTests;
using Xunit;

namespace OneGround.ZGW.Documenten.WebApi.UnitTests.MappingTests.v1_5;

public class DomainToResponseProfileTests : IDisposable
{
    // Official RvIG test BSN, reused here purely as a safe, non-real 9-digit placeholder for
    // Bronorganisatie -- never assigned to a real person or organisation.
    private const string TestBronorganisatie = "999993653";

    private readonly DrcMapperTestHost _host = new DrcMapperTestHost();
    private readonly IMapper _mapper;

    public DomainToResponseProfileTests()
    {
        _mapper = _host.Mapper;
    }

    public void Dispose() => _host.Dispose();

    private static EnkelvoudigInformatieObjectVersie CreateVersion()
    {
        return new EnkelvoudigInformatieObjectVersie
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
            Inhoud = @"202401\11111111111111111111111111111111.pdf",
            Link = "https://example.test/link",
            Beschrijving = "Beschrijving-1",
            OntvangstDatum = new DateOnly(2024, 1, 3),
            VerzendDatum = new DateOnly(2024, 1, 4),
            Ondertekening_Datum = new DateOnly(2024, 1, 5),
            Ondertekening_Soort = Soort.digitaal,
            Integriteit_Algoritme = Algoritme.sha_256,
            Integriteit_Datum = new DateOnly(2024, 1, 6),
            Integriteit_Waarde = "abc123",
            Verschijningsvorm = "digitaal",
            Trefwoorden = ["een", "twee"],
            InhoudIsVervallen = true,
        };
    }

    [Fact]
    public void EnkelvoudigInformatieObject_Maps_To_GetResponseDto_via_AfterMapping_with_DI_resolved_Inhoud()
    {
        // Covers the v1.5 MapLatestEnkelvoudigInformatieObjectVersieResponse port: EnkelvoudigInformatieObjectGetResponseDto
        // is populated entirely from src.LatestEnkelvoudigInformatieObjectVersie (and its own LatestInformatieObject)
        // inside .AfterMapping, since every one of these members is .Ignore()-d in the main config.
        var latestVersion = CreateVersion();

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

        // v1.5-specific fields, absent from the v1.1 sibling -- copied straight from the latest version.
        Assert.Equal(latestVersion.Verschijningsvorm, result.Verschijningsvorm);
        Assert.Equal(latestVersion.Trefwoorden, result.Trefwoorden);
        Assert.Equal(latestVersion.InhoudIsVervallen, result.InhoudIsVervallen);

        // The DI-resolved value -- proves the ported IMappingAction's uriService.GetUri(latestVersion)
        // call actually ran through MapContext.Current.GetService<IEntityUriService>(), and that
        // BestandsDelen.Count == 0 took the "resolve via uriService" branch (not the null branch).
        Assert.Equal("MOCKED-INHOUD-URL", result.Inhoud);
    }

    [Fact]
    public void EnkelvoudigInformatieObject_Maps_To_GetResponseDto_Inhoud_Is_Null_When_BestandsDelen_Present()
    {
        // Covers the "Note: New in v1.1" guard, still present verbatim in v1.5's MapLatestEnkelvoudigInformatieObjectVersieResponse:
        // when BestandsDelen.Count != 0, Inhoud must be null regardless of what the (mocked) uriService would otherwise return.
        var latestVersion = CreateVersion();

        var value = new EnkelvoudigInformatieObject
        {
            Id = Guid.NewGuid(),
            InformatieObjectType = "https://example.test/informatieobjecttypen/1b",
            IndicatieGebruiksrecht = true,
            Locked = true,
            Lock = "bestandsdeel-lock-token",
            EnkelvoudigInformatieObjectVersies = [latestVersion],
            LatestEnkelvoudigInformatieObjectVersie = latestVersion,
        };

        latestVersion.InformatieObject = value;
        latestVersion.LatestInformatieObject = value;
        latestVersion.BestandsDelen =
        [
            new BestandsDeel
            {
                Id = Guid.NewGuid(),
                Volgnummer = 2,
                Omvang = 100,
                Voltooid = true,
                EnkelvoudigInformatieObjectVersie = latestVersion,
            },
            new BestandsDeel
            {
                Id = Guid.NewGuid(),
                Volgnummer = 1,
                Omvang = 50,
                Voltooid = false,
                EnkelvoudigInformatieObjectVersie = latestVersion,
            },
        ];

        // Even though the uriService would happily resolve a URL for this version, BestandsDelen
        // being present must force Inhoud to null.
        _host.UriService.Setup(s => s.GetUri(latestVersion)).Returns("SHOULD-NOT-BE-USED");

        var result = _mapper.Map<EnkelvoudigInformatieObjectGetResponseDto>(value);

        Assert.Null(result.Inhoud);

        // Also verify BestandsDelen are mapped and ordered by Volgnummer.
        Assert.Equal(2, result.BestandsDelen.Count);
        Assert.Equal(1, result.BestandsDelen[0].Volgnummer);
        Assert.Equal(2, result.BestandsDelen[1].Volgnummer);
        Assert.Equal(50, result.BestandsDelen[0].Omvang);
        Assert.True(result.BestandsDelen[0].Voltooid == false);
        Assert.Equal(value.Lock, result.BestandsDelen[0].Lock);
    }

    [Fact]
    public void EnkelvoudigInformatieObject_Maps_To_UpdateRequestDto_via_AfterMapping_without_DI()
    {
        // Covers the v1.5 MapLatestEnkelvoudigInformatieObjectVersieRequest port: this AfterMapping has no DI
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
            Bestandsomvang = 128,
            Inhoud = @"202401\11111111111111111111111111111111.xml",
            Link = "https://example.test/link2",
            Beschrijving = "Beschrijving-2",
            OntvangstDatum = new DateOnly(2024, 2, 2),
            VerzendDatum = new DateOnly(2024, 2, 3),
            Verschijningsvorm = "papier",
            Trefwoorden = ["drie"],
            InhoudIsVervallen = true,
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
        Assert.Equal(latestVersion.Bestandsomvang, result.Bestandsomvang);
        // Copied straight from the domain field -- NOT resolved through IEntityUriService (contrast with
        // the GetResponseDto port above).
        Assert.Equal(latestVersion.Inhoud, result.Inhoud);
        Assert.Equal(latestVersion.Link, result.Link);
        Assert.Equal(latestVersion.Beschrijving, result.Beschrijving);
        Assert.Equal(latestVersion.OntvangstDatum.Value.ToString("yyyy-MM-dd"), result.OntvangstDatum);
        Assert.Equal(latestVersion.VerzendDatum.Value.ToString("yyyy-MM-dd"), result.VerzendDatum);
        Assert.Equal(value.InformatieObjectType, result.InformatieObjectType);
        Assert.Equal(value.IndicatieGebruiksrecht, result.IndicatieGebruiksrecht);

        // v1.5-specific fields, absent from the v1.1 sibling -- copied straight from the latest version.
        Assert.Equal(latestVersion.Verschijningsvorm, result.Verschijningsvorm);
        Assert.Equal(latestVersion.Trefwoorden, result.Trefwoorden);
        Assert.Equal(latestVersion.InhoudIsVervallen, result.InhoudIsVervallen);

        // Deliberately NOT merged: the caller must validate the Lock value from the incoming request,
        // not the value currently stored on the entity.
        Assert.Null(result.Lock);
    }

    [Theory]
    [InlineData("", 0L, 0, "null-because-empty-and-zero-size")]
    [InlineData("", 5L, 0, "resolved-because-nonzero-bestandsomvang")]
    [InlineData(@"202401\some-file.bin", 0L, 0, "resolved-because-nonempty-inhoud")]
    public void EnkelvoudigInformatieObjectVersie_Maps_To_CreateResponseDto_MapDownloadLink_Branches(
        string inhoud,
        long bestandsomvang,
        int bestandsDelenCount,
        string scenario
    )
    {
        // Covers all real branches of the ported MapDownloadLink conditional (identical to v1.1's), applied to the Create-response config.
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
            Inhoud = inhoud,
            Bestandsomvang = bestandsomvang,
            CreatieDatum = new DateOnly(2024, 3, 1),
            OntvangstDatum = new DateOnly(2024, 3, 2),
            BeginRegistratie = new DateTime(2024, 3, 3, 4, 5, 6, DateTimeKind.Utc),
            VerzendDatum = new DateOnly(2024, 3, 4),
            InformatieObject = informatieObject,
        };

        if (bestandsDelenCount != 0)
        {
            value.BestandsDelen =
            [
                new BestandsDeel
                {
                    Id = Guid.NewGuid(),
                    Volgnummer = 1,
                    EnkelvoudigInformatieObjectVersie = value,
                },
            ];
        }

        _host.UriService.Setup(s => s.GetUri(value)).Returns("MOCKED-CREATE-INHOUD-URL");

        var result = _mapper.Map<EnkelvoudigInformatieObjectCreateResponseDto>(value);

        // Empty Inhoud AND zero Bestandsomvang -> null; otherwise (and no BestandsDelen) -> DI-resolved.
        var expectNull = string.IsNullOrEmpty(inhoud) && bestandsomvang == 0;
        if (expectNull)
        {
            Assert.StartsWith("null", scenario);
            Assert.Null(result.Inhoud);
        }
        else
        {
            Assert.StartsWith("resolved", scenario);
            Assert.Equal("MOCKED-CREATE-INHOUD-URL", result.Inhoud);
        }

        Assert.Equal(DrcMapperTestHost.Resolved(informatieObject), result.Url);
        Assert.Equal(informatieObject.InformatieObjectType, result.InformatieObjectType);
    }

    [Fact]
    public void EnkelvoudigInformatieObjectVersie_Maps_To_CreateResponseDto_Inhoud_Is_Null_When_BestandsDelen_Present()
    {
        // Even with a non-empty Inhoud and non-zero Bestandsomvang, BestandsDelen.Count != 0 must force null.
        var informatieObject = new EnkelvoudigInformatieObject
        {
            Id = Guid.NewGuid(),
            InformatieObjectType = "https://example.test/informatieobjecttypen/3b",
            IndicatieGebruiksrecht = true,
            Locked = false,
        };

        var value = new EnkelvoudigInformatieObjectVersie
        {
            Id = Guid.NewGuid(),
            Versie = 1,
            Inhoud = @"202401\present.bin",
            Bestandsomvang = 999,
            CreatieDatum = new DateOnly(2024, 3, 1),
            InformatieObject = informatieObject,
        };
        value.BestandsDelen =
        [
            new BestandsDeel
            {
                Id = Guid.NewGuid(),
                Volgnummer = 1,
                EnkelvoudigInformatieObjectVersie = value,
            },
        ];

        _host.UriService.Setup(s => s.GetUri(value)).Returns("SHOULD-NOT-BE-USED");

        var result = _mapper.Map<EnkelvoudigInformatieObjectCreateResponseDto>(value);

        Assert.Null(result.Inhoud);
    }

    [Fact]
    public void EnkelvoudigInformatieObjectVersie_Maps_To_UpdateResponseDto_MapDownloadLink_Resolved_Case_And_Includes_Lock()
    {
        // Covers MapDownloadLink applied to the Update-response config (confirming both configs invoke it),
        // plus the Lock/Locked/optional-DTO-defaults behavior specific to the Update-response DTO.
        var informatieObject = new EnkelvoudigInformatieObject
        {
            Id = Guid.NewGuid(),
            InformatieObjectType = "https://example.test/informatieobjecttypen/4",
            IndicatieGebruiksrecht = false,
            Locked = true,
            Lock = "lock-token-4",
        };

        var value = new EnkelvoudigInformatieObjectVersie
        {
            Id = Guid.NewGuid(),
            Versie = 2,
            Inhoud = @"202401\present-update.bin",
            Bestandsomvang = 256,
            InformatieObject = informatieObject,
        };

        _host.UriService.Setup(s => s.GetUri(value)).Returns("MOCKED-UPDATE-INHOUD-URL");

        var result = _mapper.Map<EnkelvoudigInformatieObjectUpdateResponseDto>(value);

        Assert.Equal(DrcMapperTestHost.Resolved(informatieObject), result.Url);
        Assert.Equal(informatieObject.Lock, result.Lock);
        Assert.Equal(informatieObject.Locked, result.Locked);
        Assert.Equal(informatieObject.InformatieObjectType, result.InformatieObjectType);
        Assert.Equal("MOCKED-UPDATE-INHOUD-URL", result.Inhoud);
        // Ondertekening/Integriteit fields left at their defaults -- CreateOptionalOndertekeningDto/
        // CreateOptionalIntegriteitDto(..., createDefaultWhenEmpty: true) must still return a non-null,
        // empty DTO (not null) for the Create/Update response maps.
        Assert.NotNull(result.Ondertekening);
        Assert.NotNull(result.Integriteit);
    }

    [Fact]
    public void EnkelvoudigInformatieObjectVersie_Maps_To_UpdateResponseDto_MapDownloadLink_Null_Case_When_Empty()
    {
        // Empty Inhoud AND Bestandsomvang == 0 on the Update-response config -> Inhoud must be null.
        var informatieObject = new EnkelvoudigInformatieObject
        {
            Id = Guid.NewGuid(),
            InformatieObjectType = "https://example.test/informatieobjecttypen/4b",
            IndicatieGebruiksrecht = false,
            Locked = true,
            Lock = "lock-token-4b",
        };

        var value = new EnkelvoudigInformatieObjectVersie
        {
            Id = Guid.NewGuid(),
            Versie = 2,
            InformatieObject = informatieObject,
        };

        _host.UriService.Setup(s => s.GetUri(value)).Returns("SHOULD-NOT-BE-USED");

        var result = _mapper.Map<EnkelvoudigInformatieObjectUpdateResponseDto>(value);

        Assert.Null(result.Inhoud);
    }

    [Fact]
    public void Verzending_Maps_To_VerzendingResponseDto_Defaults_Null_Correspondence_Addresses_To_Empty_Dtos()
    {
        // Covers the Verzending -> VerzendingResponseDto ??= defaults: when the domain entity's three
        // correspondence-address sub-objects are null, the response DTO must still get non-null, empty DTOs.
        var value = new Verzending
        {
            Id = Guid.NewGuid(),
            Betrokkene = "Betrokkene-1",
            AardRelatie = DataModel.AardRelatie.afzender,
            Toelichting = "Toelichting-1",
            Contactpersoon = "Contactpersoon-1",
            BinnenlandsCorrespondentieAdres = null,
            BuitenlandsCorrespondentieAdres = null,
            CorrespondentiePostadres = null,
        };

        var result = _mapper.Map<VerzendingResponseDto>(value);

        Assert.Equal(DrcMapperTestHost.Resolved(value), result.Url);
        Assert.Equal(value.Betrokkene, result.Betrokkene);
        Assert.Equal(value.AardRelatie.ToString(), result.AardRelatie);
        Assert.Equal(value.Toelichting, result.Toelichting);
        Assert.Equal(value.Contactpersoon, result.Contactpersoon);

        // The discriminating assertion: null sub-objects on the domain entity must be defaulted to
        // non-null, empty DTOs (NOT left null) -- this is the ??= behavior under test. The DTOs'
        // own field defaults are the empty string (see e.g. BinnenlandsCorrespondentieAdresDto),
        // not null, so we assert against those actual defaults rather than null.
        Assert.NotNull(result.BinnenlandsCorrespondentieAdres);
        Assert.NotNull(result.BuitenlandsCorrespondentieAdres);
        Assert.NotNull(result.CorrespondentiePostadres);
        Assert.Equal(string.Empty, result.BinnenlandsCorrespondentieAdres.NaamOpenbareRuimte);
        Assert.Equal(string.Empty, result.BuitenlandsCorrespondentieAdres.AdresBuitenland1);
        Assert.Equal(0, result.CorrespondentiePostadres.PostbusOfAntwoordnummer);
    }

    [Fact]
    public void Verzending_Maps_To_VerzendingResponseDto_Preserves_Non_Null_Correspondence_Addresses()
    {
        // Counterpart to the defaults test above: when the sub-objects ARE present, the ??= must be a
        // no-op and the actual mapped values must come through untouched.
        var value = new Verzending
        {
            Id = Guid.NewGuid(),
            Betrokkene = "Betrokkene-2",
            AardRelatie = DataModel.AardRelatie.geadresseerde,
            Toelichting = "Toelichting-2",
            Contactpersoon = "Contactpersoon-2",
            Ontvangstdatum = new DateOnly(2024, 4, 1),
            Verzenddatum = new DateOnly(2024, 4, 2),
            Faxnummer = "0201234567",
            EmailAdres = "test@example.test",
            MijnOverheid = true,
            Telefoonnummer = "0209876543",
            BinnenlandsCorrespondentieAdres = new BinnenlandsCorrespondentieAdres
            {
                Huisletter = "A",
                Huisnummer = 12,
                HuisnummerToevoeging = "bis",
                NaamOpenbareRuimte = "Teststraat",
                Postcode = "1234AB",
                WoonplaatsNaam = "Testdorp",
            },
            BuitenlandsCorrespondentieAdres = new BuitenlandsCorrespondentieAdres
            {
                AdresBuitenland1 = "Adres 1",
                AdresBuitenland2 = "Adres 2",
                AdresBuitenland3 = "Adres 3",
                LandPostadres = "Landcode",
            },
            CorrespondentiePostadres = new CorrespondentiePostadres
            {
                PostbusOfAntwoordnummer = 42,
                PostadresPostcode = "5678CD",
                PostadresType = PostadresType.postbusnummer,
                WoonplaatsNaam = "Testdorp",
            },
        };

        var result = _mapper.Map<VerzendingResponseDto>(value);

        Assert.Equal(value.Ontvangstdatum.Value.ToString("yyyy-MM-dd"), result.OntvangstDatum);
        Assert.Equal(value.Verzenddatum.Value.ToString("yyyy-MM-dd"), result.Verzenddatum);
        Assert.Equal(value.Faxnummer, result.Faxnummer);
        Assert.Equal(value.EmailAdres, result.EmailAdres);
        Assert.Equal(value.MijnOverheid, result.MijnOverheid);
        Assert.Equal(value.Telefoonnummer, result.Telefoonnummer);

        Assert.Equal(value.BinnenlandsCorrespondentieAdres.Huisletter, result.BinnenlandsCorrespondentieAdres.Huisletter);
        Assert.Equal(value.BinnenlandsCorrespondentieAdres.Huisnummer, result.BinnenlandsCorrespondentieAdres.Huisnummer);
        Assert.Equal(value.BinnenlandsCorrespondentieAdres.HuisnummerToevoeging, result.BinnenlandsCorrespondentieAdres.HuisnummerToevoeging);
        Assert.Equal(value.BinnenlandsCorrespondentieAdres.NaamOpenbareRuimte, result.BinnenlandsCorrespondentieAdres.NaamOpenbareRuimte);
        Assert.Equal(value.BinnenlandsCorrespondentieAdres.Postcode, result.BinnenlandsCorrespondentieAdres.Postcode);
        Assert.Equal(value.BinnenlandsCorrespondentieAdres.WoonplaatsNaam, result.BinnenlandsCorrespondentieAdres.WoonplaatsNaam);

        Assert.Equal(value.BuitenlandsCorrespondentieAdres.AdresBuitenland1, result.BuitenlandsCorrespondentieAdres.AdresBuitenland1);
        Assert.Equal(value.BuitenlandsCorrespondentieAdres.AdresBuitenland2, result.BuitenlandsCorrespondentieAdres.AdresBuitenland2);
        Assert.Equal(value.BuitenlandsCorrespondentieAdres.AdresBuitenland3, result.BuitenlandsCorrespondentieAdres.AdresBuitenland3);
        Assert.Equal(value.BuitenlandsCorrespondentieAdres.LandPostadres, result.BuitenlandsCorrespondentieAdres.LandPostadres);

        Assert.Equal(value.CorrespondentiePostadres.PostbusOfAntwoordnummer, result.CorrespondentiePostadres.PostbusOfAntwoordnummer);
        Assert.Equal(value.CorrespondentiePostadres.PostadresPostcode, result.CorrespondentiePostadres.PostadresPostcode);
        Assert.Equal(value.CorrespondentiePostadres.PostadresType.ToString(), result.CorrespondentiePostadres.PostadresType);
        Assert.Equal(value.CorrespondentiePostadres.WoonplaatsNaam, result.CorrespondentiePostadres.WoonplaatsNaam);
    }

    [Fact]
    public void Verzending_Maps_To_VerzendingRequestDto_For_Patch_Merge()
    {
        // Covers the PATCH-merge map: Verzending -> VerzendingRequestDto, purely mechanical, no ??= defaults
        // (that behavior is specific to the Response DTO's AfterMapping above).
        var informatieObject = new EnkelvoudigInformatieObject { Id = Guid.NewGuid() };

        var value = new Verzending
        {
            Id = Guid.NewGuid(),
            Betrokkene = "Betrokkene-3",
            AardRelatie = DataModel.AardRelatie.afzender,
            Toelichting = "Toelichting-3",
            Contactpersoon = "Contactpersoon-3",
            Ontvangstdatum = new DateOnly(2024, 5, 1),
            Verzenddatum = new DateOnly(2024, 5, 2),
            Faxnummer = "0301234567",
            EmailAdres = "patch@example.test",
            MijnOverheid = false,
            Telefoonnummer = "0309876543",
            InformatieObject = informatieObject,
        };

        var result = _mapper.Map<VerzendingRequestDto>(value);

        Assert.Equal(DrcMapperTestHost.Resolved(informatieObject), result.InformatieObject);
        Assert.Equal(value.Betrokkene, result.Betrokkene);
        Assert.Equal(value.AardRelatie.ToString(), result.AardRelatie);
        Assert.Equal(value.Toelichting, result.Toelichting);
        Assert.Equal(value.Ontvangstdatum.Value.ToString("yyyy-MM-dd"), result.OntvangstDatum);
        Assert.Equal(value.Verzenddatum.Value.ToString("yyyy-MM-dd"), result.Verzenddatum);
        Assert.Equal(value.Contactpersoon, result.Contactpersoon);
        Assert.Equal(value.Faxnummer, result.Faxnummer);
        Assert.Equal(value.EmailAdres, result.EmailAdres);
        Assert.Equal(value.MijnOverheid, result.MijnOverheid);
        Assert.Equal(value.Telefoonnummer, result.Telefoonnummer);
    }
}
