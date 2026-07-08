using System.Linq;
using AutoFixture;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using OneGround.ZGW.Autorisaties.Contracts.v1.Requests;
using OneGround.ZGW.Autorisaties.Contracts.v1.Responses;
using OneGround.ZGW.Autorisaties.DataModel;
using OneGround.ZGW.Autorisaties.Web.MappingProfiles.v1;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.DataAccess;
using Xunit;

namespace OneGround.ZGW.Autorisaties.WebApi.UnitTests.MappingTests;

public class DomainToResponseProfileTests
{
    private readonly OmitOnRecursionFixture _fixture = new OmitOnRecursionFixture();
    private readonly Mock<IEntityUriService> _mockedUriService = new Mock<IEntityUriService>();
    private readonly IMapper _mapper;

    public DomainToResponseProfileTests()
    {
        _mockedUriService.Setup(s => s.GetUri(It.IsAny<IUrlEntity>())).Returns<IUrlEntity>(e => e.Url);

        var config = new TypeAdapterConfig();
        new DomainToResponseRegister().Register(config);
        config.Compile();

        // MapsterUrlResolver reads IEntityUriService from MapContext, which ServiceMapper populates
        // from the DI container — so the mapper must be a ServiceMapper with the mock registered.
        var services = new ServiceCollection();
        services.AddSingleton(_mockedUriService.Object);
        services.AddSingleton(config);
        services.AddScoped<IMapper, ServiceMapper>();
        var provider = services.BuildServiceProvider();
        _mapper = provider.GetRequiredService<IMapper>();
    }

    [Fact]
    public void Applicatie_Maps_To_ApplicatieResponseDto()
    {
        var value = _fixture.Create<Applicatie>();
        var result = _mapper.Map<ApplicatieResponseDto>(value);

        Assert.True(value.ClientIds.All(c => result.ClientIds.Contains(c.ClientId)));
        Assert.Equal(value.HeeftAlleAutorisaties, result.HeeftAlleAutorisaties);
        Assert.Equal(value.Label, result.Label);
        Assert.Equal(value.Url, result.Url);
    }

    [Fact]
    public void Autorisatie_Maps_to_AutorisatieDto()
    {
        var value = _fixture.Create<Autorisatie>();
        var result = _mapper.Map<AutorisatieResponseDto>(value);

        Assert.Equal(value.BesluitType, result.BesluitType);
        Assert.Equal(value.ZaakType, result.ZaakType);
        Assert.Equal(value.InformatieObjectType, result.InformatieObjectType);
        Assert.Equal(value.Component.ToString(), result.Component);
        Assert.Equal(value.MaxVertrouwelijkheidaanduiding.ToString(), result.MaxVertrouwelijkheidaanduiding);
    }

    [Fact]
    public void Applicatie_Maps_to_ApplicatieRequestDto()
    {
        var value = _fixture.Create<Applicatie>();
        var result = _mapper.Map<ApplicatieRequestDto>(value);

        Assert.True(value.ClientIds.All(c => result.ClientIds.Contains(c.ClientId)));
        Assert.Equal(value.HeeftAlleAutorisaties, result.HeeftAlleAutorisaties);
        Assert.Equal(value.Label, result.Label);
    }

    [Fact]
    public void Autorisatie_Maps_to_AutorisatieRequestDto()
    {
        var value = _fixture.Create<Autorisatie>();
        var result = _mapper.Map<AutorisatieRequestDto>(value);

        Assert.Equal(value.BesluitType, result.BesluitType);
        Assert.Equal(value.ZaakType, result.ZaakType);
        Assert.Equal(value.InformatieObjectType, result.InformatieObjectType);
        Assert.Equal(value.Component.ToString(), result.Component);
        Assert.Equal(value.MaxVertrouwelijkheidaanduiding.ToString(), result.MaxVertrouwelijkheidaanduiding);
    }
}
