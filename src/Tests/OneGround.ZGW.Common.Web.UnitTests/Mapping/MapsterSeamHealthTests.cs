using System.Linq;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Common.Web.Extensions.ServiceCollection.ZGWApiExtensions;
using Xunit;

namespace OneGround.ZGW.Common.Web.UnitTests.Mapping;

public class MapsterSeamHealthTests
{
    private sealed class NodeSource
    {
        public int Value { get; set; }
        public NodeSource Child { get; set; }
    }

    private sealed class NodeDto
    {
        public int Value { get; set; }
        public NodeDto Child { get; set; }
    }

    [Fact]
    public void AddZgwMapster_configuration_compiles_without_error()
    {
        var services = new ServiceCollection();
        services.AddZgwMapster(typeof(MapsterSeamHealthTests).Assembly);
        using var provider = services.BuildServiceProvider();

        var config = provider.GetRequiredService<TypeAdapterConfig>();

        var exception = Record.Exception(() => config.Compile());

        Assert.Null(exception);
    }

    [Fact]
    public void Deeply_nested_acyclic_graph_maps_without_stack_overflow()
    {
        var services = new ServiceCollection();
        services.AddZgwMapster(typeof(MapsterSeamHealthTests).Assembly);
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

        NodeSource head = null;
        for (var i = 0; i < 100; i++)
        {
            head = new NodeSource { Value = i, Child = head };
        }

        var result = mapper.Map<NodeDto>(head);

        var depth = 0;
        for (var node = result; node != null; node = node.Child)
        {
            depth++;
        }

        Assert.Equal(100, depth);
    }

    private sealed class CyclicNode
    {
        public int Value { get; set; }
        public CyclicNode Self { get; set; }
    }

    [Fact]
    public void Cyclic_self_referencing_graph_terminates_without_crashing()
    {
        var services = new ServiceCollection();
        services.AddZgwMapster(typeof(MapsterSeamHealthTests).Assembly);
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

        var cyclic = new CyclicNode { Value = 1 };
        cyclic.Self = cyclic; // genuine cycle: object references itself

        var result = mapper.Map<CyclicNode>(cyclic);

        Assert.NotNull(result);
        Assert.Equal(1, result.Value);

        // Walk Self until it terminates (null) or we exceed a generous bound — must NOT infinitely recurse/hang.
        var depth = 0;
        var node = result;
        while (node?.Self != null && depth < 1000)
        {
            node = node.Self;
            depth++;
        }

        // Mapster's MaxDepth(200) substitutes a default (null) value once the recursion counter
        // reaches the cap, so a self-referencing chain terminates at exactly MaxDepth - 1 levels.
        // Asserting a tight range (not just "< 1000") pins the cap as the actual termination
        // cause, not merely "it eventually stopped for some reason."
        Assert.InRange(depth, 190, 199);
    }
}
