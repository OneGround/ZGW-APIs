using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Common.Messaging;
using OneGround.ZGW.IntegrationTests.Common;
using OneGround.ZGW.IntegrationTests.Common.Containers;
using Xunit;

namespace OneGround.ZGW.Documenten.WebApi.IntegrationTests;

internal sealed class DocumentenWebApplicationFactory : ZgwWebApplicationFactory<Program>
{
    public DocumentenWebApplicationFactory(IntegrationTestContainers containers)
        : base(containers) { }

    protected override void ConfigureTestServices(IServiceCollection services)
    {
        services.AddMassTransitTestHarness(x => x.AddRequestClient<ISendNotificaties>());
    }
}

public sealed class DocumentenApiFixture : IAsyncLifetime
{
    private readonly IntegrationTestContainers _containers = new();
    internal DocumentenWebApplicationFactory Factory { get; private set; }

    public async Task InitializeAsync()
    {
        await _containers.InitializeAsync();
        Factory = new DocumentenWebApplicationFactory(_containers);
        _ = Factory.Server;
    }

    public async Task DisposeAsync()
    {
        if (Factory != null)
            await Factory.DisposeAsync();
        await _containers.DisposeAsync();
    }
}

[CollectionDefinition(Name)]
public sealed class DocumentenApiCollection : ICollectionFixture<DocumentenApiFixture>
{
    public const string Name = "Documenten API";
}
