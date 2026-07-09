using System;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using OneGround.ZGW.Common.Web.Extensions.ServiceCollection.ZGWApiExtensions;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.DataAccess;
using OneGround.ZGW.Notificaties.Contracts.v1.Responses;
using OneGround.ZGW.Notificaties.DataModel;
using OneGround.ZGW.Notificaties.Web.MappingProfiles.v1;
using Xunit;

namespace OneGround.ZGW.Notificaties.WebApi.UnitTests.MappingTests;

public class NrcMapsterWiringTests
{
    [Fact]
    public void AddZgwMapster_discovers_NRC_registers_from_the_web_assembly()
    {
        var mockedUriService = new Mock<IEntityUriService>();
        mockedUriService.Setup(s => s.GetUri(It.IsAny<IUrlEntity>())).Returns("https://example.test/resolved-via-di");

        var services = new ServiceCollection();
        services.AddSingleton(mockedUriService.Object);
        services.AddZgwMapster(typeof(DomainToResponseRegister).Assembly);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

        var result = mapper.Map<KanaalResponseDto>(new Kanaal { Id = Guid.NewGuid(), Naam = "zaken" });

        // Distinct literal (not derivable from the source) so this only passes if MapsterUrlResolver
        // actually ran through DI via config.Scan discovery — not if a same-name convention copy
        // satisfied it. (Kanaal has no `Url` source member anyway; the URL comes only from the resolver.
        // Kanaal.Url is a computed, read-only property, so it's never assigned directly — the assertion
        // below is only satisfiable via the DI-backed resolver, matching a bug the AC migration's own
        // wiring test found and fixed: a mock that echoes the source's own value is a false positive.)
        Assert.Equal("https://example.test/resolved-via-di", result.Url);
        mockedUriService.Verify(s => s.GetUri(It.IsAny<IUrlEntity>()), Times.AtLeastOnce());
    }
}
