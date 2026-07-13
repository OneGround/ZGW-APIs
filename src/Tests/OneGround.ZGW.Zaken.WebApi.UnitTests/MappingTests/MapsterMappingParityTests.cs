using System;
using System.Linq;
using AutoFixture;
using AutoMapper;
using AutoMapper.Internal;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.DataModel.ZaakObject;
using OneGround.ZGW.Zaken.DataModel.ZaakRol;
using Xunit;
using AutoMapperIMapper = AutoMapper.IMapper;
using Contracts1 = OneGround.ZGW.Zaken.Contracts.v1;
using Contracts12 = OneGround.ZGW.Zaken.Contracts.v1._2;
using Contracts5 = OneGround.ZGW.Zaken.Contracts.v1._5;
using Filters = OneGround.ZGW.Zaken.Web.Models.v1;
using Filters5 = OneGround.ZGW.Zaken.Web.Models.v1._5;
using MapsterIMapper = MapsterMapper.IMapper;
using Queries = OneGround.ZGW.Zaken.Contracts.v1.Queries;
using Queries5 = OneGround.ZGW.Zaken.Contracts.v1._5.Queries;
using RegRoot = OneGround.ZGW.Zaken.Web.MappingProfiles;
using RegV1 = OneGround.ZGW.Zaken.Web.MappingProfiles.v1;
using RegV12 = OneGround.ZGW.Zaken.Web.MappingProfiles.v1._2;
using RegV15 = OneGround.ZGW.Zaken.Web.MappingProfiles.v1._5;
using Req = OneGround.ZGW.Zaken.Contracts.v1.Requests;
using Req5 = OneGround.ZGW.Zaken.Contracts.v1._5.Requests;
using ReqObj = OneGround.ZGW.Zaken.Contracts.v1.Requests.ZaakObject;
using ReqObj5 = OneGround.ZGW.Zaken.Contracts.v1._5.Requests.ZaakObject;
using ReqRol = OneGround.ZGW.Zaken.Contracts.v1.Requests.ZaakRol;
using ReqRol5 = OneGround.ZGW.Zaken.Contracts.v1._5.Requests.ZaakRol;
using Resp = OneGround.ZGW.Zaken.Contracts.v1.Responses;
using Resp5 = OneGround.ZGW.Zaken.Contracts.v1._5.Responses;
using RespObj = OneGround.ZGW.Zaken.Contracts.v1.Responses.ZaakObject;
using RespObj5 = OneGround.ZGW.Zaken.Contracts.v1._5.Responses.ZaakObject;
using RespRol = OneGround.ZGW.Zaken.Contracts.v1.Responses.ZaakRol;
using RespRol5 = OneGround.ZGW.Zaken.Contracts.v1._5.Responses.ZaakRol;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.MappingTests;

/// <summary>
/// Temporary A/B parity guard (deleted once the AutoMapper profiles are removed in a later task):
/// maps identical inputs through both the still-present AutoMapper profiles and the new Mapster
/// registers for all 6 ZRC mapping profiles/registers (RequestToPagination, v1 RequestToDomain/
/// DomainToResponse, v1._2 DomainToResponse, v1._5 RequestToDomain/DomainToResponse) and asserts
/// byte-identical serialized JSON. This is the wholesale correctness proof for the migration -
/// every <c>CreateMap</c>/<c>NewConfig</c> pair across all 6 profiles/registers gets at least one
/// fact here (167 raw CreateMap configs total; 2 of those are duplicate registrations of an
/// already-covered pair rather than a genuinely distinct one - v1 and v1._5's RequestToDomainProfile
/// both register the identical ObjectTypeOverigeDefinitieDto-&gt;ObjectTypeOverigeDefinitie mapping
/// line-for-line, and v1._5's DomainToResponseProfile registers ZaakProcessobject-&gt;ZaakProcessobjectDto
/// twice onto the same TypePair, AutoMapper-merged into a single effective config - so 165 distinct
/// (source, destination) pairs receive coverage here, not 167).
/// </summary>
public class MapsterMappingParityTests : IDisposable
{
    // Official RvIG test BSN value (elfproef-valid, never assigned to a real person/organization) -
    // reused here as a stand-in RSIN/Bronorganisatie/VerantwoordelijkeOrganisatie, which share the
    // same 9-digit elfproef structure.
    private const string TestRsin = "999993653";

    private readonly AutoMapperFixture _fixture = new();
    private readonly Mock<IEntityUriService> _mockedUriService = new();
    private readonly AutoMapperIMapper _autoMapper;
    private readonly ServiceProvider _provider;
    private readonly IServiceScope _scope;
    private readonly MapsterIMapper _mapsterMapper;

    public MapsterMappingParityTests()
    {
        _fixture.Register<DateOnly>(() => DateOnly.FromDateTime(DateTime.UtcNow));
        _mockedUriService.Setup(s => s.GetUri(It.IsAny<IUrlEntity>())).Returns<IUrlEntity>(e => e.Url);

        // AutoMapper side: all 6 profiles, with every DI-backed resolver wired plus the
        // NullableEnumMapper (request side) that ports as RegisterNullableEnumRule on Mapster.
        var amConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile(new RegRoot.RequestToPaginationProfile());
            cfg.AddProfile(new RegV1.RequestToDomainProfile());
            cfg.AddProfile(new RegV1.DomainToResponseProfile());
            cfg.AddProfile(new RegV12.DomainToResponseProfile());
            cfg.AddProfile(new RegV15.RequestToDomainProfile());
            cfg.AddProfile(new RegV15.DomainToResponseProfile());
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

            throw new NotImplementedException($"Mapper is missing the service: {t}");
        });

        // Mapster side: one TypeAdapterConfig mirroring all 4 AddZgwMapster global defaults, plus all
        // 6 registers - exactly what production wires up via AddZgwMapster + config.Scan.
        var config = new TypeAdapterConfig();
        config.Default.MaxDepth(200);
        config.Default.AddDestinationTransform(DestinationTransform.EmptyCollectionIfNull);
        config.Default.NameMatchingStrategy(NameMatchingStrategy.IgnoreCase);
        config.RegisterNullableEnumRule();
        new RegRoot.RequestToPaginationRegister().Register(config);
        new RegV1.RequestToDomainRegister().Register(config);
        new RegV1.DomainToResponseRegister().Register(config);
        new RegV12.DomainToResponseRegister().Register(config);
        new RegV15.RequestToDomainRegister().Register(config);
        new RegV15.DomainToResponseRegister().Register(config);
        config.Compile();

        var services = new ServiceCollection();
        services.AddSingleton(_mockedUriService.Object);
        services.AddSingleton(config);
        services.AddScoped<MapsterIMapper, ServiceMapper>();
        _provider = services.BuildServiceProvider();
        _scope = _provider.CreateScope();
        _mapsterMapper = _scope.ServiceProvider.GetRequiredService<MapsterIMapper>();

        // AardRelatieWeergaveToString (duplicated verbatim in both the AutoMapper profile and the
        // Mapster register) only handles 2 of the enum's members and throws on any other - pin every
        // AutoFixture-created ZaakInformatieObject to a handled value so the bulk facts below don't
        // intermittently throw instead of asserting parity.
        _fixture.Customize<ZaakInformatieObject>(c => c.With(p => p.AardRelatieWeergave, AardRelatieWeergave.hoort_bij_omgekeerd_kent));

        // OverigeZaakObject.OverigeData is a raw JSON string column that both mappers JToken.Parse(...)
        // on the way out - AutoFixture's random string won't parse, so pin it to valid JSON.
        _fixture.Customize<OverigeZaakObject>(c => c.With(p => p.OverigeData, """{"foo":"bar","n":3}"""));
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

    private static Zaak RootedZaak() => new() { Id = Guid.NewGuid(), Identificatie = "ZK1" };

    // =====================================================================================
    // RequestToPaginationRegister / RequestToPaginationProfile (1 pair)
    // =====================================================================================

    [Fact]
    public void PaginationQuery_to_PaginationFilter_parity() => AssertParity<PaginationFilter>(_fixture.Create<PaginationQuery>());

    // =====================================================================================
    // v1 RequestToDomainRegister / RequestToDomainProfile (60 pairs)
    // =====================================================================================

    [Fact]
    public void GetAllZakenQueryParameters_v1_to_Filter_parity() =>
        AssertParity<Filters.GetAllZakenFilter>(
            new Queries.GetAllZakenQueryParameters
            {
                Identificatie = "ZK1",
                Bronorganisatie = TestRsin,
                Zaaktype = "https://example.test/zaaktypen/1",
                Archiefactiedatum = "2020-11-05",
                Archiefactiedatum__gt = "2020-11-06",
                Archiefactiedatum__lt = "2020-11-07",
                Startdatum = "2020-11-08",
                Startdatum__gt = "2020-11-09",
                Startdatum__gte = "2020-11-10",
                Startdatum__lt = "2020-11-11",
                Startdatum__lte = "2020-11-12",
                Archiefnominatie = ArchiefNominatie.vernietigen.ToString(),
                Archiefstatus = ArchiefStatus.overgedragen.ToString(),
                Archiefnominatie__in = $"{ArchiefNominatie.blijvend_bewaren}, {ArchiefNominatie.vernietigen}",
                Archiefstatus__in = $"{ArchiefStatus.nog_te_archiveren}, {ArchiefStatus.gearchiveerd}",
            }
        );

    [Fact]
    public void GetAllZakenQueryParameters_v1_with_unparseable_date_throws_identically_parity()
    {
        // Both mappers invoke the identical shared ProfileHelper.DateFromStringOptional, which throws
        // a FormatException for a 10-character-but-unparseable date string - AutoMapper wraps resolver
        // exceptions in AutoMapperMappingException (its own standard behavior, not something either
        // port changed), Mapster's compiled lambda lets the original exception propagate directly.
        var source = new Queries.GetAllZakenQueryParameters { Archiefactiedatum = "not-a-date" };
        var amEx = Assert.Throws<AutoMapperMappingException>(() => _autoMapper.Map<Filters.GetAllZakenFilter>(source));
        Assert.IsType<FormatException>(amEx.InnerException);
        Assert.Throws<FormatException>(() => _mapsterMapper.Map<Filters.GetAllZakenFilter>(source));
    }

    [Fact]
    public void ZaakSearchRequestDto_v1_to_Filter_parity() =>
        AssertParity<Filters.GetAllZakenFilter>(
            new Req.ZaakSearchRequestDto
            {
                Identificatie = "ZK1",
                Bronorganisatie = TestRsin,
                ZaakType = "https://example.test/zaaktypen/1",
                Archiefactiedatum = "2020-11-05",
                Archiefactiedatum__gt = "2020-11-06",
                Archiefactiedatum__lt = "2020-11-07",
                Startdatum = "2020-11-08",
                Startdatum__gt = "2020-11-09",
                Startdatum__gte = "2020-11-10",
                Startdatum__lt = "2020-11-11",
                Startdatum__lte = "2020-11-12",
                Archiefnominatie__in = $"{ArchiefNominatie.blijvend_bewaren}, {ArchiefNominatie.vernietigen}",
                Archiefstatus__in = $"{ArchiefStatus.nog_te_archiveren}, {ArchiefStatus.gearchiveerd}",
            }
        );

    [Fact]
    public void ZaakRequestDto_v1_to_Zaak_parity()
    {
        // Note: Zaakgeometrie is deliberately left null here. Both sides route it through the
        // identical Geometry->Geometry MapWith(src => src) passthrough rule (registered by
        // RequestToDomainRegister), so it is structurally guaranteed to be the exact same object
        // reference on both sides - there is nothing for AssertParity's JSON diff to usefully catch,
        // and NetTopologySuite's Geometry/Coordinate object graph is self-referencing, which makes
        // JsonConvert.SerializeObject throw regardless of which mapper produced it.
        var source = new Req.ZaakRequestDto
        {
            Identificatie = "ZK1",
            Bronorganisatie = TestRsin,
            Omschrijving = "omschrijving",
            Toelichting = "toelichting",
            Zaaktype = "https://example.test/zaaktypen/1/",
            Registratiedatum = "2020-11-06",
            VerantwoordelijkeOrganisatie = TestRsin,
            Startdatum = "2020-11-07",
            EinddatumGepland = "2020-11-08",
            UiterlijkeEinddatumAfdoening = "2020-11-09",
            Publicatiedatum = "2020-11-10",
            Communicatiekanaal = "communicatiekanaal",
            ProductenOfDiensten = ["https://example.test/producten/1"],
            Vertrouwelijkheidaanduiding = VertrouwelijkheidAanduiding.openbaar.ToString(),
            Betalingsindicatie = BetalingsIndicatie.geheel.ToString(),
            LaatsteBetaaldatum = "2020-11-11T12:13:14Z",
            Verlenging = new Contracts1.ZaakVerlengingDto { Duur = "P365D", Reden = "reden" },
            Opschorting = new Contracts1.ZaakOpschortingDto { Indicatie = true, Reden = "opschorting reden" },
            Selectielijstklasse = "selectielijstklasse",
            RelevanteAndereZaken = [new Contracts1.RelevanteAndereZaakDto { Url = "https://example.test/zaken/2", AardRelatie = "vervolg" }],
            Kenmerken = [new Contracts1.ZaakKenmerkDto { Bron = "bron", Kenmerk = "kenmerk" }],
            Archiefnominatie = ArchiefNominatie.blijvend_bewaren.ToString(),
            Archiefstatus = ArchiefStatus.nog_te_archiveren.ToString(),
            Archiefactiedatum = "2020-11-12",
        };
        AssertParity<Zaak>(source);
    }

    [Fact]
    public void RelevanteAndereZaakDto_to_RelevanteAndereZaak_parity() =>
        AssertParity<RelevanteAndereZaak>(new Contracts1.RelevanteAndereZaakDto { Url = "https://example.test/zaken/2", AardRelatie = "vervolg" });

    [Fact]
    public void ZaakKenmerkDto_to_ZaakKenmerk_parity() =>
        AssertParity<ZaakKenmerk>(new Contracts1.ZaakKenmerkDto { Bron = "bron", Kenmerk = "kenmerk" });

    [Fact]
    public void ZaakVerlengingDto_to_ZaakVerlenging_parity() =>
        AssertParity<ZaakVerlenging>(new Contracts1.ZaakVerlengingDto { Duur = "P365D", Reden = "reden" });

    [Fact]
    public void ZaakOpschortingDto_to_ZaakOpschorting_parity() =>
        AssertParity<ZaakOpschorting>(new Contracts1.ZaakOpschortingDto { Indicatie = true, Reden = "opschorting reden" });

    [Fact]
    public void GetAllZaakStatussenQueryParameters_v1_to_Filter_parity() =>
        AssertParity<Filters.GetAllZaakStatussenFilter>(
            new Queries.GetAllZaakStatussenQueryParameters
            {
                Zaak = "https://example.test/zaken/1",
                StatusType = "https://example.test/statustypen/1",
            }
        );

    [Fact]
    public void ZaakStatusRequestDto_v1_to_ZaakStatus_parity() =>
        AssertParity<ZaakStatus>(
            new Req.ZaakStatusRequestDto
            {
                Zaak = "https://example.test/zaken/1",
                StatusType = "https://example.test/statustypen/1",
                DatumStatusGezet = "2020-11-06T12:13:14Z",
                StatusToelichting = "toelichting",
            }
        );

    [Fact]
    public void GetAllZaakObjectenQueryParameters_to_Filter_parity() =>
        AssertParity<Filters.GetAllZaakObjectenFilter>(
            new Queries.GetAllZaakObjectenQueryParameters
            {
                Zaak = "https://example.test/zaken/1",
                Object = "https://example.test/objects/1",
                ObjectType = ObjectType.gemeentelijke_openbare_ruimte.ToString(),
            }
        );

    [Fact]
    public void ObjectTypeOverigeDefinitieDto_to_ObjectTypeOverigeDefinitie_parity() =>
        // Shared: this exact (source, dest) pair is registered identically by both the v1 and v1._5
        // RequestToDomainRegister - one fact covers both registrations.
        AssertParity<ObjectTypeOverigeDefinitie>(
            new Contracts12.ObjectTypeOverigeDefinitieDto
            {
                Url = "https://example.test/objecttypen/1",
                Schema = "schema",
                ObjectData = "objectdata",
            }
        );

    private static Contracts1.AdresZaakObjectDto SampleAdresDto() =>
        new()
        {
            Huisletter = "A",
            Huisnummer = 12,
            HuisnummerToevoeging = "bis",
            GorOpenbareRuimteNaam = "Teststraat",
            Identificatie = "ID1",
            WplWoonplaatsNaam = "Teststad",
            Postcode = "1234AB",
        };

    [Fact]
    public void AdresZaakObjectRequestDto_v1_MapsWith_CreateAdresZaakObject_parity() =>
        AssertParity<AdresZaakObject>(new ReqObj.AdresZaakObjectRequestDto { ObjectIdentificatie = SampleAdresDto() });

    [Fact]
    public void BuurtZaakObjectRequestDto_v1_MapsWith_CreateBuurtZaakObject_parity() =>
        AssertParity<BuurtZaakObject>(
            new ReqObj.BuurtZaakObjectRequestDto
            {
                ObjectIdentificatie = new Contracts1.BuurtZaakObjectDto
                {
                    BuurtCode = "BC1",
                    BuurtNaam = "Buurtnaam",
                    GemGemeenteCode = "GC1",
                    WykWijkCode = "WC1",
                },
            }
        );

    [Fact]
    public void PandZaakObjectRequestDto_v1_MapsWith_CreatePandZaakObject_parity() =>
        AssertParity<PandZaakObject>(
            new ReqObj.PandZaakObjectRequestDto { ObjectIdentificatie = new Contracts1.PandZaakObjectDto { Identificatie = "ID1" } }
        );

    [Fact]
    public void KadastraleOnroerendeZaakObjectRequestDto_v1_MapsWith_CreateKadastraleOnroerendeZaakObject_parity() =>
        AssertParity<KadastraleOnroerendeZaakObject>(
            new ReqObj.KadastraleOnroerendeZaakObjectRequestDto
            {
                ObjectIdentificatie = new Contracts1.KadastraleOnroerendeZaakObjectDto
                {
                    KadastraleAanduiding = "aanduiding",
                    KadastraleIdentificatie = "ID1",
                },
            }
        );

    [Fact]
    public void GemeenteZaakObjectRequestDto_v1_MapsWith_CreateGemeenteZaakObject_parity() =>
        AssertParity<GemeenteZaakObject>(
            new ReqObj.GemeenteZaakObjectRequestDto
            {
                ObjectIdentificatie = new Contracts1.GemeenteZaakObjectDto { GemeenteCode = "GC1", GemeenteNaam = "Gemeentenaam" },
            }
        );

    private static Contracts1.AdresAanduidingGrpDto SampleAdresAanduidingGrpDto() =>
        new()
        {
            AoaHuisletter = "A",
            AoaHuisnummer = 1,
            AoaHuisnummertoevoeging = "bis",
            AoaPostcode = "1234AB",
            GorOpenbareRuimteNaam = "Teststraat",
            NumIdentificatie = "NUM1",
            OaoIdentificatie = "OAO1",
            OgoLocatieAanduiding = "OGO1",
            WplWoonplaatsNaam = "Teststad",
        };

    [Fact]
    public void TerreinGebouwdObjectZaakObjectRequestDto_v1_MapsWith_CreateTerreinGebouwdObjectZaakObject_parity() =>
        AssertParity<TerreinGebouwdObjectZaakObject>(
            new ReqObj.TerreinGebouwdObjectZaakObjectRequestDto
            {
                ObjectIdentificatie = new Contracts1.TerreinGebouwdObjectZaakObjectDto
                {
                    Identificatie = "ID1",
                    AdresAanduidingGrp = SampleAdresAanduidingGrpDto(),
                },
            }
        );

    [Fact]
    public void TerreinGebouwdObjectZaakObjectRequestDto_v1_with_null_AdresAanduidingGrp_parity() =>
        AssertParity<TerreinGebouwdObjectZaakObject>(
            new ReqObj.TerreinGebouwdObjectZaakObjectRequestDto
            {
                ObjectIdentificatie = new Contracts1.TerreinGebouwdObjectZaakObjectDto { Identificatie = "ID1", AdresAanduidingGrp = null },
            }
        );

    [Fact]
    public void OverigeZaakObjectRequestDto_v1_MapsWith_CreateOverigeZaakObject_parity() =>
        AssertParity<OverigeZaakObject>(
            new ReqObj.OverigeZaakObjectRequestDto
            {
                ObjectIdentificatie = new Contracts1.OverigeZaakObjectDto { OverigeData = JToken.Parse("""{"foo":"bar","n":3}""") },
            }
        );

    [Fact]
    public void WozWaardeZaakObjectRequestDto_v1_MapsWith_CreateWozWaardeZaakObject_parity() =>
        AssertParity<WozWaardeZaakObject>(
            new ReqObj.WozWaardeZaakObjectRequestDto
            {
                ObjectIdentificatie = new Contracts1.WozWaardeZaakObjectDto
                {
                    WaardePeildatum = "2020-01-01",
                    IsVoor = new Contracts1.WozObjectDto
                    {
                        WozObjectNummer = "WOZ1",
                        AanduidingWozObject = new Contracts1.AanduidingWozObjectDto
                        {
                            AoaHuisletter = "A",
                            AoaHuisnummer = 1,
                            AoaHuisnummerToevoeging = "bis",
                            AoaIdentificatie = "AOA1",
                            AoaPostcode = "1234AB",
                            GorOpenbareRuimteNaam = "Teststraat",
                            LocatieOmschrijving = "locatie",
                            WplWoonplaatsNaam = "Teststad",
                        },
                    },
                },
            }
        );

    [Fact]
    public void WozWaardeZaakObjectRequestDto_v1_with_null_IsVoor_parity() =>
        AssertParity<WozWaardeZaakObject>(
            new ReqObj.WozWaardeZaakObjectRequestDto
            {
                ObjectIdentificatie = new Contracts1.WozWaardeZaakObjectDto { WaardePeildatum = "2020-01-01" },
            }
        );

    [Fact]
    public void ZaakObjectRequestDto_v1_without_derived_data_to_ZaakObject_parity() =>
        AssertParity<ZaakObject>(
            new ReqObj.ZaakObjectRequestDto
            {
                Object = "https://example.test/objects/1",
                ObjectType = ObjectType.gemeentelijke_openbare_ruimte.ToString(),
                ObjectTypeOverige = "overige",
                RelatieOmschrijving = "relatie omschrijving",
            }
        );

    [Fact]
    public void ZaakObjectRequestDto_v1_base_typed_reference_holding_AdresZaakObjectRequestDto_dispatches_parity()
    {
        // The whole point of this fact: `request` is declared and passed around as the BASE type
        // ZaakObjectRequestDto, but at runtime holds an AdresZaakObjectRequestDto instance - proving
        // both AutoMapper's .IncludeAllDerived() and Mapster's runtime dispatch on source.GetType()
        // agree, real-config-to-real-config.
        ReqObj.ZaakObjectRequestDto request = new ReqObj.AdresZaakObjectRequestDto
        {
            Object = "https://example.test/objects/1",
            ObjectType = ObjectType.adres.ToString(),
            RelatieOmschrijving = "relatie omschrijving",
            ObjectIdentificatie = SampleAdresDto(),
        };
        AssertParity<ZaakObject>(request);
    }

    [Fact]
    public void AdresZaakObjectDto_to_AdresZaakObject_parity() => AssertParity<AdresZaakObject>(SampleAdresDto());

    [Fact]
    public void AdresZaakObjectRequestDto_v1_to_ZaakObject_parity() =>
        AssertParity<ZaakObject>(new ReqObj.AdresZaakObjectRequestDto { ObjectIdentificatie = SampleAdresDto() });

    [Fact]
    public void BuurtZaakObjectDto_to_BuurtZaakObject_parity() =>
        AssertParity<BuurtZaakObject>(
            new Contracts1.BuurtZaakObjectDto
            {
                BuurtCode = "BC1",
                BuurtNaam = "Buurtnaam",
                GemGemeenteCode = "GC1",
                WykWijkCode = "WC1",
            }
        );

    [Fact]
    public void BuurtZaakObjectRequestDto_v1_to_ZaakObject_parity() =>
        AssertParity<ZaakObject>(
            new ReqObj.BuurtZaakObjectRequestDto
            {
                ObjectIdentificatie = new Contracts1.BuurtZaakObjectDto { BuurtCode = "BC1", BuurtNaam = "Buurtnaam" },
            }
        );

    [Fact]
    public void PandZaakObjectDto_to_PandZaakObject_parity() =>
        AssertParity<PandZaakObject>(new Contracts1.PandZaakObjectDto { Identificatie = "ID1" });

    [Fact]
    public void PandZaakObjectRequestDto_v1_to_ZaakObject_parity() =>
        AssertParity<ZaakObject>(
            new ReqObj.PandZaakObjectRequestDto { ObjectIdentificatie = new Contracts1.PandZaakObjectDto { Identificatie = "ID1" } }
        );

    [Fact]
    public void KadastraleOnroerendeZaakObjectDto_to_KadastraleOnroerendeZaakObject_parity() =>
        AssertParity<KadastraleOnroerendeZaakObject>(
            new Contracts1.KadastraleOnroerendeZaakObjectDto { KadastraleAanduiding = "aanduiding", KadastraleIdentificatie = "ID1" }
        );

    [Fact]
    public void KadastraleOnroerendeZaakObjectRequestDto_v1_to_ZaakObject_parity() =>
        AssertParity<ZaakObject>(
            new ReqObj.KadastraleOnroerendeZaakObjectRequestDto
            {
                ObjectIdentificatie = new Contracts1.KadastraleOnroerendeZaakObjectDto { KadastraleIdentificatie = "ID1" },
            }
        );

    [Fact]
    public void GemeenteZaakObjectDto_to_GemeenteZaakObject_parity() =>
        AssertParity<GemeenteZaakObject>(new Contracts1.GemeenteZaakObjectDto { GemeenteCode = "GC1", GemeenteNaam = "Gemeentenaam" });

    [Fact]
    public void GemeenteZaakObjectRequestDto_v1_to_ZaakObject_parity() =>
        AssertParity<ZaakObject>(
            new ReqObj.GemeenteZaakObjectRequestDto { ObjectIdentificatie = new Contracts1.GemeenteZaakObjectDto { GemeenteCode = "GC1" } }
        );

    [Fact]
    public void TerreinGebouwdObjectZaakObjectDto_to_TerreinGebouwdObjectZaakObject_parity() =>
        AssertParity<TerreinGebouwdObjectZaakObject>(
            new Contracts1.TerreinGebouwdObjectZaakObjectDto { Identificatie = "ID1", AdresAanduidingGrp = SampleAdresAanduidingGrpDto() }
        );

    [Fact]
    public void TerreinGebouwdObjectZaakObjectRequestDto_v1_to_ZaakObject_parity() =>
        AssertParity<ZaakObject>(
            new ReqObj.TerreinGebouwdObjectZaakObjectRequestDto
            {
                ObjectIdentificatie = new Contracts1.TerreinGebouwdObjectZaakObjectDto { Identificatie = "ID1" },
            }
        );

    [Fact]
    public void OverigeZaakObjectDto_to_OverigeZaakObject_parity() =>
        AssertParity<OverigeZaakObject>(new Contracts1.OverigeZaakObjectDto { OverigeData = JToken.Parse("""{"foo":"bar"}""") });

    [Fact]
    public void OverigeZaakObjectRequestDto_v1_to_ZaakObject_parity() =>
        AssertParity<ZaakObject>(
            new ReqObj.OverigeZaakObjectRequestDto
            {
                ObjectIdentificatie = new Contracts1.OverigeZaakObjectDto { OverigeData = JToken.Parse("""{"foo":"bar"}""") },
            }
        );

    [Fact]
    public void AanduidingWozObjectDto_to_AanduidingWozObject_parity() =>
        AssertParity<AanduidingWozObject>(
            new Contracts1.AanduidingWozObjectDto
            {
                AoaHuisletter = "A",
                AoaHuisnummer = 1,
                AoaHuisnummerToevoeging = "bis",
                AoaIdentificatie = "AOA1",
                AoaPostcode = "1234AB",
                GorOpenbareRuimteNaam = "Teststraat",
                LocatieOmschrijving = "locatie",
                WplWoonplaatsNaam = "Teststad",
            }
        );

    [Fact]
    public void WozObjectDto_to_WozObject_parity() => AssertParity<WozObject>(new Contracts1.WozObjectDto { WozObjectNummer = "WOZ1" });

    [Fact]
    public void WozWaardeZaakObjectDto_to_WozWaardeZaakObject_parity() =>
        AssertParity<WozWaardeZaakObject>(new Contracts1.WozWaardeZaakObjectDto { WaardePeildatum = "2020-01-01" });

    [Fact]
    public void WozWaardeZaakObjectRequestDto_v1_to_ZaakObject_parity() =>
        AssertParity<ZaakObject>(
            new ReqObj.WozWaardeZaakObjectRequestDto
            {
                ObjectIdentificatie = new Contracts1.WozWaardeZaakObjectDto { WaardePeildatum = "2020-01-01" },
            }
        );

    [Fact]
    public void GetAllZaakInformatieObjectenQueryParameters_to_Filter_parity() =>
        AssertParity<Filters.GetAllZaakInformatieObjectenFilter>(
            new Queries.GetAllZaakInformatieObjectenQueryParameters
            {
                Zaak = "https://example.test/zaken/1",
                InformatieObject = "https://example.test/informatieobjecten/1",
            }
        );

    [Fact]
    public void ZaakInformatieObjectRequestDto_v1_to_ZaakInformatieObject_parity() =>
        AssertParity<ZaakInformatieObject>(
            new Req.ZaakInformatieObjectRequestDto
            {
                Zaak = "https://example.test/zaken/1",
                InformatieObject = "https://example.test/informatieobjecten/1",
                Beschrijving = "beschrijving",
                Titel = "titel",
            }
        );

    [Fact]
    public void GetAllZaakRollenQueryParameters_v1_to_Filter_parity() =>
        AssertParity<Filters.GetAllZaakRollenFilter>(
            new Queries.GetAllZaakRollenQueryParameters
            {
                Zaak = "https://example.test/zaken/1",
                Betrokkene = "https://example.test/betrokkenen/1",
                BetrokkeneType = BetrokkeneType.niet_natuurlijk_persoon.ToString(),
                BetrokkeneIdentificatie__natuurlijkPersoon__inpBsn = "999993653",
                BetrokkeneIdentificatie__natuurlijkPersoon__anpIdentificatie = "ANP1",
                BetrokkeneIdentificatie__natuurlijkPersoon__inpA_nummer = "A1",
                BetrokkeneIdentificatie__nietNatuurlijkPersoon__innNnpId = "NNP1",
                BetrokkeneIdentificatie__nietNatuurlijkPersoon__annIdentificatie = "ANN1",
                BetrokkeneIdentificatie__vestiging__vestigingsNummer = "VN1",
                BetrokkeneIdentificatie__organisatorischeEenheid__identificatie = "OE1",
                BetrokkeneIdentificatie__medewerker__identificatie = "MW1",
                RolType = "https://example.test/roltypen/1",
                Omschrijving = "omschrijving",
                OmschrijvingGeneriek = OmschrijvingGeneriek.belanghebbende.ToString(),
            }
        );

    [Fact]
    public void ZaakRolRequestDto_v1_to_ZaakRol_parity() =>
        AssertParity<ZaakRol>(
            new ReqRol.ZaakRolRequestDto
            {
                Zaak = "https://example.test/zaken/1",
                Betrokkene = "https://example.test/betrokkenen/1",
                BetrokkeneType = BetrokkeneType.natuurlijk_persoon.ToString(),
                RolType = "https://example.test/roltypen/1",
                RolToelichting = "roltoelichting",
                IndicatieMachtiging = IndicatieMachtiging.gemachtigde.ToString(),
            }
        );

    [Fact]
    public void ZaakRolRequestDto_v1_base_typed_reference_holding_NatuurlijkPersoonZaakRolRequestDto_dispatches_parity()
    {
        ReqRol.ZaakRolRequestDto request = new ReqRol.NatuurlijkPersoonZaakRolRequestDto
        {
            Zaak = "https://example.test/zaken/1",
            Betrokkene = "https://example.test/betrokkenen/1",
            BetrokkeneType = BetrokkeneType.natuurlijk_persoon.ToString(),
            RolType = "https://example.test/roltypen/1",
            RolToelichting = "toelichting",
            BetrokkeneIdentificatie = new Contracts1.NatuurlijkPersoonZaakRolDto { InpBsn = TestRsin, Geslachtsnaam = "Jansen" },
        };
        AssertParity<ZaakRol>(request);
    }

    [Fact]
    public void VerblijfsadresDto_to_Verblijfsadres_parity() =>
        AssertParity<Verblijfsadres>(
            new Contracts1.VerblijfsadresDto
            {
                AoaIdentificatie = "AOA1",
                WplWoonplaatsNaam = "Teststad",
                GorOpenbareRuimteNaam = "Teststraat",
                AoaPostcode = "1234AB",
                AoaHuisnummer = 1,
                AoaHuisletter = "A",
                AoaHuisnummertoevoeging = "bis",
                InpLocatiebeschrijving = "beschrijving",
            }
        );

    [Fact]
    public void SubVerblijfBuitenlandDto_to_SubVerblijfBuitenland_parity() =>
        AssertParity<SubVerblijfBuitenland>(
            new Contracts1.SubVerblijfBuitenlandDto
            {
                LndLandcode = "NL",
                LndLandnaam = "Nederland",
                SubAdresBuitenland1 = "adres1",
                SubAdresBuitenland2 = "adres2",
                SubAdresBuitenland3 = "adres3",
            }
        );

    [Fact]
    public void NatuurlijkPersoonZaakRolDto_to_NatuurlijkPersoonZaakRol_parity() =>
        AssertParity<NatuurlijkPersoonZaakRol>(
            new Contracts1.NatuurlijkPersoonZaakRolDto
            {
                InpBsn = TestRsin,
                AnpIdentificatie = "ANP1",
                InpANummer = "A1",
                Geslachtsnaam = "Jansen",
                VoorvoegselGeslachtsnaam = "van",
                Voorletters = "J.",
                Voornamen = "Jan",
                Geslachtsaanduiding = Geslachtsaanduiding.m.ToString(),
                Geboortedatum = "2020-11-04",
            }
        );

    [Fact]
    public void NietNatuurlijkPersoonZaakRolDto_to_NietNatuurlijkPersoonZaakRol_parity() =>
        AssertParity<NietNatuurlijkPersoonZaakRol>(
            new Contracts1.NietNatuurlijkPersoonZaakRolDto
            {
                InnNnpId = "NNP1",
                AnnIdentificatie = "ANN1",
                StatutaireNaam = "Naam BV",
                InnRechtsvorm = InnRechtsvorm.besloten_vennootschap.ToString(),
                Bezoekadres = "Bezoekadres 1",
            }
        );

    [Fact]
    public void NietNatuurlijkPersoonZaakRolRequestDto_v1_to_ZaakRol_parity()
    {
        ReqRol.ZaakRolRequestDto request = new ReqRol.NietNatuurlijkPersoonZaakRolRequestDto
        {
            Zaak = "https://example.test/zaken/1",
            Betrokkene = "https://example.test/betrokkenen/1",
            BetrokkeneType = BetrokkeneType.niet_natuurlijk_persoon.ToString(),
            RolType = "https://example.test/roltypen/1",
            BetrokkeneIdentificatie = new Contracts1.NietNatuurlijkPersoonZaakRolDto { InnNnpId = "NNP1", StatutaireNaam = "Naam BV" },
        };
        AssertParity<ZaakRol>(request);
    }

    [Fact]
    public void VestigingZaakRolDto_v1_to_VestigingZaakRol_parity() =>
        AssertParity<VestigingZaakRol>(new Contracts1.VestigingZaakRolDto { VestigingsNummer = "VN1", Handelsnaam = ["Naam 1", "Naam 2"] });

    [Fact]
    public void VestigingZaakRolRequestDto_v1_to_ZaakRol_parity()
    {
        ReqRol.ZaakRolRequestDto request = new ReqRol.VestigingZaakRolRequestDto
        {
            Zaak = "https://example.test/zaken/1",
            Betrokkene = "https://example.test/betrokkenen/1",
            BetrokkeneType = BetrokkeneType.vestiging.ToString(),
            RolType = "https://example.test/roltypen/1",
            BetrokkeneIdentificatie = new Contracts1.VestigingZaakRolDto { VestigingsNummer = "VN1" },
        };
        AssertParity<ZaakRol>(request);
    }

    [Fact]
    public void OrganisatorischeEenheidZaakRolDto_to_OrganisatorischeEenheidZaakRol_parity() =>
        AssertParity<OrganisatorischeEenheidZaakRol>(
            new Contracts1.OrganisatorischeEenheidZaakRolDto
            {
                Identificatie = "OE1",
                Naam = "Naam",
                IsGehuisvestIn = "https://example.test/vestigingen/1",
            }
        );

    [Fact]
    public void OrganisatorischeEenheidZaakRolRequestDto_v1_to_ZaakRol_parity()
    {
        ReqRol.ZaakRolRequestDto request = new ReqRol.OrganisatorischeEenheidZaakRolRequestDto
        {
            Zaak = "https://example.test/zaken/1",
            Betrokkene = "https://example.test/betrokkenen/1",
            BetrokkeneType = BetrokkeneType.organisatorische_eenheid.ToString(),
            RolType = "https://example.test/roltypen/1",
            BetrokkeneIdentificatie = new Contracts1.OrganisatorischeEenheidZaakRolDto { Identificatie = "OE1", Naam = "Naam" },
        };
        AssertParity<ZaakRol>(request);
    }

    [Fact]
    public void MedewerkerZaakRolDto_to_MedewerkerZaakRol_parity() =>
        AssertParity<MedewerkerZaakRol>(
            new Contracts1.MedewerkerZaakRolDto
            {
                Identificatie = "MW1",
                Achternaam = "Achternaam",
                Voorletters = "V.",
                VoorvoegselAchternaam = "van",
            }
        );

    [Fact]
    public void MedewerkerZaakRolRequestDto_v1_to_ZaakRol_parity()
    {
        ReqRol.ZaakRolRequestDto request = new ReqRol.MedewerkerZaakRolRequestDto
        {
            Zaak = "https://example.test/zaken/1",
            Betrokkene = "https://example.test/betrokkenen/1",
            BetrokkeneType = BetrokkeneType.medewerker.ToString(),
            RolType = "https://example.test/roltypen/1",
            BetrokkeneIdentificatie = new Contracts1.MedewerkerZaakRolDto { Identificatie = "MW1" },
        };
        AssertParity<ZaakRol>(request);
    }

    [Fact]
    public void ZaakResultaatRequestDto_v1_to_ZaakResultaat_parity() =>
        AssertParity<ZaakResultaat>(
            new Req.ZaakResultaatRequestDto
            {
                Zaak = "https://example.test/zaken/1",
                ResultaatType = "https://example.test/resultaattypen/1",
                Toelichting = "toelichting",
            }
        );

    [Fact]
    public void GetAllZaakResultatenQueryParameters_to_Filter_parity() =>
        AssertParity<Filters.GetAllZaakResultatenFilter>(
            new Queries.GetAllZaakResultatenQueryParameters
            {
                Zaak = "https://example.test/zaken/1",
                ResultaatType = "https://example.test/resultaattypen/1",
            }
        );

    [Fact]
    public void ZaakEigenschapRequestDto_v1_to_ZaakEigenschap_parity() =>
        AssertParity<ZaakEigenschap>(
            new Req.ZaakEigenschapRequestDto
            {
                Zaak = "https://example.test/zaken/9337ba82-999a-4440-aa02-2b7b0b6c33f6",
                Eigenschap = "https://example.test/eigenschappen/1",
                Waarde = "waarde",
            }
        );

    [Fact]
    public void ZaakEigenschapRequestDto_v1_with_unparseable_zaak_url_throws_identically_parity()
    {
        // Both mappers invoke the identical shared ExtractIdFromZaak helper (duplicated verbatim in
        // the profile and the register) - confirm both surface the same InvalidOperationException
        // rather than one silently succeeding with a wrong/default value. AutoMapper wraps resolver
        // exceptions in AutoMapperMappingException (its own standard behavior); Mapster's compiled
        // lambda lets the original exception propagate directly.
        var source = new Req.ZaakEigenschapRequestDto
        {
            Zaak = "https://example.test/zaken/not-a-guid",
            Eigenschap = "e",
            Waarde = "w",
        };
        var amEx = Assert.Throws<AutoMapperMappingException>(() => _autoMapper.Map<ZaakEigenschap>(source));
        Assert.IsType<InvalidOperationException>(amEx.InnerException);
        Assert.Throws<InvalidOperationException>(() => _mapsterMapper.Map<ZaakEigenschap>(source));
    }

    [Fact]
    public void ZaakBesluitRequestDto_v1_to_ZaakBesluit_parity() =>
        AssertParity<ZaakBesluit>(new Req.ZaakBesluitRequestDto { Besluit = "https://example.test/besluiten/1" });

    [Fact]
    public void GetAllKlantContactenQueryParameters_to_Filter_parity() =>
        AssertParity<Filters.GetAllKlantContactenFilter>(new Queries.GetAllKlantContactenQueryParameters { Zaak = "https://example.test/zaken/1" });

    [Fact]
    public void KlantContactRequestDto_v1_to_KlantContact_parity() =>
        AssertParity<KlantContact>(
            new Req.KlantContactRequestDto
            {
                Zaak = "https://example.test/zaken/1",
                Identificatie = "KC1",
                DatumTijd = "2020-11-05 12:59:01",
                Kanaal = "kanaal",
                Onderwerp = "onderwerp",
                Toelichting = "toelichting",
            }
        );

    // =====================================================================================
    // v1 DomainToResponseRegister / DomainToResponseProfile (45 pairs)
    // =====================================================================================

    [Fact]
    public void Zaak_v1_to_ZaakResponseDto_parity()
    {
        var value = _fixture.Create<Zaak>();
        value.Deelzaken = [new Zaak { Id = Guid.NewGuid() }];
        AssertParity<Resp.ZaakResponseDto>(value);
    }

    [Fact]
    public void Zaak_v1_with_null_ZaakStatussen_to_ZaakResponseDto_Status_null_parity()
    {
        var value = _fixture.Create<Zaak>();
        value.ZaakStatussen = null;
        AssertParity<Resp.ZaakResponseDto>(value);
    }

    [Fact]
    public void Zaak_v1_with_multiple_ZaakStatussen_to_ZaakResponseDto_Status_latest_parity()
    {
        var zaak = new Zaak { Id = Guid.NewGuid() };
        var oldest = new ZaakStatus
        {
            Id = Guid.NewGuid(),
            Zaak = zaak,
            DatumStatusGezet = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        };
        var latest = new ZaakStatus
        {
            Id = Guid.NewGuid(),
            Zaak = zaak,
            DatumStatusGezet = new DateTime(2023, 6, 15, 0, 0, 0, DateTimeKind.Utc),
        };
        var middle = new ZaakStatus
        {
            Id = Guid.NewGuid(),
            Zaak = zaak,
            DatumStatusGezet = new DateTime(2021, 3, 3, 0, 0, 0, DateTimeKind.Utc),
        };
        zaak.ZaakStatussen = [oldest, latest, middle];
        AssertParity<Resp.ZaakResponseDto>(zaak);
    }

    [Fact]
    public void RelevanteAndereZaak_to_RelevanteAndereZaakDto_parity() =>
        AssertParity<Contracts1.RelevanteAndereZaakDto>(_fixture.Create<RelevanteAndereZaak>());

    [Fact]
    public void ZaakKenmerk_to_ZaakKenmerkDto_parity() => AssertParity<Contracts1.ZaakKenmerkDto>(_fixture.Create<ZaakKenmerk>());

    [Fact]
    public void ZaakVerlenging_to_ZaakVerlengingDto_parity() => AssertParity<Contracts1.ZaakVerlengingDto>(_fixture.Create<ZaakVerlenging>());

    [Fact]
    public void ZaakOpschorting_to_ZaakOpschortingDto_parity() => AssertParity<Contracts1.ZaakOpschortingDto>(_fixture.Create<ZaakOpschorting>());

    [Fact]
    public void Zaak_v1_to_ZaakRequestDto_parity() => AssertParity<Req.ZaakRequestDto>(_fixture.Create<Zaak>());

    [Fact]
    public void ZaakStatus_v1_to_ZaakStatusResponseDto_parity() => AssertParity<Resp.ZaakStatusResponseDto>(_fixture.Create<ZaakStatus>());

    [Fact]
    public void ZaakEigenschap_v1_to_ZaakEigenschapResponseDto_parity() =>
        AssertParity<Resp.ZaakEigenschapResponseDto>(_fixture.Create<ZaakEigenschap>());

    [Fact]
    public void ObjectTypeOverigeDefinitie_to_ObjectTypeOverigeDefinitieDto_parity() =>
        AssertParity<Contracts12.ObjectTypeOverigeDefinitieDto>(_fixture.Create<ObjectTypeOverigeDefinitie>());

    [Fact]
    public void ZaakObject_v1_bare_to_ZaakObjectResponseDto_parity()
    {
        var value = _fixture.Create<ZaakObject>();
        value.ObjectType = ObjectType.besluit; // arm that returns a bare ZaakObjectResponseDto()
        AssertParity<RespObj.ZaakObjectResponseDto>(value);
    }

    [Fact]
    public void ZaakObject_v1_with_ObjectType_adres_to_AdresZaakObjectResponseDto_parity()
    {
        var zaak = new Zaak { Id = Guid.NewGuid() };
        var adres = new AdresZaakObject
        {
            Id = Guid.NewGuid(),
            Identificatie = "adres-identificatie",
            WplWoonplaatsNaam = "Enschede",
            GorOpenbareRuimteNaam = "Hoofdstraat",
            Huisnummer = 42,
        };
        var source = new ZaakObject
        {
            Id = Guid.NewGuid(),
            Zaak = zaak,
            ObjectType = ObjectType.adres,
            Adres = adres,
        };
        AssertParity<RespObj.ZaakObjectResponseDto>(source);
    }

    [Fact]
    public void ZaakObject_v1_with_ObjectType_overige_to_OverigeZaakObjectResponseDto_parity()
    {
        var zaak = new Zaak { Id = Guid.NewGuid() };
        var overige = new OverigeZaakObject { Id = Guid.NewGuid(), OverigeData = "{\"key\":\"value\",\"count\":3}" };
        var source = new ZaakObject
        {
            Id = Guid.NewGuid(),
            Zaak = zaak,
            ObjectType = ObjectType.overige,
            Overige = overige,
        };
        AssertParity<RespObj.ZaakObjectResponseDto>(source);
    }

    [Fact]
    public void ZaakObject_v1_with_ObjectType_terrein_gebouwd_object_to_ResponseDto_parity()
    {
        var zaak = new Zaak { Id = Guid.NewGuid() };
        var terrein = new TerreinGebouwdObjectZaakObject { Id = Guid.NewGuid(), Identificatie = "TGO1" };
        var source = new ZaakObject
        {
            Id = Guid.NewGuid(),
            Zaak = zaak,
            ObjectType = ObjectType.terrein_gebouwd_object,
            TerreinGebouwdObject = terrein,
        };
        AssertParity<RespObj.ZaakObjectResponseDto>(source);
    }

    [Fact]
    public void AdresZaakObject_to_AdresZaakObjectDto_parity() => AssertParity<Contracts1.AdresZaakObjectDto>(_fixture.Create<AdresZaakObject>());

    [Fact]
    public void BuurtZaakObject_to_BuurtZaakObjectDto_parity() => AssertParity<Contracts1.BuurtZaakObjectDto>(_fixture.Create<BuurtZaakObject>());

    [Fact]
    public void PandZaakObject_to_PandZaakObjectDto_parity() => AssertParity<Contracts1.PandZaakObjectDto>(_fixture.Create<PandZaakObject>());

    [Fact]
    public void GemeenteZaakObject_to_GemeenteZaakObjectDto_parity() =>
        AssertParity<Contracts1.GemeenteZaakObjectDto>(_fixture.Create<GemeenteZaakObject>());

    [Fact]
    public void KadastraleOnroerendeZaakObject_to_KadastraleOnroerendeZaakObjectDto_parity() =>
        AssertParity<Contracts1.KadastraleOnroerendeZaakObjectDto>(_fixture.Create<KadastraleOnroerendeZaakObject>());

    [Fact]
    public void TerreinGebouwdObjectZaakObject_with_AdresAanduidingGrp_to_Dto_parity()
    {
        var value = _fixture.Create<TerreinGebouwdObjectZaakObject>();
        value.AdresAanduidingGrp_NumIdentificatie = "NUM1"; // ensures IsAdresAanduidingGrp true branch
        AssertParity<Contracts1.TerreinGebouwdObjectZaakObjectDto>(value);
    }

    [Fact]
    public void TerreinGebouwdObjectZaakObject_without_AdresAanduidingGrp_to_Dto_parity()
    {
        var value = new TerreinGebouwdObjectZaakObject { Id = Guid.NewGuid(), Identificatie = "ID1" };
        AssertParity<Contracts1.TerreinGebouwdObjectZaakObjectDto>(value);
    }

    [Fact]
    public void OverigeZaakObject_to_OverigeZaakObjectDto_parity() =>
        AssertParity<Contracts1.OverigeZaakObjectDto>(_fixture.Create<OverigeZaakObject>());

    [Fact]
    public void AanduidingWozObject_to_AanduidingWozObjectDto_parity() =>
        AssertParity<Contracts1.AanduidingWozObjectDto>(_fixture.Create<AanduidingWozObject>());

    [Fact]
    public void WozObject_to_WozObjectDto_parity() => AssertParity<Contracts1.WozObjectDto>(_fixture.Create<WozObject>());

    [Fact]
    public void WozWaardeZaakObject_to_WozWaardeZaakObjectDto_parity() =>
        AssertParity<Contracts1.WozWaardeZaakObjectDto>(_fixture.Create<WozWaardeZaakObject>());

    [Fact]
    public void ObjectTypeOverigeDefinitieDto_to_ObjectTypeOverigeDefinitie_reverse_parity() =>
        AssertParity<ObjectTypeOverigeDefinitie>(
            new Contracts12.ObjectTypeOverigeDefinitieDto
            {
                Url = "https://example.test/objecttypen/1",
                Schema = "schema",
                ObjectData = "objectdata",
            }
        );

    [Fact]
    public void ZaakObject_v1_to_ZaakObjectRequestDto_parity() => AssertParity<ReqObj.ZaakObjectRequestDto>(_fixture.Create<ZaakObject>());

    [Fact]
    public void AdresZaakObject_to_AdresZaakObjectRequestDto_parity() =>
        AssertParity<ReqObj.AdresZaakObjectRequestDto>(_fixture.Create<AdresZaakObject>());

    [Fact]
    public void BuurtZaakObject_to_BuurtZaakObjectRequestDto_parity() =>
        AssertParity<ReqObj.BuurtZaakObjectRequestDto>(_fixture.Create<BuurtZaakObject>());

    [Fact]
    public void GemeenteZaakObject_to_GemeenteZaakObjectRequestDto_parity() =>
        AssertParity<ReqObj.GemeenteZaakObjectRequestDto>(_fixture.Create<GemeenteZaakObject>());

    [Fact]
    public void KadastraleOnroerendeZaakObject_to_KadastraleOnroerendeZaakObjectRequestDto_parity() =>
        AssertParity<ReqObj.KadastraleOnroerendeZaakObjectRequestDto>(_fixture.Create<KadastraleOnroerendeZaakObject>());

    [Fact]
    public void OverigeZaakObject_to_OverigeZaakObjectRequestDto_parity() =>
        AssertParity<ReqObj.OverigeZaakObjectRequestDto>(_fixture.Create<OverigeZaakObject>());

    [Fact]
    public void PandZaakObject_to_PandZaakObjectRequestDto_parity() =>
        AssertParity<ReqObj.PandZaakObjectRequestDto>(_fixture.Create<PandZaakObject>());

    [Fact]
    public void TerreinGebouwdObjectZaakObject_to_TerreinGebouwdObjectZaakObjectRequestDto_parity() =>
        AssertParity<ReqObj.TerreinGebouwdObjectZaakObjectRequestDto>(_fixture.Create<TerreinGebouwdObjectZaakObject>());

    [Fact]
    public void WozWaardeZaakObject_to_WozWaardeZaakObjectRequestDto_parity() =>
        AssertParity<ReqObj.WozWaardeZaakObjectRequestDto>(_fixture.Create<WozWaardeZaakObject>());

    [Fact]
    public void ZaakInformatieObject_v1_to_ZaakInformatieObjectResponseDto_parity() =>
        AssertParity<Resp.ZaakInformatieObjectResponseDto>(_fixture.Create<ZaakInformatieObject>());

    [Fact]
    public void ZaakInformatieObject_v1_to_ZaakInformatieObjectRequestDto_parity() =>
        AssertParity<Req.ZaakInformatieObjectRequestDto>(_fixture.Create<ZaakInformatieObject>());

    [Fact]
    public void AardRelatieWeergave_legt_vast_to_expected_string_parity()
    {
        _fixture.Customize<ZaakInformatieObject>(c =>
            c.With(p => p.AardRelatieWeergave, AardRelatieWeergave.legt_vast_omgekeerd_kan_vastgelegd_zijn_als)
        );
        AssertParity<Resp.ZaakInformatieObjectResponseDto>(_fixture.Create<ZaakInformatieObject>());
    }

    [Fact]
    public void ZaakRol_v1_bare_to_ZaakRolResponseDto_parity()
    {
        var value = _fixture.Create<ZaakRol>();
        value.Registratiedatum = DateTime.UtcNow;
        value.BetrokkeneType = default; // arm that returns a bare ZaakRolResponseDto()
        AssertParity<RespRol.ZaakRolResponseDto>(value);
    }

    [Fact]
    public void ZaakRol_v1_with_BetrokkeneType_natuurlijk_persoon_to_ZaakRolResponseDto_parity()
    {
        var zaak = new Zaak { Id = Guid.NewGuid() };
        var natuurlijkPersoon = new NatuurlijkPersoonZaakRol
        {
            Id = Guid.NewGuid(),
            InpBsnEncrypted = "123456789",
            Geslachtsnaam = "Jansen",
        };
        var source = new ZaakRol
        {
            Id = Guid.NewGuid(),
            Zaak = zaak,
            BetrokkeneType = BetrokkeneType.natuurlijk_persoon,
            NatuurlijkPersoon = natuurlijkPersoon,
            Roltoelichting = "toelichting",
            Omschrijving = "omschrijving",
        };
        AssertParity<RespRol.ZaakRolResponseDto>(source);
    }

    [Fact]
    public void ZaakRol_v1_with_BetrokkeneType_vestiging_to_ZaakRolResponseDto_parity()
    {
        var zaak = new Zaak { Id = Guid.NewGuid() };
        var vestiging = new VestigingZaakRol { Id = Guid.NewGuid(), VestigingsNummer = "VN1" };
        var source = new ZaakRol
        {
            Id = Guid.NewGuid(),
            Zaak = zaak,
            BetrokkeneType = BetrokkeneType.vestiging,
            Vestiging = vestiging,
        };
        AssertParity<RespRol.ZaakRolResponseDto>(source);
    }

    [Fact]
    public void NatuurlijkPersoonZaakRol_to_NatuurlijkPersoonZaakRolDto_parity()
    {
        _fixture.Customize<NatuurlijkPersoonZaakRol>(c => c.With(p => p.Geboortedatum, DateTime.UtcNow));
        AssertParity<Contracts1.NatuurlijkPersoonZaakRolDto>(_fixture.Create<NatuurlijkPersoonZaakRol>());
    }

    [Fact]
    public void NietNatuurlijkPersoonZaakRol_to_NietNatuurlijkPersoonZaakRolDto_parity()
    {
        _fixture.Customize<NietNatuurlijkPersoonZaakRol>(c => c.With(p => p.InnRechtsvorm, _fixture.Create<InnRechtsvorm>()));
        AssertParity<Contracts1.NietNatuurlijkPersoonZaakRolDto>(_fixture.Create<NietNatuurlijkPersoonZaakRol>());
    }

    [Fact]
    public void VestigingZaakRol_v1_to_VestigingZaakRolDto_parity() =>
        AssertParity<Contracts1.VestigingZaakRolDto>(_fixture.Create<VestigingZaakRol>());

    [Fact]
    public void OrganisatorischeEenheidZaakRol_to_OrganisatorischeEenheidZaakRolDto_parity() =>
        AssertParity<Contracts1.OrganisatorischeEenheidZaakRolDto>(_fixture.Create<OrganisatorischeEenheidZaakRol>());

    [Fact]
    public void MedewerkerZaakRol_to_MedewerkerZaakRolDto_parity() =>
        AssertParity<Contracts1.MedewerkerZaakRolDto>(_fixture.Create<MedewerkerZaakRol>());

    [Fact]
    public void Verblijfsadres_to_VerblijfsadresDto_parity() => AssertParity<Contracts1.VerblijfsadresDto>(_fixture.Create<Verblijfsadres>());

    [Fact]
    public void SubVerblijfBuitenland_to_SubVerblijfBuitenlandDto_parity() =>
        AssertParity<Contracts1.SubVerblijfBuitenlandDto>(_fixture.Create<SubVerblijfBuitenland>());

    [Fact]
    public void ZaakResultaat_v1_to_ZaakResultaatResponseDto_parity() =>
        AssertParity<Resp.ZaakResultaatResponseDto>(_fixture.Create<ZaakResultaat>());

    [Fact]
    public void ZaakResultaat_v1_to_ZaakResultaatRequestDto_parity() => AssertParity<Req.ZaakResultaatRequestDto>(_fixture.Create<ZaakResultaat>());

    [Fact]
    public void ZaakBesluit_to_ZaakBesluitResponseDto_parity() => AssertParity<Resp.ZaakBesluitResponseDto>(_fixture.Create<ZaakBesluit>());

    [Fact]
    public void AuditTrailRegel_to_AuditTrailRegelDto_parity()
    {
        var value = _fixture
            .Build<AuditTrailRegel>()
            .With(a => a.Oud, "{\"naam\":\"oud-waarde\"}")
            .With(a => a.Nieuw, "{\"naam\":\"nieuw-waarde\"}")
            .Create();
        AssertParity<AuditTrailRegelDto>(value);
    }

    [Fact]
    public void AuditTrailRegel_with_null_or_empty_Oud_Nieuw_to_AuditTrailRegelDto_parity()
    {
        var value = _fixture.Build<AuditTrailRegel>().With(a => a.Oud, (string)null).With(a => a.Nieuw, "").Create();
        AssertParity<AuditTrailRegelDto>(value);
    }

    [Fact]
    public void KlantContact_v1_to_KlantContactResponseDto_parity()
    {
        _fixture.Customize<KlantContact>(c => c.With(p => p.DatumTijd, DateTime.UtcNow));
        AssertParity<Resp.KlantContactResponseDto>(_fixture.Create<KlantContact>());
    }

    // =====================================================================================
    // v1._2 DomainToResponseRegister / DomainToResponseProfile (1 pair)
    // =====================================================================================

    [Fact]
    public void ZaakEigenschap_v1_2_to_ZaakEigenschapRequestDto_parity()
    {
        var zaak = new Zaak { Id = Guid.NewGuid() };
        var source = new ZaakEigenschap
        {
            Id = Guid.NewGuid(),
            Zaak = zaak,
            Naam = "eigenschap-naam",
            Waarde = "eigenschap-waarde",
        };
        AssertParity<Req.ZaakEigenschapRequestDto>(source);
    }

    [Fact]
    public void ZaakEigenschap_v1_2_with_null_Zaak_to_ZaakEigenschapRequestDto_parity()
    {
        var source = new ZaakEigenschap
        {
            Id = Guid.NewGuid(),
            Zaak = null,
            Naam = "eigenschap-naam",
            Waarde = "eigenschap-waarde",
        };
        AssertParity<Req.ZaakEigenschapRequestDto>(source);
    }

    // =====================================================================================
    // v1._5 RequestToDomainRegister / RequestToDomainProfile (36 pairs; 1 shared with v1, see above)
    // =====================================================================================

    [Fact]
    public void GetAllZakenQueryParameters_v1_5_to_Filter_parity() =>
        AssertParity<Filters5.GetAllZakenFilter>(
            new Queries5.GetAllZakenQueryParameters
            {
                Identificatie = "ZK1",
                Bronorganisatie = TestRsin,
                Zaaktype = "https://example.test/zaaktypen/1",
                Archiefactiedatum = "2020-11-05",
                Startdatum = "2020-11-08",
                Archiefnominatie__in = $"{ArchiefNominatie.blijvend_bewaren}, {ArchiefNominatie.vernietigen}",
                Archiefstatus__in = $"{ArchiefStatus.nog_te_archiveren}, {ArchiefStatus.gearchiveerd}",
                Rol__betrokkeneType = BetrokkeneType.natuurlijk_persoon.ToString(),
                MaximaleVertrouwelijkheidaanduiding = VertrouwelijkheidAanduiding.openbaar.ToString(),
            }
        );

    [Fact]
    public void ZaakSearchRequestDto_v1_5_with_null_arrays_to_Filter_empty_arrays_parity() =>
        AssertParity<Filters5.GetAllZakenFilter>(
            new Req5.ZaakSearchRequestDto
            {
                Archiefnominatie__in = null,
                Archiefstatus__in = null,
                Bronorganisatie__in = null,
                Uuid__in = null,
                Zaaktype__in = null,
            }
        );

    [Fact]
    public void ZaakSearchRequestDto_v1_5_with_populated_arrays_to_Filter_parity() =>
        AssertParity<Filters5.GetAllZakenFilter>(
            new Req5.ZaakSearchRequestDto
            {
                Archiefnominatie__in = [ArchiefNominatie.vernietigen.ToString()],
                Archiefstatus__in = [ArchiefStatus.overgedragen.ToString()],
                Bronorganisatie__in = [TestRsin],
                Uuid__in = ["9337ba82-999a-4440-aa02-2b7b0b6c33f6"],
                Zaaktype__in = ["https://example.test/zaaktypen/1"],
            }
        );

    [Fact]
    public void ZaakProcessobjectDto_to_ZaakProcessobject_parity() =>
        AssertParity<ZaakProcessobject>(
            new Contracts5.ZaakProcessobjectDto
            {
                Datumkenmerk = "datumkenmerk",
                Identificatie = "ID1",
                Objecttype = "objecttype",
                Registratie = "registratie",
            }
        );

    [Fact]
    public void ZaakRequestDto_v1_5_to_Zaak_parity() =>
        AssertParity<Zaak>(
            new Req5.ZaakRequestDto
            {
                Identificatie = "ZK1",
                Bronorganisatie = TestRsin,
                Omschrijving = "omschrijving",
                Toelichting = "toelichting",
                Zaaktype = "https://example.test/zaaktypen/1/",
                Registratiedatum = "2020-11-06",
                VerantwoordelijkeOrganisatie = TestRsin,
                Startdatum = "2020-11-07",
                EinddatumGepland = "2020-11-08",
                UiterlijkeEinddatumAfdoening = "2020-11-09",
                Publicatiedatum = "2020-11-10",
                Vertrouwelijkheidaanduiding = VertrouwelijkheidAanduiding.openbaar.ToString(),
                Betalingsindicatie = BetalingsIndicatie.geheel.ToString(),
                LaatsteBetaaldatum = "2020-11-11T12:13:14Z",
                Archiefnominatie = ArchiefNominatie.blijvend_bewaren.ToString(),
                Archiefstatus = ArchiefStatus.nog_te_archiveren.ToString(),
                Archiefactiedatum = "2020-11-12",
                OpdrachtgevendeOrganisatie = TestRsin,
                Processobjectaard = "processobjectaard",
                StartdatumBewaartermijn = "2020-11-13",
                Processobject = new Contracts5.ZaakProcessobjectDto
                {
                    Datumkenmerk = "datumkenmerk",
                    Identificatie = "ID1",
                    Objecttype = "objecttype",
                    Registratie = "registratie",
                },
            }
        );

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void MapVertrouwelijkheidAanduiding_v1_5_with_blank_input_to_nullvalue_parity(string vertrouwelijkheidaanduiding) =>
        AssertParity<Zaak>(
            new Req5.ZaakRequestDto
            {
                Zaaktype = "https://example.test/zaaktypen/1",
                Startdatum = "2020-11-07",
                Vertrouwelijkheidaanduiding = vertrouwelijkheidaanduiding,
            }
        );

    [Fact]
    public void MapVertrouwelijkheidAanduiding_v1_5_with_valid_input_to_parsed_enum_parity() =>
        AssertParity<Zaak>(
            new Req5.ZaakRequestDto
            {
                Zaaktype = "https://example.test/zaaktypen/1",
                Startdatum = "2020-11-07",
                Vertrouwelijkheidaanduiding = VertrouwelijkheidAanduiding.zeer_geheim.ToString(),
            }
        );

    [Fact]
    public void GetAllZaakStatussenQueryParameters_v1_5_to_Filter_parity() =>
        AssertParity<Filters5.GetAllZaakStatussenFilter>(
            new Queries5.GetAllZaakStatussenQueryParameters
            {
                Zaak = "https://example.test/zaken/1",
                StatusType = "https://example.test/statustypen/1",
                IndicatieLaatstGezetteStatus = "true",
            }
        );

    [Fact]
    public void ZaakStatusRequestDto_v1_5_to_ZaakStatus_parity() =>
        AssertParity<ZaakStatus>(
            new Req5.ZaakStatusRequestDto
            {
                Zaak = "https://example.test/zaken/1",
                StatusType = "https://example.test/statustypen/1",
                DatumStatusGezet = "2020-11-06T12:13:14Z",
                StatusToelichting = "toelichting",
                GezetDoor = "https://example.test/rollen/1",
            }
        );

    [Fact]
    public void AdresZaakObjectRequestDto_v1_5_MapsWith_CreateAdresZaakObject_parity() =>
        AssertParity<AdresZaakObject>(new ReqObj5.AdresZaakObjectRequestDto { ObjectIdentificatie = SampleAdresDto() });

    [Fact]
    public void BuurtZaakObjectRequestDto_v1_5_MapsWith_CreateBuurtZaakObject_parity() =>
        AssertParity<BuurtZaakObject>(
            new ReqObj5.BuurtZaakObjectRequestDto
            {
                ObjectIdentificatie = new Contracts1.BuurtZaakObjectDto { BuurtCode = "BC1", BuurtNaam = "Buurtnaam" },
            }
        );

    [Fact]
    public void PandZaakObjectRequestDto_v1_5_MapsWith_CreatePandZaakObject_parity() =>
        AssertParity<PandZaakObject>(
            new ReqObj5.PandZaakObjectRequestDto { ObjectIdentificatie = new Contracts1.PandZaakObjectDto { Identificatie = "ID1" } }
        );

    [Fact]
    public void KadastraleOnroerendeZaakObjectRequestDto_v1_5_MapsWith_CreateKadastraleOnroerendeZaakObject_parity() =>
        AssertParity<KadastraleOnroerendeZaakObject>(
            new ReqObj5.KadastraleOnroerendeZaakObjectRequestDto
            {
                ObjectIdentificatie = new Contracts1.KadastraleOnroerendeZaakObjectDto { KadastraleIdentificatie = "ID1" },
            }
        );

    [Fact]
    public void GemeenteZaakObjectRequestDto_v1_5_MapsWith_CreateGemeenteZaakObject_parity() =>
        AssertParity<GemeenteZaakObject>(
            new ReqObj5.GemeenteZaakObjectRequestDto { ObjectIdentificatie = new Contracts1.GemeenteZaakObjectDto { GemeenteCode = "GC1" } }
        );

    [Fact]
    public void TerreinGebouwdObjectZaakObjectRequestDto_v1_5_MapsWith_CreateTerreinGebouwdObjectZaakObject_parity() =>
        AssertParity<TerreinGebouwdObjectZaakObject>(
            new ReqObj5.TerreinGebouwdObjectZaakObjectRequestDto
            {
                ObjectIdentificatie = new Contracts1.TerreinGebouwdObjectZaakObjectDto
                {
                    Identificatie = "ID1",
                    AdresAanduidingGrp = SampleAdresAanduidingGrpDto(),
                },
            }
        );

    [Fact]
    public void TerreinGebouwdObjectZaakObjectRequestDto_v1_5_with_null_AdresAanduidingGrp_parity() =>
        AssertParity<TerreinGebouwdObjectZaakObject>(
            new ReqObj5.TerreinGebouwdObjectZaakObjectRequestDto
            {
                ObjectIdentificatie = new Contracts1.TerreinGebouwdObjectZaakObjectDto { Identificatie = "ID1", AdresAanduidingGrp = null },
            }
        );

    [Fact]
    public void OverigeZaakObjectRequestDto_v1_5_MapsWith_CreateOverigeZaakObject_parity() =>
        // This version's factory calls .ToString(Formatting.None) directly on the JToken, not
        // JsonConvert.SerializeObject(...) like the v1 sibling's factory - a genuinely different
        // duplicated implementation, worth its own dedicated fact.
        AssertParity<OverigeZaakObject>(
            new ReqObj5.OverigeZaakObjectRequestDto
            {
                ObjectIdentificatie = new Contracts1.OverigeZaakObjectDto { OverigeData = JToken.Parse("""{"foo":"bar","n":3}""") },
            }
        );

    [Fact]
    public void WozWaardeZaakObjectRequestDto_v1_5_MapsWith_CreateWozWaardeZaakObject_parity() =>
        AssertParity<WozWaardeZaakObject>(
            new ReqObj5.WozWaardeZaakObjectRequestDto
            {
                ObjectIdentificatie = new Contracts1.WozWaardeZaakObjectDto { WaardePeildatum = "2020-01-01" },
            }
        );

    [Fact]
    public void ZaakObjectRequestDto_v1_5_without_derived_data_to_ZaakObject_parity() =>
        AssertParity<ZaakObject>(
            new ReqObj5.ZaakObjectRequestDto
            {
                Object = "https://example.test/objects/1",
                ObjectType = ObjectType.gemeentelijke_openbare_ruimte.ToString(),
                ObjectTypeOverige = "overige",
                RelatieOmschrijving = "relatie omschrijving",
            }
        );

    [Fact]
    public void ZaakObjectRequestDto_v1_5_base_typed_reference_holding_AdresZaakObjectRequestDto_dispatches_parity()
    {
        ReqObj5.ZaakObjectRequestDto request = new ReqObj5.AdresZaakObjectRequestDto
        {
            Object = "https://example.test/objects/1",
            ObjectType = ObjectType.adres.ToString(),
            RelatieOmschrijving = "relatie omschrijving",
            ObjectIdentificatie = SampleAdresDto(),
        };
        AssertParity<ZaakObject>(request);
    }

    [Fact]
    public void BuurtZaakObjectRequestDto_v1_5_to_ZaakObject_parity() =>
        AssertParity<ZaakObject>(
            new ReqObj5.BuurtZaakObjectRequestDto { ObjectIdentificatie = new Contracts1.BuurtZaakObjectDto { BuurtCode = "BC1" } }
        );

    [Fact]
    public void PandZaakObjectRequestDto_v1_5_to_ZaakObject_parity() =>
        AssertParity<ZaakObject>(
            new ReqObj5.PandZaakObjectRequestDto { ObjectIdentificatie = new Contracts1.PandZaakObjectDto { Identificatie = "ID1" } }
        );

    [Fact]
    public void KadastraleOnroerendeZaakObjectRequestDto_v1_5_to_ZaakObject_parity() =>
        AssertParity<ZaakObject>(
            new ReqObj5.KadastraleOnroerendeZaakObjectRequestDto
            {
                ObjectIdentificatie = new Contracts1.KadastraleOnroerendeZaakObjectDto { KadastraleIdentificatie = "ID1" },
            }
        );

    [Fact]
    public void GemeenteZaakObjectRequestDto_v1_5_to_ZaakObject_parity() =>
        AssertParity<ZaakObject>(
            new ReqObj5.GemeenteZaakObjectRequestDto { ObjectIdentificatie = new Contracts1.GemeenteZaakObjectDto { GemeenteCode = "GC1" } }
        );

    [Fact]
    public void TerreinGebouwdObjectZaakObjectRequestDto_v1_5_to_ZaakObject_parity() =>
        AssertParity<ZaakObject>(
            new ReqObj5.TerreinGebouwdObjectZaakObjectRequestDto
            {
                ObjectIdentificatie = new Contracts1.TerreinGebouwdObjectZaakObjectDto { Identificatie = "ID1" },
            }
        );

    [Fact]
    public void OverigeZaakObjectRequestDto_v1_5_to_ZaakObject_parity() =>
        AssertParity<ZaakObject>(
            new ReqObj5.OverigeZaakObjectRequestDto
            {
                ObjectIdentificatie = new Contracts1.OverigeZaakObjectDto { OverigeData = JToken.Parse("""{"a":1}""") },
            }
        );

    [Fact]
    public void WozWaardeZaakObjectRequestDto_v1_5_to_ZaakObject_parity() =>
        AssertParity<ZaakObject>(
            new ReqObj5.WozWaardeZaakObjectRequestDto
            {
                ObjectIdentificatie = new Contracts1.WozWaardeZaakObjectDto { WaardePeildatum = "2020-01-01" },
            }
        );

    [Fact]
    public void ZaakInformatieObjectRequestDto_v1_5_to_ZaakInformatieObject_parity() =>
        AssertParity<ZaakInformatieObject>(
            new Req5.ZaakInformatieObjectRequestDto
            {
                Zaak = "https://example.test/zaken/1",
                InformatieObject = "https://example.test/informatieobjecten/1",
                Beschrijving = "beschrijving",
                Titel = "titel",
                VernietigingsDatum = "2020-11-06T12:13:14Z",
            }
        );

    [Fact]
    public void ZaakRolRequestDto_v1_5_to_ZaakRol_parity() =>
        AssertParity<ZaakRol>(
            new ReqRol5.ZaakRolRequestDto
            {
                Zaak = "https://example.test/zaken/1",
                Betrokkene = "https://example.test/betrokkenen/1",
                BetrokkeneType = BetrokkeneType.natuurlijk_persoon.ToString(),
                RolType = "https://example.test/roltypen/1",
                RolToelichting = "roltoelichting",
                IndicatieMachtiging = IndicatieMachtiging.gemachtigde.ToString(),
                ContactpersoonRol = new Contracts5.ContactpersoonRolDto { Naam = "naam" },
            }
        );

    [Fact]
    public void ContactpersoonRolDto_to_ContactpersoonRol_parity() =>
        AssertParity<ContactpersoonRol>(
            new Contracts5.ContactpersoonRolDto
            {
                EmailAdres = "test@example.test",
                Functie = "functie",
                Telefoonnummer = "0612345678",
                Naam = "naam",
            }
        );

    [Fact]
    public void NatuurlijkPersoonZaakRolRequestDto_v1_5_base_typed_reference_dispatches_parity()
    {
        ReqRol5.ZaakRolRequestDto request = new ReqRol5.NatuurlijkPersoonZaakRolRequestDto
        {
            Zaak = "https://example.test/zaken/1",
            Betrokkene = "https://example.test/betrokkenen/1",
            BetrokkeneType = BetrokkeneType.natuurlijk_persoon.ToString(),
            RolType = "https://example.test/roltypen/1",
            RolToelichting = "toelichting",
            AfwijkendeNaamBetrokkene = "afwijkende naam",
            BetrokkeneIdentificatie = new Contracts1.NatuurlijkPersoonZaakRolDto { InpBsn = TestRsin, Geslachtsnaam = "Jansen" },
        };
        AssertParity<ZaakRol>(request);
    }

    [Fact]
    public void NietNatuurlijkPersoonZaakRolRequestDto_v1_5_to_ZaakRol_parity()
    {
        ReqRol5.ZaakRolRequestDto request = new ReqRol5.NietNatuurlijkPersoonZaakRolRequestDto
        {
            Zaak = "https://example.test/zaken/1",
            Betrokkene = "https://example.test/betrokkenen/1",
            BetrokkeneType = BetrokkeneType.niet_natuurlijk_persoon.ToString(),
            RolType = "https://example.test/roltypen/1",
            BetrokkeneIdentificatie = new Contracts1.NietNatuurlijkPersoonZaakRolDto { InnNnpId = "NNP1", StatutaireNaam = "Naam BV" },
        };
        AssertParity<ZaakRol>(request);
    }

    [Fact]
    public void VestigingZaakRolDto_v1_5_to_VestigingZaakRol_parity() =>
        AssertParity<VestigingZaakRol>(new Contracts5.VestigingZaakRolDto { VestigingsNummer = "VN1", KvKNummer = "12345678" });

    [Fact]
    public void VestigingZaakRolRequestDto_v1_5_to_ZaakRol_including_new_KvkNummer_field_parity()
    {
        // Discriminates that KvKNummer (v1.5's new field) round-trips to the domain's KvkNummer despite
        // the casing difference, via NameMatchingStrategy.IgnoreCase (matching AutoMapper's own
        // case-insensitive default convention, which has no explicit ForMember for this field either).
        ReqRol5.ZaakRolRequestDto request = new ReqRol5.VestigingZaakRolRequestDto
        {
            Zaak = "https://example.test/zaken/1",
            Betrokkene = "https://example.test/betrokkenen/1",
            BetrokkeneType = BetrokkeneType.vestiging.ToString(),
            RolType = "https://example.test/roltypen/1",
            BetrokkeneIdentificatie = new Contracts5.VestigingZaakRolDto { VestigingsNummer = "VN1", KvKNummer = "12345678" },
        };
        AssertParity<ZaakRol>(request);
    }

    [Fact]
    public void OrganisatorischeEenheidZaakRolRequestDto_v1_5_to_ZaakRol_parity()
    {
        ReqRol5.ZaakRolRequestDto request = new ReqRol5.OrganisatorischeEenheidZaakRolRequestDto
        {
            Zaak = "https://example.test/zaken/1",
            Betrokkene = "https://example.test/betrokkenen/1",
            BetrokkeneType = BetrokkeneType.organisatorische_eenheid.ToString(),
            RolType = "https://example.test/roltypen/1",
            BetrokkeneIdentificatie = new Contracts1.OrganisatorischeEenheidZaakRolDto { Identificatie = "OE1", Naam = "Naam" },
        };
        AssertParity<ZaakRol>(request);
    }

    [Fact]
    public void MedewerkerZaakRolRequestDto_v1_5_to_ZaakRol_parity()
    {
        ReqRol5.ZaakRolRequestDto request = new ReqRol5.MedewerkerZaakRolRequestDto
        {
            Zaak = "https://example.test/zaken/1",
            Betrokkene = "https://example.test/betrokkenen/1",
            BetrokkeneType = BetrokkeneType.medewerker.ToString(),
            RolType = "https://example.test/roltypen/1",
            BetrokkeneIdentificatie = new Contracts1.MedewerkerZaakRolDto { Identificatie = "MW1" },
        };
        AssertParity<ZaakRol>(request);
    }

    [Fact]
    public void GetAllZaakVerzoekenQueryParameters_to_Filter_parity() =>
        AssertParity<Filters5.GetAllZaakVerzoekenFilter>(
            new Queries5.GetAllZaakVerzoekenQueryParameters { Zaak = "https://example.test/zaken/1", Verzoek = "https://example.test/verzoeken/1" }
        );

    [Fact]
    public void ZaakVerzoekRequestDto_to_ZaakVerzoek_parity() =>
        AssertParity<ZaakVerzoek>(
            new Req5.ZaakVerzoekRequestDto { Zaak = "https://example.test/zaken/1", Verzoek = "https://example.test/verzoeken/1" }
        );

    [Fact]
    public void GetAllZaakContactmomentenQueryParameters_to_Filter_parity() =>
        AssertParity<Filters5.GetAllZaakContactmomentenFilter>(
            new Queries5.GetAllZaakContactmomentenQueryParameters
            {
                Zaak = "https://example.test/zaken/1",
                Contactmoment = "https://example.test/contactmomenten/1",
            }
        );

    [Fact]
    public void ZaakContactmomentRequestDto_to_ZaakContactmoment_parity() =>
        AssertParity<ZaakContactmoment>(
            new Req5.ZaakContactmomentRequestDto { Zaak = "https://example.test/zaken/1", Contactmoment = "https://example.test/contactmomenten/1" }
        );

    // =====================================================================================
    // v1._5 DomainToResponseRegister / DomainToResponseProfile (22 pairs)
    // =====================================================================================

    [Fact]
    public void Zaak_v1_5_to_ZaakResponseDto_parity()
    {
        var value = _fixture.Create<Zaak>();
        value.Deelzaken = [new Zaak { Id = Guid.NewGuid() }];
        AssertParity<Resp5.ZaakResponseDto>(value);
    }

    [Fact]
    public void Zaak_v1_5_with_null_ZaakStatussen_to_ZaakResponseDto_Status_null_parity()
    {
        var value = _fixture.Create<Zaak>();
        value.ZaakStatussen = null;
        AssertParity<Resp5.ZaakResponseDto>(value);
    }

    [Fact]
    public void Zaak_v1_5_with_multiple_ZaakStatussen_to_ZaakResponseDto_Status_latest_parity()
    {
        var zaak = new Zaak { Id = Guid.NewGuid() };
        var older = new ZaakStatus
        {
            Id = Guid.NewGuid(),
            Zaak = zaak,
            DatumStatusGezet = DateTime.UtcNow.AddDays(-2),
        };
        var latest = new ZaakStatus
        {
            Id = Guid.NewGuid(),
            Zaak = zaak,
            DatumStatusGezet = DateTime.UtcNow,
        };
        zaak.ZaakStatussen = [older, latest];
        AssertParity<Resp5.ZaakResponseDto>(zaak);
    }

    [Fact]
    public void ZaakProcessobject_to_ZaakProcessobjectDto_parity() =>
        // Deduplicates the source AutoMapper profile's double registration of this exact TypePair
        // (once empty, once with the real member maps - AutoMapper merges both onto the same TypePair).
        AssertParity<Contracts5.ZaakProcessobjectDto>(_fixture.Create<ZaakProcessobject>());

    [Fact]
    public void Zaak_v1_5_to_ZaakRequestDto_parity() => AssertParity<Req5.ZaakRequestDto>(_fixture.Create<Zaak>());

    [Fact]
    public void ZaakStatus_v1_5_to_ZaakStatusCreateResponseDto_parity() =>
        AssertParity<Resp5.ZaakStatusCreateResponseDto>(_fixture.Create<ZaakStatus>());

    [Fact]
    public void ZaakStatus_v1_5_to_ZaakStatusGetResponseDto_parity()
    {
        var zaak = RootedZaak();
        var informatieObject = new ZaakInformatieObject { Id = Guid.NewGuid(), Zaak = zaak };
        zaak.ZaakInformatieObjecten = [informatieObject];
        var source = new ZaakStatus { Id = Guid.NewGuid(), Zaak = zaak };
        AssertParity<Resp5.ZaakStatusGetResponseDto>(source);
    }

    [Fact]
    public void ZaakObject_v1_5_bare_to_ZaakObjectResponseDto_parity()
    {
        var value = _fixture.Create<ZaakObject>();
        value.ObjectType = ObjectType.status; // arm that returns a bare ZaakObjectResponseDto()
        AssertParity<RespObj5.ZaakObjectResponseDto>(value);
    }

    [Fact]
    public void ZaakObject_v1_5_with_ObjectType_adres_to_AdresZaakObjectResponseDto_parity()
    {
        var zaak = new Zaak { Id = Guid.NewGuid() };
        var adres = new AdresZaakObject
        {
            Id = Guid.NewGuid(),
            Huisletter = "A",
            Huisnummer = 12,
            Postcode = "1234AB",
        };
        var source = new ZaakObject
        {
            Id = Guid.NewGuid(),
            Zaak = zaak,
            ObjectType = ObjectType.adres,
            Adres = adres,
        };
        AssertParity<RespObj5.ZaakObjectResponseDto>(source);
    }

    [Fact]
    public void ZaakObject_v1_5_with_ObjectType_overige_to_OverigeZaakObjectResponseDto_parity()
    {
        var zaak = new Zaak { Id = Guid.NewGuid() };
        var overige = new OverigeZaakObject { Id = Guid.NewGuid(), OverigeData = "{\"key\":\"value\",\"count\":3}" };
        var source = new ZaakObject
        {
            Id = Guid.NewGuid(),
            Zaak = zaak,
            ObjectType = ObjectType.overige,
            Overige = overige,
        };
        AssertParity<RespObj5.ZaakObjectResponseDto>(source);
    }

    [Fact]
    public void ZaakObject_v1_5_to_ZaakObjectRequestDto_parity() => AssertParity<ReqObj5.ZaakObjectRequestDto>(_fixture.Create<ZaakObject>());

    [Fact]
    public void AdresZaakObject_to_AdresZaakObjectRequestDto_v1_5_parity() =>
        AssertParity<ReqObj5.AdresZaakObjectRequestDto>(_fixture.Create<AdresZaakObject>());

    [Fact]
    public void BuurtZaakObject_to_BuurtZaakObjectRequestDto_v1_5_parity() =>
        AssertParity<ReqObj5.BuurtZaakObjectRequestDto>(_fixture.Create<BuurtZaakObject>());

    [Fact]
    public void GemeenteZaakObject_to_GemeenteZaakObjectRequestDto_v1_5_parity() =>
        AssertParity<ReqObj5.GemeenteZaakObjectRequestDto>(_fixture.Create<GemeenteZaakObject>());

    [Fact]
    public void KadastraleOnroerendeZaakObject_to_KadastraleOnroerendeZaakObjectRequestDto_v1_5_parity() =>
        AssertParity<ReqObj5.KadastraleOnroerendeZaakObjectRequestDto>(_fixture.Create<KadastraleOnroerendeZaakObject>());

    [Fact]
    public void OverigeZaakObject_to_OverigeZaakObjectRequestDto_v1_5_parity() =>
        AssertParity<ReqObj5.OverigeZaakObjectRequestDto>(_fixture.Create<OverigeZaakObject>());

    [Fact]
    public void PandZaakObject_to_PandZaakObjectRequestDto_v1_5_parity() =>
        AssertParity<ReqObj5.PandZaakObjectRequestDto>(_fixture.Create<PandZaakObject>());

    [Fact]
    public void TerreinGebouwdObjectZaakObject_to_TerreinGebouwdObjectZaakObjectRequestDto_v1_5_parity() =>
        AssertParity<ReqObj5.TerreinGebouwdObjectZaakObjectRequestDto>(_fixture.Create<TerreinGebouwdObjectZaakObject>());

    [Fact]
    public void WozWaardeZaakObject_to_WozWaardeZaakObjectRequestDto_v1_5_parity() =>
        AssertParity<ReqObj5.WozWaardeZaakObjectRequestDto>(_fixture.Create<WozWaardeZaakObject>());

    [Fact]
    public void ZaakInformatieObject_v1_5_to_ZaakInformatieObjectResponseDto_parity()
    {
        var zaak = RootedZaak();
        var status = new ZaakStatus { Id = Guid.NewGuid(), Zaak = zaak };
        var value = _fixture.Create<ZaakInformatieObject>();
        value.Zaak = zaak;
        value.Status = status;
        AssertParity<Resp5.ZaakInformatieObjectResponseDto>(value);
    }

    [Fact]
    public void ZaakInformatieObject_v1_5_to_ZaakInformatieObjectRequestDto_parity()
    {
        var zaak = RootedZaak();
        var status = new ZaakStatus { Id = Guid.NewGuid(), Zaak = zaak };
        var value = _fixture.Create<ZaakInformatieObject>();
        value.Zaak = zaak;
        value.Status = status;
        AssertParity<Req5.ZaakInformatieObjectRequestDto>(value);
    }

    [Fact]
    public void ZaakRol_v1_5_bulk_to_ZaakRolResponseDto_parity()
    {
        var zaak = new Zaak { Id = Guid.NewGuid() };
        var value = _fixture.Create<ZaakRol>();
        value.Zaak = zaak;
        value.Registratiedatum = DateTime.UtcNow;
        value.BetrokkeneType = default;
        AssertParity<RespRol5.ZaakRolResponseDto>(value);
    }

    [Fact]
    public void ZaakRol_v1_5_with_BetrokkeneType_natuurlijk_persoon_to_ZaakRolResponseDto_parity()
    {
        var zaak = new Zaak { Id = Guid.NewGuid() };
        var natuurlijkPersoon = new NatuurlijkPersoonZaakRol
        {
            Id = Guid.NewGuid(),
            InpBsnEncrypted = "123456789",
            Geslachtsnaam = "Jansen",
        };
        var source = new ZaakRol
        {
            Id = Guid.NewGuid(),
            Zaak = zaak,
            BetrokkeneType = BetrokkeneType.natuurlijk_persoon,
            NatuurlijkPersoon = natuurlijkPersoon,
        };
        AssertParity<RespRol5.ZaakRolResponseDto>(source);
    }

    [Fact]
    public void ZaakRol_v1_5_with_null_Zaak_ZaakStatussen_Statussen_parity()
    {
        // *** THE OPEN QUESTION ***: ZaakRol.Statussen (v1.5 only) is a plain MapFrom (not a
        // PreCondition) whose lambda body itself does `src.Zaak.ZaakStatussen != null ? ... : null`.
        // A bare-config unit test (v1_5/DomainToResponseProfileTests.cs) already confirmed Mapster's
        // OWN isolated behavior stays null for a null-source case, but that test has NO
        // EmptyCollectionIfNull transform active, so it does NOT prove what happens in the REAL
        // AddZgwMapster-wired config (which THIS harness wires). This fact is the definitive real-vs-
        // real comparison: if AutoMapper's own AllowNullCollections=false coalesces this explicit-
        // MapFrom-null to [] the same way Mapster's EmptyCollectionIfNull transform would, they agree
        // and no fix is needed; if AutoMapper keeps null while Mapster's transform turns it into [],
        // this fails and the fix (documented in the register: convert to
        // .Ignore(dest.Statussen) + .AfterMapping(...) to bypass the transform) must be applied.
        var zaak = new Zaak { Id = Guid.NewGuid(), ZaakStatussen = null };
        var source = new ZaakRol
        {
            Id = Guid.NewGuid(),
            Zaak = zaak,
            Betrokkene = "https://example.test/betrokkenen/1",
            BetrokkeneType = BetrokkeneType.vestiging,
        };
        AssertParity<RespRol5.ZaakRolResponseDto>(source);
    }

    [Fact]
    public void ZaakRol_v1_5_with_ZaakStatussen_Statussen_filtered_by_GezetDoor_and_ordered_parity()
    {
        var zaak = new Zaak { Id = Guid.NewGuid() };
        var betrokkene = "https://example.test/betrokkenen/1";
        var matchingOlder = new ZaakStatus
        {
            Id = Guid.NewGuid(),
            Zaak = zaak,
            GezetDoor = betrokkene,
            DatumStatusGezet = DateTime.UtcNow.AddDays(-1),
        };
        var matchingNewer = new ZaakStatus
        {
            Id = Guid.NewGuid(),
            Zaak = zaak,
            GezetDoor = betrokkene,
            DatumStatusGezet = DateTime.UtcNow,
        };
        var nonMatching = new ZaakStatus
        {
            Id = Guid.NewGuid(),
            Zaak = zaak,
            GezetDoor = "https://example.test/betrokkenen/other",
            DatumStatusGezet = DateTime.UtcNow.AddHours(1),
        };
        zaak.ZaakStatussen = [matchingNewer, nonMatching, matchingOlder];
        var source = new ZaakRol
        {
            Id = Guid.NewGuid(),
            Zaak = zaak,
            Betrokkene = betrokkene,
            BetrokkeneType = BetrokkeneType.vestiging,
        };
        AssertParity<RespRol5.ZaakRolResponseDto>(source);
    }

    [Fact]
    public void ContactpersoonRol_to_ContactpersoonRolDto_parity() =>
        AssertParity<Contracts5.ContactpersoonRolDto>(_fixture.Create<ContactpersoonRol>());

    [Fact]
    public void VestigingZaakRol_v1_5_to_VestigingZaakRolDto_parity() =>
        AssertParity<Contracts5.VestigingZaakRolDto>(_fixture.Create<VestigingZaakRol>());

    [Fact]
    public void ZaakVerzoek_to_ZaakVerzoekResponseDto_parity() => AssertParity<Resp5.ZaakVerzoekResponseDto>(_fixture.Create<ZaakVerzoek>());

    [Fact]
    public void ZaakContactmoment_to_ZaakContactmomentResponseDto_parity() =>
        AssertParity<Resp5.ZaakContactmomentResponseDto>(_fixture.Create<ZaakContactmoment>());
}
