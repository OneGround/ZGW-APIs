using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Catalogi.Web;
using OneGround.ZGW.Common.Web.Extensions.ServiceCollection.ZGWApiExtensions;
using OneGround.ZGW.Common.Web.Mapping;
using Xunit;

namespace OneGround.ZGW.Catalogi.WebApi.UnitTests.MappingTests;

/// <summary>
/// ZTC has not adopted Mapster, so its audit trail and PATCH merge must keep resolving the
/// AutoMapper-backed adapter. Mapster does not throw on a missing map — it convention-maps — so enabling
/// it here without writing registers would silently persist wrong audit records instead of failing.
/// This test is that tripwire, and should be replaced by real per-map coverage when ZTC migrates.
/// </summary>
public class ZtcMapperRoutingTests
{
    [Fact]
    public void ZTC_resolves_the_AutoMapper_backed_mapper()
    {
        var services = new ServiceCollection();
        services.AddAutoMapper(typeof(Startup).Assembly);
        services.AddZgwMapster(typeof(Startup).Assembly, enable: false); // mirrors ZTC's Startup

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        Assert.IsType<AutoMapperZgwMapper>(scope.ServiceProvider.GetRequiredService<IZgwMapper>());
    }
}
