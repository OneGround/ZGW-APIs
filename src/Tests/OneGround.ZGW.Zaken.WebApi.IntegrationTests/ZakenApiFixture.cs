using System.Threading.Tasks;
using OneGround.ZGW.IntegrationTests.Common.Containers;
using Xunit;

namespace OneGround.ZGW.Zaken.WebApi.IntegrationTests;

/// <summary>
/// Starts the containers, then boots the Zaken API against them; shared by every test in <see cref="ZakenApiCollection"/>.
/// </summary>
public sealed class ZakenApiFixture : IAsyncLifetime
{
    private readonly IntegrationTestContainers _containers = new();

    internal ZakenWebApplicationFactory Factory { get; private set; }

    public async Task InitializeAsync()
    {
        await _containers.InitializeAsync();

        Factory = new ZakenWebApplicationFactory(_containers);

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
public sealed class ZakenApiCollection : ICollectionFixture<ZakenApiFixture>
{
    public const string Name = "Zaken API";
}
