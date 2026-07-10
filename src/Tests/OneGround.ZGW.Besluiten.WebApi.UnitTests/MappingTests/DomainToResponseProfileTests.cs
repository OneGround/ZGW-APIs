using System;
using AutoFixture;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using OneGround.ZGW.Besluiten.Contracts.v1.Responses;
using OneGround.ZGW.Besluiten.DataModel;
using OneGround.ZGW.Besluiten.Web.MappingProfiles.v1;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.DataAccess;
using Xunit;

namespace OneGround.ZGW.Besluiten.WebApi.UnitTests.MappingTests;

public class DomainToResponseProfileTests : IDisposable
{
    private readonly AutoMapperFixture _fixture = new AutoMapperFixture();
    private readonly Mock<IEntityUriService> _mockedUriService = new Mock<IEntityUriService>();
    private readonly ServiceProvider _provider;
    private readonly IServiceScope _scope;
    private readonly IMapper _mapper;

    public DomainToResponseProfileTests()
    {
        _fixture.Register<DateOnly>(() => DateOnly.FromDateTime(DateTime.UtcNow));
        _mockedUriService.Setup(s => s.GetUri(It.IsAny<IUrlEntity>())).Returns<IUrlEntity>(e => e.Url);

        var config = new TypeAdapterConfig();
        new DomainToResponseRegister().Register(config);
        config.Compile();

        var services = new ServiceCollection();
        services.AddSingleton(_mockedUriService.Object);
        services.AddSingleton(config);
        services.AddScoped<IMapper, ServiceMapper>();
        _provider = services.BuildServiceProvider();
        _scope = _provider.CreateScope();
        _mapper = _scope.ServiceProvider.GetRequiredService<IMapper>();
    }

    public void Dispose()
    {
        _scope.Dispose();
        _provider.Dispose();
    }

    [Fact]
    public void Besluit_Maps_To_BesluitResponseDto()
    {
        var value = _fixture.Create<Besluit>();
        var result = _mapper.Map<BesluitResponseDto>(value);

        Assert.Equal(value.Identificatie, result.Identificatie);
        Assert.Equal(value.VerantwoordelijkeOrganisatie, result.VerantwoordelijkeOrganisatie);
        Assert.Equal(value.BesluitType, result.BesluitType);
        Assert.Equal(value.Zaak, result.Zaak);
        Assert.Equal(value.Datum.ToString("yyyy-MM-dd"), result.Datum);
        Assert.Equal(value.Toelichting, result.Toelichting);
        Assert.Equal(value.BestuursOrgaan, result.BestuursOrgaan);
        Assert.Equal(value.IngangsDatum.ToString("yyyy-MM-dd"), result.IngangsDatum);
        Assert.Equal(value.VervalDatum.Value.ToString("yyyy-MM-dd"), result.VervalDatum);
        Assert.Equal(value.VervalReden.ToString(), result.VervalReden);
        Assert.Equal(value.PublicatieDatum.Value.ToString("yyyy-MM-dd"), result.PublicatieDatum);
        Assert.Equal(value.VerzendDatum.Value.ToString("yyyy-MM-dd"), result.VerzendDatum);
        Assert.Equal(value.UiterlijkeReactieDatum.Value.ToString("yyyy-MM-dd"), result.UiterlijkeReactieDatum);
        Assert.Equal(value.Url, result.Url);
    }

    [Fact]
    public void BesluitInformatieObject_Maps_To_BesluitInformatieResponseDto()
    {
        var value = _fixture.Create<BesluitInformatieObject>();
        var result = _mapper.Map<BesluitInformatieObjectResponseDto>(value);

        Assert.Equal(value.InformatieObject, result.InformatieObject);
        Assert.Equal(value.Besluit.Url, result.Besluit);
    }

    [Fact]
    public void Besluit_with_null_VervalReden_Maps_VervalReden_To_Empty_String_via_AfterMapping()
    {
        // AutoMapper original: .AfterMap sets dest.VervalReden = "" when src.VervalReden is null,
        // because Mapster's default enum->string conversion on a null Nullable<enum> produces a
        // null string, not "". Pin this explicitly with a source value distinguishable from Mapster's
        // own possible default handling (null is the input we care about; asserting the OUTPUT is ""
        // rather than null proves the .AfterMapping override actually ran and wasn't skipped/no-op'd).
        var value = _fixture.Build<Besluit>().Without(b => b.VervalReden).Create();

        var result = _mapper.Map<BesluitResponseDto>(value);

        Assert.Equal("", result.VervalReden);
    }
}
