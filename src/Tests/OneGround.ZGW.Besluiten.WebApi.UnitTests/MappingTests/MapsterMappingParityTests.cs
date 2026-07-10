using System;
using AutoFixture;
using AutoMapper;
using AutoMapper.Internal;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Newtonsoft.Json;
using OneGround.ZGW.Besluiten.Contracts.v1.Requests;
using OneGround.ZGW.Besluiten.Contracts.v1.Responses;
using OneGround.ZGW.Besluiten.DataModel;
using OneGround.ZGW.Besluiten.Web.MappingProfiles.v1;
using OneGround.ZGW.Common.Contracts.v1.AuditTrail;
using OneGround.ZGW.Common.Web;
using OneGround.ZGW.Common.Web.Mapping.Mapster;
using OneGround.ZGW.Common.Web.Mapping.ValueResolvers;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.DataAccess;
using OneGround.ZGW.DataAccess.AuditTrail;
using Xunit;
using AutoMapperIMapper = AutoMapper.IMapper;
using MapsterIMapper = MapsterMapper.IMapper;

namespace OneGround.ZGW.Besluiten.WebApi.UnitTests.MappingTests;

public class MapsterMappingParityTests : IDisposable
{
    private readonly AutoMapperFixture _fixture = new AutoMapperFixture();
    private readonly Mock<IEntityUriService> _mockedUriService = new Mock<IEntityUriService>();
    private readonly AutoMapperIMapper _autoMapper;
    private readonly ServiceProvider _provider;
    private readonly IServiceScope _scope;
    private readonly MapsterIMapper _mapsterMapper;

    public MapsterMappingParityTests()
    {
        _fixture.Register<DateOnly>(() => DateOnly.FromDateTime(DateTime.UtcNow));
        _mockedUriService.Setup(s => s.GetUri(It.IsAny<IUrlEntity>())).Returns<IUrlEntity>(e => e.Url);

        // AutoMapper side: both profiles, with the DI-backed resolvers + NullableEnumMapper (request side).
        var amConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile(new DomainToResponseProfile());
            cfg.AddProfile(new RequestToDomainProfile());
            cfg.Internal().Mappers.Insert(0, new NullableEnumMapper());
            cfg.ShouldMapMethod = _ => false;
        });
        _autoMapper = amConfig.CreateMapper(t =>
        {
            if (t == typeof(UrlResolver))
                return new UrlResolver(_mockedUriService.Object);
            if (t == typeof(MemberUrlResolver))
                return new MemberUrlResolver(_mockedUriService.Object);
            throw new NotImplementedException($"Mapper is missing the service: {t}");
        });

        // Mapster side: mirror the AddZgwMapster global defaults this service depends on (see
        // MapsterServiceCollectionExtensions.AddZgwMapster): MaxDepth, EmptyCollectionIfNull,
        // IgnoreCase name matching, and the nullable-enum rule.
        var config = new TypeAdapterConfig();
        config.Default.MaxDepth(200);
        config.Default.AddDestinationTransform(DestinationTransform.EmptyCollectionIfNull);
        config.Default.NameMatchingStrategy(NameMatchingStrategy.IgnoreCase);
        config.RegisterNullableEnumRule();
        new DomainToResponseRegister().Register(config);
        new RequestToDomainRegister().Register(config);
        config.Compile();

        var services = new ServiceCollection();
        services.AddSingleton(_mockedUriService.Object);
        services.AddSingleton(config);
        services.AddScoped<MapsterIMapper, ServiceMapper>();
        _provider = services.BuildServiceProvider();
        _scope = _provider.CreateScope();
        _mapsterMapper = _scope.ServiceProvider.GetRequiredService<MapsterIMapper>();
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

    [Fact]
    public void Besluit_to_BesluitResponseDto_parity() => AssertParity<BesluitResponseDto>(_fixture.Create<Besluit>());

    [Fact]
    public void Besluit_to_BesluitRequestDto_parity() => AssertParity<BesluitRequestDto>(_fixture.Create<Besluit>());

    [Fact]
    public void Besluit_with_null_VervalReden_to_BesluitResponseDto_parity()
    {
        // Guards the .AfterMapping block on Besluit -> BesluitResponseDto, which only fires when
        // src.VervalReden is null. A plain _fixture.Create<Besluit>() essentially never produces a
        // null nullable-enum, so that branch would never run on either mapper side without this case.
        var value = _fixture.Build<Besluit>().Without(b => b.VervalReden).Create();
        AssertParity<BesluitResponseDto>(value);
    }

    [Fact]
    public void Besluit_with_null_VervalReden_to_BesluitRequestDto_parity()
    {
        // Same as above, but for the separate .AfterMapping block on the Besluit -> BesluitRequestDto
        // (PATCH-merge) config.
        var value = _fixture.Build<Besluit>().Without(b => b.VervalReden).Create();
        AssertParity<BesluitRequestDto>(value);
    }

    [Fact]
    public void BesluitInformatieObject_to_ResponseDto_parity() =>
        AssertParity<BesluitInformatieObjectResponseDto>(_fixture.Create<BesluitInformatieObject>());

    [Fact]
    public void BesluitInformatieObject_to_RequestDto_parity() =>
        AssertParity<BesluitInformatieObjectRequestDto>(_fixture.Create<BesluitInformatieObject>());

    [Fact]
    public void BesluitInformatieObjectRequestDto_to_BesluitInformatieObject_parity() =>
        AssertParity<BesluitInformatieObject>(_fixture.Create<BesluitInformatieObjectRequestDto>());

    [Fact]
    public void AuditTrailRegel_to_Dto_parity()
    {
        var value = _fixture.Build<AuditTrailRegel>().With(a => a.Oud, "{\"naam\":\"oud\"}").With(a => a.Nieuw, "{\"naam\":\"nieuw\"}").Create();
        AssertParity<AuditTrailRegelDto>(value);
    }

    [Fact]
    public void BesluitRequestDto_to_Besluit_parity()
    {
        _fixture.Customize<BesluitRequestDto>(c =>
            c.With(p => p.VervalReden, VervalReden.ingetrokken_overheid.ToString())
                .With(p => p.Datum, "2020-12-17")
                .With(p => p.IngangsDatum, "2020-12-18")
                .With(p => p.VervalDatum, "2020-12-19")
                .With(p => p.PublicatieDatum, "2020-12-20")
                .With(p => p.VerzendDatum, "2020-12-21")
                .With(p => p.UiterlijkeReactieDatum, "2020-12-22")
        );
        AssertParity<Besluit>(_fixture.Create<BesluitRequestDto>());
    }

    [Fact]
    public void BesluitRequestDto_with_empty_VervalReden_to_Besluit_parity()
    {
        // Guards against Mapster's known "silently substitutes the enum's zero-value member" gotcha
        // for this exact field (see RequestToDomainProfileTests.
        // BesluitRequestDto_Maps_EmptyVervalReden_To_Null_Not_ZeroValueEnumMember) -- but here cross-
        // checked byte-for-byte against what AutoMapper itself actually produces for the same input,
        // rather than against a hand-written expectation.
        _fixture.Customize<BesluitRequestDto>(c =>
            c.With(p => p.VervalReden, string.Empty)
                .With(p => p.Datum, "2020-12-17")
                .With(p => p.IngangsDatum, "2020-12-18")
                .With(p => p.VervalDatum, "2020-12-19")
                .With(p => p.PublicatieDatum, "2020-12-20")
                .With(p => p.VerzendDatum, "2020-12-21")
                .With(p => p.UiterlijkeReactieDatum, "2020-12-22")
        );
        AssertParity<Besluit>(_fixture.Create<BesluitRequestDto>());
    }
}
