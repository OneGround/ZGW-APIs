using System;
using System.Collections.Generic;
using System.Linq;
using MapsterMapper;
using OneGround.ZGW.Common.DataModel;
using OneGround.ZGW.Documenten.Contracts.v1._7.Requests;
using OneGround.ZGW.Documenten.Contracts.v1._7.Responses;
using OneGround.ZGW.Documenten.DataModel;
using Xunit;

namespace OneGround.ZGW.Documenten.WebApi.UnitTests.MappingTests.v1_7;

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

    private static EnkelvoudigInformatieObject InformatieObjectWithLatestVersion(
        List<BestandsDeel> bestandsDelen = null,
        bool isGereedVoorPublicatie = true,
        bool tonenAanInitiator = true
    )
    {
        var informatieObject = new EnkelvoudigInformatieObject
        {
            Id = Guid.NewGuid(),
            InformatieObjectType = "https://example.test/informatieobjecttypen/1",
            IndicatieGebruiksrecht = true,
            Locked = true,
            Lock = "the-lock-value",
        };

        var latestVersion = new EnkelvoudigInformatieObjectVersie
        {
            Id = Guid.NewGuid(),
            Versie = 7,
            Bronorganisatie = TestBronorganisatie,
            Identificatie = "DOCUMENT-0001",
            CreatieDatum = new DateOnly(2026, 1, 15),
            Titel = "een titel",
            Auteur = "een auteur",
            Taal = "nld",
            IsGereedVoorPublicatie = isGereedVoorPublicatie,
            TonenAanInitiator = tonenAanInitiator,
            LatestInformatieObject = informatieObject,
            BestandsDelen = bestandsDelen ?? [],
        };

        informatieObject.LatestEnkelvoudigInformatieObjectVersie = latestVersion;
        return informatieObject;
    }

    /// <summary>
    /// The ported after-mapping copies the latest version's fields onto the response and resolves the
    /// download link through the injected uri service. Asserting the RESOLVED (absolute) url is what
    /// gives this teeth: the DTO has a same-named Url that Mapster convention-copies from the entity's
    /// relative one, so comparing against entity.Url would pass with the resolver rule deleted.
    /// </summary>
    [Fact]
    public void A_get_response_carries_the_latest_versions_fields_and_a_resolved_download_link()
    {
        var informatieObject = InformatieObjectWithLatestVersion();
        var latestVersion = informatieObject.LatestEnkelvoudigInformatieObjectVersie;

        var dto = _mapper.Map<EnkelvoudigInformatieObjectGetResponseDto>(informatieObject);

        Assert.Equal(DrcMapperTestHost.Resolved(informatieObject), dto.Url);
        Assert.Equal(7, dto.Versie);
        Assert.Equal("DOCUMENT-0001", dto.Identificatie);
        // DateOnly on the entity, string on the DTO: a convention copy cannot satisfy this.
        Assert.Equal("2026-01-15", dto.CreatieDatum);
        Assert.Equal(DrcMapperTestHost.Resolved(latestVersion), dto.Inhoud);
        Assert.True(dto.Locked);
        Assert.Equal("https://example.test/informatieobjecttypen/1", dto.InformatieObjectType);
    }

    /// <summary>The two members v1.7 adds over v1.5, both routed through the ported after-mapping.</summary>
    [Fact]
    public void A_get_response_carries_the_v1_7_publication_flags()
    {
        var informatieObject = InformatieObjectWithLatestVersion(isGereedVoorPublicatie: false, tonenAanInitiator: false);

        var dto = _mapper.Map<EnkelvoudigInformatieObjectGetResponseDto>(informatieObject);

        Assert.False(dto.IsGereedVoorPublicatie);
        Assert.False(dto.TonenAanInitiator);
    }

    /// <summary>
    /// When the version is still being uploaded in parts, the download link must be suppressed rather
    /// than pointing at an incomplete file.
    /// </summary>
    [Fact]
    public void A_get_response_suppresses_the_download_link_while_parts_are_outstanding()
    {
        var informatieObject = InformatieObjectWithLatestVersion(
            bestandsDelen:
            [
                new BestandsDeel
                {
                    Id = Guid.NewGuid(),
                    Volgnummer = 1,
                    Omvang = 10,
                },
            ]
        );
        informatieObject.LatestEnkelvoudigInformatieObjectVersie.BestandsDelen[0].EnkelvoudigInformatieObjectVersie =
            informatieObject.LatestEnkelvoudigInformatieObjectVersie;

        var dto = _mapper.Map<EnkelvoudigInformatieObjectGetResponseDto>(informatieObject);

        Assert.Null(dto.Inhoud);
        var bestandsDeel = Assert.Single(dto.BestandsDelen);
        Assert.Equal(1, bestandsDeel.Volgnummer);
        Assert.Equal("the-lock-value", bestandsDeel.Lock);
    }

    /// <summary>The download-link port's suppressing branch: no content and nothing uploaded yet.</summary>
    [Fact]
    public void A_create_response_suppresses_the_download_link_for_an_empty_version()
    {
        var versie = VersieFor(inhoud: null, bestandsomvang: 0);

        var dto = _mapper.Map<EnkelvoudigInformatieObjectCreateResponseDto>(versie);

        Assert.Null(dto.Inhoud);
    }

    /// <summary>The download-link port's resolving branch, at the same two call sites.</summary>
    [Fact]
    public void A_create_response_resolves_the_download_link_for_a_populated_version()
    {
        var versie = VersieFor(inhoud: "the-content", bestandsomvang: 11);

        var dto = _mapper.Map<EnkelvoudigInformatieObjectCreateResponseDto>(versie);

        Assert.Equal(DrcMapperTestHost.Resolved(versie), dto.Inhoud);
        Assert.Equal(DrcMapperTestHost.Resolved(versie.InformatieObject), dto.Url);
        Assert.Equal("the-lock-value", dto.Lock);
    }

    [Fact]
    public void An_update_response_resolves_the_download_link_for_a_populated_version()
    {
        var versie = VersieFor(inhoud: "the-content", bestandsomvang: 11);

        var dto = _mapper.Map<EnkelvoudigInformatieObjectUpdateResponseDto>(versie);

        Assert.Equal(DrcMapperTestHost.Resolved(versie), dto.Inhoud);
        Assert.Equal(DrcMapperTestHost.Resolved(versie.InformatieObject), dto.Url);
    }

    /// <summary>
    /// The map the PATCH merge reads: an existing entity projected back onto its update-request DTO.
    /// The Lock member is deliberately NOT carried -- the request's own lock value has to be validated,
    /// not the stored one -- so asserting it stays null is a real requirement, not an omission.
    /// </summary>
    [Fact]
    public void An_update_request_projection_carries_the_latest_version_but_not_the_lock()
    {
        var informatieObject = InformatieObjectWithLatestVersion();

        var dto = _mapper.Map<EnkelvoudigInformatieObjectUpdateRequestDto>(informatieObject);

        Assert.Equal("DOCUMENT-0001", dto.Identificatie);
        Assert.Equal(TestBronorganisatie, dto.Bronorganisatie);
        Assert.Equal("2026-01-15", dto.CreatieDatum);
        Assert.True(dto.IsGereedVoorPublicatie);
        Assert.True(dto.TonenAanInitiator);
        Assert.Null(dto.Lock);
    }

    /// <summary>
    /// Full projection of <c>MapLatestVersieToGetResponse</c>: one assertion per member that ported body
    /// assigns.
    /// </summary>
    /// <remarks>
    /// This fact exists because neither Mapster gate can see these members. Every one of them is
    /// <c>.Ignore(...)</c>'d on the register's config -- which is what satisfies the completeness gate --
    /// and then assigned inside an <c>.AfterMapping</c> the compile gate never looks into. Deleting an
    /// assignment from that body therefore leaves both gates and the rest of the suite green. The A/B
    /// parity harness would have caught it, but it cannot outlive the AutoMapper profiles it compares
    /// against, so this fact is the durable replacement. Every value is deliberately non-default so a
    /// dropped or wrong-source assignment cannot pass by coincidence.
    ///
    /// Three members -- InformatieObjectType, IndicatieGebruiksrecht and Locked -- are read from
    /// <c>latestVersion.LatestInformatieObject</c>, which in any valid graph IS the root entity the map
    /// runs on, so Mapster's convention copy and the after-mapping assignment necessarily agree. For
    /// those three this fact pins the value, not the source; splitting them would take a model-invalid
    /// fixture.
    /// </remarks>
    [Fact]
    public void A_get_response_carries_every_member_the_after_mapping_assigns()
    {
        var informatieObject = FullyPopulatedInformatieObject();
        var latestVersion = informatieObject.LatestEnkelvoudigInformatieObjectVersie;

        var dto = _mapper.Map<EnkelvoudigInformatieObjectGetResponseDto>(informatieObject);

        Assert.Equal(DrcMapperTestHost.Resolved(informatieObject), dto.Url);
        Assert.Equal(7, dto.Versie);
        Assert.Equal(TestBronorganisatie, dto.Bronorganisatie);
        Assert.Equal("DOCUMENT-0042", dto.Identificatie);
        Assert.Equal(4096, dto.Bestandsomvang);
        Assert.Equal("2026-01-15T10:30:00Z", dto.BeginRegistratie);
        Assert.Equal("2026-01-15", dto.CreatieDatum);
        Assert.Equal("een titel", dto.Titel);
        Assert.Equal("zaakvertrouwelijk", dto.Vertrouwelijkheidaanduiding);
        Assert.True(dto.IsGereedVoorPublicatie);
        Assert.True(dto.TonenAanInitiator);
        Assert.Equal("een auteur", dto.Auteur);
        Assert.Equal("ter_vaststelling", dto.Status);
        Assert.Equal("application/pdf", dto.Formaat);
        Assert.Equal("nld", dto.Taal);
        Assert.Equal("bestand.pdf", dto.Bestandsnaam);
        Assert.Equal("https://example.test/link", dto.Link);
        Assert.Equal("een beschrijving", dto.Beschrijving);
        Assert.Equal("2026-01-16", dto.OntvangstDatum);
        Assert.Equal("2026-01-17", dto.VerzendDatum);
        Assert.Equal("digitaal", dto.Ondertekening.Soort);
        Assert.Equal("2026-01-18", dto.Ondertekening.Datum);
        Assert.Equal("sha_256", dto.Integriteit.Algoritme);
        Assert.Equal("de-integriteitswaarde", dto.Integriteit.Waarde);
        Assert.Equal("2026-01-19", dto.Integriteit.Datum);
        Assert.Equal("https://example.test/informatieobjecttypen/42", dto.InformatieObjectType);
        Assert.True(dto.IndicatieGebruiksrecht);
        Assert.True(dto.Locked);
        Assert.Equal("digitaal", dto.Verschijningsvorm);
        Assert.Equal(["vergunning", "bouwtekening"], dto.Trefwoorden);
        Assert.True(dto.InhoudIsVervallen);

        // Parts are outstanding, so the download link is suppressed rather than pointing at a
        // half-uploaded file. The parts themselves are projected in Volgnummer order -- the fixture
        // deliberately holds them the other way round, so a lost OrderBy fails here.
        Assert.Null(dto.Inhoud);
        Assert.Equal([1, 2], dto.BestandsDelen.Select(d => d.Volgnummer));
        Assert.Equal(DrcMapperTestHost.Resolved(latestVersion.BestandsDelen[1]), dto.BestandsDelen[0].Url);
        Assert.Equal(1024, dto.BestandsDelen[0].Omvang);
        Assert.True(dto.BestandsDelen[0].Voltooid);
        Assert.False(dto.BestandsDelen[1].Voltooid);
        Assert.Equal("the-lock-value", dto.BestandsDelen[0].Lock);
    }

    /// <summary>
    /// Full projection of <c>MapLatestVersieToUpdateRequest</c>: one assertion per member that ported
    /// body assigns, plus the member it deliberately does not.
    /// </summary>
    /// <remarks>
    /// Same blind spot as the get-response fact above: every member here is <c>.Ignore(...)</c>'d for the
    /// completeness gate and assigned inside an <c>.AfterMapping</c> the compile gate cannot see, so
    /// without this fact a dropped assignment fails nothing. Lock is the one member the body must NOT
    /// carry -- the request's own lock value has to be validated, not the stored one -- so asserting it
    /// stays null is a requirement rather than an omission.
    /// </remarks>
    [Fact]
    public void An_update_request_projection_carries_every_member_the_after_mapping_assigns()
    {
        var informatieObject = FullyPopulatedInformatieObject();

        var dto = _mapper.Map<EnkelvoudigInformatieObjectUpdateRequestDto>(informatieObject);

        Assert.Equal(TestBronorganisatie, dto.Bronorganisatie);
        Assert.Equal("DOCUMENT-0042", dto.Identificatie);
        Assert.Equal("2026-01-15", dto.CreatieDatum);
        Assert.Equal("een titel", dto.Titel);
        Assert.Equal("zaakvertrouwelijk", dto.Vertrouwelijkheidaanduiding);
        Assert.True(dto.IsGereedVoorPublicatie);
        Assert.True(dto.TonenAanInitiator);
        Assert.Equal("een auteur", dto.Auteur);
        Assert.Equal("ter_vaststelling", dto.Status);
        Assert.Equal("application/pdf", dto.Formaat);
        Assert.Equal("nld", dto.Taal);
        Assert.Equal("bestand.pdf", dto.Bestandsnaam);
        Assert.Equal(4096, dto.Bestandsomvang);
        // The raw stored content, NOT a download url: this map feeds the PATCH merge, not a response.
        Assert.Equal("de-inhoud", dto.Inhoud);
        Assert.Equal("https://example.test/link", dto.Link);
        Assert.Equal("een beschrijving", dto.Beschrijving);
        Assert.Equal("2026-01-16", dto.OntvangstDatum);
        Assert.Equal("2026-01-17", dto.VerzendDatum);
        Assert.Equal("digitaal", dto.Ondertekening.Soort);
        Assert.Equal("2026-01-18", dto.Ondertekening.Datum);
        Assert.Equal("sha_256", dto.Integriteit.Algoritme);
        Assert.Equal("de-integriteitswaarde", dto.Integriteit.Waarde);
        Assert.Equal("2026-01-19", dto.Integriteit.Datum);
        Assert.Equal("https://example.test/informatieobjecttypen/42", dto.InformatieObjectType);
        Assert.True(dto.IndicatieGebruiksrecht);
        Assert.Equal("digitaal", dto.Verschijningsvorm);
        Assert.Equal(["vergunning", "bouwtekening"], dto.Trefwoorden);
        Assert.True(dto.InhoudIsVervallen);
        Assert.Null(dto.Lock);
    }

    /// <summary>
    /// The download-link port's third disjunct (<c>src.BestandsDelen.Count != 0</c>) at its OWN call
    /// site. The version has content and a non-zero size, so only that disjunct can suppress the link.
    /// </summary>
    /// <remarks>
    /// The get-response body reaches the same outcome through a different expression, so covering it
    /// there leaves this branch untested: losing it would hand a client a download url for an incomplete
    /// multipart upload. Like the two facts above, the member is <c>.Ignore(...)</c>'d for the gate and
    /// assigned only inside an <c>.AfterMapping</c>, so neither gate can catch its removal.
    /// </remarks>
    [Fact]
    public void A_create_response_suppresses_the_download_link_while_parts_are_outstanding()
    {
        var versie = VersieFor(inhoud: "de-inhoud", bestandsomvang: 4096);
        versie.BestandsDelen =
        [
            new BestandsDeel
            {
                Id = Guid.NewGuid(),
                Volgnummer = 1,
                Omvang = 4096,
                EnkelvoudigInformatieObjectVersie = versie,
            },
        ];

        var dto = _mapper.Map<EnkelvoudigInformatieObjectCreateResponseDto>(versie);

        Assert.Null(dto.Inhoud);
    }

    /// <summary>
    /// A document whose latest version carries a distinctive, non-default value in every member the two
    /// ported after-mapping bodies assign.
    /// </summary>
    private static EnkelvoudigInformatieObject FullyPopulatedInformatieObject()
    {
        var informatieObject = new EnkelvoudigInformatieObject
        {
            Id = Guid.NewGuid(),
            InformatieObjectType = "https://example.test/informatieobjecttypen/42",
            IndicatieGebruiksrecht = true,
            Locked = true,
            Lock = "the-lock-value",
        };

        var latestVersion = new EnkelvoudigInformatieObjectVersie
        {
            Id = Guid.NewGuid(),
            Versie = 7,
            Bronorganisatie = TestBronorganisatie,
            Identificatie = "DOCUMENT-0042",
            CreatieDatum = new DateOnly(2026, 1, 15),
            Titel = "een titel",
            Vertrouwelijkheidaanduiding = VertrouwelijkheidAanduiding.zaakvertrouwelijk,
            Auteur = "een auteur",
            Status = DataModel.Status.ter_vaststelling,
            Formaat = "application/pdf",
            Taal = "nld",
            BeginRegistratie = new DateTime(2026, 1, 15, 10, 30, 0, DateTimeKind.Utc),
            Bestandsnaam = "bestand.pdf",
            Inhoud = "de-inhoud",
            Bestandsomvang = 4096,
            Link = "https://example.test/link",
            Beschrijving = "een beschrijving",
            OntvangstDatum = new DateOnly(2026, 1, 16),
            VerzendDatum = new DateOnly(2026, 1, 17),
            Ondertekening_Soort = Soort.digitaal,
            Ondertekening_Datum = new DateOnly(2026, 1, 18),
            Integriteit_Algoritme = Algoritme.sha_256,
            Integriteit_Waarde = "de-integriteitswaarde",
            Integriteit_Datum = new DateOnly(2026, 1, 19),
            Verschijningsvorm = "digitaal",
            Trefwoorden = ["vergunning", "bouwtekening"],
            InhoudIsVervallen = true,
            IsGereedVoorPublicatie = true,
            TonenAanInitiator = true,
            LatestInformatieObject = informatieObject,
        };

        // Held in descending Volgnummer so the get-response body's OrderBy is load-bearing.
        latestVersion.BestandsDelen =
        [
            new BestandsDeel
            {
                Id = Guid.NewGuid(),
                Volgnummer = 2,
                Omvang = 3072,
                Voltooid = false,
                EnkelvoudigInformatieObjectVersie = latestVersion,
            },
            new BestandsDeel
            {
                Id = Guid.NewGuid(),
                Volgnummer = 1,
                Omvang = 1024,
                Voltooid = true,
                EnkelvoudigInformatieObjectVersie = latestVersion,
            },
        ];

        informatieObject.LatestEnkelvoudigInformatieObjectVersie = latestVersion;
        return informatieObject;
    }

    private static EnkelvoudigInformatieObjectVersie VersieFor(string inhoud, long bestandsomvang)
    {
        var informatieObject = new EnkelvoudigInformatieObject
        {
            Id = Guid.NewGuid(),
            InformatieObjectType = "https://example.test/informatieobjecttypen/1",
            IndicatieGebruiksrecht = true,
            Locked = true,
            Lock = "the-lock-value",
        };

        return new EnkelvoudigInformatieObjectVersie
        {
            Id = Guid.NewGuid(),
            Versie = 7,
            Bronorganisatie = TestBronorganisatie,
            Identificatie = "DOCUMENT-0001",
            CreatieDatum = new DateOnly(2026, 1, 15),
            Inhoud = inhoud,
            Bestandsomvang = bestandsomvang,
            InformatieObject = informatieObject,
            LatestInformatieObject = informatieObject,
            BestandsDelen = [],
        };
    }
}
