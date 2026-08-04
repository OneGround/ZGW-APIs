using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Common.Web.Extensions.ServiceCollection.ZGWApiExtensions;
using OneGround.ZGW.Common.Web.Mapping;
using OneGround.ZGW.Zaken.Web;
using Xunit;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.MappingTests;

/// <summary>
/// ZRC has not adopted Mapster, so its audit trail, PATCH merge and 9 AutoMapper-backed expanders must
/// keep resolving the AutoMapper-backed adapter. Mapster does not throw on a missing map — it
/// convention-maps — so enabling it here without writing registers would silently persist wrong audit
/// records instead of failing. This test is that tripwire, and should be replaced by real per-map
/// coverage when ZRC migrates.
/// </summary>
public class ZrcMapperRoutingTests
{
    [Fact]
    public void ZRC_resolves_the_AutoMapper_backed_mapper()
    {
        var services = new ServiceCollection();
        services.AddAutoMapper(typeof(Startup).Assembly);
        services.AddZgwMapster(typeof(Startup).Assembly, enable: false); // mirrors ZRC's Startup

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        Assert.IsType<AutoMapperZgwMapper>(scope.ServiceProvider.GetRequiredService<IZgwMapper>());
    }
}
