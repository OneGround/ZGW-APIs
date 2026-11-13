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

    [Fact]
    public async Task The_url_of_a_relevanteAndereZaak_is_built_from_the_id_of_the_referenced_zaak_not_from_the_stored_url()
    {
        var clientId = ZrcClients.WithAllAuthorizations(Factory);
        var referenced = await ZakenSeed.SeedZaakAsync(Factory);
        var referencing = await ZakenSeed.SeedZaakAsync(Factory);
        await ZakenSeed.AddRelevanteAndereZaakAsync(
            Factory,
            referencing.Id,
            referenced.Id,
            storedUrl: $"https://stale-host.test/zaken/api/v1/zaken/{referenced.Id}"
        );

        var read = await SendAsync(HttpMethod.Get, V1Routes.Zaken.Get, referencing.Id, clientId, referencing.Rsin);

        var body = JObject.Parse(await read.Content.ReadAsStringAsync());
        Assert.Equal([referenced.Url], body["relevanteAndereZaken"]!.Select(r => r.Value<string>("url")).ToArray());
    }

    [Fact]
    public async Task A_relevanteAndereZaak_without_a_stored_url_is_returned_with_the_url_of_the_referenced_zaak()
    {
        var clientId = ZrcClients.WithAllAuthorizations(Factory);
        var referenced = await ZakenSeed.SeedZaakAsync(Factory);
        var referencing = await ZakenSeed.SeedZaakAsync(Factory);
        await ZakenSeed.AddRelevanteAndereZaakAsync(Factory, referencing.Id, referenced.Id, withoutUrl: true);

        var read = await SendAsync(HttpMethod.Get, V1Routes.Zaken.Get, referencing.Id, clientId, referencing.Rsin);

        var body = JObject.Parse(await read.Content.ReadAsStringAsync());
        Assert.Equal([referenced.Url], body["relevanteAndereZaken"]!.Select(r => r.Value<string>("url")).ToArray());
        // Note: a PATCH/PUT/POST that keeps a relevanteAndereZaak cannot be exercised here: the test host does not answer the zaken service agent
        // that validates the referenced zaak.
    }

    [Fact]
    public async Task Patching_another_field_of_a_zaak_keeps_its_relevanteAndereZaken_without_validating_them_again()
    {
        var clientId = ZrcClients.WithAllAuthorizations(Factory);
        var referenced = await ZakenSeed.SeedZaakAsync(Factory);
        var referencing = await ZakenSeed.SeedZaakAsync(Factory);
        await ZakenSeed.AddRelevanteAndereZaakAsync(Factory, referencing.Id, referenced.Id, withoutUrl: true);

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
        Assert.Equal([referenced.Url], body["relevanteAndereZaken"]!.Select(r => r.Value<string>("url")).ToArray());

        // The relation is kept by the id of the referenced zaak only: the patch (which rewrites the relations) did not bring a url back
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ZrcDbContext>();
        var relation = await context.RelevanteAndereZaken.SingleAsync(r => r.ZaakId == referencing.Id);
        Assert.Equal(referenced.Id, relation.GerelateerdeZaakId);
#pragma warning disable CS0618 // Url is obsolete: it must not be written any more
        Assert.Null(relation.Url);
#pragma warning restore CS0618
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
