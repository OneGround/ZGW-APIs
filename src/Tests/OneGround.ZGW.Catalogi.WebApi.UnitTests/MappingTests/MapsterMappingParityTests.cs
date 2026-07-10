using System;
using AutoFixture;
using AutoMapper;
using AutoMapper.Internal;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Newtonsoft.Json;
using OneGround.ZGW.Catalogi.DataModel;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.DataModel;
using OneGround.ZGW.Common.Web;
using OneGround.ZGW.Common.Web.Mapping.Mapster;
using OneGround.ZGW.Common.Web.Mapping.ValueResolvers;
using OneGround.ZGW.Common.Web.Models;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.DataAccess;
using Xunit;
using AutoMapperIMapper = AutoMapper.IMapper;
using ClassOmschrijvingGeneriek = OneGround.ZGW.Catalogi.DataModel.OmschrijvingGeneriek;
using Contracts1 = OneGround.ZGW.Catalogi.Contracts.v1;
using Contracts13 = OneGround.ZGW.Catalogi.Contracts.v1._3;
using EnumOmschrijvingGeneriek = OneGround.ZGW.Common.DataModel.OmschrijvingGeneriek;
using Filters = OneGround.ZGW.Catalogi.Web.Models.v1;
using Filters13 = OneGround.ZGW.Catalogi.Web.Models.v1._3;
using MapsterIMapper = MapsterMapper.IMapper;
using Queries = OneGround.ZGW.Catalogi.Contracts.v1.Queries;
using Queries12 = OneGround.ZGW.Catalogi.Contracts.v1._2.Queries;
using Queries13 = OneGround.ZGW.Catalogi.Contracts.v1._3.Queries;
using RegProfilesRoot = OneGround.ZGW.Catalogi.Web.MappingProfiles;
using RegProfilesV1 = OneGround.ZGW.Catalogi.Web.MappingProfiles.v1;
using RegProfilesV12 = OneGround.ZGW.Catalogi.Web.MappingProfiles.v1._2;
using RegProfilesV13 = OneGround.ZGW.Catalogi.Web.MappingProfiles.v1._3;
using Requests = OneGround.ZGW.Catalogi.Contracts.v1.Requests;
using Requests13 = OneGround.ZGW.Catalogi.Contracts.v1._3.Requests;
using Responses = OneGround.ZGW.Catalogi.Contracts.v1.Responses;
using Responses13 = OneGround.ZGW.Catalogi.Contracts.v1._3.Responses;

namespace OneGround.ZGW.Catalogi.WebApi.UnitTests.MappingTests;

/// <summary>
/// Temporary A/B parity guard (deleted once the AutoMapper profiles are removed in a later task):
/// maps identical inputs through both the still-present AutoMapper profiles and the new Mapster
/// registers for all 6 ZTC mapping profiles (v1 RequestToDomain/DomainToResponse, v1._2
/// RequestToDomain, v1._3 RequestToDomain/DomainToResponse, RequestToPagination) and asserts
/// byte-identical serialized JSON. This is the wholesale correctness proof for the migration --
/// every <c>CreateMap</c>/<c>NewConfig</c> pair across all 6 profiles/registers gets at least one
/// fact here (91 pairs total; 3 of those pairs are literally shared -- same source/dest types,
/// identical mapping code -- between v1 and v1._3 because v1._3 has no type of its own and reuses
/// v1's: GerelateerdeZaaktypeDto-&gt;ZaakTypeGerelateerdeZaakType, and both directions of
/// EigenschapSpecificatieDto&lt;-&gt;EigenschapSpecificatie -- those are covered once, not twice).
/// </summary>
public class MapsterMappingParityTests : IDisposable
{
    private readonly OmitOnRecursionFixture _fixture = new();
    private readonly Mock<IEntityUriService> _mockedUriService = new();
    private readonly AutoMapperIMapper _autoMapper;
    private readonly ServiceProvider _provider;
    private readonly IServiceScope _scope;
    private readonly MapsterIMapper _mapsterMapper;

    public MapsterMappingParityTests()
    {
        _fixture.Register<DateOnly>(() => DateOnly.FromDateTime(DateTime.UtcNow));
        _mockedUriService.Setup(s => s.GetUri(It.IsAny<IUrlEntity>())).Returns<IUrlEntity>(e => e.Url);

        // AutoMapper side: all 6 profiles, with every DI-backed resolver/mapping-action wired plus
        // the NullableEnumMapper (request side) that ports as RegisterNullableEnumRule on Mapster.
        var amConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile(new RegProfilesV1.DomainToResponseProfile());
            cfg.AddProfile(new RegProfilesV1.RequestToDomainProfile());
            cfg.AddProfile(new RegProfilesV12.RequestToDomainProfile());
            cfg.AddProfile(new RegProfilesV13.DomainToResponseProfile());
            cfg.AddProfile(new RegProfilesV13.RequestToDomainProfile());
            cfg.AddProfile(new RegProfilesRoot.RequestToPaginationProfile());
            cfg.Internal().Mappers.Insert(0, new NullableEnumMapper());
            cfg.ShouldMapMethod = _ => false;
        });
        _autoMapper = amConfig.CreateMapper(t =>
        {
            if (t == typeof(UrlResolver))
                return new UrlResolver(_mockedUriService.Object);
            if (t == typeof(MemberUrlResolver))
                return new MemberUrlResolver(_mockedUriService.Object);
            if (t == typeof(MemberUrlsResolver))
                return new MemberUrlsResolver(_mockedUriService.Object);

            // The live IMappingAction ports (Tasks 5 & 7). MapGerelateerdeZaakTypenResponse (v1) and
            // MapMergedGerelateerdeZaakTypenUrlBased (v1) / MapGerelateerdeZaakTypenResponse (v1._3) /
            // MapMergedGerelateerdeZaakTypen (v1._3, the pure identificatie-based variant, no DI) are
            // declared with no access modifier (internal) inside their profile .cs files, so this test
            // assembly cannot reference them via typeof(...) -- match by FullName and construct via
            // reflection instead (accessibility only gates compile-time references, not reflection).
            if (t.FullName == "OneGround.ZGW.Catalogi.Web.MappingProfiles.v1.MapGerelateerdeZaakTypenResponse")
                return Activator.CreateInstance(t, _mockedUriService.Object);
            if (t.FullName == "OneGround.ZGW.Catalogi.Web.MappingProfiles.v1.MapMergedGerelateerdeZaakTypenUrlBased")
                return Activator.CreateInstance(t, _mockedUriService.Object);
            if (t.FullName == "OneGround.ZGW.Catalogi.Web.MappingProfiles.v1._3.MapGerelateerdeZaakTypenResponse")
                return Activator.CreateInstance(t, _mockedUriService.Object);
            if (t.FullName == "OneGround.ZGW.Catalogi.Web.MappingProfiles.v1._3.MapMergedGerelateerdeZaakTypen")
                return Activator.CreateInstance(t);

            throw new NotImplementedException($"Mapper is missing the service: {t}");
        });

        // Mapster side: one TypeAdapterConfig mirroring all 4 AddZgwMapster global defaults, plus all
        // 6 registers -- exactly what production wires up via AddZgwMapster + config.Scan.
        var config = new TypeAdapterConfig();
        config.Default.MaxDepth(200);
        config.Default.AddDestinationTransform(DestinationTransform.EmptyCollectionIfNull);
        config.Default.NameMatchingStrategy(NameMatchingStrategy.IgnoreCase);
        config.RegisterNullableEnumRule();
        new RegProfilesRoot.RequestToPaginationRegister().Register(config);
        new RegProfilesV12.RequestToDomainRegister().Register(config);
        new RegProfilesV1.RequestToDomainRegister().Register(config);
        new RegProfilesV1.DomainToResponseRegister().Register(config);
        new RegProfilesV13.RequestToDomainRegister().Register(config);
        new RegProfilesV13.DomainToResponseRegister().Register(config);
        config.Compile();

        var services = new ServiceCollection();
        services.AddSingleton(_mockedUriService.Object);
        services.AddSingleton(config);
        services.AddScoped<MapsterIMapper, ServiceMapper>();
        _provider = services.BuildServiceProvider();
        _scope = _provider.CreateScope();
        _mapsterMapper = _scope.ServiceProvider.GetRequiredService<MapsterIMapper>();

        // ---- Shared AutoFixture customizations for format-sensitive members (valid ISO period /
        // yyyy-MM-dd date / enum-name strings) needed so both mappers actually exercise their real
        // conversion logic rather than throwing or short-circuiting on a random AutoFixture string. ----

        _fixture.Customize<Contracts1.GerelateerdeZaaktypeDto>(c => c.With(p => p.AardRelatie, _fixture.Create<AardRelatie>().ToString()));

        _fixture.Customize<Requests.ZaakTypeRequestDto>(c =>
            c.With(p => p.Doorlooptijd, "P1Y")
                .With(p => p.Servicenorm, "P35D")
                .With(p => p.VerlengingsTermijn, "P1M")
                .With(p => p.VertrouwelijkheidAanduiding, _fixture.Create<VertrouwelijkheidAanduiding>().ToString())
                .With(p => p.IndicatieInternOfExtern, _fixture.Create<IndicatieInternOfExtern>().ToString())
                .With(p => p.EindeGeldigheid, "2020-11-11")
                .With(p => p.BeginGeldigheid, "2020-11-12")
                .With(p => p.VersieDatum, "2020-11-13")
        );

        _fixture.Customize<Requests.RolTypeRequestDto>(c =>
            c.With(p => p.OmschrijvingGeneriek, _fixture.Create<EnumOmschrijvingGeneriek>().ToString())
        );

        _fixture.Customize<Requests.ZaakTypeInformatieObjectTypeRequestDto>(c => c.With(p => p.Richting, _fixture.Create<Richting>().ToString()));

        _fixture.Customize<Contracts1.BronDatumArchiefProcedureDto>(c =>
            c.With(p => p.ProcesTermijn, "P1Y")
                .With(p => p.Afleidingswijze, _fixture.Create<Afleidingswijze>().ToString())
                .With(p => p.ObjectType, _fixture.Create<ObjectType>().ToString())
        );

        _fixture.Customize<Requests.ResultaatTypeRequestDto>(c =>
            c.With(p => p.ArchiefActieTermijn, "P1Y").With(p => p.ArchiefNominatie, _fixture.Create<ArchiefNominatie>().ToString())
        );

        _fixture.Customize<Requests.InformatieObjectTypeRequestDto>(c =>
            c.With(p => p.EindeGeldigheid, "2020-11-11")
                .With(p => p.BeginGeldigheid, "2020-11-12")
                .With(p => p.VertrouwelijkheidAanduiding, VertrouwelijkheidAanduiding.confidentieel.ToString())
        );

        _fixture.Customize<Contracts1.EigenschapSpecificatieDto>(c => c.With(p => p.Formaat, _fixture.Create<Formaat>().ToString()));

        _fixture.Customize<Requests.BesluitTypeRequestDto>(c =>
            c.With(p => p.ReactieTermijn, "P1Y")
                .With(p => p.PublicatieTermijn, "P2Y")
                .With(p => p.BeginGeldigheid, "2020-11-12")
                .With(p => p.EindeGeldigheid, "2020-11-11")
        );

        _fixture.Customize<Queries.GetAllResultaatTypenQueryParameters>(c => c.With(p => p.Status, _fixture.Create<ConceptStatus>().ToString()));

        _fixture.Customize<Queries.GetAllInformatieObjectTypenQueryParameters>(c => c.With(p => p.Status, ConceptStatus.alles.ToString()));

        _fixture.Customize<Queries.GetAllBesluitTypenQueryParameters>(c =>
            c.With(p => p.DatumGeldigheid, "2024-03-15").With(p => p.Status, _fixture.Create<ConceptStatus>().ToString())
        );

        _fixture.Customize<Queries.GetAllZaakTypenQueryParameters>(c =>
            c.With(p => p.Trefwoorden, "foo,bar")
                .With(p => p.DatumGeldigheid, "2020-11-12")
                .With(p => p.Status, _fixture.Create<ConceptStatus>().ToString())
        );

        // Plain convention-mapped (no explicit .Map line in either register) string->enum Status members --
        // need a valid enum-name string so both mappers exercise the actual conversion rather than throwing.
        _fixture.Customize<Queries.GetAllRolTypenQueryParameters>(c => c.With(p => p.Status, _fixture.Create<ConceptStatus>().ToString()));
        _fixture.Customize<Queries.GetAllEigenschappenQueryParameters>(c => c.With(p => p.Status, _fixture.Create<ConceptStatus>().ToString()));
        _fixture.Customize<Queries.GetAllStatusTypenQueryParameters>(c => c.With(p => p.Status, _fixture.Create<ConceptStatus>().ToString()));

        // Domain-side: ZaakTypeDeelZaakType.DeelZaakType needs its own Id/Url computed correctly, and
        // OmitOnRecursionBehavior alone would leave it default -- mirrors the v1 DomainToResponseProfileTests
        // constructor customization exactly (needed so DeelZaakTypen resolves to a real URL, not null).
        _fixture.Customize<ZaakTypeDeelZaakType>(c => c.Do(z => z.DeelZaakType = new ZaakType { Id = _fixture.Create<Guid>() }));
    }

    public void Dispose()
    {
        _scope.Dispose();
        _provider.Dispose();
    }

    private void AssertParity<TDest>(object source)
    {
        var am = JsonConvert.SerializeObject(_autoMapper.Map<TDest>(source));
        var ms = JsonConvert.SerializeObject(_mapsterMapper.Map<TDest>(source));
        Assert.Equal(am, ms);
    }

    private static ZaakType RootedZaakType() =>
        new()
        {
            Id = Guid.NewGuid(),
            Identificatie = "ZT1",
            Catalogus = new Catalogus { Id = Guid.NewGuid() },
        };

    // =====================================================================================
    // RequestToPaginationRegister / RequestToPaginationProfile (1 pair)
    // =====================================================================================

    [Fact]
    public void PaginationQuery_to_PaginationFilter_parity() => AssertParity<PaginationFilter>(_fixture.Create<PaginationQuery>());

    // =====================================================================================
    // v1._2 RequestToDomainRegister / RequestToDomainProfile (1 pair)
    // =====================================================================================

    [Fact]
    public void GetAllInformatieObjectTypenQueryParameters_v1_2_to_Filter_parity() =>
        AssertParity<Filters.GetAllInformatieObjectTypenFilter>(
            new Queries12.GetAllInformatieObjectTypenQueryParameters
            {
                Catalogus = "https://example.test/catalogussen/abc",
                Status = "definitief",
                DatumGeldigheid = "2024-03-15",
                Omschrijving = "some description",
            }
        );

    [Fact]
    public void GetAllInformatieObjectTypenQueryParameters_v1_2_with_unparseable_date_parity() =>
        AssertParity<Filters.GetAllInformatieObjectTypenFilter>(
            new Queries12.GetAllInformatieObjectTypenQueryParameters { DatumGeldigheid = "not-a-date" }
        );

    // =====================================================================================
    // v1 RequestToDomainRegister / RequestToDomainProfile (22 pairs)
    // =====================================================================================

    [Fact]
    public void GetAllZaakTypenQueryParameters_to_Filter_parity() =>
        AssertParity<Filters.GetAllZaakTypenFilter>(_fixture.Create<Queries.GetAllZaakTypenQueryParameters>());

    [Fact]
    public void GetAllZaakTypenQueryParameters_with_unparseable_date_parity() =>
        AssertParity<Filters.GetAllZaakTypenFilter>(
            new Queries.GetAllZaakTypenQueryParameters { DatumGeldigheid = "not-a-date", Trefwoorden = "a,b" }
        );

    [Fact]
    public void ZaakTypeRequestDto_to_ZaakType_parity() => AssertParity<ZaakType>(_fixture.Create<Requests.ZaakTypeRequestDto>());

    [Fact]
    public void ZaakTypeRequestDto_with_empty_GerelateerdeZaakTypen_to_ZaakType_parity()
    {
        // Note: a second _fixture.Customize<T> call for the same T does not merge with the first --
        // it fully replaces how T is built -- so the override is applied directly on the created
        // instance instead of via a second Customize call (which would silently drop the constructor's
        // format-sensitive customizations for this same type and reintroduce unparseable random strings).
        var source = _fixture.Create<Requests.ZaakTypeRequestDto>();
        source.GerelateerdeZaakTypen = [];
        AssertParity<ZaakType>(source);
    }

    [Fact]
    public void GerelateerdeZaaktypeDto_to_ZaakTypeGerelateerdeZaakType_parity() =>
        // Shared: this exact (source, dest) pair is registered identically by both the v1 and v1._3
        // RequestToDomainRegister (v1._3 has no GerelateerdeZaaktypeDto of its own and reuses v1's) --
        // one fact covers both registrations, they are line-for-line the same mapping.
        AssertParity<ZaakTypeGerelateerdeZaakType>(_fixture.Create<Contracts1.GerelateerdeZaaktypeDto>());

    [Fact]
    public void ReferentieProcesDto_to_ReferentieProces_parity() => AssertParity<ReferentieProces>(_fixture.Create<Contracts1.ReferentieProcesDto>());

    [Fact]
    public void GetAllStatusTypenQueryParameters_v1_to_Filter_parity() =>
        AssertParity<Filters.GetAllStatusTypenFilter>(_fixture.Create<Queries.GetAllStatusTypenQueryParameters>());

    [Fact]
    public void StatusTypeRequestDto_v1_to_StatusType_parity() => AssertParity<StatusType>(_fixture.Create<Requests.StatusTypeRequestDto>());

    [Fact]
    public void GetAllRolTypenQueryParameters_v1_to_Filter_parity() =>
        AssertParity<Filters.GetAllRolTypenFilter>(_fixture.Create<Queries.GetAllRolTypenQueryParameters>());

    [Fact]
    public void RolTypeRequestDto_v1_to_RolType_parity() => AssertParity<RolType>(_fixture.Create<Requests.RolTypeRequestDto>());

    [Fact]
    public void GetAllZaakTypeInformatieObjectTypenQueryParameters_v1_to_Filter_parity()
    {
        var source = new Queries.GetAllZaakTypeInformatieObjectTypenQueryParameters
        {
            ZaakType = "https://example.test/zaaktypen/1",
            InformatieObjectType = "https://example.test/informatieobjecttypen/1",
            Richting = Richting.uitgaand.ToString(),
            Status = ConceptStatus.concept.ToString(),
        };
        AssertParity<Filters.GetAllZaakTypeInformatieObjectTypenFilter>(source);
    }

    [Fact]
    public void GetAllZaakTypeInformatieObjectTypenQueryParameters_v1_with_empty_Richting_parity() =>
        AssertParity<Filters.GetAllZaakTypeInformatieObjectTypenFilter>(
            new Queries.GetAllZaakTypeInformatieObjectTypenQueryParameters { Richting = string.Empty }
        );

    [Fact]
    public void ZaakTypeInformatieObjectTypeRequestDto_v1_to_ZaakTypeInformatieObjectType_parity()
    {
        var source = _fixture.Create<Requests.ZaakTypeInformatieObjectTypeRequestDto>();
        source.Richting = Richting.uitgaand.ToString();
        AssertParity<ZaakTypeInformatieObjectType>(source);
    }

    [Fact]
    public void GetAllCatalogussenQueryParameters_to_Filter_parity()
    {
        var source = new Queries.GetAllCatalogussenQueryParameters { Domein__in = "AAA, BBB", Rsin__in = "999993653, 999993654" };
        AssertParity<Filters.GetAllCatalogussenFilter>(source);
    }

    [Fact]
    public void CatalogusRequestDto_v1_to_Catalogus_parity() => AssertParity<Catalogus>(_fixture.Create<Requests.CatalogusRequestDto>());

    [Fact]
    public void ResultaatTypeRequestDto_v1_to_ResultaatType_parity() =>
        AssertParity<ResultaatType>(_fixture.Create<Requests.ResultaatTypeRequestDto>());

    [Fact]
    public void ResultaatTypeRequestDto_v1_with_empty_ArchiefNominatie_parity()
    {
        var source = new Requests.ResultaatTypeRequestDto { ArchiefNominatie = string.Empty, ArchiefActieTermijn = "P1Y" };
        AssertParity<ResultaatType>(source);
    }

    [Fact]
    public void BronDatumArchiefProcedureDto_v1_to_BronDatumArchiefProcedure_parity() =>
        AssertParity<BronDatumArchiefProcedure>(_fixture.Create<Contracts1.BronDatumArchiefProcedureDto>());

    [Fact]
    public void BronDatumArchiefProcedureDto_v1_with_empty_ObjectType_parity()
    {
        var source = new Contracts1.BronDatumArchiefProcedureDto { ProcesTermijn = "P1Y", ObjectType = string.Empty };
        AssertParity<BronDatumArchiefProcedure>(source);
    }

    [Fact]
    public void GetAllResultaatTypenQueryParameters_v1_to_Filter_parity() =>
        AssertParity<Filters.GetAllResultaatTypenFilter>(_fixture.Create<Queries.GetAllResultaatTypenQueryParameters>());

    [Fact]
    public void GetAllInformatieObjectTypenQueryParameters_v1_to_Filter_parity() =>
        AssertParity<Filters.GetAllInformatieObjectTypenFilter>(_fixture.Create<Queries.GetAllInformatieObjectTypenQueryParameters>());

    [Fact]
    public void InformatieObjectTypeRequestDto_v1_to_InformatieObjectType_parity() =>
        AssertParity<InformatieObjectType>(_fixture.Create<Requests.InformatieObjectTypeRequestDto>());

    [Fact]
    public void EigenschapRequestDto_v1_to_Eigenschap_parity() => AssertParity<Eigenschap>(_fixture.Create<Requests.EigenschapRequestDto>());

    [Fact]
    public void EigenschapSpecificatieDto_to_EigenschapSpecificatie_parity() =>
        // Shared: this exact (source, dest) pair is registered identically by both the v1 and v1._3
        // RequestToDomainRegister (v1._3 has no EigenschapSpecificatieDto of its own and reuses v1's).
        AssertParity<EigenschapSpecificatie>(_fixture.Create<Contracts1.EigenschapSpecificatieDto>());

    [Fact]
    public void GetAllEigenschappenQueryParameters_v1_to_Filter_parity() =>
        AssertParity<Filters.GetAllEigenschappenFilter>(_fixture.Create<Queries.GetAllEigenschappenQueryParameters>());

    [Fact]
    public void BesluitTypeRequestDto_v1_to_BesluitType_parity() => AssertParity<BesluitType>(_fixture.Create<Requests.BesluitTypeRequestDto>());

    [Fact]
    public void GetAllBesluitTypenQueryParameters_v1_to_Filter_parity() =>
        AssertParity<Filters.GetAllBesluitTypenFilter>(_fixture.Create<Queries.GetAllBesluitTypenQueryParameters>());

    // =====================================================================================
    // v1._3 RequestToDomainRegister / RequestToDomainProfile (23 pairs; 2 shared with v1, see above)
    // =====================================================================================

    // Official RvIG test BSN value (elfproef-valid, never assigned to a real person/organization) --
    // reused here as a stand-in RSIN, which shares the same 9-digit elfproef structure.
    private const string TestRsin = "999993653";

    [Fact]
    public void ZaakTypeRequestDto_v1_3_to_ZaakType_parity()
    {
        var source = new Requests13.ZaakTypeRequestDto
        {
            Identificatie = "ZT1",
            Omschrijving = "omschrijving",
            OmschrijvingGeneriek = "omschrijving generiek",
            VertrouwelijkheidAanduiding = VertrouwelijkheidAanduiding.openbaar.ToString(),
            Doel = "doel",
            Aanleiding = "aanleiding",
            IndicatieInternOfExtern = IndicatieInternOfExtern.intern.ToString(),
            HandelingInitiator = "handeling initiator",
            Onderwerp = "onderwerp",
            HandelingBehandelaar = "handeling behandelaar",
            Doorlooptijd = "P1Y",
            Servicenorm = "P35D",
            VerlengingsTermijn = "P1M",
            BeginGeldigheid = "2020-11-12",
            EindeGeldigheid = "2020-11-11",
            BeginObject = "2020-01-02",
            EindeObject = "2020-01-03",
            VersieDatum = "2020-11-13",
            BronCatalogus = new Contracts13.BronCatalogusDto
            {
                Url = "https://example.test/catalogussen/1",
                Domein = "DOM01",
                Rsin = TestRsin,
            },
            BronZaaktype = new Contracts13.BronZaaktypeDto
            {
                Url = "https://example.test/zaaktypen/1",
                Identificatie = "BRONZT1",
                Omschrijving = "bron omschrijving",
            },
            GerelateerdeZaakTypen =
            [
                new Contracts1.GerelateerdeZaaktypeDto
                {
                    ZaakType = "https://example.test/zaaktypen/2",
                    AardRelatie = AardRelatie.vervolg.ToString(),
                    Toelichting = "relatie toelichting",
                },
            ],
        };
        AssertParity<ZaakType>(source);
    }

    [Fact]
    public void BronCatalogusDto_to_BronCatalogus_parity() =>
        AssertParity<BronCatalogus>(
            new Contracts13.BronCatalogusDto
            {
                Url = "https://example.test/catalogussen/1",
                Domein = "DOM01",
                Rsin = TestRsin,
            }
        );

    [Fact]
    public void BronZaaktypeDto_to_BronZaaktype_parity() =>
        AssertParity<BronZaaktype>(
            new Contracts13.BronZaaktypeDto
            {
                Url = "https://example.test/zaaktypen/1",
                Identificatie = "BRONZT1",
                Omschrijving = "bron omschrijving",
            }
        );

    [Fact]
    public void GetAllStatusTypenQueryParameters_v1_3_to_Filter_parity() =>
        AssertParity<Filters13.GetAllStatusTypenFilter>(new Queries13.GetAllStatusTypenQueryParameters { DatumGeldigheid = "2024-03-15" });

    [Fact]
    public void GetAllStatusTypenQueryParameters_v1_3_with_unparseable_date_parity() =>
        AssertParity<Filters13.GetAllStatusTypenFilter>(new Queries13.GetAllStatusTypenQueryParameters { DatumGeldigheid = "not-a-date" });

    [Fact]
    public void StatusTypeRequestDto_v1_3_to_StatusType_parity()
    {
        var source = new Requests13.StatusTypeRequestDto
        {
            Omschrijving = "omschrijving",
            OmschrijvingGeneriek = "omschrijving generiek",
            StatusTekst = "status tekst",
            VolgNummer = 1,
            Doorlooptijd = "P2D",
            BeginGeldigheid = "2020-11-12",
            EindeGeldigheid = "2020-11-13",
            BeginObject = "2020-01-02",
            EindeObject = "2020-01-03",
            CheckListItemStatustypes =
            [
                new Contracts13.CheckListItemStatusTypeDto
                {
                    ItemNaam = "item",
                    Toelichting = "toelichting",
                    Vraagstelling = "vraag",
                    Verplicht = true,
                },
            ],
        };
        AssertParity<StatusType>(source);
    }

    [Fact]
    public void CheckListItemStatusTypeDto_to_CheckListItemStatusType_parity() =>
        AssertParity<CheckListItemStatusType>(
            new Contracts13.CheckListItemStatusTypeDto
            {
                ItemNaam = "item",
                Toelichting = "toelichting",
                Vraagstelling = "vraag",
                Verplicht = true,
            }
        );

    [Fact]
    public void GetAllRolTypenQueryParameters_v1_3_to_Filter_parity() =>
        AssertParity<Filters13.GetAllRolTypenFilter>(new Queries13.GetAllRolTypenQueryParameters { DatumGeldigheid = "2024-03-15" });

    [Fact]
    public void GetAllRolTypenQueryParameters_v1_3_with_unparseable_date_parity() =>
        AssertParity<Filters13.GetAllRolTypenFilter>(new Queries13.GetAllRolTypenQueryParameters { DatumGeldigheid = "not-a-date" });

    [Fact]
    public void RolTypeRequestDto_v1_3_to_RolType_parity() =>
        AssertParity<RolType>(
            new Requests13.RolTypeRequestDto
            {
                Omschrijving = "omschrijving",
                OmschrijvingGeneriek = EnumOmschrijvingGeneriek.behandelaar.ToString(),
                BeginGeldigheid = "2020-11-12",
                EindeGeldigheid = "2020-11-13",
                BeginObject = "2020-01-02",
                EindeObject = "2020-01-03",
            }
        );

    [Fact]
    public void GetAllZaakTypeInformatieObjectTypenQueryParameters_v1_3_to_Filter_parity()
    {
        var source = new Queries13.GetAllZaakTypeInformatieObjectTypenQueryParameters
        {
            ZaakType = "https://example.test/zaaktypen/1",
            InformatieObjectType = "https://example.test/informatieobjecttypen/1",
            Richting = Richting.uitgaand.ToString(),
            Status = ConceptStatus.concept.ToString(),
        };
        AssertParity<Filters13.GetAllZaakTypeInformatieObjectTypenFilter>(source);
    }

    [Fact]
    public void GetAllZaakTypeInformatieObjectTypenQueryParameters_v1_3_with_empty_Richting_parity() =>
        AssertParity<Filters13.GetAllZaakTypeInformatieObjectTypenFilter>(
            new Queries13.GetAllZaakTypeInformatieObjectTypenQueryParameters { Richting = string.Empty }
        );

    [Fact]
    public void ZaakTypeInformatieObjectTypeRequestDto_v1_3_to_ZaakTypeInformatieObjectType_parity() =>
        AssertParity<ZaakTypeInformatieObjectType>(
            new Requests13.ZaakTypeInformatieObjectTypeRequestDto
            {
                ZaakType = "https://example.test/zaaktypen/1",
                InformatieObjectType = "https://example.test/informatieobjecttypen/1",
                VolgNummer = 3,
                Richting = Richting.inkomend.ToString(),
            }
        );

    [Fact]
    public void CatalogusRequestDto_v1_3_to_Catalogus_parity() =>
        AssertParity<Catalogus>(
            new Requests13.CatalogusRequestDto
            {
                Domein = "DOM01",
                Rsin = TestRsin,
                ContactpersoonBeheerNaam = "Jan",
                ContactpersoonBeheerTelefoonnummer = "0101234567",
                ContactpersoonBeheerEmailadres = "jan@example.test",
                Naam = "Catalogus naam",
                Versie = "1",
                BegindatumVersie = "2021-05-04",
            }
        );

    [Fact]
    public void CatalogusRequestDto_v1_3_with_unparseable_BegindatumVersie_parity() =>
        AssertParity<Catalogus>(new Requests13.CatalogusRequestDto { BegindatumVersie = "not-a-date" });

    [Fact]
    public void ResultaatTypeRequestDto_v1_3_to_ResultaatType_parity() =>
        AssertParity<ResultaatType>(
            new Requests13.ResultaatTypeRequestDto
            {
                Omschrijving = "omschrijving",
                ResultaatTypeOmschrijving = "resultaattype omschrijving",
                SelectieLijstKlasse = "selectielijstklasse",
                Toelichting = "toelichting",
                ArchiefNominatie = ArchiefNominatie.blijvend_bewaren.ToString(),
                ArchiefActieTermijn = "P1Y",
                ProcesTermijn = "P2Y",
                BeginGeldigheid = "2020-11-12",
                EindeGeldigheid = "2020-11-13",
                BeginObject = "2020-01-02",
                EindeObject = "2020-01-03",
            }
        );

    [Fact]
    public void ResultaatTypeRequestDto_v1_3_with_empty_ArchiefNominatie_parity() =>
        AssertParity<ResultaatType>(
            new Requests13.ResultaatTypeRequestDto
            {
                ArchiefNominatie = string.Empty,
                ArchiefActieTermijn = "P1Y",
                ProcesTermijn = "P1Y",
            }
        );

    [Fact]
    public void GetAllResultaatTypenQueryParameters_v1_3_to_Filter_parity() =>
        AssertParity<Filters13.GetAllResultaatTypenFilter>(new Queries13.GetAllResultaatTypenQueryParameters { DatumGeldigheid = "2024-03-15" });

    [Fact]
    public void InformatieObjectTypeRequestDto_v1_3_to_InformatieObjectType_parity() =>
        AssertParity<InformatieObjectType>(
            new Requests13.InformatieObjectTypeRequestDto
            {
                Omschrijving = "omschrijving",
                VertrouwelijkheidAanduiding = VertrouwelijkheidAanduiding.confidentieel.ToString(),
                BeginGeldigheid = "2020-11-12",
                EindeGeldigheid = "2020-11-13",
                BeginObject = "2020-01-02",
                EindeObject = "2020-01-03",
            }
        );

    [Fact]
    public void GetAllEigenschappenQueryParameters_v1_3_to_Filter_parity() =>
        AssertParity<Filters13.GetAllEigenschappenFilter>(new Queries13.GetAllEigenschappenQueryParameters { DatumGeldigheid = "2024-03-15" });

    [Fact]
    public void EigenschapRequestDto_v1_3_to_Eigenschap_parity() =>
        AssertParity<Eigenschap>(
            new Requests13.EigenschapRequestDto
            {
                Naam = "naam",
                Definitie = "definitie",
                Toelichting = "toelichting",
                BeginGeldigheid = "2020-11-12",
                EindeGeldigheid = "2020-11-13",
                BeginObject = "2020-01-02",
                EindeObject = "2020-01-03",
                Specificatie = new Contracts1.EigenschapSpecificatieDto
                {
                    Groep = "groep",
                    Formaat = Formaat.tekst.ToString(),
                    Lengte = "10",
                    Kardinaliteit = "1",
                },
            }
        );

    [Fact]
    public void BesluitTypeRequestDto_v1_3_to_BesluitType_parity() =>
        AssertParity<BesluitType>(
            new Requests13.BesluitTypeRequestDto
            {
                Omschrijving = "omschrijving",
                OmschrijvingGeneriek = "omschrijving generiek",
                BesluitCategorie = "categorie",
                ReactieTermijn = "P1Y",
                PublicatieTermijn = "P2Y",
                Toelichting = "toelichting",
                BeginGeldigheid = "2020-11-12",
                EindeGeldigheid = "2020-11-13",
                BeginObject = "2020-01-02",
                EindeObject = "2020-01-03",
            }
        );

    [Fact]
    public void GetAllBesluitTypenQueryParameters_v1_3_to_Filter_parity() =>
        AssertParity<Filters13.GetAllBesluitTypenFilter>(new Queries13.GetAllBesluitTypenQueryParameters { DatumGeldigheid = "2024-03-15" });

    [Fact]
    public void GetAllZaakObjectTypenQueryParameters_to_Filter_parity()
    {
        var source = new Queries13.GetAllZaakObjectTypenQueryParameters
        {
            AnderObjectType = "true",
            DatumBeginGeldigheid = "2020-11-12",
            DatumEindeGeldigheid = "2020-11-13",
            DatumGeldigheid = "2020-11-14",
            ObjectType = "objecttype",
            RelatieOmschrijving = "relatie omschrijving",
            ZaakType = "https://example.test/zaaktypen/1",
        };
        AssertParity<Filters13.GetAllZaakObjectTypenFilter>(source);
    }

    [Fact]
    public void GetAllZaakObjectTypenQueryParameters_with_null_AnderObjectType_parity() =>
        AssertParity<Filters13.GetAllZaakObjectTypenFilter>(new Queries13.GetAllZaakObjectTypenQueryParameters { AnderObjectType = null });

    [Fact]
    public void ZaakObjectTypeRequestDto_to_ZaakObjectType_parity() =>
        AssertParity<ZaakObjectType>(
            new Requests13.ZaakObjectTypeRequestDto
            {
                AnderObjectType = true,
                ObjectType = "objecttype",
                RelatieOmschrijving = "relatie omschrijving",
                BeginGeldigheid = "2020-11-12",
                EindeGeldigheid = "2020-11-13",
                BeginObject = "2020-01-02",
                EindeObject = "2020-01-03",
            }
        );

    [Fact]
    public void ZaakObjectTypeRequestDto_with_unparseable_BeginObject_parity() =>
        AssertParity<ZaakObjectType>(new Requests13.ZaakObjectTypeRequestDto { BeginGeldigheid = "2020-11-12", BeginObject = "not-a-date" });

    // =====================================================================================
    // v1 DomainToResponseRegister / DomainToResponseProfile (20 pairs)
    // =====================================================================================

    [Fact]
    public void ZaakType_v1_to_ZaakTypeResponseDto_parity()
    {
        _fixture.Customize<ZaakType>(c =>
            c.With(p => p.VerlengingsTermijn, NodaTime.Period.FromDays(3))
                .With(p => p.Servicenorm, NodaTime.Period.FromDays(4))
                .With(p => p.Doorlooptijd, NodaTime.Period.FromDays(5))
        );
        AssertParity<Responses.ZaakTypeResponseDto>(_fixture.Create<ZaakType>());
    }

    [Fact]
    public void ZaakType_v1_with_null_relations_to_ZaakTypeResponseDto_parity()
    {
        var source = new ZaakType
        {
            Id = Guid.NewGuid(),
            ZaakTypeInformatieObjectTypen = null,
            ZaakTypeDeelZaakTypen = null,
            ZaakTypeBesluitTypen = null,
            ZaakTypeGerelateerdeZaakTypen = [],
        };
        AssertParity<Responses.ZaakTypeResponseDto>(source);
    }

    [Fact]
    public void ZaakType_v1_with_GerelateerdeZaakTypen_to_ZaakTypeResponseDto_parity()
    {
        var gerelateerd = new ZaakType { Id = Guid.NewGuid() };
        var relation = new ZaakTypeGerelateerdeZaakType
        {
            AardRelatie = AardRelatie.vervolg,
            Toelichting = "toelichting",
            GerelateerdeZaakType = gerelateerd,
        };
        var source = new ZaakType { Id = Guid.NewGuid(), ZaakTypeGerelateerdeZaakTypen = [relation] };
        AssertParity<Responses.ZaakTypeResponseDto>(source);
    }

    [Fact]
    public void ReferentieProces_to_ReferentieProcesDto_parity() => AssertParity<Contracts1.ReferentieProcesDto>(_fixture.Create<ReferentieProces>());

    [Fact]
    public void ZaakType_v1_to_ZaakTypeRequestDto_parity()
    {
        _fixture.Customize<ZaakType>(c =>
            c.With(p => p.VerlengingsTermijn, NodaTime.Period.FromDays(3))
                .With(p => p.Servicenorm, NodaTime.Period.FromDays(4))
                .With(p => p.Doorlooptijd, NodaTime.Period.FromDays(5))
        );
        AssertParity<Requests.ZaakTypeRequestDto>(_fixture.Create<ZaakType>());
    }

    [Fact]
    public void ZaakType_v1_with_null_DeelZaakTypen_BesluitTypen_to_ZaakTypeRequestDto_parity()
    {
        var source = new ZaakType
        {
            Id = Guid.NewGuid(),
            ZaakTypeDeelZaakTypen = null,
            ZaakTypeBesluitTypen = null,
            ZaakTypeGerelateerdeZaakTypen = [],
        };
        AssertParity<Requests.ZaakTypeRequestDto>(source);
    }

    [Fact]
    public void StatusType_v1_to_StatusTypeResponseDto_parity() => AssertParity<Responses.StatusTypeResponseDto>(_fixture.Create<StatusType>());

    [Fact]
    public void StatusType_v1_with_null_OmschrijvingGeneriek_StatusTekst_to_StatusTypeResponseDto_parity()
    {
        var source = new StatusType
        {
            Id = Guid.NewGuid(),
            OmschrijvingGeneriek = null,
            StatusTekst = null,
        };
        AssertParity<Responses.StatusTypeResponseDto>(source);
    }

    [Fact]
    public void StatusType_v1_to_StatusTypeRequestDto_parity() => AssertParity<Requests.StatusTypeRequestDto>(_fixture.Create<StatusType>());

    [Fact]
    public void RolType_v1_to_RolTypeResponseDto_parity() => AssertParity<Responses.RolTypeResponseDto>(_fixture.Create<RolType>());

    [Fact]
    public void RolType_v1_to_RolTypeRequestDto_parity() => AssertParity<Requests.RolTypeRequestDto>(_fixture.Create<RolType>());

    [Fact]
    public void ZaakTypeInformatieObjectType_v1_to_ZaakTypeInformatieObjectTypeResponseDto_parity() =>
        AssertParity<Responses.ZaakTypeInformatieObjectTypeResponseDto>(_fixture.Create<ZaakTypeInformatieObjectType>());

    [Fact]
    public void ZaakTypeInformatieObjectType_v1_to_ZaakTypeInformatieObjectTypeRequestDto_parity() =>
        AssertParity<Requests.ZaakTypeInformatieObjectTypeRequestDto>(_fixture.Create<ZaakTypeInformatieObjectType>());

    [Fact]
    public void ResultaatType_v1_to_ResultaatTypeResponseDto_parity()
    {
        _fixture.Customize<ResultaatType>(c => c.With(p => p.ArchiefActieTermijn, NodaTime.Period.FromDays(5)));
        AssertParity<Responses.ResultaatTypeResponseDto>(_fixture.Create<ResultaatType>());
    }

    [Fact]
    public void ResultaatType_v1_with_zero_ArchiefActieTermijn_to_ResultaatTypeResponseDto_parity()
    {
        _fixture.Customize<ResultaatType>(c => c.With(p => p.ArchiefActieTermijn, NodaTime.Period.FromDays(0)));
        AssertParity<Responses.ResultaatTypeResponseDto>(_fixture.Create<ResultaatType>());
    }

    [Fact]
    public void ResultaatType_v1_to_ResultaatTypeRequestDto_parity() =>
        AssertParity<Requests.ResultaatTypeRequestDto>(_fixture.Create<ResultaatType>());

    [Fact]
    public void BronDatumArchiefProcedure_v1_to_BronDatumArchiefProcedureDto_parity()
    {
        _fixture.Customize<BronDatumArchiefProcedure>(c => c.With(p => p.ProcesTermijn, NodaTime.Period.FromDays(2)));
        AssertParity<Contracts1.BronDatumArchiefProcedureDto>(_fixture.Create<BronDatumArchiefProcedure>());
    }

    [Fact]
    public void Catalogus_v1_to_CatalogusResponseDto_parity() => AssertParity<Responses.CatalogusResponseDto>(_fixture.Create<Catalogus>());

    [Fact]
    public void InformatieObjectType_v1_to_InformatieObjectTypeResponseDto_parity() =>
        AssertParity<Responses.InformatieObjectTypeResponseDto>(_fixture.Create<InformatieObjectType>());

    [Fact]
    public void InformatieObjectType_v1_with_no_relations_to_InformatieObjectTypeResponseDto_parity()
    {
        var source = new InformatieObjectType
        {
            Id = Guid.NewGuid(),
            InformatieObjectTypeZaakTypen = null,
            InformatieObjectTypeBesluitTypen = null,
        };
        AssertParity<Responses.InformatieObjectTypeResponseDto>(source);
    }

    [Fact]
    public void InformatieObjectType_v1_to_InformatieObjectTypeRequestDto_parity() =>
        AssertParity<Requests.InformatieObjectTypeRequestDto>(_fixture.Create<InformatieObjectType>());

    [Fact]
    public void EigenschapSpecificatie_v1_to_EigenschapSpecificatieDto_parity() =>
        // Shared: this exact (source, dest) pair is registered identically by both the v1 and v1._3
        // DomainToResponseRegister (v1._3 reuses the v1 EigenschapSpecificatieDto contract type).
        AssertParity<Contracts1.EigenschapSpecificatieDto>(_fixture.Create<EigenschapSpecificatie>());

    [Fact]
    public void Eigenschap_v1_to_EigenschapResponseDto_parity() => AssertParity<Responses.EigenschapResponseDto>(_fixture.Create<Eigenschap>());

    [Fact]
    public void Eigenschap_v1_to_EigenschapRequestDto_parity() => AssertParity<Requests.EigenschapRequestDto>(_fixture.Create<Eigenschap>());

    [Fact]
    public void BesluitType_v1_to_BesluitTypeResponseDto_parity()
    {
        _fixture.Customize<BesluitType>(c =>
            c.With(p => p.ReactieTermijn, NodaTime.Period.FromDays(4)).With(p => p.PublicatieTermijn, NodaTime.Period.FromDays(5))
        );
        AssertParity<Responses.BesluitTypeResponseDto>(_fixture.Create<BesluitType>());
    }

    [Fact]
    public void BesluitType_v1_with_null_relations_to_BesluitTypeResponseDto_parity()
    {
        var source = new BesluitType
        {
            Id = Guid.NewGuid(),
            ReactieTermijn = NodaTime.Period.FromDays(0),
            PublicatieTermijn = NodaTime.Period.FromDays(0),
            BesluitTypeZaakTypen = null,
            BesluitTypeInformatieObjectTypen = null,
        };
        AssertParity<Responses.BesluitTypeResponseDto>(source);
    }

    [Fact]
    public void BesluitType_v1_to_BesluitTypeRequestDto_parity() => AssertParity<Requests.BesluitTypeRequestDto>(_fixture.Create<BesluitType>());

    // =====================================================================================
    // v1._3 DomainToResponseRegister / DomainToResponseProfile (24 pairs; 1 shared with v1, see above)
    // =====================================================================================

    [Fact]
    public void ZaakType_v1_3_to_ZaakTypeResponseDto_parity()
    {
        var catalogus = new Catalogus { Id = Guid.NewGuid() };
        var statusType = new StatusType { Id = Guid.NewGuid() };
        var source = new ZaakType
        {
            Id = Guid.NewGuid(),
            Catalogus = catalogus,
            BeginGeldigheid = new DateOnly(2020, 1, 2),
            EindeGeldigheid = new DateOnly(2020, 1, 3),
            BeginObject = new DateOnly(2020, 1, 4),
            EindeObject = new DateOnly(2020, 1, 5),
            VersieDatum = new DateOnly(2020, 1, 6),
            VerlengingsTermijn = NodaTime.Period.FromDays(0),
            Servicenorm = NodaTime.Period.FromDays(5),
            Doorlooptijd = NodaTime.Period.FromDays(10),
            StatusTypen = [statusType],
            RolTypen = [],
            ResultaatTypen = [],
            Eigenschappen = [],
            ZaakObjectTypen = null,
            ZaakTypeGerelateerdeZaakTypen = [],
        };
        AssertParity<Responses13.ZaakTypeResponseDto>(source);
    }

    [Fact]
    public void ZaakType_v1_3_with_ZaakObjectTypen_to_ZaakTypeResponseDto_parity()
    {
        var zaakObjectType = new ZaakObjectType { Id = Guid.NewGuid() };
        var source = new ZaakType
        {
            Id = Guid.NewGuid(),
            ZaakObjectTypen = [zaakObjectType],
            ZaakTypeGerelateerdeZaakTypen = [],
        };
        AssertParity<Responses13.ZaakTypeResponseDto>(source);
    }

    [Fact]
    public void ZaakType_v1_3_with_null_relations_to_ZaakTypeResponseDto_parity()
    {
        var source = new ZaakType
        {
            Id = Guid.NewGuid(),
            ZaakTypeInformatieObjectTypen = null,
            ZaakTypeDeelZaakTypen = null,
            ZaakTypeBesluitTypen = null,
            ZaakObjectTypen = null,
            ZaakTypeGerelateerdeZaakTypen = [],
        };
        AssertParity<Responses13.ZaakTypeResponseDto>(source);
    }

    [Fact]
    public void ZaakType_v1_3_with_GerelateerdeZaakTypen_to_ZaakTypeResponseDto_parity()
    {
        var relatedZaakType = new ZaakType { Id = Guid.NewGuid() };
        var relationWithNavigation = new ZaakTypeGerelateerdeZaakType
        {
            AardRelatie = AardRelatie.vervolg,
            Toelichting = "toelichting-1",
            GerelateerdeZaakType = relatedZaakType,
        };
        var relationWithoutNavigation = new ZaakTypeGerelateerdeZaakType
        {
            AardRelatie = AardRelatie.bijdrage,
            Toelichting = "toelichting-2",
            GerelateerdeZaakType = null,
        };
        var source = new ZaakType { Id = Guid.NewGuid(), ZaakTypeGerelateerdeZaakTypen = [relationWithNavigation, relationWithoutNavigation] };
        AssertParity<Responses13.ZaakTypeResponseDto>(source);
    }

    [Fact]
    public void BronCatalogus_to_BronCatalogusDto_parity() =>
        AssertParity<Contracts13.BronCatalogusDto>(
            new BronCatalogus
            {
                Url = "https://example.test/catalogussen/1",
                Domein = "DOM01",
                Rsin = TestRsin,
            }
        );

    [Fact]
    public void BronZaaktype_to_BronZaaktypeDto_parity() =>
        AssertParity<Contracts13.BronZaaktypeDto>(
            new BronZaaktype
            {
                Url = "https://example.test/zaaktypen/1",
                Identificatie = "BRONZT1",
                Omschrijving = "bron omschrijving",
            }
        );

    [Fact]
    public void ZaakType_v1_3_with_GerelateerdeZaakTypen_to_ZaakTypeRequestDto_parity()
    {
        var relationWithNavigation = new ZaakTypeGerelateerdeZaakType
        {
            AardRelatie = AardRelatie.vervolg,
            Toelichting = "toelichting-1",
            GerelateerdeZaakType = new ZaakType { Id = Guid.NewGuid() },
            GerelateerdeZaakTypeIdentificatie = "ZT-1",
        };
        var relationWithoutNavigation = new ZaakTypeGerelateerdeZaakType
        {
            AardRelatie = AardRelatie.bijdrage,
            Toelichting = "toelichting-2",
            GerelateerdeZaakType = null,
            GerelateerdeZaakTypeIdentificatie = "ZT-2",
        };
        var source = new ZaakType { Id = Guid.NewGuid(), ZaakTypeGerelateerdeZaakTypen = [relationWithNavigation, relationWithoutNavigation] };
        AssertParity<Requests13.ZaakTypeRequestDto>(source);
    }

    [Fact]
    public void ZaakType_v1_3_with_null_DeelZaakTypen_BesluitTypen_to_ZaakTypeRequestDto_parity()
    {
        var source = new ZaakType
        {
            Id = Guid.NewGuid(),
            ZaakTypeDeelZaakTypen = null,
            ZaakTypeBesluitTypen = null,
            ZaakTypeGerelateerdeZaakTypen = [],
        };
        AssertParity<Requests13.ZaakTypeRequestDto>(source);
    }

    [Fact]
    public void ZaakType_v1_3_with_duplicate_DeelZaakTypen_BesluitTypen_to_ZaakTypeRequestDto_parity()
    {
        var source = new ZaakType
        {
            Id = Guid.NewGuid(),
            ZaakTypeDeelZaakTypen =
            [
                new ZaakTypeDeelZaakType { DeelZaakTypeIdentificatie = "DZT1" },
                new ZaakTypeDeelZaakType { DeelZaakTypeIdentificatie = "DZT1" },
            ],
            ZaakTypeBesluitTypen = [new ZaakTypeBesluitType { BesluitTypeOmschrijving = "BT1" }],
            ZaakTypeGerelateerdeZaakTypen = [],
        };
        AssertParity<Requests13.ZaakTypeRequestDto>(source);
    }

    [Fact]
    public void StatusType_v1_3_to_StatusTypeResponseDto_parity()
    {
        var zaakType = RootedZaakType();
        var source = new StatusType
        {
            Id = Guid.NewGuid(),
            ZaakType = zaakType,
            BeginGeldigheid = new DateOnly(2021, 2, 3),
            EindeGeldigheid = null,
            OmschrijvingGeneriek = null,
            StatusTekst = null,
            StatusTypeVerplichteEigenschappen = null,
        };
        AssertParity<Responses13.StatusTypeResponseDto>(source);
    }

    [Fact]
    public void StatusType_v1_3_with_verplichte_eigenschappen_to_StatusTypeResponseDto_parity()
    {
        var zaakType = RootedZaakType();
        var eigenschap = new Eigenschap { Id = Guid.NewGuid() };
        var source = new StatusType
        {
            Id = Guid.NewGuid(),
            ZaakType = zaakType,
            StatusTypeVerplichteEigenschappen = [new StatusTypeVerplichteEigenschap { Eigenschap = eigenschap }],
            CheckListItemStatustypes = [new CheckListItemStatusType { ItemNaam = "item", Vraagstelling = "vraag" }],
        };
        AssertParity<Responses13.StatusTypeResponseDto>(source);
    }

    [Fact]
    public void CheckListItemStatusType_to_CheckListItemStatusTypeDto_parity() =>
        AssertParity<Contracts13.CheckListItemStatusTypeDto>(
            new CheckListItemStatusType
            {
                ItemNaam = "item",
                Toelichting = "toelichting",
                Vraagstelling = "vraag",
                Verplicht = true,
            }
        );

    [Fact]
    public void StatusType_v1_3_to_StatusTypeRequestDto_parity()
    {
        var zaakType = RootedZaakType();
        var source = new StatusType
        {
            Id = Guid.NewGuid(),
            ZaakType = zaakType,
            StatusTypeVerplichteEigenschappen = null,
        };
        AssertParity<Requests13.StatusTypeRequestDto>(source);
    }

    [Fact]
    public void RolType_v1_3_to_RolTypeResponseDto_parity()
    {
        var zaakType = RootedZaakType();
        var source = new RolType
        {
            Id = Guid.NewGuid(),
            ZaakType = zaakType,
            Omschrijving = "omschrijving",
            OmschrijvingGeneriek = EnumOmschrijvingGeneriek.behandelaar,
            BeginGeldigheid = new DateOnly(2021, 2, 3),
        };
        AssertParity<Responses13.RolTypeResponseDto>(source);
    }

    [Fact]
    public void RolType_v1_3_to_RolTypeRequestDto_parity()
    {
        var zaakType = RootedZaakType();
        var source = new RolType
        {
            Id = Guid.NewGuid(),
            ZaakType = zaakType,
            Omschrijving = "omschrijving",
            OmschrijvingGeneriek = EnumOmschrijvingGeneriek.behandelaar,
        };
        AssertParity<Requests13.RolTypeRequestDto>(source);
    }

    [Fact]
    public void ZaakTypeInformatieObjectType_v1_3_to_ZaakTypeInformatieObjectTypeResponseDto_parity()
    {
        var zaakType = RootedZaakType();
        var statusType = new StatusType { Id = Guid.NewGuid() };
        var source = new ZaakTypeInformatieObjectType
        {
            Id = Guid.NewGuid(),
            ZaakType = zaakType,
            StatusType = statusType,
            VolgNummer = 1,
            Richting = Richting.uitgaand,
            InformatieObjectTypeOmschrijving = "iot-omschrijving",
        };
        AssertParity<Responses13.ZaakTypeInformatieObjectTypeResponseDto>(source);
    }

    [Fact]
    public void ZaakTypeInformatieObjectType_v1_3_to_ZaakTypeInformatieObjectTypeRequestDto_parity()
    {
        var zaakType = RootedZaakType();
        var statusType = new StatusType { Id = Guid.NewGuid() };
        var source = new ZaakTypeInformatieObjectType
        {
            Id = Guid.NewGuid(),
            ZaakType = zaakType,
            StatusType = statusType,
            VolgNummer = 1,
            Richting = Richting.uitgaand,
            InformatieObjectTypeOmschrijving = "iot-omschrijving",
        };
        AssertParity<Requests13.ZaakTypeInformatieObjectTypeRequestDto>(source);
    }

    [Fact]
    public void ResultaatType_v1_3_to_ResultaatTypeResponseDto_parity()
    {
        var zaakType = RootedZaakType();
        var source = new ResultaatType
        {
            Id = Guid.NewGuid(),
            ZaakType = zaakType,
            ArchiefActieTermijn = NodaTime.Period.FromDays(0),
            ProcesTermijn = NodaTime.Period.FromDays(3),
            ResultaatTypeBesluitTypen = null,
        };
        AssertParity<Responses13.ResultaatTypeResponseDto>(source);
    }

    [Fact]
    public void ResultaatType_v1_3_with_relations_to_ResultaatTypeResponseDto_parity()
    {
        var zaakType = RootedZaakType();
        var besluitType = new BesluitType { Id = Guid.NewGuid(), Omschrijving = "BT-omschrijving" };
        var source = new ResultaatType
        {
            Id = Guid.NewGuid(),
            ZaakType = zaakType,
            ArchiefActieTermijn = NodaTime.Period.FromDays(1),
            ProcesTermijn = NodaTime.Period.FromDays(1),
            ResultaatTypeBesluitTypen = [new ResultaatTypeBesluitType { BesluitType = besluitType }],
        };
        AssertParity<Responses13.ResultaatTypeResponseDto>(source);
    }

    [Fact]
    public void ResultaatType_v1_3_to_ResultaatTypeRequestDto_parity()
    {
        var zaakType = RootedZaakType();
        var besluitType = new BesluitType { Id = Guid.NewGuid(), Omschrijving = "BT-omschrijving" };
        var source = new ResultaatType
        {
            Id = Guid.NewGuid(),
            ZaakType = zaakType,
            ArchiefActieTermijn = NodaTime.Period.FromDays(1),
            ProcesTermijn = NodaTime.Period.FromDays(1),
            ResultaatTypeBesluitTypen = [new ResultaatTypeBesluitType { BesluitType = besluitType }],
        };
        AssertParity<Requests13.ResultaatTypeRequestDto>(source);
    }

    [Fact]
    public void Catalogus_v1_3_to_CatalogusResponseDto_parity()
    {
        var zaakType = new ZaakType { Id = Guid.NewGuid() };
        var besluitType = new BesluitType { Id = Guid.NewGuid() };
        var informatieObjectType = new InformatieObjectType { Id = Guid.NewGuid() };
        var source = new Catalogus
        {
            Id = Guid.NewGuid(),
            BegindatumVersie = new DateOnly(2019, 6, 7),
            ZaakTypes = [zaakType],
            BesluitTypes = [besluitType],
            InformatieObjectTypes = [informatieObjectType],
        };
        AssertParity<Responses13.CatalogusResponseDto>(source);
    }

    [Fact]
    public void InformatieObjectType_v1_3_with_no_relations_to_InformatieObjectTypeResponseDto_parity()
    {
        var source = new InformatieObjectType
        {
            Id = Guid.NewGuid(),
            InformatieObjectTypeZaakTypen = null,
            InformatieObjectTypeBesluitTypen = null,
        };
        AssertParity<Responses13.InformatieObjectTypeResponseDto>(source);
    }

    [Fact]
    public void InformatieObjectType_v1_3_with_relations_to_InformatieObjectTypeResponseDto_parity()
    {
        var zaakType = new ZaakType { Id = Guid.NewGuid() };
        var besluitType = new BesluitType { Id = Guid.NewGuid() };
        var source = new InformatieObjectType
        {
            Id = Guid.NewGuid(),
            InformatieObjectTypeZaakTypen = [new ZaakTypeInformatieObjectType { ZaakType = zaakType }],
            InformatieObjectTypeBesluitTypen = [new BesluitTypeInformatieObjectType { BesluitType = besluitType }],
        };
        AssertParity<Responses13.InformatieObjectTypeResponseDto>(source);
    }

    [Fact]
    public void InformatieObjectType_v1_3_to_InformatieObjectTypeRequestDto_parity() =>
        AssertParity<Requests13.InformatieObjectTypeRequestDto>(new InformatieObjectType { Id = Guid.NewGuid() });

    [Fact]
    public void OmschrijvingGeneriek_to_OmschrijvingGeneriekDto_parity() =>
        AssertParity<Contracts13.OmschrijvingGeneriekDto>(_fixture.Create<ClassOmschrijvingGeneriek>());

    [Fact]
    public void Eigenschap_v1_3_to_EigenschapResponseDto_parity()
    {
        var zaakType = RootedZaakType();
        var source = new Eigenschap
        {
            Id = Guid.NewGuid(),
            ZaakType = zaakType,
            Naam = "naam",
            Definitie = "definitie",
        };
        AssertParity<Responses13.EigenschapResponseDto>(source);
    }

    [Fact]
    public void Eigenschap_v1_3_to_EigenschapRequestDto_parity()
    {
        var zaakType = RootedZaakType();
        var statusType = new StatusType { Id = Guid.NewGuid() };
        var source = new Eigenschap
        {
            Id = Guid.NewGuid(),
            ZaakType = zaakType,
            StatusType = statusType,
            Naam = "naam",
            Definitie = "definitie",
        };
        AssertParity<Requests13.EigenschapRequestDto>(source);
    }

    [Fact]
    public void BesluitType_v1_3_with_null_relations_to_BesluitTypeResponseDto_parity()
    {
        var source = new BesluitType
        {
            Id = Guid.NewGuid(),
            ReactieTermijn = NodaTime.Period.FromDays(0),
            PublicatieTermijn = NodaTime.Period.FromDays(7),
            BesluitTypeZaakTypen = null,
            BesluitTypeInformatieObjectTypen = null,
            BesluitTypeResultaatTypen = null,
        };
        AssertParity<Responses13.BesluitTypeResponseDto>(source);
    }

    [Fact]
    public void BesluitType_v1_3_with_relations_to_BesluitTypeResponseDto_parity()
    {
        var zaakType = new ZaakType { Id = Guid.NewGuid() };
        var informatieObjectType = new InformatieObjectType { Id = Guid.NewGuid(), Omschrijving = "IOT-omschrijving" };
        var resultaatType = new ResultaatType { Id = Guid.NewGuid(), Omschrijving = "RT-omschrijving" };
        var source = new BesluitType
        {
            Id = Guid.NewGuid(),
            BesluitTypeZaakTypen = [new BesluitTypeZaakType { ZaakType = zaakType }],
            BesluitTypeInformatieObjectTypen = [new BesluitTypeInformatieObjectType { InformatieObjectType = informatieObjectType }],
            BesluitTypeResultaatTypen = [new ResultaatTypeBesluitType { ResultaatType = resultaatType }],
        };
        AssertParity<Responses13.BesluitTypeResponseDto>(source);
    }

    [Fact]
    public void BesluitType_v1_3_to_BesluitTypeRequestDto_parity()
    {
        var zaakType = new ZaakType { Id = Guid.NewGuid(), Identificatie = "ZT1" };
        var informatieObjectType = new InformatieObjectType { Id = Guid.NewGuid() };
        var source = new BesluitType
        {
            Id = Guid.NewGuid(),
            BesluitTypeZaakTypen = [new BesluitTypeZaakType { ZaakType = zaakType, ZaakTypeIdentificatie = "ZT1" }],
            BesluitTypeInformatieObjectTypen =
            [
                new BesluitTypeInformatieObjectType { InformatieObjectType = informatieObjectType, InformatieObjectTypeOmschrijving = "IOT1" },
            ],
        };
        AssertParity<Requests13.BesluitTypeRequestDto>(source);
    }

    [Fact]
    public void ZaakObjectType_v1_3_to_ZaakObjectTypeResponseDto_parity()
    {
        var zaakType = RootedZaakType();
        var source = new ZaakObjectType
        {
            Id = Guid.NewGuid(),
            ZaakType = zaakType,
            ObjectType = "objecttype",
            RelatieOmschrijving = "relatie omschrijving",
        };
        AssertParity<Responses13.ZaakObjectTypeResponseDto>(source);
    }

    [Fact]
    public void ZaakObjectType_v1_3_to_ZaakObjectTypeRequestDto_parity()
    {
        var zaakType = RootedZaakType();
        var source = new ZaakObjectType
        {
            Id = Guid.NewGuid(),
            ZaakType = zaakType,
            ObjectType = "objecttype",
            RelatieOmschrijving = "relatie omschrijving",
        };
        AssertParity<Requests13.ZaakObjectTypeRequestDto>(source);
    }
}
