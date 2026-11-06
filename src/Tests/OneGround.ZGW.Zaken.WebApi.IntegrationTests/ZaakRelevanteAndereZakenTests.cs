using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using OneGround.ZGW.IntegrationTests.Common;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.Web.Controllers;
using Xunit;
using V1Routes = OneGround.ZGW.Zaken.Web.Contracts.v1.ApiRoutes;

namespace OneGround.ZGW.Zaken.WebApi.IntegrationTests;

/// <summary>
/// A zaak that is referenced from the relevanteAndereZaken of other zaken can be deleted without leaving a dangling reference behind.
/// </summary>
[Collection(ZakenApiCollection.Name)]
public class ZaakRelevanteAndereZakenTests(ZakenApiFixture fixture) : ZakenApiTestBase(fixture)
{
    [Fact]
    public async Task Deleting_a_referenced_zaak_removes_the_reference_so_the_referencing_zaak_can_still_be_patched()
    {
        var clientId = ZrcClients.WithAllAuthorizations(Factory);
        var deleted = await ZakenSeed.SeedZaakAsync(Factory);
        var referencing = await ZakenSeed.SeedZaakAsync(Factory, relevanteAndereZaakId: deleted.Id);

        var delete = await SendAsync(HttpMethod.Delete, V1Routes.Zaken.Delete, deleted.Id, clientId, deleted.Rsin);
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var patch = await SendAsync(
            HttpMethod.Patch,
            V1Routes.Zaken.Update,
            referencing.Id,
            clientId,
            referencing.Rsin,
            new { omschrijving = "Bijgewerkt" }
        );
        Assert.True(patch.IsSuccessStatusCode, await ZakenEndpointRequests.DescribeAsync(patch));

        var read = await SendAsync(HttpMethod.Get, V1Routes.Zaken.Get, referencing.Id, clientId, referencing.Rsin);
        var body = JObject.Parse(await read.Content.ReadAsStringAsync());
        Assert.Empty(body["relevanteAndereZaken"]!);
    }

    [Fact]
    public async Task Deleting_a_referenced_zaak_keeps_the_references_to_other_zaken()
    {
        var clientId = ZrcClients.WithAllAuthorizations(Factory);
        var deleted = await ZakenSeed.SeedZaakAsync(Factory);
        var kept = await ZakenSeed.SeedZaakAsync(Factory);
        var referencing = await ZakenSeed.SeedZaakAsync(Factory, relevanteAndereZaakId: deleted.Id);
        await ZakenSeed.AddRelevanteAndereZaakAsync(Factory, referencing.Id, kept.Id);

        var delete = await SendAsync(HttpMethod.Delete, V1Routes.Zaken.Delete, deleted.Id, clientId, deleted.Rsin);
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var read = await SendAsync(HttpMethod.Get, V1Routes.Zaken.Get, referencing.Id, clientId, referencing.Rsin);
        var body = JObject.Parse(await read.Content.ReadAsStringAsync());
        var urls = body["relevanteAndereZaken"]!.Select(r => r.Value<string>("url")).ToArray();
        Assert.Equal([kept.Url], urls);
    }

    [Fact]
    public async Task Deleting_a_referenced_zaak_removes_the_references_of_zaken_of_another_owner_too()
    {
        var clientId = ZrcClients.WithAllAuthorizations(Factory);
        var deleted = await ZakenSeed.SeedZaakAsync(Factory);
        var referencing = await ZakenSeed.SeedZaakAsync(Factory, rsin: TestRsins.B, relevanteAndereZaakId: deleted.Id);

        var delete = await SendAsync(HttpMethod.Delete, V1Routes.Zaken.Delete, deleted.Id, clientId, deleted.Rsin);
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ZrcDbContext>();
        Assert.False(await context.RelevanteAndereZaken.AnyAsync(r => r.ZaakId == referencing.Id));
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string route, Guid zaakId, string clientId, string rsin, object body = null)
    {
        var request = ZakenEndpointRequests.Create(
            method,
            "/" + route.Replace("{id}", zaakId.ToString()),
            Api.LatestVersion_1_5,
            clientId,
            rsin,
            body
        );

        return await Client.SendAsync(request);
    }
}
