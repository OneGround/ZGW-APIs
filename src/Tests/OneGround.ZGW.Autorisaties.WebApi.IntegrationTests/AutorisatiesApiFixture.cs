using System.Threading.Tasks;
using OneGround.ZGW.IntegrationTests.Common.Containers;
using Xunit;

namespace OneGround.ZGW.Autorisaties.WebApi.IntegrationTests;

/// <summary>
/// Starts the containers, then boots the Autorisaties API against them; shared by every test in <see cref="AutorisatiesApiCollection"/>.
/// </summary>
public sealed class AutorisatiesApiFixture : IAsyncLifetime
{
    private readonly IntegrationTestContainers _containers = new();

    internal AutorisatiesWebApplicationFactory Factory { get; private set; }

    public async Task InitializeAsync()
    {
        await _containers.InitializeAsync();

        Factory = new AutorisatiesWebApplicationFactory(_containers);

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
public sealed class AutorisatiesApiCollection : ICollectionFixture<AutorisatiesApiFixture>
{
    public const string Name = "Autorisaties API";
}
