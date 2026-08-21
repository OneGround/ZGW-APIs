using System;
using MapsterMapper;
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
}
