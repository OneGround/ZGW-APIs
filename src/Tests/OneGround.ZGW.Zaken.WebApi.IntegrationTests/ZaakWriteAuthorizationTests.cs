using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using OneGround.ZGW.Common.DataModel;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.IntegrationTests.Common;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.Web.Controllers;
using Xunit;
using AutorisatieResponseDto = OneGround.ZGW.Autorisaties.Contracts.v1.Responses.AutorisatieResponseDto;

namespace OneGround.ZGW.Zaken.WebApi.IntegrationTests;

/// <summary>
/// Write endpoints check the client's authorization against the zaaktype of the zaak they change, on every supported API version.
/// </summary>
[Collection(ZakenApiCollection.Name)]
public class ZaakWriteAuthorizationTests : IDisposable
{
    private const string Rsin = TestRsins.A;
    private const string StatusType = "https://catalogi.integrationtest.invalid/api/v1/statustypen/3f1c2a5e-0000-4000-8000-0000000000c1";

    private readonly ZakenWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _previousResponder;

    public ZaakWriteAuthorizationTests(ZakenApiFixture fixture)
    {
        _factory = fixture.Factory;
        _client = _factory.CreateClient();

        _previousResponder = _factory.OutboundHttp.Responder;
        _factory.OutboundHttp.Responder = request => TryRespondAsCatalogi(request) ?? _previousResponder(request);
    }

    public void Dispose()
    {
        _factory.OutboundHttp.Responder = _previousResponder;
    }

    public static TheoryData<string> ApiVersions => [Api.LatestVersion_1_0, Api.LatestVersion_1_2, Api.LatestVersion_1_5, Api.LatestVersion_1_7];

    [Theory]
    [MemberData(nameof(ApiVersions))]
    public async Task Deleting_a_zaak_of_a_zaaktype_the_client_is_not_authorized_for_is_forbidden(string apiVersion)
    {
        var zaakId = await SeedZaakAsync(ZaakTypes.A);

        var response = await _client.SendAsync(
            ZakenRequests.DeleteZaak(zaakId, NewClient(ZaakTypes.B, AuthorizationScopes.Zaken.Delete), Rsin, apiVersion)
        );

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.True(await ZaakExistsAsync(zaakId));
    }

    [Theory]
    [MemberData(nameof(ApiVersions))]
    public async Task Deleting_a_zaak_with_a_deelzaak_the_client_is_not_authorized_for_is_forbidden(string apiVersion)
    {
        var hoofdzaakId = await SeedZaakAsync(ZaakTypes.A);
        var deelzaakId = await SeedZaakAsync(ZaakTypes.B, hoofdzaakId);

        var response = await _client.SendAsync(
            ZakenRequests.DeleteZaak(hoofdzaakId, NewClient(ZaakTypes.A, AuthorizationScopes.Zaken.Delete), Rsin, apiVersion)
        );

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.True(await ZaakExistsAsync(hoofdzaakId));
        Assert.True(await ZaakExistsAsync(deelzaakId));
    }

    [Theory]
    [MemberData(nameof(ApiVersions))]
    public async Task Deleting_a_zaak_of_a_zaaktype_the_client_is_authorized_for_removes_it(string apiVersion)
    {
        var zaakId = await SeedZaakAsync(ZaakTypes.A);

        var response = await _client.SendAsync(
            ZakenRequests.DeleteZaak(zaakId, NewClient(ZaakTypes.A, AuthorizationScopes.Zaken.Delete), Rsin, apiVersion)
        );

        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.False(await ZaakExistsAsync(zaakId));
    }

    [Theory]
    [MemberData(nameof(ApiVersions))]
    public async Task Setting_the_first_status_on_a_zaak_of_a_zaaktype_the_client_is_not_authorized_for_is_forbidden(string apiVersion)
    {
        var zaakId = await SeedZaakAsync(ZaakTypes.A);

        var response = await _client.SendAsync(
            ZakenRequests.CreateZaakStatus(StatusBody(zaakId), NewClient(ZaakTypes.B, AuthorizationScopes.Zaken.Create), Rsin, apiVersion)
        );

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(ApiVersions))]
    public async Task Setting_the_first_status_with_only_the_create_scope_for_the_zaaktype_saves_it(string apiVersion)
    {
        var zaakId = await SeedZaakAsync(ZaakTypes.A);

        var response = await _client.SendAsync(
            ZakenRequests.CreateZaakStatus(StatusBody(zaakId), NewClient(ZaakTypes.A, AuthorizationScopes.Zaken.Create), Rsin, apiVersion)
        );

        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(1, await ZaakStatusCountAsync(zaakId));
    }

    [Theory]
    [MemberData(nameof(ApiVersions))]
    public async Task Setting_a_second_status_with_only_the_create_scope_for_the_zaaktype_is_forbidden(string apiVersion)
    {
        var zaakId = await SeedZaakAsync(ZaakTypes.A);
        await SeedZaakStatusAsync(zaakId);

        var response = await _client.SendAsync(
            ZakenRequests.CreateZaakStatus(StatusBody(zaakId), NewClient(ZaakTypes.A, AuthorizationScopes.Zaken.Create), Rsin, apiVersion)
        );

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(1, await ZaakStatusCountAsync(zaakId));
    }

    [Theory]
    [MemberData(nameof(ApiVersions))]
    public async Task Creating_a_zaakobject_with_only_the_create_scope_for_the_zaaktype_saves_it(string apiVersion)
    {
        var zaakId = await SeedZaakAsync(ZaakTypes.A);

        var response = await _client.SendAsync(
            ZakenRequests.CreateZaakObject(ZaakObjectBody(zaakId), NewClient(ZaakTypes.A, AuthorizationScopes.Zaken.Create), Rsin, apiVersion)
        );

        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.True(await ZaakObjectExistsAsync(zaakId));
    }

    [Theory]
    [MemberData(nameof(ApiVersions))]
    public async Task Creating_a_zaakobject_on_a_zaak_of_a_zaaktype_the_client_is_not_authorized_for_is_forbidden(string apiVersion)
    {
        var zaakId = await SeedZaakAsync(ZaakTypes.A);

        var response = await _client.SendAsync(
            ZakenRequests.CreateZaakObject(ZaakObjectBody(zaakId), NewClient(ZaakTypes.B, AuthorizationScopes.Zaken.Create), Rsin, apiVersion)
        );

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
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

    private static string ZaakUrl(Guid zaakId) => $"http://localhost/api/v1/zaken/{zaakId}";

    private string NewClient(string zaakType, string scope)
    {
        var clientId = $"integration-test-{Guid.NewGuid():N}";

        _factory.AutorisatiesApi.Grant(
            clientId,
            new AutorisatieResponseDto
            {
                Component = "zrc",
                Scopes = [scope],
                ZaakType = zaakType,
                MaxVertrouwelijkheidaanduiding = nameof(VertrouwelijkheidAanduiding.zeer_geheim),
            }
        );

        return clientId;
    }

    private async Task<Guid> SeedZaakAsync(string zaakType, Guid? hoofdzaakId = null)
    {
        using var scope = _factory.Services.CreateScope();
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
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ZrcDbContext>();

        return await context.Zaken.AnyAsync(z => z.Id == zaakId);
    }

    private async Task<bool> ZaakObjectExistsAsync(Guid zaakId)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ZrcDbContext>();

        return await context.ZaakObjecten.AnyAsync(o => o.ZaakId == zaakId);
    }

    private async Task SeedZaakStatusAsync(Guid zaakId)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ZrcDbContext>();

        context.ZaakStatussen.Add(
            new ZaakStatus
            {
                Id = Guid.NewGuid(),
                Owner = Rsin,
                ZaakId = zaakId,
                StatusType = StatusType,
                DatumStatusGezet = DateTime.UtcNow.AddDays(-1),
                IndicatieLaatstGezetteStatus = true,
            }
        );
        await context.SaveChangesAsync();
    }

    private async Task<int> ZaakStatusCountAsync(Guid zaakId)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ZrcDbContext>();

        return await context.ZaakStatussen.CountAsync(s => s.ZaakId == zaakId);
    }

    private static HttpResponseMessage TryRespondAsCatalogi(HttpRequestMessage request)
    {
        if (request.Method != HttpMethod.Get)
            return null;

        object body = request.RequestUri!.ToString() switch
        {
            ZaakTypes.A => new { url = ZaakTypes.A, statustypen = new[] { StatusType } },
            StatusType => new { url = StatusType, isEindstatus = false },
            _ => null,
        };

        if (body == null)
            return null;

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json"),
        };
    }
}
