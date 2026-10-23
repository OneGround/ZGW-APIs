using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using OneGround.ZGW.Common.DataModel;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.IntegrationTests.Common;
using OneGround.ZGW.Zaken.Web.Controllers;
using Xunit;

namespace OneGround.ZGW.Zaken.WebApi.IntegrationTests;

/// <summary>
/// The zaken that <c>GET /zaken/{id}?expand=...</c> pulls in beside the requested one (API version 1.7). The client may read zaaktype A up to
/// <c>vertrouwelijk</c>; a related zaak of another zaaktype, or above that maximum, never shows up in the expansion, while the zaak itself is still returned.
/// </summary>
[Collection(ZakenApiCollection.Name)]
public class ZaakExpandAuthorizationTests
{
    private const VertrouwelijkheidAanduiding Maximum = VertrouwelijkheidAanduiding.vertrouwelijk;

    private readonly ZakenWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ZaakExpandAuthorizationTests(ZakenApiFixture fixture)
    {
        _factory = fixture.Factory;
        _client = _factory.CreateClient();

        ZakenSeed.PrepareFactory(_factory);
    }

    public static IEnumerable<object[]> ExpandPaths() =>
        [
            ["hoofdzaak"],
            ["deelzaken"],
            ["relevanteanderezaken"],
        ];

    [Theory]
    [MemberData(nameof(ExpandPaths))]
    public async Task Related_zaak_the_client_is_authorized_for_is_expanded(string path)
    {
        var (response, related) = await ExpandAsync(path, ZaakTypes.A, VertrouwelijkheidAanduiding.openbaar);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(related.Identificatie, await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [MemberData(nameof(ExpandPaths))]
    public async Task Related_zaak_of_a_zaaktype_the_client_is_not_authorized_for_is_not_expanded(string path)
    {
        var (response, related) = await ExpandAsync(path, ZaakTypes.B, VertrouwelijkheidAanduiding.openbaar);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain(related.Identificatie, await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [MemberData(nameof(ExpandPaths))]
    public async Task Related_zaak_above_the_clients_maximum_vertrouwelijkheidaanduiding_is_not_expanded(string path)
    {
        var (response, related) = await ExpandAsync(path, ZaakTypes.A, VertrouwelijkheidAanduiding.geheim);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain(related.Identificatie, await response.Content.ReadAsStringAsync());
    }

    // Seeds zaak X (zaaktype A, openbaar) and a related zaak Y in the role the path expands, then reads X with the expansion.
    private async Task<(HttpResponseMessage Response, SeededZaak Related)> ExpandAsync(
        string path,
        string relatedZaakType,
        VertrouwelijkheidAanduiding relatedVertrouwelijkheid
    )
    {
        var related = await ZakenSeed.SeedZaakAsync(_factory, relatedZaakType, relatedVertrouwelijkheid, hoofdzaakId: null);

        SeededZaak zaak;
        switch (path)
        {
            case "hoofdzaak":
                zaak = await ZakenSeed.SeedZaakAsync(_factory, hoofdzaakId: related.Id);
                break;
            case "relevanteanderezaken":
                zaak = await ZakenSeed.SeedZaakAsync(_factory, relevanteAndereZaakId: related.Id);
                break;
            default:
                // A deelzaak points at its hoofdzaak, so the related zaak becomes the child of X.
                zaak = await ZakenSeed.SeedZaakAsync(_factory);
                related = await ZakenSeed.SeedZaakAsync(_factory, relatedZaakType, relatedVertrouwelijkheid, hoofdzaakId: zaak.Id);
                break;
        }

        var clientId = ZrcClients.Authorized(_factory, ZaakTypes.A, Maximum, AuthorizationScopes.Zaken.Read);
        var request = ZakenEndpointRequests.Create(
            HttpMethod.Get,
            $"/api/v1/zaken/{zaak.Id}?expand={path}",
            Api.LatestVersion_1_7,
            clientId,
            zaak.Rsin
        );

        return (await _client.SendAsync(request), related);
    }
}
