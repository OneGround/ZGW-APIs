using System;
using AutoFixture;
using AutoMapper;
using AutoMapper.Internal;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Newtonsoft.Json;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.Contracts.v1.AuditTrail;
using OneGround.ZGW.Common.DataModel;
using OneGround.ZGW.Common.Web;
using OneGround.ZGW.Common.Web.Mapping.Mapster;
using OneGround.ZGW.Common.Web.Mapping.ValueResolvers;
using OneGround.ZGW.Common.Web.Models;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.DataAccess;
using OneGround.ZGW.DataAccess.AuditTrail;
using OneGround.ZGW.Documenten.DataModel;
using Xunit;
using AutoMapperIMapper = AutoMapper.IMapper;
using ContractsV1 = OneGround.ZGW.Documenten.Contracts.v1;
using ContractsV15 = OneGround.ZGW.Documenten.Contracts.v1._5;
using MappingV1 = OneGround.ZGW.Documenten.Web.MappingProfiles.v1;
using MappingV11 = OneGround.ZGW.Documenten.Web.MappingProfiles.v1._1;
using MappingV15 = OneGround.ZGW.Documenten.Web.MappingProfiles.v1._5;
using MapsterIMapper = MapsterMapper.IMapper;
using ModelsV1 = OneGround.ZGW.Documenten.Web.Models.v1;
using ModelsV15 = OneGround.ZGW.Documenten.Web.Models.v1._5;
using QueriesV1 = OneGround.ZGW.Documenten.Contracts.v1.Queries;
using QueriesV15 = OneGround.ZGW.Documenten.Contracts.v1._5.Queries;
using RequestsV1 = OneGround.ZGW.Documenten.Contracts.v1.Requests;
using RequestsV11 = OneGround.ZGW.Documenten.Contracts.v1._1.Requests;
using RequestsV15 = OneGround.ZGW.Documenten.Contracts.v1._5.Requests;
using ResponsesV1 = OneGround.ZGW.Documenten.Contracts.v1.Responses;
using ResponsesV11 = OneGround.ZGW.Documenten.Contracts.v1._1.Responses;
using ResponsesV15 = OneGround.ZGW.Documenten.Contracts.v1._5.Responses;

namespace OneGround.ZGW.Documenten.WebApi.UnitTests.MappingTests;

/// <summary>
/// Temporary A/B parity guard (deleted once the AutoMapper profiles are removed in a later task):
/// maps identical inputs through both the still-present AutoMapper profiles and the new Mapster
/// registers for all 8 DRC mapping profiles (v1 RequestToPagination/RequestToDomain/DomainToResponse,
/// v1.1 RequestToDomain/DomainToResponse, v1.5 RequestToPagination/RequestToDomain/DomainToResponse)
/// and asserts byte-identical serialized JSON. This is the wholesale correctness proof for the
/// migration -- every <c>CreateMap</c>/<c>NewConfig</c> pair across all 8 profiles/registers gets at
/// least one fact here (52 configs total; one pair -- GetAllEnkelvoudigInformatieObjectenQueryParameters
/// (v1) -&gt; GetAllEnkelvoudigInformatieObjectenFilter (v1 Models) -- is registered identically by both
/// the v1 AND v1.1 RequestToDomainRegister, so it is covered once, not twice).
///
/// The single most important fact in this file is the pair proving the INVERTED Risk #17 handling:
/// the two v1.5 <c>Trefwoorden_In</c> reset configs must produce <c>null</c> (not <c>[]</c>) under the
/// REAL <c>AddZgwMapster</c>-equivalent global config (MaxDepth, EmptyCollectionIfNull, IgnoreCase,
/// RegisterNullableEnumRule) built in this test's constructor -- a bare, isolated
/// <c>TypeAdapterConfig()</c> (as used by the per-register unit tests) cannot exercise this class of
/// bug, because it has no <c>EmptyCollectionIfNull</c> transform registered at all.
/// </summary>
/// <summary>
/// Builds the dual AutoMapper/Mapster setup once and shares it across every fact in
/// <see cref="MapsterMappingParityTests"/> via <c>IClassFixture</c>. xUnit gives each <c>[Fact]</c> a
/// fresh test-class instance by default; without this fixture, all ~54 facts would each rebuild a full
/// 8-profile AutoMapper <c>MapperConfiguration</c> plus an 8-register Mapster <c>TypeAdapterConfig</c>
/// from scratch. DRC's domain model has a genuine multi-path cyclic EF navigation graph
/// (<c>EnkelvoudigInformatieObject</c> &lt;-&gt; <c>EnkelvoudigInformatieObjectVersie</c> &lt;-&gt;
/// <c>Verzending</c>, see <c>MaxDepth(200)</c>'s comment in <c>AddZgwMapster</c>); rebuilding both
/// configs 54 times over compounds their (individually modest) cost into minutes of runtime and
/// runaway memory growth. Sharing the setup once, as any normal xUnit test class would for expensive
/// fixtures, keeps each fact's actual cost to just its own <c>Map</c> call.
/// </summary>
public class MapsterMappingParityFixture : IDisposable
{
    public Mock<IEntityUriService> MockedUriService { get; } = new();
    public AutoMapperIMapper AutoMapper { get; }
    public MapsterIMapper MapsterMapper { get; }
    private readonly ServiceProvider _provider;
    private readonly IServiceScope _scope;

    public MapsterMappingParityFixture()
    {
        MockedUriService.Setup(s => s.GetUri(It.IsAny<IUrlEntity>())).Returns<IUrlEntity>(e => e.Url);

        // AutoMapper side: all 8 profiles, with every DI-backed resolver/mapping-action wired plus the
        // NullableEnumMapper (request side) that ports as RegisterNullableEnumRule on Mapster.
        var amConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile(new MappingV1.RequestToPaginationProfile());
            cfg.AddProfile(new MappingV1.RequestToDomainProfile());
            cfg.AddProfile(new MappingV1.DomainToResponseProfile());
            cfg.AddProfile(new MappingV11.RequestToDomainProfile());
            cfg.AddProfile(new MappingV11.DomainToResponseProfile());
            cfg.AddProfile(new MappingV15.RequestToPaginationProfile());
            cfg.AddProfile(new MappingV15.RequestToDomainProfile());
            cfg.AddProfile(new MappingV15.DomainToResponseProfile());
            cfg.Internal().Mappers.Insert(0, new NullableEnumMapper());
            cfg.ShouldMapMethod = _ => false;
        });
        AutoMapper = amConfig.CreateMapper(t =>
        {
            if (t == typeof(UrlResolver))
                return new UrlResolver(MockedUriService.Object);
            if (t == typeof(MemberUrlResolver))
                return new MemberUrlResolver(MockedUriService.Object);

            // The three IMappingAction shapes, replicated per version: a pure "request" port with no
            // ctor args, and two DI-backed ports (uriService-injecting "response" and "download link").
            if (t == typeof(MappingV1.MapLatestEnkelvoudigInformatieObjectVersieRequest))
                return new MappingV1.MapLatestEnkelvoudigInformatieObjectVersieRequest();
            if (t == typeof(MappingV1.MapLatestEnkelvoudigInformatieObjectVersieResponse))
                return new MappingV1.MapLatestEnkelvoudigInformatieObjectVersieResponse(MockedUriService.Object);
            if (t == typeof(MappingV11.MapLatestEnkelvoudigInformatieObjectVersieRequest))
                return new MappingV11.MapLatestEnkelvoudigInformatieObjectVersieRequest();
            if (t == typeof(MappingV11.MapLatestEnkelvoudigInformatieObjectVersieResponse))
                return new MappingV11.MapLatestEnkelvoudigInformatieObjectVersieResponse(MockedUriService.Object);
            if (t == typeof(MappingV11.MapDownloadLink))
                return new MappingV11.MapDownloadLink(MockedUriService.Object);
            if (t == typeof(MappingV15.MapLatestEnkelvoudigInformatieObjectVersieRequest))
                return new MappingV15.MapLatestEnkelvoudigInformatieObjectVersieRequest();
            if (t == typeof(MappingV15.MapLatestEnkelvoudigInformatieObjectVersieResponse))
                return new MappingV15.MapLatestEnkelvoudigInformatieObjectVersieResponse(MockedUriService.Object);
            if (t == typeof(MappingV15.MapDownloadLink))
                return new MappingV15.MapDownloadLink(MockedUriService.Object);

            throw new NotImplementedException($"Mapper is missing the service: {t}");
        });

        // Mapster side: one TypeAdapterConfig mirroring all 4 AddZgwMapster global defaults, plus all
        // 8 registers -- exactly what production wires up via AddZgwMapster + config.Scan.
        var config = new TypeAdapterConfig();
        config.Default.MaxDepth(200);
        config.Default.AddDestinationTransform(DestinationTransform.EmptyCollectionIfNull);
        config.Default.NameMatchingStrategy(NameMatchingStrategy.IgnoreCase);
        config.RegisterNullableEnumRule();
        new MappingV1.RequestToPaginationRegister().Register(config);
        new MappingV1.RequestToDomainRegister().Register(config);
        new MappingV1.DomainToResponseRegister().Register(config);
        new MappingV11.RequestToDomainRegister().Register(config);
        new MappingV11.DomainToResponseRegister().Register(config);
        new MappingV15.RequestToPaginationRegister().Register(config);
        new MappingV15.RequestToDomainRegister().Register(config);
        new MappingV15.DomainToResponseRegister().Register(config);

        // Deliberately NOT calling config.Compile() here: production's AddZgwMapster never does
        // either, relying on Mapster's lazy per-type-pair compilation, which only ever pays the
        // cyclic-graph traversal cost for the specific type pairs actually mapped.

        var services = new ServiceCollection();
        services.AddSingleton(MockedUriService.Object);
        services.AddSingleton(config);
        services.AddScoped<MapsterIMapper, ServiceMapper>();
        _provider = services.BuildServiceProvider();
        _scope = _provider.CreateScope();
        MapsterMapper = _scope.ServiceProvider.GetRequiredService<MapsterIMapper>();
    }

    public void Dispose()
    {
        _scope.Dispose();
        _provider.Dispose();
    }
}

public class MapsterMappingParityTests : IClassFixture<MapsterMappingParityFixture>
{
    // Official RvIG test BSN, reused here purely as a safe, non-real 9-digit placeholder for
    // Bronorganisatie -- never assigned to a real person or organisation.
    private const string TestBronorganisatie = "999993653";

    private readonly OmitOnRecursionFixture _fixture = new();
    private readonly MapsterMappingParityFixture _mappers;
    private Mock<IEntityUriService> _mockedUriService => _mappers.MockedUriService;
    private AutoMapperIMapper _autoMapper => _mappers.AutoMapper;
    private MapsterIMapper _mapsterMapper => _mappers.MapsterMapper;

    public MapsterMappingParityTests(MapsterMappingParityFixture mappers)
    {
        _mappers = mappers;
        _fixture.Register<DateOnly>(() => DateOnly.FromDateTime(DateTime.UtcNow));
    }

    private void AssertParity<TDest>(object source)
    {
        var am = JsonConvert.SerializeObject(_autoMapper.Map<TDest>(source));
        var ms = JsonConvert.SerializeObject(_mapsterMapper.Map<TDest>(source));
        Assert.Equal(am, ms);
    }

    // =====================================================================================
    // RequestToPaginationRegister / RequestToPaginationProfile (v1 + v1.5 -- 4 configs)
    // =====================================================================================

    [Fact]
    public void PaginationQuery_to_PaginationFilter_parity() => AssertParity<PaginationFilter>(new PaginationQuery(page: 3, pageSize: 42));

    [Fact]
    public void V1_GetEnkelvoudigInformatieObjectQueryParameters_to_Filter_parity() =>
        AssertParity<ModelsV1.GetEnkelvoudigInformatieObjectFilter>(
            new QueriesV1.GetEnkelvoudigInformatieObjectQueryParameters { RegistratieOp = "2024-03-15T10:30:00Z" }
        );

    [Fact]
    public void V1_GetEnkelvoudigInformatieObjectQueryParameters_with_null_RegistratieOp_parity() =>
        AssertParity<ModelsV1.GetEnkelvoudigInformatieObjectFilter>(
            new QueriesV1.GetEnkelvoudigInformatieObjectQueryParameters { RegistratieOp = null }
        );

    [Fact]
    public void V15_GetEnkelvoudigInformatieObjectQueryParameters_to_Filter_parity() =>
        AssertParity<ModelsV1.GetEnkelvoudigInformatieObjectFilter>(
            new QueriesV15.GetEnkelvoudigInformatieObjectQueryParameters { RegistratieOp = "2024-03-15T10:30:00Z" }
        );

    [Fact]
    public void DownloadEnkelvoudigInformatieObjectQueryParameters_to_Filter_parity()
    {
        // Versie/RegistratieOp are numeric/date strings on the wire (parsed on the domain filter) --
        // AutoFixture's default anonymous strings ("Versie<guid>"/"RegistratieOp<guid>") aren't
        // parseable, so give both real values.
        _fixture.Customize<QueriesV15.DownloadEnkelvoudigInformatieObjectQueryParameters>(c =>
            c.With(p => p.Versie, "3").With(p => p.RegistratieOp, "2024-03-15T10:30:00Z")
        );

        AssertParity<ModelsV1.GetEnkelvoudigInformatieObjectFilter>(_fixture.Create<QueriesV15.DownloadEnkelvoudigInformatieObjectQueryParameters>());
    }

    // =====================================================================================
    // v1 RequestToDomainRegister / RequestToDomainProfile (9 configs)
    // =====================================================================================

    [Fact]
    public void GetAllEnkelvoudigInformatieObjectenQueryParameters_v1_to_Filter_parity() =>
        // Shared: this exact (source, dest) pair is registered identically by both the v1 and v1.1
        // RequestToDomainRegister (v1.1 has no GetAllEnkelvoudigInformatieObjectenQueryParameters of
        // its own and reuses v1's, mapping to the same v1 Models filter type) -- one fact covers both.
        AssertParity<ModelsV1.GetAllEnkelvoudigInformatieObjectenFilter>(
            _fixture.Create<QueriesV1.GetAllEnkelvoudigInformatieObjectenQueryParameters>()
        );

    private static RequestsV1.EnkelvoudigInformatieObjectCreateRequestDto CreateRequestDtoV1() =>
        new()
        {
            Identificatie = "DOC-2020-0000001",
            Bronorganisatie = "999990561",
            CreatieDatum = "2020-11-12",
            Titel = "My document",
            Auteur = "somebody",
            Formaat = "application/pdf",
            Taal = "eng",
            Bestandsnaam = "document.pdf",
            Inhoud = "TWFuIGlzIGRpc3Rpbmd1aXNoZWQsIG5vdCBvbmx5IGJ5IGhpcyByZWFzb24sIGJ1dCAuLi4=",
            Link = "(no link)",
            Beschrijving = "My description of the document",
            OntvangstDatum = "2020-11-13",
            VerzendDatum = "2020-11-14",
            IndicatieGebruiksrecht = true,
            Ondertekening = new ContractsV1.OndertekeningDto { Soort = Soort.digitaal.ToString(), Datum = "2020-11-18" },
            Integriteit = new ContractsV1.IntegriteitDto
            {
                Algoritme = Algoritme.crc_32.ToString(),
                Waarde = "123",
                Datum = "2020-11-17",
            },
            InformatieObjectType = "https://some-informatieobjecttype",
        };

    private static RequestsV1.EnkelvoudigInformatieObjectUpdateRequestDto UpdateRequestDtoV1() =>
        new()
        {
            Lock = "8494eecb2495447a8b29a8e31d10c4b4",
            CreatieDatum = "2020-11-12",
            Taal = "eng",
            OntvangstDatum = "2020-11-13",
            VerzendDatum = "2020-11-14",
            IndicatieGebruiksrecht = true,
            Ondertekening = new ContractsV1.OndertekeningDto { Soort = Soort.digitaal.ToString(), Datum = "2020-11-18" },
            Integriteit = new ContractsV1.IntegriteitDto
            {
                Algoritme = Algoritme.crc_32.ToString(),
                Waarde = "123",
                Datum = "2020-11-17",
            },
            InformatieObjectType = "https://some-informatieobjecttype/",
        };

    [Fact]
    public void EnkelvoudigInformatieObjectCreateRequestDto_v1_to_EnkelvoudigInformatieObject_parity() =>
        AssertParity<EnkelvoudigInformatieObject>(CreateRequestDtoV1());

    [Fact]
    public void EnkelvoudigInformatieObjectCreateRequestDto_v1_to_EnkelvoudigInformatieObjectVersie_parity() =>
        AssertParity<EnkelvoudigInformatieObjectVersie>(CreateRequestDtoV1());

    [Fact]
    public void EnkelvoudigInformatieObjectUpdateRequestDto_v1_to_EnkelvoudigInformatieObject_parity() =>
        AssertParity<EnkelvoudigInformatieObject>(UpdateRequestDtoV1());

    [Fact]
    public void EnkelvoudigInformatieObjectUpdateRequestDto_v1_to_EnkelvoudigInformatieObjectVersie_parity() =>
        AssertParity<EnkelvoudigInformatieObjectVersie>(UpdateRequestDtoV1());

    [Fact]
    public void GetAllObjectInformatieObjectenQueryParameters_to_Filter_parity() =>
        AssertParity<ModelsV1.GetAllObjectInformatieObjectenFilter>(_fixture.Create<QueriesV1.GetAllObjectInformatieObjectenQueryParameters>());

    [Fact]
    public void ObjectInformatieObjectRequestDto_to_ObjectInformatieObject_parity()
    {
        _fixture.Customize<RequestsV1.ObjectInformatieObjectRequestDto>(c => c.With(a => a.ObjectType, ObjectType.besluit.ToString()));

        AssertParity<ObjectInformatieObject>(_fixture.Create<RequestsV1.ObjectInformatieObjectRequestDto>());
    }

    [Fact]
    public void GetAllGebruiksRechtenQueryParameters_v1_to_Filter_parity() =>
        AssertParity<ModelsV1.GetAllGebruiksRechtenFilter>(
            new QueriesV1.GetAllGebruiksRechtenQueryParameters
            {
                Startdatum__gt = "2020-11-13",
                Startdatum__gte = "2020-11-14",
                Startdatum__lt = "2020-11-15",
                Startdatum__lte = "2020-11-16",
                Einddatum__gt = "2020-11-17",
                Einddatum__gte = "2020-11-18",
                Einddatum__lt = "2020-11-19",
                Einddatum__lte = "2020-11-20",
                InformatieObject = "https://some-informatieobject",
            }
        );

    [Fact]
    public void GebruiksRechtRequestDto_v1_to_GebruiksRecht_parity()
    {
        _fixture.Customize<RequestsV1.GebruiksRechtRequestDto>(c =>
            c.With(p => p.Startdatum, "2020-11-16").With(p => p.Einddatum, "2020-11-17").Without(p => p.InformatieObject)
        );

        AssertParity<GebruiksRecht>(_fixture.Create<RequestsV1.GebruiksRechtRequestDto>());
    }

    // =====================================================================================
    // v1 DomainToResponseRegister / DomainToResponseProfile (8 configs)
    // =====================================================================================

    private static EnkelvoudigInformatieObjectVersie CreateVersionV1() =>
        new()
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

    [Fact]
    public void EnkelvoudigInformatieObject_v1_to_GetResponseDto_parity()
    {
        var latestVersion = CreateVersionV1();
        var value = new EnkelvoudigInformatieObject
        {
            Id = Guid.NewGuid(),
            InformatieObjectType = "https://example.test/informatieobjecttypen/1",
            IndicatieGebruiksrecht = true,
            Locked = true,
            EnkelvoudigInformatieObjectVersies = [latestVersion],
            LatestEnkelvoudigInformatieObjectVersie = latestVersion,
        };
        latestVersion.InformatieObject = value;
        latestVersion.LatestInformatieObject = value;

        // Distinguishable from the default e.Url convention-based stub configured in the constructor,
        // to prove the AfterMapping port really calls uriService.GetUri(latestVersion).
        _mockedUriService.Setup(s => s.GetUri(latestVersion)).Returns("PARITY-INHOUD-URL-V1");

        AssertParity<ResponsesV1.EnkelvoudigInformatieObjectGetResponseDto>(value);
    }

    [Fact]
    public void EnkelvoudigInformatieObjectVersie_v1_to_CreateResponseDto_parity()
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

        AssertParity<ResponsesV1.EnkelvoudigInformatieObjectCreateResponseDto>(value);
    }

    [Fact]
    public void EnkelvoudigInformatieObjectVersie_v1_to_UpdateResponseDto_parity()
    {
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
            InformatieObject = informatieObject,
        };

        AssertParity<ResponsesV1.EnkelvoudigInformatieObjectUpdateResponseDto>(value);
    }

    [Fact]
    public void EnkelvoudigInformatieObject_v1_to_UpdateRequestDto_merge_parity()
    {
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

        AssertParity<RequestsV1.EnkelvoudigInformatieObjectUpdateRequestDto>(value);
    }

    [Fact]
    public void ObjectInformatieObject_v1_to_ResponseDto_parity() =>
        AssertParity<ResponsesV1.ObjectInformatieObjectResponseDto>(_fixture.Create<ObjectInformatieObject>());

    [Fact]
    public void GebruiksRecht_v1_to_ResponseDto_parity() => AssertParity<ResponsesV1.GebruiksRechtResponseDto>(_fixture.Create<GebruiksRecht>());

    [Fact]
    public void GebruiksRecht_v1_to_RequestDto_merge_parity() => AssertParity<RequestsV1.GebruiksRechtRequestDto>(_fixture.Create<GebruiksRecht>());

    [Fact]
    public void AuditTrailRegel_to_AuditTrailRegelDto_parity_with_json_values()
    {
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

        AssertParity<AuditTrailRegelDto>(value);
    }

    [Fact]
    public void AuditTrailRegel_to_AuditTrailRegelDto_parity_with_null_and_empty_json()
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

        AssertParity<AuditTrailRegelDto>(value);
    }

    // =====================================================================================
    // v1.1 RequestToDomainRegister / RequestToDomainProfile (4 configs -- the 5th, the
    // GetAllEnkelvoudigInformatieObjectenQueryParameters -> Filter pair, is the shared/dedup
    // pair covered above)
    // =====================================================================================

    private static RequestsV11.EnkelvoudigInformatieObjectCreateRequestDto CreateRequestDtoV11() =>
        new()
        {
            Identificatie = "DOC-2020-0000001",
            Bronorganisatie = "999990561",
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
            Ondertekening = new ContractsV1.OndertekeningDto { Soort = Soort.digitaal.ToString(), Datum = "2020-11-18" },
            Integriteit = new ContractsV1.IntegriteitDto
            {
                Algoritme = Algoritme.crc_32.ToString(),
                Waarde = "123",
                Datum = "2020-11-17",
            },
            InformatieObjectType = "https://some-informatieobjecttype",
            Vertrouwelijkheidaanduiding = VertrouwelijkheidAanduiding.openbaar.ToString(),
            Status = Status.definitief.ToString(),
        };

    private static RequestsV11.EnkelvoudigInformatieObjectUpdateRequestDto UpdateRequestDtoV11() =>
        new()
        {
            Lock = "8494eecb2495447a8b29a8e31d10c4b4",
            CreatieDatum = "2020-11-12",
            Taal = "eng",
            Bestandsomvang = 12345,
            OntvangstDatum = "2020-11-13",
            VerzendDatum = "2020-11-14",
            IndicatieGebruiksrecht = true,
            Ondertekening = new ContractsV1.OndertekeningDto { Soort = Soort.digitaal.ToString(), Datum = "2020-11-18" },
            Integriteit = new ContractsV1.IntegriteitDto
            {
                Algoritme = Algoritme.crc_32.ToString(),
                Waarde = "123",
                Datum = "2020-11-17",
            },
            InformatieObjectType = "https://some-informatieobjecttype/",
            Vertrouwelijkheidaanduiding = VertrouwelijkheidAanduiding.openbaar.ToString(),
            Status = Status.definitief.ToString(),
        };

    [Fact]
    public void EnkelvoudigInformatieObjectCreateRequestDto_v1_1_to_EnkelvoudigInformatieObject_parity() =>
        AssertParity<EnkelvoudigInformatieObject>(CreateRequestDtoV11());

    [Fact]
    public void EnkelvoudigInformatieObjectCreateRequestDto_v1_1_to_EnkelvoudigInformatieObjectVersie_parity() =>
        AssertParity<EnkelvoudigInformatieObjectVersie>(CreateRequestDtoV11());

    [Fact]
    public void EnkelvoudigInformatieObjectUpdateRequestDto_v1_1_to_EnkelvoudigInformatieObject_parity() =>
        AssertParity<EnkelvoudigInformatieObject>(UpdateRequestDtoV11());

    [Fact]
    public void EnkelvoudigInformatieObjectUpdateRequestDto_v1_1_to_EnkelvoudigInformatieObjectVersie_parity() =>
        AssertParity<EnkelvoudigInformatieObjectVersie>(UpdateRequestDtoV11());

    // =====================================================================================
    // v1.1 DomainToResponseRegister / DomainToResponseProfile (5 configs)
    // =====================================================================================

    private static EnkelvoudigInformatieObjectVersie CreateVersionV11() =>
        new()
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
        };

    [Fact]
    public void EnkelvoudigInformatieObject_v1_1_to_GetResponseDto_parity()
    {
        var latestVersion = CreateVersionV11();
        var value = new EnkelvoudigInformatieObject
        {
            Id = Guid.NewGuid(),
            InformatieObjectType = "https://example.test/informatieobjecttypen/1",
            IndicatieGebruiksrecht = true,
            Locked = true,
            EnkelvoudigInformatieObjectVersies = [latestVersion],
            LatestEnkelvoudigInformatieObjectVersie = latestVersion,
        };
        latestVersion.InformatieObject = value;
        latestVersion.LatestInformatieObject = value;

        _mockedUriService.Setup(s => s.GetUri(latestVersion)).Returns("PARITY-INHOUD-URL-V11");

        AssertParity<ResponsesV11.EnkelvoudigInformatieObjectGetResponseDto>(value);
    }

    [Fact]
    public void EnkelvoudigInformatieObject_v1_1_to_GetResponseDto_with_BestandsDelen_parity()
    {
        // Covers the "Note: New in v1.1" branch inside MapLatestVersieToGetResponse: when
        // BestandsDelen.Count != 0, Inhoud must be null regardless of what uriService would resolve.
        var latestVersion = CreateVersionV11();
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

        _mockedUriService.Setup(s => s.GetUri(latestVersion)).Returns("SHOULD-NOT-BE-USED");

        AssertParity<ResponsesV11.EnkelvoudigInformatieObjectGetResponseDto>(value);
    }

    [Theory]
    [InlineData("", 0L, false)]
    [InlineData("", 5L, false)]
    [InlineData(@"202401\some-file.bin", 0L, false)]
    [InlineData(@"202401\some-file.bin", 5L, true)]
    public void EnkelvoudigInformatieObjectVersie_v1_1_to_CreateResponseDto_MapDownloadLink_parity(
        string inhoud,
        long bestandsomvang,
        bool withBestandsDelen
    )
    {
        // Covers all real branches of the ported MapDownloadLink conditional, applied to the
        // Create-response config (v1.1).
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
        if (withBestandsDelen)
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

        _mockedUriService.Setup(s => s.GetUri(value)).Returns("PARITY-CREATE-INHOUD-URL-V11");

        AssertParity<ResponsesV11.EnkelvoudigInformatieObjectCreateResponseDto>(value);
    }

    [Fact]
    public void EnkelvoudigInformatieObjectVersie_v1_1_to_UpdateResponseDto_parity()
    {
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

        _mockedUriService.Setup(s => s.GetUri(value)).Returns("PARITY-UPDATE-INHOUD-URL-V11");

        AssertParity<ResponsesV11.EnkelvoudigInformatieObjectUpdateResponseDto>(value);
    }

    [Fact]
    public void EnkelvoudigInformatieObject_v1_1_to_UpdateRequestDto_merge_parity()
    {
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

        AssertParity<RequestsV11.EnkelvoudigInformatieObjectUpdateRequestDto>(value);
    }

    [Fact]
    public void BestandsDeel_v1_1_to_ResponseDto_parity()
    {
        var informatieObject = new EnkelvoudigInformatieObject { Id = Guid.NewGuid(), Lock = "lock-bd" };
        var version = new EnkelvoudigInformatieObjectVersie
        {
            Id = Guid.NewGuid(),
            Versie = 1,
            InformatieObject = informatieObject,
        };
        var value = new BestandsDeel
        {
            Id = Guid.NewGuid(),
            Volgnummer = 3,
            Omvang = 777,
            Voltooid = true,
            EnkelvoudigInformatieObjectVersie = version,
        };

        AssertParity<ResponsesV11.BestandsDeelResponseDto>(value);
    }

    // =====================================================================================
    // v1.5 RequestToDomainRegister / RequestToDomainProfile (12 configs)
    // =====================================================================================

    // ---- THE definitive Trefwoorden_In null-preservation facts (inverted Risk #17) --------
    // Run against the REAL AddZgwMapster-equivalent config built in this test's constructor
    // (MaxDepth, EmptyCollectionIfNull, IgnoreCase, RegisterNullableEnumRule -- exactly what
    // AddZgwMapster wires up in production). If the .AfterMapping port were broken, the real
    // EmptyCollectionIfNull transform would coalesce these nulls to [] and these facts would fail.

    [Fact]
    public void Trefwoorden_In_Null_Preservation_QueryParameters_Under_Real_Transform()
    {
        var source = new QueriesV15.GetAllEnkelvoudigInformatieObjectenQueryParameters { Trefwoorden = null };

        var amResult = _autoMapper.Map<ModelsV15.GetAllEnkelvoudigInformatieObjectenFilter>(source);
        var msResult = _mapsterMapper.Map<ModelsV15.GetAllEnkelvoudigInformatieObjectenFilter>(source);

        // The load-bearing assertion: NOT Assert.Empty. A null Trefwoorden_In means "no filter" to the
        // EF Where query; an empty array means "match nothing" -- these are not interchangeable.
        Assert.Null(amResult.Trefwoorden_In);
        Assert.Null(msResult.Trefwoorden_In);

        AssertParity<ModelsV15.GetAllEnkelvoudigInformatieObjectenFilter>(source);
    }

    [Fact]
    public void Trefwoorden_In_Null_Preservation_SearchRequestDto_Under_Real_Transform()
    {
        var source = new RequestsV15.EnkelvoudigInformatieObjectSearchRequestDto { Uuid_In = ["11111111-1111-1111-1111-111111111111"] };

        var amResult = _autoMapper.Map<ModelsV15.GetAllEnkelvoudigInformatieObjectenFilter>(source);
        var msResult = _mapsterMapper.Map<ModelsV15.GetAllEnkelvoudigInformatieObjectenFilter>(source);

        Assert.Null(amResult.Trefwoorden_In);
        Assert.Null(msResult.Trefwoorden_In);

        AssertParity<ModelsV15.GetAllEnkelvoudigInformatieObjectenFilter>(source);
    }

    [Fact]
    public void GetAllEnkelvoudigInformatieObjectenQueryParameters_v1_5_with_Trefwoorden_parity() =>
        AssertParity<ModelsV15.GetAllEnkelvoudigInformatieObjectenFilter>(
            new QueriesV15.GetAllEnkelvoudigInformatieObjectenQueryParameters { Trefwoorden = "bouwtekening,vergunning,aanvraag" }
        );

    private static RequestsV15.EnkelvoudigInformatieObjectCreateRequestDto CreateRequestDtoV15() =>
        new()
        {
            Identificatie = "DOC-2020-0000001",
            Bronorganisatie = "999990561",
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
            Ondertekening = new ContractsV1.OndertekeningDto { Soort = Soort.digitaal.ToString(), Datum = "2020-11-18" },
            Integriteit = new ContractsV1.IntegriteitDto
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
        };

    private static RequestsV15.EnkelvoudigInformatieObjectUpdateRequestDto UpdateRequestDtoV15() =>
        new()
        {
            Lock = "8494eecb2495447a8b29a8e31d10c4b4",
            CreatieDatum = "2020-11-12",
            Taal = "eng",
            Bestandsomvang = 12345,
            OntvangstDatum = "2020-11-13",
            VerzendDatum = "2020-11-14",
            IndicatieGebruiksrecht = true,
            Ondertekening = new ContractsV1.OndertekeningDto { Soort = Soort.digitaal.ToString(), Datum = "2020-11-18" },
            Integriteit = new ContractsV1.IntegriteitDto
            {
                Algoritme = Algoritme.crc_32.ToString(),
                Waarde = "123",
                Datum = "2020-11-17",
            },
            InformatieObjectType = "https://some-informatieobjecttype/",
            Vertrouwelijkheidaanduiding = VertrouwelijkheidAanduiding.openbaar.ToString(),
            Status = Status.definitief.ToString(),
            Verschijningsvorm = "some-verschijningsvorm",
        };

    [Fact]
    public void EnkelvoudigInformatieObjectCreateRequestDto_v1_5_to_EnkelvoudigInformatieObject_parity() =>
        AssertParity<EnkelvoudigInformatieObject>(CreateRequestDtoV15());

    [Fact]
    public void EnkelvoudigInformatieObjectCreateRequestDto_v1_5_to_EnkelvoudigInformatieObjectVersie_parity() =>
        AssertParity<EnkelvoudigInformatieObjectVersie>(CreateRequestDtoV15());

    [Fact]
    public void EnkelvoudigInformatieObjectUpdateRequestDto_v1_5_to_EnkelvoudigInformatieObject_parity() =>
        AssertParity<EnkelvoudigInformatieObject>(UpdateRequestDtoV15());

    [Fact]
    public void EnkelvoudigInformatieObjectUpdateRequestDto_v1_5_to_EnkelvoudigInformatieObjectVersie_parity() =>
        AssertParity<EnkelvoudigInformatieObjectVersie>(UpdateRequestDtoV15());

    [Fact]
    public void GetAllGebruiksRechtenQueryParameters_v1_5_to_Filter_parity() =>
        AssertParity<ModelsV1.GetAllGebruiksRechtenFilter>(
            new QueriesV15.GetAllGebruiksRechtenQueryParameters
            {
                Startdatum__gt = "2020-11-13",
                Startdatum__gte = "2020-11-14",
                Startdatum__lt = "2020-11-15",
                Startdatum__lte = "2020-11-16",
                Einddatum__gt = "2020-11-17",
                Einddatum__gte = "2020-11-18",
                Einddatum__lt = "2020-11-19",
                Einddatum__lte = "2020-11-20",
            }
        );

    [Fact]
    public void GetAllVerzendingenQueryParameters_to_Filter_parity() =>
        AssertParity<ModelsV15.GetAllVerzendingenFilter>(_fixture.Create<QueriesV15.GetAllVerzendingenQueryParameters>());

    [Fact]
    public void BinnenlandsCorrespondentieAdresDto_to_Domain_parity() =>
        AssertParity<BinnenlandsCorrespondentieAdres>(
            new ContractsV15.BinnenlandsCorrespondentieAdresDto
            {
                Huisletter = "A",
                Huisnummer = 1,
                HuisnummerToevoeging = "bis",
                NaamOpenbareRuimte = "some street",
                Postcode = "1234AB",
                WoonplaatsNaam = "some city",
            }
        );

    [Fact]
    public void BuitenlandsCorrespondentieAdresDto_to_Domain_parity() =>
        AssertParity<BuitenlandsCorrespondentieAdres>(
            new ContractsV15.BuitenlandsCorrespondentieAdresDto
            {
                AdresBuitenland1 = "Some street 1",
                AdresBuitenland2 = "Some place 2",
                AdresBuitenland3 = "Some place 3",
                LandPostadres = "https://some-land",
            }
        );

    [Fact]
    public void CorrespondentiePostAdresDto_to_Domain_parity() =>
        AssertParity<CorrespondentiePostadres>(
            new ContractsV15.CorrespondentiePostAdresDto
            {
                PostbusOfAntwoordnummer = 123,
                PostadresPostcode = "1234AB",
                PostadresType = PostadresType.postbusnummer.ToString(),
                WoonplaatsNaam = "some city",
            }
        );

    private static RequestsV15.VerzendingRequestDto VerzendingRequestDto() =>
        new()
        {
            Betrokkene = "https://some-betrokkene",
            AardRelatie = OneGround.ZGW.Documenten.DataModel.AardRelatie.afzender.ToString(),
            Toelichting = "some toelichting",
            OntvangstDatum = "2020-11-13",
            Verzenddatum = "2020-11-14",
            Contactpersoon = "some contactpersoon",
            BinnenlandsCorrespondentieAdres = new ContractsV15.BinnenlandsCorrespondentieAdresDto
            {
                Huisletter = "A",
                Huisnummer = 1,
                HuisnummerToevoeging = "bis",
                NaamOpenbareRuimte = "some street",
                Postcode = "1234AB",
                WoonplaatsNaam = "some city",
            },
            Faxnummer = "0101234567",
            EmailAdres = "someone@example.com",
            MijnOverheid = true,
            Telefoonnummer = "0101234567",
        };

    [Fact]
    public void VerzendingRequestDto_to_Verzending_parity() => AssertParity<Verzending>(VerzendingRequestDto());

    // =====================================================================================
    // v1.5 DomainToResponseRegister / DomainToResponseProfile (9 configs)
    // =====================================================================================

    private static EnkelvoudigInformatieObjectVersie CreateVersionV15() =>
        new()
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

    [Fact]
    public void EnkelvoudigInformatieObject_v1_5_to_GetResponseDto_parity()
    {
        var latestVersion = CreateVersionV15();
        var value = new EnkelvoudigInformatieObject
        {
            Id = Guid.NewGuid(),
            InformatieObjectType = "https://example.test/informatieobjecttypen/1",
            IndicatieGebruiksrecht = true,
            Locked = true,
            EnkelvoudigInformatieObjectVersies = [latestVersion],
            LatestEnkelvoudigInformatieObjectVersie = latestVersion,
        };
        latestVersion.InformatieObject = value;
        latestVersion.LatestInformatieObject = value;

        _mockedUriService.Setup(s => s.GetUri(latestVersion)).Returns("PARITY-INHOUD-URL-V15");

        AssertParity<ResponsesV15.EnkelvoudigInformatieObjectGetResponseDto>(value);
    }

    [Theory]
    [InlineData("", 0L, false)]
    [InlineData("", 5L, false)]
    [InlineData(@"202401\some-file.bin", 0L, false)]
    [InlineData(@"202401\some-file.bin", 5L, true)]
    public void EnkelvoudigInformatieObjectVersie_v1_5_to_CreateResponseDto_MapDownloadLink_parity(
        string inhoud,
        long bestandsomvang,
        bool withBestandsDelen
    )
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
            Inhoud = inhoud,
            Bestandsomvang = bestandsomvang,
            CreatieDatum = new DateOnly(2024, 3, 1),
            OntvangstDatum = new DateOnly(2024, 3, 2),
            BeginRegistratie = new DateTime(2024, 3, 3, 4, 5, 6, DateTimeKind.Utc),
            VerzendDatum = new DateOnly(2024, 3, 4),
            Verschijningsvorm = "digitaal",
            Trefwoorden = ["een"],
            InformatieObject = informatieObject,
        };
        if (withBestandsDelen)
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

        _mockedUriService.Setup(s => s.GetUri(value)).Returns("PARITY-CREATE-INHOUD-URL-V15");

        AssertParity<ResponsesV15.EnkelvoudigInformatieObjectCreateResponseDto>(value);
    }

    [Fact]
    public void EnkelvoudigInformatieObjectVersie_v1_5_to_UpdateResponseDto_parity()
    {
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

        _mockedUriService.Setup(s => s.GetUri(value)).Returns("PARITY-UPDATE-INHOUD-URL-V15");

        AssertParity<ResponsesV15.EnkelvoudigInformatieObjectUpdateResponseDto>(value);
    }

    [Fact]
    public void EnkelvoudigInformatieObject_v1_5_to_UpdateRequestDto_merge_parity()
    {
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

        AssertParity<RequestsV15.EnkelvoudigInformatieObjectUpdateRequestDto>(value);
    }

    [Fact]
    public void BinnenlandsCorrespondentieAdres_to_Dto_parity() =>
        AssertParity<ContractsV15.BinnenlandsCorrespondentieAdresDto>(
            new BinnenlandsCorrespondentieAdres
            {
                Huisletter = "A",
                Huisnummer = 12,
                HuisnummerToevoeging = "bis",
                NaamOpenbareRuimte = "Teststraat",
                Postcode = "1234AB",
                WoonplaatsNaam = "Testdorp",
            }
        );

    [Fact]
    public void BuitenlandsCorrespondentieAdres_to_Dto_parity() =>
        AssertParity<ContractsV15.BuitenlandsCorrespondentieAdresDto>(
            new BuitenlandsCorrespondentieAdres
            {
                AdresBuitenland1 = "Adres 1",
                AdresBuitenland2 = "Adres 2",
                AdresBuitenland3 = "Adres 3",
                LandPostadres = "Landcode",
            }
        );

    [Fact]
    public void CorrespondentiePostadres_to_Dto_parity() =>
        AssertParity<ContractsV15.CorrespondentiePostAdresDto>(
            new CorrespondentiePostadres
            {
                PostbusOfAntwoordnummer = 42,
                PostadresPostcode = "5678CD",
                PostadresType = PostadresType.postbusnummer,
                WoonplaatsNaam = "Testdorp",
            }
        );

    [Fact]
    public void Verzending_to_VerzendingResponseDto_defaults_null_addresses_parity()
    {
        // Covers the ??= new XxxDto() non-collection defaults: null correspondence-address
        // sub-objects on the domain entity must come out as non-null, empty DTOs on BOTH sides.
        var value = new Verzending
        {
            Id = Guid.NewGuid(),
            Betrokkene = "Betrokkene-1",
            AardRelatie = OneGround.ZGW.Documenten.DataModel.AardRelatie.afzender,
            Toelichting = "Toelichting-1",
            Contactpersoon = "Contactpersoon-1",
            BinnenlandsCorrespondentieAdres = null,
            BuitenlandsCorrespondentieAdres = null,
            CorrespondentiePostadres = null,
        };

        AssertParity<ResponsesV15.VerzendingResponseDto>(value);
    }

    [Fact]
    public void Verzending_to_VerzendingResponseDto_preserves_non_null_addresses_parity()
    {
        var value = new Verzending
        {
            Id = Guid.NewGuid(),
            Betrokkene = "Betrokkene-2",
            AardRelatie = OneGround.ZGW.Documenten.DataModel.AardRelatie.geadresseerde,
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

        AssertParity<ResponsesV15.VerzendingResponseDto>(value);
    }

    [Fact]
    public void Verzending_to_VerzendingRequestDto_merge_parity()
    {
        var informatieObject = new EnkelvoudigInformatieObject { Id = Guid.NewGuid() };

        var value = new Verzending
        {
            Id = Guid.NewGuid(),
            Betrokkene = "Betrokkene-3",
            AardRelatie = OneGround.ZGW.Documenten.DataModel.AardRelatie.afzender,
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

        AssertParity<RequestsV15.VerzendingRequestDto>(value);
    }
}
