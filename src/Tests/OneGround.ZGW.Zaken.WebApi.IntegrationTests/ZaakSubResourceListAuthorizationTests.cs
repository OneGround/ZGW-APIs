using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using OneGround.ZGW.Common.DataModel;
using Xunit;
using V1 = OneGround.ZGW.Zaken.Web.Controllers.v1;
using V1_2 = OneGround.ZGW.Zaken.Web.Controllers.v1._2;
using V1_5 = OneGround.ZGW.Zaken.Web.Controllers.v1._5;
using V1_7 = OneGround.ZGW.Zaken.Web.Controllers.v1._7;

namespace OneGround.ZGW.Zaken.WebApi.IntegrationTests;

/// <summary>
/// Lists and searches over one seeded zaak (zaaktype A, <c>geheim</c>) follow the client's zaaktype and maximum vertrouwelijkheidaanduiding.
/// </summary>
[Collection(ZakenApiCollection.Name)]
public class ZaakSubResourceListAuthorizationTests(ZakenApiFixture fixture) : ZakenApiTestBase(fixture)
{
    private const VertrouwelijkheidAanduiding ZaakVertrouwelijkheid = VertrouwelijkheidAanduiding.geheim;
    private const VertrouwelijkheidAanduiding BelowZaak = VertrouwelijkheidAanduiding.vertrouwelijk;

    internal static readonly string[] FilteredLists =
    [
        ScopeMatrix.Name<V1.ZaakStatussenController>(nameof(V1.ZaakStatussenController.GetAllAsync)),
        ScopeMatrix.Name<V1_5.ZaakStatussenController>(nameof(V1_5.ZaakStatussenController.GetAllAsync)),
        ScopeMatrix.Name<V1.ZaakResultatenController>(nameof(V1.ZaakResultatenController.GetAllAsync)),
        ScopeMatrix.Name<V1.ZaakRollenController>(nameof(V1.ZaakRollenController.GetAllAsync)),
        ScopeMatrix.Name<V1_5.ZaakRollenController>(nameof(V1_5.ZaakRollenController.GetAllAsync)),
        ScopeMatrix.Name<V1.ZaakObjectenController>(nameof(V1.ZaakObjectenController.GetAllAsync)),
        ScopeMatrix.Name<V1_2.ZaakObjectenController>(nameof(V1_2.ZaakObjectenController.GetAllAsync)),
        ScopeMatrix.Name<V1_5.ZaakObjectenController>(nameof(V1_5.ZaakObjectenController.GetAllAsync)),
        ScopeMatrix.Name<V1.ZaakInformatieObjectenController>(nameof(V1.ZaakInformatieObjectenController.GetAllAsync)),
        ScopeMatrix.Name<V1_5.ZaakInformatieObjectenController>(nameof(V1_5.ZaakInformatieObjectenController.GetAllAsync)),
        ScopeMatrix.Name<V1.KlantContactenController>(nameof(V1.KlantContactenController.GetAllAsync)),
        ScopeMatrix.Name<V1_5.ZaakContactmomentenController>(nameof(V1_5.ZaakContactmomentenController.GetAllAsync)),
        ScopeMatrix.Name<V1_5.ZaakVerzoekenController>(nameof(V1_5.ZaakVerzoekenController.GetAllAsync)),
        ScopeMatrix.Name<V1.ZakenController>(nameof(V1.ZakenController.SearchAsync)),
        ScopeMatrix.Name<V1_5.ZakenController>(nameof(V1_5.ZakenController.SearchAsync)),
        ScopeMatrix.Name<V1_7.ZaakStatussenController>(nameof(V1_7.ZaakStatussenController.GetAllAsync)),
        ScopeMatrix.Name<V1_7.ZaakResultatenController>(nameof(V1_7.ZaakResultatenController.GetAllAsync)),
        ScopeMatrix.Name<V1_7.ZaakRollenController>(nameof(V1_7.ZaakRollenController.GetAllAsync)),
        ScopeMatrix.Name<V1_7.ZaakObjectenController>(nameof(V1_7.ZaakObjectenController.GetAllAsync)),
        ScopeMatrix.Name<V1_7.ZaakInformatieObjectenController>(nameof(V1_7.ZaakInformatieObjectenController.GetAllAsync)),
        ScopeMatrix.Name<V1_7.ZaakContactmomentenController>(nameof(V1_7.ZaakContactmomentenController.GetAllAsync)),
        ScopeMatrix.Name<V1_7.ZakenController>(nameof(V1_7.ZakenController.SearchAsync)),
        ScopeMatrix.Name<V1_7.ZaakNotitiesController>(nameof(V1_7.ZaakNotitiesController.GetAllAsync)),
    ];

    // Lists under /zaken/{zaak_uuid}, which answer 403 for a zaak the client is not authorized for instead of leaving it out.
    internal static readonly string[] ListsOfOneZaak =
    [
        ScopeMatrix.Name<V1.ZakenController>(nameof(V1.ZakenController.GetAllZaakBesluitenAsync)),
        ScopeMatrix.Name<V1_7.ZakenController>(nameof(V1_7.ZakenController.GetAllZaakBesluitenAsync)),
        ScopeMatrix.Name<V1.ZakenController>(nameof(V1.ZakenController.GetAllZaakEigenschappenAsync)),
        ScopeMatrix.Name<V1_7.ZakenController>(nameof(V1_7.ZakenController.GetAllZaakEigenschappenAsync)),
        ScopeMatrix.Name<V1.ZakenController>(nameof(V1.ZakenController.GetAllZaakAuditTrailRegelsAsync)),
    ];

    public static IEnumerable<object[]> FilteredListRows() => WithServedVersions(FilteredLists);

    public static IEnumerable<object[]> ListOfOneZaakRows() => WithServedVersions(ListsOfOneZaak);

    [Theory]
    [MemberData(nameof(FilteredListRows))]
    public async Task Resource_is_listed_for_a_client_authorized_for_its_zaaktype(string row, string apiVersion)
    {
        var (matrixRow, zaak) = await SeedAsync(row, apiVersion);

        var listed = await ListAsync(matrixRow, zaak, ZrcClients.Authorized(Factory, ZaakTypes.A, ZaakVertrouwelijkheid, matrixRow.Scopes));

        Assert.Contains(listed, IsOf(zaak));
    }

    [Theory]
    [MemberData(nameof(FilteredListRows))]
    public async Task Resource_is_not_listed_for_a_client_authorized_for_another_zaaktype_only(string row, string apiVersion)
    {
        var (matrixRow, zaak) = await SeedAsync(row, apiVersion);

        var listed = await ListAsync(
            matrixRow,
            zaak,
            ZrcClients.Authorized(Factory, ZaakTypes.B, VertrouwelijkheidAanduiding.zeer_geheim, matrixRow.Scopes)
        );

        Assert.DoesNotContain(listed, IsOf(zaak));
    }

    [Theory]
    [MemberData(nameof(FilteredListRows))]
    public async Task Resource_is_not_listed_above_the_clients_maximum_vertrouwelijkheidaanduiding(string row, string apiVersion)
    {
        var (matrixRow, zaak) = await SeedAsync(row, apiVersion);

        var listed = await ListAsync(matrixRow, zaak, ZrcClients.Authorized(Factory, ZaakTypes.A, BelowZaak, matrixRow.Scopes));

        Assert.DoesNotContain(listed, IsOf(zaak));
    }

    [Theory]
    [MemberData(nameof(ListOfOneZaakRows))]
    public async Task List_of_a_zaak_is_returned_to_a_client_authorized_for_its_zaaktype(string row, string apiVersion)
    {
        var (matrixRow, zaak) = await SeedAsync(row, apiVersion);

        var listed = await ListAsync(matrixRow, zaak, ZrcClients.Authorized(Factory, ZaakTypes.A, ZaakVertrouwelijkheid, matrixRow.Scopes));

        Assert.Contains(listed, IsOf(zaak));
    }

    [Theory]
    [MemberData(nameof(ListOfOneZaakRows))]
    public async Task List_of_a_zaak_is_forbidden_for_a_client_authorized_for_another_zaaktype_only(string row, string apiVersion)
    {
        var (matrixRow, zaak) = await SeedAsync(row, apiVersion);
        var clientId = ZrcClients.Authorized(Factory, ZaakTypes.B, VertrouwelijkheidAanduiding.zeer_geheim, matrixRow.Scopes);

        var response = await Client.SendAsync(ZakenEndpointRequests.For(matrixRow, zaak, clientId));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(ListOfOneZaakRows))]
    public async Task List_of_a_zaak_is_forbidden_above_the_clients_maximum_vertrouwelijkheidaanduiding(string row, string apiVersion)
    {
        var (matrixRow, zaak) = await SeedAsync(row, apiVersion);
        var clientId = ZrcClients.Authorized(Factory, ZaakTypes.A, BelowZaak, matrixRow.Scopes);

        var response = await Client.SendAsync(ZakenEndpointRequests.For(matrixRow, zaak, clientId));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static IEnumerable<object[]> WithServedVersions(IEnumerable<string> rows) =>
        rows.SelectMany(r => ScopeMatrix.Named(r).ServedApiVersions.Select(v => new object[] { r, v }));

    private async Task<(ScopeMatrixRow Row, SeededZaak Zaak)> SeedAsync(string row, string apiVersion)
    {
        var matrixRow = ScopeMatrix.Named(row) with { ApiVersion = apiVersion };
        var zaak = await ZakenSeed.SeedZaakAsync(Factory, ZaakTypes.A, ZaakVertrouwelijkheid, resource: matrixRow.Seeds);

        return (matrixRow, zaak);
    }

    // How every listed item identifies itself: by its URL, or by its uuid in an audittrail.
    private async Task<string[]> ListAsync(ScopeMatrixRow row, SeededZaak zaak, string clientId)
    {
        var response = await Client.SendAsync(ZakenEndpointRequests.For(row, zaak, clientId));
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
