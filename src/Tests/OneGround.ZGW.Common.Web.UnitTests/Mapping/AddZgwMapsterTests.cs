using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Common.Web.Extensions.ServiceCollection.ZGWApiExtensions;
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
        services.AddZgwMapster(typeof(AddZgwMapsterTests).Assembly, enable: true);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

        var result = mapper.Map<TargetPoco>(new SourcePoco { Name = "x", Count = 3 });

        Assert.Equal("x", result.Name);
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void AddZgwMapster_disabled_by_default_registers_nothing()
    {
        var services = new ServiceCollection();
        services.AddZgwMapster(typeof(AddZgwMapsterTests).Assembly);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        Assert.Null(scope.ServiceProvider.GetService<IMapper>());
    }
}
