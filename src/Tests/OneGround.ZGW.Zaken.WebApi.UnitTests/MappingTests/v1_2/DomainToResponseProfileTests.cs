using System;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.DataAccess;
using OneGround.ZGW.Zaken.Contracts.v1.Requests;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.Web.MappingProfiles.v1._2;
using Xunit;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.MappingTests.v1_2;

public class DomainToResponseProfileTests : IDisposable
{
    private readonly Mock<IEntityUriService> _mockedUriService = new Mock<IEntityUriService>();
    private readonly ServiceProvider _provider;
    private readonly IServiceScope _scope;
    private readonly IMapper _mapper;

    public DomainToResponseProfileTests()
    {
        _mockedUriService.Setup(s => s.GetUri(It.IsAny<IUrlEntity>())).Returns("https://example.test/resolved-via-di");

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
    public void ZaakEigenschap_Maps_To_ZaakEigenschapRequestDto()
    {
        var zaak = new Zaak { Id = Guid.NewGuid() };
        var source = new ZaakEigenschap
        {
            Id = Guid.NewGuid(),
            Zaak = zaak,
            Naam = "eigenschap-naam",
            Waarde = "eigenschap-waarde",
        };

        var result = _mapper.Map<ZaakEigenschapRequestDto>(source);

        // The mocked literal is distinguishable from any same-name convention copy, so this only
        // passes if MemberUrlResolver was correctly ported to MapsterUrlResolver.ResolveUrl(src.Zaak)
        // resolving IEntityUriService through DI.
        Assert.Equal("https://example.test/resolved-via-di", result.Zaak);
        _mockedUriService.Verify(s => s.GetUri(It.IsAny<IUrlEntity>()), Times.AtLeastOnce());
    }

    [Fact]
    public void ZaakEigenschap_with_null_Zaak_maps_to_null()
    {
        var source = new ZaakEigenschap
        {
            Id = Guid.NewGuid(),
            Zaak = null,
            Naam = "eigenschap-naam",
            Waarde = "eigenschap-waarde",
        };

        var result = _mapper.Map<ZaakEigenschapRequestDto>(source);

        Assert.Null(result.Zaak);
    }
}
