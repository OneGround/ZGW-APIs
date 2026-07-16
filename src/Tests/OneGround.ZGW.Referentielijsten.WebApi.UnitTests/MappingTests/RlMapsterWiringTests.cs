using MapsterMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using OneGround.ZGW.Common.Web.Extensions.ServiceCollection.ZGWApiExtensions;
using OneGround.ZGW.Referentielijsten.Contracts.v1.Responses;
using OneGround.ZGW.Referentielijsten.Web.MappingProfiles.v1;
using OneGround.ZGW.Referentielijsten.Web.Models;
using Xunit;

namespace OneGround.ZGW.Referentielijsten.WebApi.UnitTests.MappingTests;

public class RlMapsterWiringTests
{
    [Fact]
    public void AddZgwMapster_discovers_RL_registers_and_runs_the_host_rewrite_through_DI()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "https";
        // Port is explicitly 443 (the https default) rather than left unspecified: AdjustUrlMapster
        // only overwrites the port when Host.Port.HasValue is true, and 443 is the one explicit
        // value that then gets stripped again by the IsDefaultPort normalization, producing a clean
        // "https://wired-host.example.test/..." URL. An unspecified port would instead leave the
        // *source* URL's http-default port (80) in place, which is a documented, separately-tested
        // quirk of AdjustUrlMapster (see AdjustUrlMapsterTests.Adjust_omits_the_port_when_it_is_the_scheme_default)
        // and not what this test is trying to pin down.
        httpContext.Request.Host = new HostString("wired-host.example.test", 443);
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(a => a.HttpContext).Returns(httpContext);

        var services = new ServiceCollection();
        services.AddSingleton(accessor.Object);
        services.AddZgwMapster(typeof(DomainToResponseRegister).Assembly, enable: true);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

        var result = mapper.Map<CommunicatieKanaalResponseDto>(
            new CommunicatieKanaal
            {
                Url = "http://original-upstream-host/api/v1/communicatiekanalen/x",
                Naam = "x",
                Omschrijving = "y",
            }
        );

        // The host is rewritten to the request host. This value cannot arise from a same-name
        // convention copy of the source Url (which still carries "original-upstream-host"), so the
        // assertion only passes if DomainToResponseRegister was discovered by config.Scan AND
        // AdjustUrlMapster resolved IHttpContextAccessor through DI.
        Assert.Equal("https://wired-host.example.test/api/v1/communicatiekanalen/x", result.Url);
    }
}
