using System;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using OneGround.ZGW.Besluiten.Contracts.v1.Responses;
using OneGround.ZGW.Besluiten.DataModel;
using OneGround.ZGW.Besluiten.Web.MappingProfiles.v1;
using OneGround.ZGW.Common.Web.Extensions.ServiceCollection.ZGWApiExtensions;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.DataAccess;
using Xunit;

namespace OneGround.ZGW.Besluiten.WebApi.UnitTests.MappingTests;

public class BrcMapsterWiringTests
{
    [Fact]
    public void AddZgwMapster_discovers_BRC_registers_and_runs_the_url_resolver_through_DI()
    {
        var mockedUriService = new Mock<IEntityUriService>();
        mockedUriService.Setup(s => s.GetUri(It.IsAny<IUrlEntity>())).Returns("https://example.test/resolved-via-di");

        var services = new ServiceCollection();
        services.AddSingleton(mockedUriService.Object);
        services.AddZgwMapster(typeof(DomainToResponseRegister).Assembly, enable: true);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

        var result = mapper.Map<BesluitResponseDto>(new Besluit { Id = Guid.NewGuid() });

        // The mocked literal is distinguishable from any same-name convention copy of the source's
        // own Url, so this only passes if DomainToResponseRegister was discovered by config.Scan AND
        // MapsterUrlResolver resolved IEntityUriService through DI.
        Assert.Equal("https://example.test/resolved-via-di", result.Url);
        mockedUriService.Verify(s => s.GetUri(It.IsAny<IUrlEntity>()), Times.AtLeastOnce());
    }
}
