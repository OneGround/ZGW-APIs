using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using OneGround.ZGW.Common.DataModel;
using Xunit;

namespace OneGround.ZGW.Zaken.WebApi.IntegrationTests;

/// <summary>
/// Which resources of a zaak a list or search returns, decided by the zaaktype and maximum vertrouwelijkheidaanduiding of the client's
/// authorization. Each list is filtered down to one seeded zaak of zaaktype A at <c>geheim</c>.
/// </summary>
[Collection(ZakenApiCollection.Name)]
public class ZaakSubResourceListAuthorizationTests
{
    private const VertrouwelijkheidAanduiding ZaakVertrouwelijkheid = VertrouwelijkheidAanduiding.geheim;
    private const VertrouwelijkheidAanduiding BelowZaak = VertrouwelijkheidAanduiding.vertrouwelijk;

    // Lists and searches that leave out what the client is not authorized for.
    private static readonly string[] FilteredLists =
    [
        "v1.ZaakStatussenController.GetAllAsync",
        "v1._5.ZaakStatussenController.GetAllAsync",
        "v1.ZaakResultatenController.GetAllAsync",
        "v1.ZaakRollenController.GetAllAsync",
        "v1._5.ZaakRollenController.GetAllAsync",
        "v1.ZaakObjectenController.GetAllAsync",
        "v1._2.ZaakObjectenController.GetAllAsync",
        "v1._5.ZaakObjectenController.GetAllAsync",
        "v1.ZaakInformatieObjectenController.GetAllAsync",
        "v1._5.ZaakInformatieObjectenController.GetAllAsync",
        "v1.KlantContactenController.GetAllAsync",
        "v1._5.ZaakContactmomentenController.GetAllAsync",
        "v1._5.ZaakVerzoekenController.GetAllAsync",
        "v1.ZakenController.SearchAsync",
        "v1._5.ZakenController.SearchAsync",
    ];

    // Lists under /zaken/{zaak_uuid}, which answer 403 for a zaak the client is not authorized for instead of leaving it out.
    private static readonly string[] ListsOfOneZaak =
    [
        "v1.ZakenController.GetAllZaakBesluitenAsync",
        "v1.ZakenController.GetAllZaakEigenschappenAsync",
        "v1.ZakenController.GetAllZaakAuditTrailRegelsAsync",
    ];

    private readonly ZakenWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ZaakSubResourceListAuthorizationTests(ZakenApiFixture fixture)
    {
        _factory = fixture.Factory;
        _client = _factory.CreateClient();

        ZakenSeed.PrepareFactory(_factory);
    }

    public static IEnumerable<object[]> FilteredListRows() => FilteredLists.Select(r => new object[] { r });

    public static IEnumerable<object[]> ListOfOneZaakRows() => ListsOfOneZaak.Select(r => new object[] { r });

    [Theory]
    [MemberData(nameof(FilteredListRows))]
    public async Task Resource_is_listed_for_a_client_authorized_for_its_zaaktype(string row)
    {
        var (matrixRow, zaak) = await SeedAsync(row);

        var listed = await ListAsync(matrixRow, zaak, ZrcClients.Authorized(_factory, ZaakTypes.A, ZaakVertrouwelijkheid, matrixRow.Scopes));

        Assert.Contains(listed, IsOf(zaak));
    }

    [Theory]
    [MemberData(nameof(FilteredListRows))]
    public async Task Resource_is_not_listed_for_a_client_authorized_for_another_zaaktype_only(string row)
    {
        var (matrixRow, zaak) = await SeedAsync(row);

        var listed = await ListAsync(
            matrixRow,
            zaak,
            ZrcClients.Authorized(_factory, ZaakTypes.B, VertrouwelijkheidAanduiding.zeer_geheim, matrixRow.Scopes)
        );

        Assert.DoesNotContain(listed, IsOf(zaak));
    }

    [Theory]
    [MemberData(nameof(FilteredListRows))]
    public async Task Resource_is_not_listed_above_the_clients_maximum_vertrouwelijkheidaanduiding(string row)
    {
        var (matrixRow, zaak) = await SeedAsync(row);

        var listed = await ListAsync(matrixRow, zaak, ZrcClients.Authorized(_factory, ZaakTypes.A, BelowZaak, matrixRow.Scopes));

        Assert.DoesNotContain(listed, IsOf(zaak));
    }

    [Theory]
    [MemberData(nameof(ListOfOneZaakRows))]
    public async Task List_of_a_zaak_is_returned_to_a_client_authorized_for_its_zaaktype(string row)
    {
        var (matrixRow, zaak) = await SeedAsync(row);

        var listed = await ListAsync(matrixRow, zaak, ZrcClients.Authorized(_factory, ZaakTypes.A, ZaakVertrouwelijkheid, matrixRow.Scopes));

        Assert.Contains(listed, IsOf(zaak));
    }

    [Theory]
    [MemberData(nameof(ListOfOneZaakRows))]
    public async Task List_of_a_zaak_is_forbidden_for_a_client_authorized_for_another_zaaktype_only(string row)
    {
        var (matrixRow, zaak) = await SeedAsync(row);
        var clientId = ZrcClients.Authorized(_factory, ZaakTypes.B, VertrouwelijkheidAanduiding.zeer_geheim, matrixRow.Scopes);

        var response = await _client.SendAsync(ZakenEndpointRequests.For(matrixRow, zaak, clientId));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(ListOfOneZaakRows))]
    public async Task List_of_a_zaak_is_forbidden_above_the_clients_maximum_vertrouwelijkheidaanduiding(string row)
    {
        var (matrixRow, zaak) = await SeedAsync(row);
        var clientId = ZrcClients.Authorized(_factory, ZaakTypes.A, BelowZaak, matrixRow.Scopes);

        var response = await _client.SendAsync(ZakenEndpointRequests.For(matrixRow, zaak, clientId));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<(ScopeMatrixRow Row, SeededZaak Zaak)> SeedAsync(string row)
    {
        var matrixRow = ScopeMatrix.Named(row);
        var zaak = await ZakenSeed.SeedZaakAsync(_factory, ZaakTypes.A, ZaakVertrouwelijkheid, resource: matrixRow.Seeds);

        return (matrixRow, zaak);
    }

    // How every listed item identifies itself: by its URL, or by its uuid in an audittrail.
    private async Task<string[]> ListAsync(ScopeMatrixRow row, SeededZaak zaak, string clientId)
    {
        var response = await _client.SendAsync(ZakenEndpointRequests.For(row, zaak, clientId));
        var body = await response.Content.ReadAsStringAsync();

        Assert.True(response.StatusCode == HttpStatusCode.OK, $"{row.Name} answered {(int)response.StatusCode}: {body}");

        var json = JToken.Parse(body);
        var items = json is JArray array ? array : (JArray)json["results"]!;

        return items.Select(i => i.Value<string>("url") ?? i.Value<string>("uuid")).ToArray();
    }

    // A search lists zaken, every other list the resource seeded under the zaak.
    private static Predicate<string> IsOf(SeededZaak zaak)
    {
        var id = (zaak.ResourceId ?? zaak.Id).ToString();

        return listed => listed == id || listed.EndsWith($"/{id}");
    }
}
