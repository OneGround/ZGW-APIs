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
        services.AddZgwMapster(typeof(AddZgwMapsterTests).Assembly);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

        var result = mapper.Map<TargetPoco>(new SourcePoco { Name = "x", Count = 3 });

        Assert.Equal("x", result.Name);
        Assert.Equal(3, result.Count);
    }

    /// <summary>
    /// <c>ServiceMapper</c>, never the plain <c>Mapper</c>. Only <c>ServiceMapper</c> publishes the
    /// request's <see cref="System.IServiceProvider"/> on <c>MapContext</c>, which every DI-resolved
    /// resolver in the registers depends on -- the url resolvers and the host rewriter among them.
    /// Registering the plain <c>Mapper</c> compiles, and fails only when such a resolver runs.
    /// </summary>
    [Fact]
    public void AddZgwMapster_registers_ServiceMapper_so_resolvers_can_reach_DI()
    {
        var services = new ServiceCollection();
        services.AddZgwMapster(typeof(AddZgwMapsterTests).Assembly);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        Assert.IsType<ServiceMapper>(scope.ServiceProvider.GetRequiredService<IMapper>());
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
