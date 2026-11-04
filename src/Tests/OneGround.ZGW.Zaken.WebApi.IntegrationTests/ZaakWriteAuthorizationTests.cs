using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Common.DataModel;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.IntegrationTests.Common;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.Web.Controllers;
using Xunit;

namespace OneGround.ZGW.Zaken.WebApi.IntegrationTests;

/// <summary>
/// Write endpoints check the client's authorization against the zaaktype of the zaak they change, on every supported API version.
/// </summary>
[Collection(ZakenApiCollection.Name)]
public class ZaakWriteAuthorizationTests(ZakenApiFixture fixture) : ZakenApiTestBase(fixture)
{
    private const string Rsin = TestRsins.A;
    private static readonly string StatusType = CatalogiStub.A.BeginStatusType;

    public static TheoryData<string> ApiVersions => [Api.LatestVersion_1_0, Api.LatestVersion_1_2, Api.LatestVersion_1_5, Api.LatestVersion_1_7];

    [Theory]
    [MemberData(nameof(ApiVersions))]
    public async Task Deleting_a_zaak_with_a_deelzaak_the_client_is_not_authorized_for_is_forbidden(string apiVersion)
    {
        var hoofdzaakId = await SeedZaakAsync(ZaakTypes.A);
        var deelzaakId = await SeedZaakAsync(ZaakTypes.B, hoofdzaakId);

        var response = await Client.SendAsync(
            ZakenRequests.DeleteZaak(hoofdzaakId, NewClient(ZaakTypes.A, AuthorizationScopes.Zaken.Delete), Rsin, apiVersion)
        );

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.True(await ZaakExistsAsync(hoofdzaakId));
        Assert.True(await ZaakExistsAsync(deelzaakId));
    }

    [Theory]
    [MemberData(nameof(ApiVersions))]
    public async Task Setting_the_first_status_on_a_zaak_of_a_zaaktype_the_client_is_not_authorized_for_is_forbidden(string apiVersion)
    {
        var zaakId = await SeedZaakAsync(ZaakTypes.A);

        var response = await Client.SendAsync(
            ZakenRequests.CreateZaakStatus(StatusBody(zaakId), NewClient(ZaakTypes.B, AuthorizationScopes.Zaken.Create), Rsin, apiVersion)
        );

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(ApiVersions))]
    public async Task Creating_a_zaakobject_with_only_the_create_scope_for_the_zaaktype_saves_it(string apiVersion)
    {
        var zaakId = await SeedZaakAsync(ZaakTypes.A);

        var response = await Client.SendAsync(
            ZakenRequests.CreateZaakObject(ZaakObjectBody(zaakId), NewClient(ZaakTypes.A, AuthorizationScopes.Zaken.Create), Rsin, apiVersion)
        );

        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.True(await ZaakObjectExistsAsync(zaakId));
    }

    private static object StatusBody(Guid zaakId)
    {
        return new
        {
            zaak = ZaakUrl(zaakId),
            statustype = StatusType,
            datumStatusGezet = DateTime.UtcNow.AddMinutes(-1).ToString("yyyy-MM-ddTHH:mm:ssZ"),
            statustoelichting = "",
        };
    }

    private static object ZaakObjectBody(Guid zaakId)
    {
        return new
        {
            zaak = ZaakUrl(zaakId),
            @object = "https://besluiten.integrationtest.invalid/api/v1/besluiten/3f1c2a5e-0000-4000-8000-0000000000d1",
            objectType = "besluit",
            relatieomschrijving = "",
        };
    }

    private static string ZaakUrl(Guid zaakId) => ZakenEndpointRequests.ZaakUrl(zaakId);

    private async Task<Guid> SeedZaakAsync(string zaakType, Guid? hoofdzaakId = null)
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ZrcDbContext>();

        var zaak = new Zaak
        {
            Id = Guid.NewGuid(),
            Owner = Rsin,
            Bronorganisatie = Rsin,
            VerantwoordelijkeOrganisatie = Rsin,
            Identificatie = $"ZAAK-{Guid.NewGuid():N}",
            Zaaktype = zaakType,
            Startdatum = DateOnly.FromDateTime(DateTime.UtcNow),
            Communicatiekanaal = "",
            Selectielijstklasse = "",
            VertrouwelijkheidAanduiding = VertrouwelijkheidAanduiding.openbaar,
            HoofdzaakId = hoofdzaakId,
        };

        context.Zaken.Add(zaak);
        await context.SaveChangesAsync();

        return zaak.Id;
    }

    private async Task<bool> ZaakExistsAsync(Guid zaakId)
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ZrcDbContext>();

        return await context.Zaken.AnyAsync(z => z.Id == zaakId);
    }

    private async Task<bool> ZaakObjectExistsAsync(Guid zaakId)
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ZrcDbContext>();

        return await context.ZaakObjecten.AnyAsync(o => o.ZaakId == zaakId);
    }
}
