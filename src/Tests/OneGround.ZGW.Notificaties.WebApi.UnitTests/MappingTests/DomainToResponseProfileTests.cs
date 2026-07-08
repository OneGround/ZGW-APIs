using System;
using System.Linq;
using AutoFixture;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.DataAccess;
using OneGround.ZGW.Notificaties.Contracts.v1;
using OneGround.ZGW.Notificaties.Contracts.v1.Responses;
using OneGround.ZGW.Notificaties.DataModel;
using OneGround.ZGW.Notificaties.Web.MappingProfiles.v1;
using Xunit;

namespace OneGround.ZGW.Notificaties.WebApi.UnitTests.MappingTests;

public class DomainToResponseProfileTests : IDisposable
{
    private readonly OmitOnRecursionFixture _fixture = new OmitOnRecursionFixture();
    private readonly Mock<IEntityUriService> _mockedUriService = new Mock<IEntityUriService>();
    private readonly ServiceProvider _provider;
    private readonly IServiceScope _scope;
    private readonly IMapper _mapper;

    public DomainToResponseProfileTests()
    {
        _mockedUriService.Setup(s => s.GetUri(It.IsAny<IUrlEntity>())).Returns<IUrlEntity>(e => e.Url);

        var config = new TypeAdapterConfig();
        new DomainToResponseRegister().Register(config);
        config.Compile();

        // MapsterUrlResolver resolves IEntityUriService lazily via MapContext at Map()-call time, so
        // provider/scope must live for the class's lifetime (disposed in Dispose(), not constructor-scoped).
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
    public void Kanaal_Maps_To_KanaalResponseDto()
    {
        var value = _fixture.Create<Kanaal>();
        var result = _mapper.Map<KanaalResponseDto>(value);

        Assert.Equal(value.DocumentatieLink, result.DocumentatieLink);
        Assert.Equal(value.Naam, result.Naam);
        Assert.Equal(value.Filters, result.Filters);
        Assert.Equal(value.Url, result.Url);
    }

    [Fact]
    public void Abonnement_Maps_To_AbonnementResponseDto()
    {
        var value = _fixture.Create<Abonnement>();
        var result = _mapper.Map<AbonnementResponseDto>(value);

        Assert.Equal("<hidden>", result.Auth);
        Assert.Equal(value.CallbackUrl, result.CallbackUrl);
        Assert.Equal(value.AbonnementKanalen.Count, result.Kanalen.Count);
        Assert.Equal(value.Url, result.Url);
    }

    [Fact]
    public void AbonnementKanalen_Maps_To_AbonnementKanaalResponseDto()
    {
        var value = _fixture.Create<AbonnementKanaal>();
        var result = _mapper.Map<AbonnementKanaalDto>(value);

        Assert.Equal(value.Kanaal.Naam, result.Naam);
        Assert.Equal(value.Filters.Count, result.Filters.Count);
        Assert.Equal(value.Filters.Select(f => f.Value), result.Filters.Values);
        Assert.Equal(value.Filters.Select(f => f.Key), result.Filters.Keys);
    }
}
