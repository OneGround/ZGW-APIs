using System.Threading.Tasks;
using OneGround.ZGW.IntegrationTests.Common.Containers;
using Xunit;

namespace OneGround.ZGW.Notificaties.WebApi.IntegrationTests;

/// <summary>
/// Starts the containers, then boots the Notificaties API against them; shared by every test in <see cref="NotificatiesApiCollection"/>.
/// </summary>
public sealed class NotificatiesApiFixture : IAsyncLifetime
{
    private readonly IntegrationTestContainers _containers = new();

    internal NotificatiesWebApplicationFactory Factory { get; private set; }

    public async Task InitializeAsync()
    {
        await _containers.InitializeAsync();

        Factory = new NotificatiesWebApplicationFactory(_containers);

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
public sealed class NotificatiesApiCollection : ICollectionFixture<NotificatiesApiFixture>
{
    public const string Name = "Notificaties API";
}
