using System;
using System.Collections.Generic;
using MapsterMapper;
using OneGround.ZGW.Common.DataModel;
using OneGround.ZGW.Documenten.Contracts.v1;
using OneGround.ZGW.Documenten.Contracts.v1._7.Queries;
using OneGround.ZGW.Documenten.Contracts.v1._7.Requests;
using OneGround.ZGW.Documenten.DataModel;
using OneGround.ZGW.Documenten.Web.Models.v1._7;
using Xunit;

namespace OneGround.ZGW.Documenten.WebApi.UnitTests.MappingTests.v1_7;

public class RequestToDomainProfileTests : IDisposable
{
    // Official RvIG test BSN, reused here purely as a safe, non-real 9-digit placeholder for
    // Bronorganisatie -- never assigned to a real person or organisation.
    private const string TestBronorganisatie = "999993653";

    private readonly DrcMapperTestHost _host = new DrcMapperTestHost();
    private readonly IMapper _mapper;

    public RequestToDomainProfileTests()
    {
        _mapper = _host.Mapper;
    }

    public void Dispose() => _host.Dispose();

    /// <summary>
    /// A null Trefwoorden must produce a null filter, NOT an empty list: the query layer reads null as
    /// "no filter" and an empty list as "match nothing", so folding it would silently return zero rows.
    /// Runs on the real seam config, where the global empty-collection transform is active -- a
    /// hand-rolled config cannot tell the two apart.
    /// </summary>
    [Fact]
    public void Query_parameters_with_no_trefwoorden_leave_the_filter_null()
    {
        var queryParameters = new GetAllEnkelvoudigInformatieObjectenQueryParameters { Trefwoorden = null };

        var filter = _mapper.Map<GetAllEnkelvoudigInformatieObjectenFilter>(queryParameters);

        Assert.Null(filter.Trefwoorden_In);
        // Uuid_In is not present on these query parameters at all (only the search-request DTO has it),
        // so it must stay null here too -- same silent-zero-rows risk class as Trefwoorden_In.
        Assert.Null(filter.Uuid_In);
    }

    [Fact]
    public void Query_parameters_with_trefwoorden_split_them_into_the_filter()
    {
        var queryParameters = new GetAllEnkelvoudigInformatieObjectenQueryParameters { Trefwoorden = "een,twee" };

        var filter = _mapper.Map<GetAllEnkelvoudigInformatieObjectenFilter>(queryParameters);

        Assert.Equal(["een", "twee"], filter.Trefwoorden_In);
    }

    /// <summary>Both resets on the search map, which v1.5's equivalent does not have for Uuid_In.</summary>
    [Fact]
    public void Search_request_with_no_trefwoorden_and_no_uuids_leaves_both_filters_null()
    {
        var searchRequest = new EnkelvoudigInformatieObjectSearchRequestDto { Trefwoorden = null, Uuid_In = null };

        var filter = _mapper.Map<GetAllEnkelvoudigInformatieObjectenFilter>(searchRequest);

        Assert.Null(filter.Trefwoorden_In);
        Assert.Null(filter.Uuid_In);
    }

    /// <summary>
    /// The comprehensive counterpart to v1.1's/v1.5's identically-named fact: every scalar and nested
    /// member the create map carries onto EnkelvoudigInformatieObjectVersie in one request, including
    /// the two members added to this DTO since 1.5 -- IsGereedVoorPublicatie and TonenAanInitiator --
    /// which carry through on Mapster's name/type convention alone (no explicit .Map in the register).
    /// </summary>
    [Fact]
    public void EnkelvoudigInformatieObjectCreateRequestDto_Maps_To_EnkelvoudigInformatieObjectVersie()
    {
        var value = new EnkelvoudigInformatieObjectCreateRequestDto
        {
            Identificatie = "DOC-2020-0000001",
            Bronorganisatie = TestBronorganisatie,
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
            IsGereedVoorPublicatie = true,
            TonenAanInitiator = true,
            InhoudIsVervallen = true,
        };

        var result = _mapper.Map<EnkelvoudigInformatieObjectVersie>(value);

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
        Assert.Equal(value.IsGereedVoorPublicatie, result.IsGereedVoorPublicatie);
        Assert.Equal(value.TonenAanInitiator, result.TonenAanInitiator);
        Assert.Equal(value.InhoudIsVervallen, result.InhoudIsVervallen);
        // Create-map InformatieObject: InformatieObjectType is TrimEnd('/')'d - source has no trailing slash here,
        // dedicated trim-behavior test above proves the trimming itself.
        Assert.Equal(value.InformatieObjectType, result.InformatieObject.InformatieObjectType);
    }

    /// <summary>
    /// Ondertekening and Integriteit carry no [Required] attribute, so a real request may omit them.
    /// The source selectors compile to expression trees that cannot use ?., so each needs an explicit
    /// guard; AlgoritmeFromString additionally throws on a null argument by design, so its whole call
    /// must be skipped rather than its argument guarded.
    /// </summary>
    [Fact]
    public void A_create_request_without_ondertekening_or_integriteit_maps_without_throwing()
    {
        var request = new EnkelvoudigInformatieObjectCreateRequestDto
        {
            Bronorganisatie = TestBronorganisatie,
            InformatieObjectType = "https://example.test/informatieobjecttypen/1/",
            CreatieDatum = "2026-01-15",
            Taal = "nl",
            Ondertekening = null,
            Integriteit = null,
        };

        var versie = _mapper.Map<EnkelvoudigInformatieObjectVersie>(request);

        Assert.Null(versie.Ondertekening_Datum);
        Assert.Null(versie.Ondertekening_Soort);
        Assert.Null(versie.Integriteit_Datum);
        Assert.Null(versie.Integriteit_Waarde);
        Assert.Equal(new DateOnly(2026, 1, 15), versie.CreatieDatum);
        // Convert2letterTo3Letter ran: a convention copy would leave "nl".
        Assert.Equal("nld", versie.Taal);
    }

    /// <summary>
    /// InformatieObject is assigned in .AfterMapping to keep the cyclic entity graph out of Mapster's
    /// compiler, so this asserts the assignment still happens -- and that the trailing slash is trimmed.
    /// </summary>
    [Fact]
    public void A_create_request_builds_the_informatieobject_with_a_trimmed_type_url()
    {
        var request = new EnkelvoudigInformatieObjectCreateRequestDto
        {
            Bronorganisatie = TestBronorganisatie,
            InformatieObjectType = "https://example.test/informatieobjecttypen/1/",
            IndicatieGebruiksrecht = true,
            // Taal is otherwise required on the wire (enforced by FluentValidation before mapping ever
            // runs) and unrelated to what this fact checks -- set here purely so Convert2letterTo3Letter
            // has a non-null argument.
            Taal = "nl",
        };

        var versie = _mapper.Map<EnkelvoudigInformatieObjectVersie>(request);

        Assert.NotNull(versie.InformatieObject);
        Assert.Equal("https://example.test/informatieobjecttypen/1", versie.InformatieObject.InformatieObjectType);
        Assert.True(versie.InformatieObject.IndicatieGebruiksrecht);
    }

    /// <summary>
    /// The update map carries Lock onto the InformatieObject it builds; the create map does not.
    /// </summary>
    [Fact]
    public void An_update_request_carries_the_lock_onto_the_informatieobject()
    {
        var request = new EnkelvoudigInformatieObjectUpdateRequestDto
        {
            Bronorganisatie = TestBronorganisatie,
            InformatieObjectType = "https://example.test/informatieobjecttypen/1",
            Lock = "the-lock-value",
            // Taal is otherwise required on the wire (enforced by FluentValidation before mapping ever
            // runs) and unrelated to what this fact checks -- set here purely so Convert2letterTo3Letter
            // has a non-null argument.
            Taal = "nl",
        };

        var versie = _mapper.Map<EnkelvoudigInformatieObjectVersie>(request);

        Assert.Equal("the-lock-value", versie.InformatieObject.Lock);
    }

    /// <summary>
    /// Same guard as the create-map fact above, for the update map's five Ondertekening/Integriteit
    /// members. Correct by inspection is not tested: without this fact, removing any one of the update
    /// map's null guards would fail nothing here.
    /// </summary>
    [Fact]
    public void An_update_request_without_ondertekening_or_integriteit_maps_without_throwing()
    {
        var request = new EnkelvoudigInformatieObjectUpdateRequestDto
        {
            Bronorganisatie = TestBronorganisatie,
            InformatieObjectType = "https://example.test/informatieobjecttypen/1",
            CreatieDatum = "2026-01-15",
            Taal = "nl",
            Ondertekening = null,
            Integriteit = null,
        };

        var versie = _mapper.Map<EnkelvoudigInformatieObjectVersie>(request);

        Assert.Null(versie.Ondertekening_Datum);
        Assert.Null(versie.Ondertekening_Soort);
        Assert.Null(versie.Integriteit_Datum);
        Assert.Null(versie.Integriteit_Waarde);
        Assert.Equal(new DateOnly(2026, 1, 15), versie.CreatieDatum);
    }

    /// <summary>
    /// Five members v1.5 handles differently: IsGereedVoorPublicatie, TonenAanInitiator and
    /// InhoudIsVervallen carry through on Mapster's name/type convention alone (no explicit .Map or
    /// .Ignore here), while Trefwoorden and Verschijningsvorm get an explicit identity .Map. Neither
    /// Mapster gate would catch a later .Ignore(...) added to align this register with v1.5's -- the
    /// completeness gate accepts an .Ignore as readily as a mapped member, and the compile gate only
    /// checks that the config compiles. This fact is what would actually fail.
    /// </summary>
    [Fact]
    public void A_create_request_carries_the_five_members_v1_5_ignores_or_maps_explicitly()
    {
        var request = new EnkelvoudigInformatieObjectCreateRequestDto
        {
            Bronorganisatie = TestBronorganisatie,
            InformatieObjectType = "https://example.test/informatieobjecttypen/1",
            Taal = "nl",
            IsGereedVoorPublicatie = true,
            TonenAanInitiator = true,
            InhoudIsVervallen = true,
            Trefwoorden = new List<string> { "vergunning" },
            Verschijningsvorm = "digitaal",
        };

        var versie = _mapper.Map<EnkelvoudigInformatieObjectVersie>(request);

        Assert.True(versie.IsGereedVoorPublicatie);
        Assert.True(versie.TonenAanInitiator);
        Assert.True(versie.InhoudIsVervallen);
        Assert.Equal(["vergunning"], versie.Trefwoorden);
        Assert.Equal("digitaal", versie.Verschijningsvorm);
    }
}
