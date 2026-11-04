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
using V1Routes = OneGround.ZGW.Zaken.Web.Contracts.v1.ApiRoutes;

namespace OneGround.ZGW.Zaken.WebApi.IntegrationTests;

/// <summary>
/// Related zaken pulled in by <c>GET /zaken/{id}?expand=...</c> (1.7) are left out when the client may not read them, while the zaak itself is returned.
/// </summary>
[Collection(ZakenApiCollection.Name)]
public class ZaakExpandAuthorizationTests(ZakenApiFixture fixture) : ZakenApiTestBase(fixture)
{
    private const VertrouwelijkheidAanduiding Maximum = VertrouwelijkheidAanduiding.vertrouwelijk;

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

    private async Task<(HttpResponseMessage Response, SeededZaak Related)> ExpandAsync(
        string path,
        string relatedZaakType,
        VertrouwelijkheidAanduiding relatedVertrouwelijkheid
    )
    {
        SeededZaak zaak;
        SeededZaak related;
        switch (path)
        {
            case "hoofdzaak":
                related = await ZakenSeed.SeedZaakAsync(Factory, relatedZaakType, relatedVertrouwelijkheid);
                zaak = await ZakenSeed.SeedZaakAsync(Factory, hoofdzaakId: related.Id);
                break;
            case "relevanteanderezaken":
                related = await ZakenSeed.SeedZaakAsync(Factory, relatedZaakType, relatedVertrouwelijkheid);
                zaak = await ZakenSeed.SeedZaakAsync(Factory, relevanteAndereZaakId: related.Id);
                break;
            default:
                // A deelzaak points at its hoofdzaak, so the related zaak is seeded as the child.
                zaak = await ZakenSeed.SeedZaakAsync(Factory);
                related = await ZakenSeed.SeedZaakAsync(Factory, relatedZaakType, relatedVertrouwelijkheid, hoofdzaakId: zaak.Id);
                break;
        }

        var clientId = ZrcClients.Authorized(Factory, ZaakTypes.A, Maximum, AuthorizationScopes.Zaken.Read);
        var request = ZakenEndpointRequests.Create(
            HttpMethod.Get,
            $"/{V1Routes.Zaken.Get.Replace("{id}", zaak.Id.ToString())}?expand={path}",
            Api.LatestVersion_1_7,
            clientId,
            zaak.Rsin
        );

        return (await Client.SendAsync(request), related);
    }
}
