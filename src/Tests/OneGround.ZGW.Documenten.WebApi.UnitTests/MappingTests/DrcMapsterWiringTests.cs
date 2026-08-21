using System;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using OneGround.ZGW.Common.Web.Extensions.ServiceCollection.ZGWApiExtensions;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.DataAccess;
using OneGround.ZGW.Documenten.Contracts.v1.Responses;
using OneGround.ZGW.Documenten.DataModel;
using OneGround.ZGW.Documenten.Web.MappingProfiles.v1;
using Xunit;

namespace OneGround.ZGW.Documenten.WebApi.UnitTests.MappingTests;

public class DrcMapsterWiringTests
{
    [Fact]
    public void AddZgwMapster_discovers_DRC_registers_and_runs_the_url_resolvers_through_DI()
    {
        var mockedUriService = new Mock<IEntityUriService>();
        mockedUriService.Setup(s => s.GetUri(It.IsAny<IUrlEntity>())).Returns("https://example.test/resolved-via-di");

        var services = new ServiceCollection();
        services.AddSingleton(mockedUriService.Object);
        services.AddZgwMapster(typeof(DomainToResponseRegister).Assembly, enable: true);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

        var latestInformatieObject = new EnkelvoudigInformatieObject { Id = Guid.NewGuid(), InformatieObjectType = "https://example.test/iot" };
        var latestVersion = new EnkelvoudigInformatieObjectVersie { Id = Guid.NewGuid(), LatestInformatieObject = latestInformatieObject };
        var source = new EnkelvoudigInformatieObject
        {
            Id = Guid.NewGuid(),
            InformatieObjectType = "https://example.test/iot",
            LatestEnkelvoudigInformatieObjectVersie = latestVersion,
        };

        var result = mapper.Map<EnkelvoudigInformatieObjectGetResponseDto>(source);

        // The mocked literal is distinguishable from any same-name convention copy of the source's own
        // Url, so this only passes if DomainToResponseRegister was discovered by config.Scan AND both
        // MapsterUrlResolver.ResolveUrl (for Url) and the .AfterMapping port's
        // MapContext.Current.GetService<IEntityUriService>() (for Inhoud) resolved through DI.
        Assert.Equal("https://example.test/resolved-via-di", result.Url);
        Assert.Equal("https://example.test/resolved-via-di", result.Inhoud);
        mockedUriService.Verify(s => s.GetUri(It.IsAny<IUrlEntity>()), Times.AtLeastOnce());
    }
}
