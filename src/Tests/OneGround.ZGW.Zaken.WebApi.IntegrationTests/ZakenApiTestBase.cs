using System.Net.Http;
using OneGround.ZGW.Common.DataModel;

namespace OneGround.ZGW.Zaken.WebApi.IntegrationTests;

/// <summary>
/// The collection's Zaken API host and an HTTP client on it; a derived class still needs its own <c>[Collection]</c> to share the containers.
/// </summary>
public abstract class ZakenApiTestBase
{
    protected ZakenApiTestBase(ZakenApiFixture fixture)
    {
        Factory = fixture.Factory;
        Client = Factory.CreateClient();
    }

    private protected ZakenWebApplicationFactory Factory { get; }

    protected HttpClient Client { get; }

    private protected string NewClient(string zaakType, string scope, VertrouwelijkheidAanduiding maximum = VertrouwelijkheidAanduiding.zeer_geheim)
    {
        return ZrcClients.Authorized(Factory, zaakType, maximum, scope);
    }
}
