using System;
using System.Collections.Generic;
using MapsterMapper;
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
