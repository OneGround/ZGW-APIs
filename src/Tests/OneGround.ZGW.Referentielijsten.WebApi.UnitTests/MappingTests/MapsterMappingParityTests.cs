using System;
using AutoFixture;
using AutoMapper;
using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Newtonsoft.Json;
using OneGround.ZGW.Referentielijsten.Contracts.v1.Responses;
using OneGround.ZGW.Referentielijsten.Web.MappingProfiles;
using OneGround.ZGW.Referentielijsten.Web.MappingProfiles.v1;
using OneGround.ZGW.Referentielijsten.Web.Models;
using Xunit;
using AutoMapperIMapper = AutoMapper.IMapper;
using MapsterIMapper = MapsterMapper.IMapper;

namespace OneGround.ZGW.Referentielijsten.WebApi.UnitTests.MappingTests;

public class MapsterMappingParityTests : IDisposable
{
    private readonly Fixture _fixture = new Fixture();
    private readonly AutoMapperIMapper _autoMapper;
    private readonly ServiceProvider _provider;
    private readonly IServiceScope _scope;
    private readonly MapsterIMapper _mapsterMapper;
    private readonly Mock<IHttpContextAccessor> _accessor = new Mock<IHttpContextAccessor>();

    public MapsterMappingParityTests()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("api.example.test", 8443);
        _accessor.Setup(a => a.HttpContext).Returns(httpContext);

        // AutoMapper side: build the real profile, construct AdjustUrl with the mock accessor.
        var amConfig = new MapperConfiguration(cfg => cfg.AddProfile(new DomainToResponseProfile()));
        amConfig.AssertConfigurationIsValid();
        _autoMapper = amConfig.CreateMapper(t =>
        {
            if (t == typeof(AdjustUrl))
                return new AdjustUrl(_accessor.Object);
            throw new NotImplementedException($"Mapper is missing the service: {t}");
        });

        // Mapster side: mirror ALL AddZgwMapster global defaults so the comparison is faithful.
        var config = new TypeAdapterConfig();
        config.Default.NameMatchingStrategy(NameMatchingStrategy.IgnoreCase);
        config.Default.AddDestinationTransform(DestinationTransform.EmptyCollectionIfNull);
        new DomainToResponseRegister().Register(config);
        config.Compile();

        var services = new ServiceCollection();
        services.AddSingleton(_accessor.Object);
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

    private void AssertParity<TSource, TDest>(TSource source)
    {
        var am = JsonConvert.SerializeObject(_autoMapper.Map<TDest>(source));
        var ms = JsonConvert.SerializeObject(_mapsterMapper.Map<TDest>(source));
        Assert.Equal(am, ms);
    }

    [Fact]
    public void CommunicatieKanaal_parity()
    {
        var value = _fixture.Build<CommunicatieKanaal>().With(x => x.Url, "http://upstream/api/v1/communicatiekanalen/x").Create();
        AssertParity<CommunicatieKanaal, CommunicatieKanaalResponseDto>(value);
    }

    [Fact]
    public void ProcesType_parity()
    {
        var value = _fixture.Build<ProcesType>().With(x => x.Url, "http://upstream/api/v1/procestypen/x").Create();
        AssertParity<ProcesType, ProcesTypeResponseDto>(value);
    }

    [Fact]
    public void ResultaatTypeOmschrijving_parity()
    {
        AssertParity<ResultaatTypeOmschrijving, ResultaatTypeOmschrijvingResponseDto>(_fixture.Create<ResultaatTypeOmschrijving>());
    }

    [Fact]
    public void Resultaat_parity()
    {
        var value = _fixture
            .Build<Resultaat>()
            .With(x => x.Url, "http://upstream/api/v1/resultaten/x")
            .With(x => x.ProcesType, "http://upstream/api/v1/procestypen/y")
            .Create();
        AssertParity<Resultaat, ResultaatResponseDto>(value);
    }
}
