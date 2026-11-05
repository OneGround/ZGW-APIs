using System.Threading.Tasks;
using OneGround.ZGW.IntegrationTests.Common.Containers;
using Xunit;

namespace OneGround.ZGW.Gesprekken.WebApi.IntegrationTests;

/// <summary>
/// Starts the containers, then boots the Gesprekken API against them; shared by every test in <see cref="GesprekkenApiCollection"/>.
/// </summary>
public sealed class GesprekkenApiFixture : IAsyncLifetime
{
    private readonly IntegrationTestContainers _containers = new();

    internal GesprekkenWebApplicationFactory Factory { get; private set; }

    public async Task InitializeAsync()
    {
        await _containers.InitializeAsync();

        Factory = new GesprekkenWebApplicationFactory(_containers);

        // Boot the host once here, so a startup failure shows as a fixture failure instead of in the first test
        _ = Factory.Server;
    }

    public async Task DisposeAsync()
    {
        if (Factory != null)
        {
            await Factory.DisposeAsync();
        }

        await _containers.DisposeAsync();
    }
}

[CollectionDefinition(Name)]
public sealed class GesprekkenApiCollection : ICollectionFixture<GesprekkenApiFixture>
{
    public const string Name = "Gesprekken API";
}
