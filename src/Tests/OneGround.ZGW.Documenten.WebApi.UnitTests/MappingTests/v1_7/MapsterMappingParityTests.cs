using System;
using System.Collections.Generic;
using AutoMapper;
using AutoMapper.Internal;
using Newtonsoft.Json;
using OneGround.ZGW.Common.DataModel;
using OneGround.ZGW.Common.Web;
using OneGround.ZGW.Common.Web.Mapping.ValueResolvers;
using OneGround.ZGW.Documenten.Contracts.v1;
using OneGround.ZGW.Documenten.Contracts.v1._7.Queries;
using OneGround.ZGW.Documenten.Contracts.v1._7.Requests;
using OneGround.ZGW.Documenten.Contracts.v1._7.Responses;
using OneGround.ZGW.Documenten.DataModel;
using OneGround.ZGW.Documenten.Web.MappingProfiles.v1._7;
using OneGround.ZGW.Documenten.Web.Models.v1._7;
using Xunit;
using AutoMapperIMapper = AutoMapper.IMapper;
using MapsterIMapper = MapsterMapper.IMapper;

namespace OneGround.ZGW.Documenten.WebApi.UnitTests.MappingTests.v1_7;

/// <summary>
/// TEMPORARY. Maps identical input through the v1.7 AutoMapper profiles and the v1.7 Mapster registers
/// and asserts byte-identical serialized output. Deleted together with the profiles once its result is
/// recorded, because it cannot compile without them.
/// </summary>
/// <remarks>
/// v1.7 is the last DRC version where both implementations still exist in the tree, so this comparison
/// is possible exactly once. The earlier versions (v1, v1.1, v1.5) had a harness like this one; it went
/// away with their profiles, so their registers ship on the permanent register tests, the two Mapster
/// gates and the collection-root fact alone.
///
/// Both sides are configured the way production configures them, not the way a hand-rolled test config
/// would:
/// <list type="bullet">
/// <item>The Mapster side comes from <see cref="DrcMapperTestHost"/>, which calls the real
/// <c>AddZgwMapster</c> seam. That is what puts <c>MaxDepth(200)</c>,
/// <c>DestinationTransform.EmptyCollectionIfNull</c>, <c>NameMatchingStrategy.IgnoreCase</c> and
/// <c>RegisterNullableEnumRule()</c> in play, and it cannot drift from the seam.</item>
/// <item>The AutoMapper side mirrors <c>AddAutoMapper</c>'s two non-default settings —
/// <c>ShouldMapMethod = _ => false</c> and the inserted <c>NullableEnumMapper</c> — but constructs the
/// two v1.7 profiles explicitly rather than scanning the assembly, which would also pull in v1's
/// profiles and change what is under comparison.</item>
/// <item>Both mappers share ONE mocked <see cref="OneGround.ZGW.Common.Web.Services.UriServices.IEntityUriService"/>
/// instance, so every url comparison is a comparison of the mapping and not of two different mocks.</item>
/// </list>
/// </remarks>
public class MapsterMappingParityTests : IDisposable
{
    // Official RvIG test BSN, reused here purely as a safe, non-real 9-digit placeholder for
    // Bronorganisatie -- never assigned to a real person or organisation.
    private const string TestBronorganisatie = "999993653";

    private readonly DrcMapperTestHost _host = new DrcMapperTestHost();
    private readonly AutoMapperIMapper _autoMapper;
    private readonly MapsterIMapper _mapster;

    public MapsterMappingParityTests()
    {
        _mapster = _host.Mapper;

        var uriService = _host.UriService.Object;

        // Note: AssertConfigurationIsValid() is deliberately NOT called, and adding it would turn this
        // harness red on the pre-existing profiles rather than on anything it or the registers do. It
        // was run once to check: 8 of the 10 v1.7 pairs have destination members with no source and no
        // ForMember(...Ignore()) -- Owner/CatalogusId/Verzendingen/LegacyAuditTrail on the two
        // EnkelvoudigInformatieObject maps, CreationTime/ModificationTime/CreatedBy/ModifiedBy/
        // BestandsDelen/MultiPartDocumentId/Owner on the two Versie maps, Uuid_In on the query-parameter
        // map, BestandsDelen/InhoudIsVervallen on the get response, Bestandsomvang/Verschijningsvorm/
        // InhoudIsVervallen/Trefwoorden on the update-request projection, and BestandsDelen on both
        // Create/Update response maps. That gap is precisely what the Mapster completeness gate closes:
        // the registers carry an explicit .Ignore(...) for every one of them (see DrcMapsterCompileTests).
        var autoMapperConfiguration = new MapperConfiguration(c =>
        {
            c.AddProfile(new RequestToDomainProfile());
            c.AddProfile(new DomainToResponseProfile());
            c.Internal().Mappers.Insert(0, new NullableEnumMapper());
            c.ShouldMapMethod = _ => false;
        });

        _autoMapper = autoMapperConfiguration.CreateMapper(t =>
        {
            if (t == typeof(UrlResolver))
                return new UrlResolver(uriService);
            if (t == typeof(MemberUrlResolver))
                return new MemberUrlResolver(uriService);
            if (t == typeof(MapLatestEnkelvoudigInformatieObjectVersieResponse))
                return new MapLatestEnkelvoudigInformatieObjectVersieResponse(uriService);
            if (t == typeof(MapDownloadLink))
                return new MapDownloadLink(uriService);
            if (t == typeof(MapLatestEnkelvoudigInformatieObjectVersieRequest))
                return new MapLatestEnkelvoudigInformatieObjectVersieRequest();

            throw new NotImplementedException($"Mapper is missing the service: {t}");
        });
    }

    public void Dispose() => _host.Dispose();

    /// <summary>Destinations that are plain DTOs: compare the whole serialized graph.</summary>
    private void AssertParity<TDestination>(object source)
    {
        var fromAutoMapper = JsonConvert.SerializeObject(_autoMapper.Map<TDestination>(source));
        var fromMapster = JsonConvert.SerializeObject(_mapster.Map<TDestination>(source));

        Assert.Equal(fromAutoMapper, fromMapster);
    }

    /// <summary>
    /// Destinations that are EF entities: compare a flat projection instead of the graph.
    /// <c>EnkelvoudigInformatieObject</c> and <c>EnkelvoudigInformatieObjectVersie</c> navigate back to
    /// each other, so <c>SerializeObject</c> on the entity itself recurses, and
    /// <c>EnkelvoudigInformatieObjectVersie.Url</c> throws when both navigations are null. The
    /// projections below cover every destination member of the two maps (scalars in full, collections by
    /// null-ness and count), so nothing either map writes escapes the comparison.
    /// </summary>
    private void AssertVersieParity(object source)
    {
        var fromAutoMapper = JsonConvert.SerializeObject(Shape(_autoMapper.Map<EnkelvoudigInformatieObjectVersie>(source)));
        var fromMapster = JsonConvert.SerializeObject(Shape(_mapster.Map<EnkelvoudigInformatieObjectVersie>(source)));

        Assert.Equal(fromAutoMapper, fromMapster);
    }

    private void AssertInformatieObjectParity(object source)
    {
        var fromAutoMapper = JsonConvert.SerializeObject(Shape(_autoMapper.Map<EnkelvoudigInformatieObject>(source)));
        var fromMapster = JsonConvert.SerializeObject(Shape(_mapster.Map<EnkelvoudigInformatieObject>(source)));

        Assert.Equal(fromAutoMapper, fromMapster);
    }

    private static object Shape(EnkelvoudigInformatieObjectVersie v) =>
        new
        {
            v.Id,
            v.Identificatie,
            v.Bronorganisatie,
            v.CreatieDatum,
            v.Titel,
            v.Vertrouwelijkheidaanduiding,
            v.Auteur,
            v.Status,
            v.Formaat,
            v.Taal,
            v.Versie,
            v.BeginRegistratie,
            v.Bestandsnaam,
            v.Inhoud,
            v.Bestandsomvang,
            v.Link,
            v.Beschrijving,
            v.OntvangstDatum,
            v.VerzendDatum,
            v.Integriteit_Algoritme,
            v.Ondertekening_Soort,
            v.Ondertekening_Datum,
            v.Integriteit_Waarde,
            v.Integriteit_Datum,
            v.Verschijningsvorm,
            v.Trefwoorden,
            v.InhoudIsVervallen,
            v.IsGereedVoorPublicatie,
            v.TonenAanInitiator,
            v.EnkelvoudigInformatieObjectId,
            v.CreationTime,
            v.ModificationTime,
            v.CreatedBy,
            v.ModifiedBy,
            v.MultiPartDocumentId,
            v.Owner,
            v.RowVersion,
            BestandsDelenIsNull = v.BestandsDelen == null,
            BestandsDelenCount = v.BestandsDelen?.Count,
            // The InformatieObject the map builds, flattened for the same reason.
            InformatieObject = v.InformatieObject == null ? null : Shape(v.InformatieObject),
            LatestInformatieObjectIsNull = v.LatestInformatieObject == null,
        };

    private static object Shape(EnkelvoudigInformatieObject e) =>
        new
        {
            e.Id,
            e.InformatieObjectType,
            e.IndicatieGebruiksrecht,
            e.Locked,
            e.Lock,
            e.LatestEnkelvoudigInformatieObjectVersieId,
            e.LatestVertrouwelijkheidAanduiding,
            e.CatalogusId,
            e.LegacyAuditTrail,
            e.CreationTime,
            e.ModificationTime,
            e.CreatedBy,
            e.ModifiedBy,
            e.Owner,
            e.RowVersion,
            LatestVersieIsNull = e.LatestEnkelvoudigInformatieObjectVersie == null,
            ObjectInformatieObjectenIsNull = e.ObjectInformatieObjecten == null,
            ObjectInformatieObjectenCount = e.ObjectInformatieObjecten?.Count,
            VersiesIsNull = e.EnkelvoudigInformatieObjectVersies == null,
            VersiesCount = e.EnkelvoudigInformatieObjectVersies?.Count,
            GebruiksRechtenIsNull = e.GebruiksRechten == null,
            GebruiksRechtenCount = e.GebruiksRechten?.Count,
            VerzendingenIsNull = e.Verzendingen == null,
            VerzendingenCount = e.Verzendingen?.Count,
        };

    // =====================================================================================
    // RequestToDomainProfile / RequestToDomainRegister -- 6 pairs
    // =====================================================================================

    /// <summary>
    /// Pair 1, the reset branch. A null Trefwoorden must stay null rather than fold to an empty list:
    /// the query layer reads null as "no filter" and [] as "match nothing". The Mapster side runs under
    /// the seam's EmptyCollectionIfNull transform, so this is the fact that proves the register's
    /// .AfterMapping reset actually neutralizes it.
    /// </summary>
    [Fact]
    public void GetAllQueryParameters_to_Filter_parity_without_trefwoorden() =>
        AssertParity<GetAllEnkelvoudigInformatieObjectenFilter>(GetAllQueryParameters(trefwoorden: null));

    /// <summary>Pair 1, the mapping branch.</summary>
    [Fact]
    public void GetAllQueryParameters_to_Filter_parity_with_trefwoorden() =>
        AssertParity<GetAllEnkelvoudigInformatieObjectenFilter>(GetAllQueryParameters(trefwoorden: "een,twee"));

    /// <summary>Pair 2, both reset branches -- this map resets Uuid_In as well, which v1.5's does not.</summary>
    [Fact]
    public void SearchRequest_to_Filter_parity_without_trefwoorden_or_uuids() =>
        AssertParity<GetAllEnkelvoudigInformatieObjectenFilter>(SearchRequest(trefwoorden: null, uuidIn: null));

    /// <summary>Pair 2, both mapping branches.</summary>
    [Fact]
    public void SearchRequest_to_Filter_parity_with_trefwoorden_and_uuids() =>
        AssertParity<GetAllEnkelvoudigInformatieObjectenFilter>(
            SearchRequest(trefwoorden: "een,twee", uuidIn: ["b1f3a9c4-0000-4000-8000-000000000001", "b1f3a9c4-0000-4000-8000-000000000002"])
        );

    /// <summary>Pair 3.</summary>
    [Fact]
    public void CreateRequest_to_InformatieObject_parity() => AssertInformatieObjectParity(CreateRequest(withOndertekeningAndIntegriteit: true));

    /// <summary>
    /// Pair 4, populated. Ondertekening/Integriteit drive five destination members through helper calls
    /// on both sides.
    /// </summary>
    [Fact]
    public void CreateRequest_to_Versie_parity_with_ondertekening_and_integriteit() =>
        AssertVersieParity(CreateRequest(withOndertekeningAndIntegriteit: true));

    /// <summary>
    /// Pair 4, omitted. Neither DTO member carries [Required], so a real request may leave both out.
    /// AutoMapper null-guards its MapFrom member paths automatically; the register has to say so
    /// explicitly, and this is the fact that proves the two agree.
    /// </summary>
    [Fact]
    public void CreateRequest_to_Versie_parity_without_ondertekening_or_integriteit() =>
        AssertVersieParity(CreateRequest(withOndertekeningAndIntegriteit: false));

    /// <summary>Pair 5.</summary>
    [Fact]
    public void UpdateRequest_to_InformatieObject_parity() => AssertInformatieObjectParity(UpdateRequest(withOndertekeningAndIntegriteit: true));

    /// <summary>Pair 6, populated.</summary>
    [Fact]
    public void UpdateRequest_to_Versie_parity_with_ondertekening_and_integriteit() =>
        AssertVersieParity(UpdateRequest(withOndertekeningAndIntegriteit: true));

    /// <summary>Pair 6, omitted.</summary>
    [Fact]
    public void UpdateRequest_to_Versie_parity_without_ondertekening_or_integriteit() =>
        AssertVersieParity(UpdateRequest(withOndertekeningAndIntegriteit: false));

    // =====================================================================================
    // DomainToResponseProfile / DomainToResponseRegister -- 4 pairs
    // =====================================================================================

    /// <summary>Pair 7, nothing outstanding: the download link resolves.</summary>
    [Fact]
    public void InformatieObject_to_GetResponse_parity_without_bestandsdelen() =>
        AssertParity<EnkelvoudigInformatieObjectGetResponseDto>(InformatieObjectWithLatestVersion(withBestandsDelen: false));

    /// <summary>
    /// Pair 7, parts outstanding: the link is suppressed and the parts are projected in Volgnummer
    /// order, each carrying the lock of the version's document.
    /// </summary>
    [Fact]
    public void InformatieObject_to_GetResponse_parity_with_bestandsdelen() =>
        AssertParity<EnkelvoudigInformatieObjectGetResponseDto>(InformatieObjectWithLatestVersion(withBestandsDelen: true));

    /// <summary>Pair 8, the download-link port's resolving branch.</summary>
    [Fact]
    public void Versie_to_CreateResponse_parity_for_a_populated_version() =>
        AssertParity<EnkelvoudigInformatieObjectCreateResponseDto>(Versie(populated: true));

    /// <summary>Pair 8, the download-link port's suppressing branch (no content, nothing uploaded).</summary>
    [Fact]
    public void Versie_to_CreateResponse_parity_for_an_empty_version() =>
        AssertParity<EnkelvoudigInformatieObjectCreateResponseDto>(Versie(populated: false));

    /// <summary>Pair 9, resolving branch.</summary>
    [Fact]
    public void Versie_to_UpdateResponse_parity_for_a_populated_version() =>
        AssertParity<EnkelvoudigInformatieObjectUpdateResponseDto>(Versie(populated: true));

    /// <summary>Pair 9, suppressing branch.</summary>
    [Fact]
    public void Versie_to_UpdateResponse_parity_for_an_empty_version() =>
        AssertParity<EnkelvoudigInformatieObjectUpdateResponseDto>(Versie(populated: false));

    /// <summary>Pair 10, the map the PATCH merge reads.</summary>
    [Fact]
    public void InformatieObject_to_UpdateRequest_parity() =>
        AssertParity<EnkelvoudigInformatieObjectUpdateRequestDto>(InformatieObjectWithLatestVersion(withBestandsDelen: false));

    // =====================================================================================
    // Sources
    // =====================================================================================

    private static GetAllEnkelvoudigInformatieObjectenQueryParameters GetAllQueryParameters(string trefwoorden) =>
        new()
        {
            Bronorganisatie = TestBronorganisatie,
            Identificatie = "DOCUMENT-0001",
            Trefwoorden = trefwoorden,
            ObjectInformatieObjecten_Object = "https://example.test/zaken/1",
            ObjectInformatieObjecten_ObjectType = "zaak",
            Expand = "objectinformatieobjecten",
        };

    private static EnkelvoudigInformatieObjectSearchRequestDto SearchRequest(string trefwoorden, string[] uuidIn) =>
        new()
        {
            Bronorganisatie = TestBronorganisatie,
            Identificatie = "DOCUMENT-0001",
            Trefwoorden = trefwoorden,
            Uuid_In = uuidIn,
            ObjectInformatieObjecten_Object = "https://example.test/zaken/1",
            ObjectInformatieObjecten_ObjectType = "zaak",
            Expand = "objectinformatieobjecten",
        };

    private static EnkelvoudigInformatieObjectCreateRequestDto CreateRequest(bool withOndertekeningAndIntegriteit)
    {
        var request = new EnkelvoudigInformatieObjectCreateRequestDto
        {
            // The create map trims a trailing slash off the type url; the update map does not.
            InformatieObjectType = "https://example.test/informatieobjecttypen/1/",
        };
        Fill(request, withOndertekeningAndIntegriteit);
        return request;
    }

    private static EnkelvoudigInformatieObjectUpdateRequestDto UpdateRequest(bool withOndertekeningAndIntegriteit)
    {
        var request = new EnkelvoudigInformatieObjectUpdateRequestDto
        {
            InformatieObjectType = "https://example.test/informatieobjecttypen/1",
            Lock = "the-lock-value",
        };
        Fill(request, withOndertekeningAndIntegriteit);
        return request;
    }

    private static void Fill(EnkelvoudigInformatieObjectBaseRequestDto request, bool withOndertekeningAndIntegriteit)
    {
        request.Identificatie = "DOCUMENT-0001";
        request.Bronorganisatie = TestBronorganisatie;
        request.CreatieDatum = "2026-01-15";
        request.Titel = "een titel";
        request.Vertrouwelijkheidaanduiding = nameof(VertrouwelijkheidAanduiding.zaakvertrouwelijk);
        request.IsGereedVoorPublicatie = true;
        request.TonenAanInitiator = true;
        request.Auteur = "een auteur";
        request.Status = nameof(DataModel.Status.ter_vaststelling);
        request.Formaat = "application/pdf";
        // "nl" rather than "nld": Convert2letterTo3Letter has to run for the fact to see it.
        request.Taal = "nl";
        request.Bestandsnaam = "bestand.pdf";
        request.Bestandsomvang = 4096;
        request.Inhoud = "de-inhoud";
        request.Link = "https://example.test/link";
        request.Beschrijving = "een beschrijving";
        request.OntvangstDatum = "2026-01-16";
        request.VerzendDatum = "2026-01-17";
        request.IndicatieGebruiksrecht = true;
        request.Verschijningsvorm = "digitaal";
        request.InhoudIsVervallen = true;
        request.Trefwoorden = ["vergunning", "bouwtekening"];
        request.Ondertekening = withOndertekeningAndIntegriteit
            ? new OndertekeningDto { Soort = nameof(Soort.digitaal), Datum = "2026-01-18" }
            : null;
        request.Integriteit = withOndertekeningAndIntegriteit
            ? new IntegriteitDto
            {
                Algoritme = nameof(Algoritme.sha_256),
                Waarde = "de-integriteitswaarde",
                Datum = "2026-01-19",
            }
            : null;
    }

    private static EnkelvoudigInformatieObject InformatieObjectWithLatestVersion(bool withBestandsDelen)
    {
        var informatieObject = new EnkelvoudigInformatieObject
        {
            Id = Guid.NewGuid(),
            InformatieObjectType = "https://example.test/informatieobjecttypen/1",
            IndicatieGebruiksrecht = true,
            Locked = true,
            Lock = "the-lock-value",
        };

        var latestVersion = FullVersie();
        latestVersion.LatestInformatieObject = informatieObject;

        if (withBestandsDelen)
        {
            // Deliberately out of Volgnummer order: the ported body sorts, and a fact on an
            // already-sorted list could not tell whether it still does.
            latestVersion.BestandsDelen =
            [
                new BestandsDeel
                {
                    Id = Guid.NewGuid(),
                    Volgnummer = 2,
                    Omvang = 2048,
                    Voltooid = false,
                    EnkelvoudigInformatieObjectVersie = latestVersion,
                },
                new BestandsDeel
                {
                    Id = Guid.NewGuid(),
                    Volgnummer = 1,
                    Omvang = 2048,
                    Voltooid = true,
                    EnkelvoudigInformatieObjectVersie = latestVersion,
                },
            ];
        }

        informatieObject.LatestEnkelvoudigInformatieObjectVersie = latestVersion;
        return informatieObject;
    }

    /// <summary>
    /// A version whose own <c>InformatieObject</c> navigation is set, which is what the Create/Update
    /// response maps read. <paramref name="populated"/> false is the download-link port's suppressing
    /// branch: no Inhoud and nothing uploaded.
    /// </summary>
    private static EnkelvoudigInformatieObjectVersie Versie(bool populated)
    {
        var informatieObject = new EnkelvoudigInformatieObject
        {
            Id = Guid.NewGuid(),
            InformatieObjectType = "https://example.test/informatieobjecttypen/1",
            IndicatieGebruiksrecht = true,
            Locked = true,
            Lock = "the-lock-value",
        };

        var versie = FullVersie();
        versie.InformatieObject = informatieObject;
        versie.LatestInformatieObject = informatieObject;
        versie.Inhoud = populated ? "de-inhoud" : null;
        versie.Bestandsomvang = populated ? 4096 : 0;

        informatieObject.LatestEnkelvoudigInformatieObjectVersie = versie;
        return versie;
    }

    private static EnkelvoudigInformatieObjectVersie FullVersie() =>
        new()
        {
            Id = Guid.NewGuid(),
            Versie = 7,
            Owner = TestBronorganisatie,
            Bronorganisatie = TestBronorganisatie,
            Identificatie = "DOCUMENT-0001",
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
            Trefwoorden = new List<string> { "vergunning", "bouwtekening" },
            InhoudIsVervallen = true,
            IsGereedVoorPublicatie = true,
            TonenAanInitiator = true,
            MultiPartDocumentId = "de-multipart-id",
            BestandsDelen = [],
        };
}
