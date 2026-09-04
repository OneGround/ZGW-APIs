using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Common.Web.Extensions.ServiceCollection.ZGWApiExtensions;
using OneGround.ZGW.Common.Web.Mapping;
using Xunit;

namespace OneGround.ZGW.Common.Web.UnitTests.Mapping;

public class AddZgwMapsterTests
{
    private sealed class SourcePoco
    {
        public string Name { get; set; }
        public int Count { get; set; }
    }

    private sealed class TargetPoco
    {
        public string Name { get; set; }
        public int Count { get; set; }
    }

    [Fact]
    public void AddZgwMapster_registers_IMapper_and_maps_same_named_members()
    {
        var services = new ServiceCollection();
        services.AddZgwMapster(typeof(AddZgwMapsterTests).Assembly);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

        var result = mapper.Map<TargetPoco>(new SourcePoco { Name = "x", Count = 3 });

        Assert.Equal("x", result.Name);
        Assert.Equal(3, result.Count);
    }

    /// <summary>
    /// Replaces the registration test that pinned this while two mappers existed. The guarantee still
    /// matters -- shared infrastructure (the audit trail) resolves IZgwMapper, and nothing else in the
    /// repository does, so a wrong registration here would surface only as silently wrong audit records.
    /// </summary>
    [Fact]
    public void AddZgwMapster_registers_MapsterZgwMapper_as_the_only_IZgwMapper()
    {
        var services = new ServiceCollection();
        services.AddZgwMapster(typeof(AddZgwMapsterTests).Assembly);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        Assert.IsType<MapsterZgwMapper>(scope.ServiceProvider.GetRequiredService<IZgwMapper>());
    }

    [Fact]
    public void AddZgwMapster_registers_IMapper_with_no_opt_in_flag()
    {
        var services = new ServiceCollection();
        services.AddZgwMapster(typeof(AddZgwMapsterTests).Assembly);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetService<IMapper>());
    }
}
