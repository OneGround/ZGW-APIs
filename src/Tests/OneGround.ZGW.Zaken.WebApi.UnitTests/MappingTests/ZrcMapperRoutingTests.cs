using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Common.Web.Extensions.ServiceCollection.ZGWApiExtensions;
using OneGround.ZGW.Common.Web.Mapping;
using OneGround.ZGW.Zaken.Web;
using Xunit;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.MappingTests;

/// <summary>
/// ZRC has not adopted Mapster, so its audit trail and PATCH merge must keep resolving the
/// AutoMapper-backed adapter. Mapster does not throw on a missing map — it convention-maps — so enabling
/// it here without writing registers would silently persist wrong audit records instead of failing.
/// This test is that tripwire, and should be replaced by real per-map coverage when ZRC migrates.
///
/// Note: ZRC's expanders inject AutoMapper's IMapper directly (unconditionally registered by
/// AddAutoMapper) and never resolve IZgwMapper, so this test does not cover them — they would need
/// their own migration to Mapster, the same way BRC's expander was switched directly to
/// MapsterMapper.IMapper.
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
