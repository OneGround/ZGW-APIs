using System;
using AutoFixture;
using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using OneGround.ZGW.Referentielijsten.Contracts.v1.Responses;
using OneGround.ZGW.Referentielijsten.Web.MappingProfiles.v1;
using OneGround.ZGW.Referentielijsten.Web.Models;
using Xunit;

namespace OneGround.ZGW.Referentielijsten.WebApi.UnitTests.MappingTests;

public class DomainToResponseProfileTests : IDisposable
{
    private readonly Fixture _fixture = new Fixture();
    private readonly ServiceProvider _provider;
    private readonly IServiceScope _scope;
    private readonly IMapper _mapper;

    public DomainToResponseProfileTests()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("api.example.test", 8443);
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(a => a.HttpContext).Returns(httpContext);

        var config = new TypeAdapterConfig();
        // Reproduce the seam's global case-insensitive matching (set in AddZgwMapster) so this
        // config-only test exercises the same behavior production uses for Resultaat's casing.
        config.Default.NameMatchingStrategy(NameMatchingStrategy.IgnoreCase);
        new DomainToResponseRegister().Register(config);
        config.Compile();

        var services = new ServiceCollection();
        services.AddSingleton(accessor.Object);
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
    public void CommunicatieKanaal_Maps_To_ResponseDto_with_host_rewritten()
    {
        var value = _fixture.Build<CommunicatieKanaal>().With(x => x.Url, "http://upstream/api/v1/communicatiekanalen/x").Create();

        var result = _mapper.Map<CommunicatieKanaalResponseDto>(value);

        Assert.Equal("https://api.example.test:8443/api/v1/communicatiekanalen/x", result.Url);
        Assert.Equal(value.Naam, result.Naam);
        Assert.Equal(value.Omschrijving, result.Omschrijving);
    }

    [Fact]
    public void ProcesType_Maps_To_ResponseDto_with_host_rewritten()
    {
        var value = _fixture.Build<ProcesType>().With(x => x.Url, "http://upstream/api/v1/procestypen/x").Create();

        var result = _mapper.Map<ProcesTypeResponseDto>(value);

        Assert.Equal("https://api.example.test:8443/api/v1/procestypen/x", result.Url);
        Assert.Equal(value.Naam, result.Naam);
        Assert.Equal(value.Jaar, result.Jaar);
        Assert.Equal(value.ProcesObject, result.ProcesObject);
    }

    [Fact]
    public void ResultaatTypeOmschrijving_Maps_To_ResponseDto_with_host_rewritten()
    {
        // Url is computed: https://dummy/api/v1/resultaattypeomschrijvingen/{Id}
        var value = _fixture.Create<ResultaatTypeOmschrijving>();

        var result = _mapper.Map<ResultaatTypeOmschrijvingResponseDto>(value);

        Assert.StartsWith("https://api.example.test:8443/api/v1/resultaattypeomschrijvingen/", result.Url);
        Assert.Equal(value.Omschrijving, result.Omschrijving);
        Assert.Equal(value.Definitie, result.Definitie);
        Assert.Equal(value.Opmerking, result.Opmerking);
    }

    [Fact]
    public void Resultaat_Maps_To_ResponseDto_with_urls_rewritten_and_casing_mismatched_members_mapped()
    {
        var value = _fixture
            .Build<Resultaat>()
            .With(x => x.Url, "http://upstream/api/v1/resultaten/x")
            .With(x => x.ProcesType, "http://upstream/api/v1/procestypen/y")
            .Create();

        var result = _mapper.Map<ResultaatResponseDto>(value);

        // Both URL string members are host-rewritten.
        Assert.Equal("https://api.example.test:8443/api/v1/resultaten/x", result.Url);
        Assert.Equal("https://api.example.test:8443/api/v1/procestypen/y", result.ProcesType);

        // The 5 casing-mismatched members map only because of the seam's global IgnoreCase.
        Assert.Equal(value.Procestermijn, result.ProcesTermijn);
        Assert.Equal(value.ProcestermijnWeergave, result.ProcesTermijnWeergave);
        Assert.Equal(value.Bewaartermijn, result.BewaarTermijn);
        Assert.Equal(value.Burgerzaken, result.BurgerZaken);
        Assert.Equal(value.ProcestermijnOpmerking, result.ProcesTermijnOpmerking);

        // A representative exactly-named member still maps.
        Assert.Equal(value.VolledigNummer, result.VolledigNummer);
    }
}
