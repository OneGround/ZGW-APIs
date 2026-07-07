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
}
