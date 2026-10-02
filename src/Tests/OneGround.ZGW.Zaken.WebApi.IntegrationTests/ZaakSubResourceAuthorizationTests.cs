using System.Collections.Generic;
using System.Linq;
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
/// Reads, updates, deletes and creates on one zaak, decided by the zaaktype and maximum vertrouwelijkheidaanduiding of the client's
/// authorization. Each request goes to a seeded zaak of zaaktype A at <c>geheim</c>, from a client holding every scope the action accepts.
/// </summary>
[Collection(ZakenApiCollection.Name)]
public class ZaakSubResourceAuthorizationTests
{
    private const VertrouwelijkheidAanduiding ZaakVertrouwelijkheid = VertrouwelijkheidAanduiding.geheim;
    private const VertrouwelijkheidAanduiding BelowZaak = VertrouwelijkheidAanduiding.vertrouwelijk;

    // The action, and the resource seeded under the zaak when it differs from the row's own.
    private static readonly (string Row, ZaakResource? Seeds)[] Endpoints =
    [
        ("v1.ZakenController.UpdateAsync", null),
        ("v1.ZakenController.PartialUpdateAsync", null),
        ("v1._5.ZakenController.UpdateAsync", null),
        ("v1._5.ZakenController.PartialUpdateAsync", null),
        ("v1.ZakenController.GetZaakAuditTrailRegelAsync", null),
        ("v1.ZaakStatussenController.GetAsync", null),
        ("v1._5.ZaakStatussenController.GetAsync", null),
        // Statuses are added to a zaak that already has one.
        ("v1.ZaakStatussenController.AddAsync", ZaakResource.Status),
        ("v1._5.ZaakStatussenController.AddAsync", ZaakResource.Status),
        ("v1.ZaakResultatenController.GetAsync", null),
        ("v1._5.ZaakResultatenController.GetAsync", null),
        ("v1.ZaakResultatenController.AddAsync", null),
        ("v1.ZaakResultatenController.UpdateAsync", null),
        ("v1.ZaakResultatenController.PartialUpdateAsync", null),
        ("v1.ZaakResultatenController.DeleteAsync", null),
        ("v1.ZaakRollenController.GetAsync", null),
        ("v1._5.ZaakRollenController.GetAsync", null),
        ("v1.ZaakRollenController.AddAsync", null),
        ("v1._5.ZaakRollenController.AddAsync", null),
        ("v1.ZaakRollenController.DeleteAsync", null),
        ("v1._5.ZaakRollenController.DeleteAsync", null),
        ("v1.ZaakObjectenController.GetAsync", null),
        ("v1._5.ZaakObjectenController.GetAsync", null),
        ("v1.ZaakObjectenController.AddAsync", null),
        ("v1._2.ZaakObjectenController.AddAsync", null),
        ("v1._5.ZaakObjectenController.AddAsync", null),
        ("v1._2.ZaakObjectenController.UpdateAsync", null),
        ("v1._5.ZaakObjectenController.UpdateAsync", null),
        ("v1._2.ZaakObjectenController.PartialUpdateAsync", null),
        ("v1._5.ZaakObjectenController.PartialUpdateAsync", null),
        ("v1._2.ZaakObjectenController.DeleteAsync", null),
        ("v1._5.ZaakObjectenController.DeleteAsync", null),
        ("v1.ZakenController.GetZaakEigenschapAsync", null),
        ("v1._5.ZakenController.GetZaakEigenschapAsync", null),
        ("v1.ZakenController.AddZaakEigenschapAsync", null),
        ("v1._2.ZakenController.UpdateAsync", null),
        ("v1._2.ZakenController.PartialUpdateAsync", null),
        ("v1._2.ZakenController.DeleteAsync", null),
        ("v1.ZakenController.GetZaakBesluitAsync", null),
        ("v1.ZakenController.AddZaakBesluitenAsync", null),
        ("v1.ZakenController.DeleteZaakBesluitAsync", null),
        ("v1._5.ZaakContactmomentenController.GetAsync", null),
        ("v1._5.ZaakContactmomentenController.AddAsync", null),
        ("v1._5.ZaakContactmomentenController.DeleteAsync", null),
        ("v1._5.ZaakVerzoekenController.GetAsync", null),
        ("v1._5.ZaakVerzoekenController.AddAsync", null),
        ("v1._5.ZaakVerzoekenController.DeleteAsync", null),
        ("v1.ZaakInformatieObjectenController.AddAsync", null),
        ("v1._5.ZaakInformatieObjectenController.AddAsync", null),
    ];

    private readonly ZakenWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ZaakSubResourceAuthorizationTests(ZakenApiFixture fixture)
    {
        _factory = fixture.Factory;
        _client = _factory.CreateClient();

        ZakenSeed.PrepareFactory(_factory);
    }

    public static IEnumerable<object[]> EndpointRows() => Endpoints.Select(e => new object[] { e.Row });

    public static IEnumerable<object[]> ZaakCreateVersions() =>
        [
            [Api.LatestVersion_1_0],
            [Api.LatestVersion_1_5],
        ];

    [Theory]
    [MemberData(nameof(EndpointRows))]
    public async Task Client_authorized_for_the_zaaktype_is_not_forbidden(string row)
    {
        var response = await SendAsync(row, ZaakTypes.A, VertrouwelijkheidAanduiding.zeer_geheim);

        Assert.True(
            response.StatusCode is not (HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden) && (int)response.StatusCode < 500,
            await ZakenEndpointRequests.DescribeAsync(response)
        );
    }

    [Theory]
    [MemberData(nameof(EndpointRows))]
    public async Task Client_authorized_for_another_zaaktype_only_is_forbidden(string row)
    {
        var response = await SendAsync(row, ZaakTypes.B, VertrouwelijkheidAanduiding.zeer_geheim);

        Assert.True(response.StatusCode == HttpStatusCode.Forbidden, await ZakenEndpointRequests.DescribeAsync(response));
    }

    [Theory]
    [MemberData(nameof(EndpointRows))]
    public async Task Client_whose_maximum_is_below_the_zaaks_vertrouwelijkheidaanduiding_is_forbidden(string row)
    {
        var response = await SendAsync(row, ZaakTypes.A, BelowZaak);

        Assert.True(response.StatusCode == HttpStatusCode.Forbidden, await ZakenEndpointRequests.DescribeAsync(response));
    }

    [Theory]
    [MemberData(nameof(ZaakCreateVersions))]
    public async Task Zaak_of_a_zaaktype_the_client_may_create_zaken_for_is_created(string apiVersion)
    {
        var clientId = ZrcClients.Authorized(_factory, ZaakTypes.A, ZaakVertrouwelijkheid, AuthorizationScopes.Zaken.Create);

        var response = await CreateZaakAsync(clientId, ZaakTypes.A, ZaakVertrouwelijkheid, apiVersion);

        Assert.True(response.StatusCode == HttpStatusCode.Created, await ZakenEndpointRequests.DescribeAsync(response));
    }

    [Theory]
    [MemberData(nameof(ZaakCreateVersions))]
    public async Task Zaak_of_a_zaaktype_outside_the_clients_authorization_is_not_created(string apiVersion)
    {
        var clientId = ZrcClients.Authorized(_factory, ZaakTypes.A, VertrouwelijkheidAanduiding.zeer_geheim, AuthorizationScopes.Zaken.Create);

        var response = await CreateZaakAsync(clientId, ZaakTypes.B, ZaakVertrouwelijkheid, apiVersion);

        Assert.True(response.StatusCode == HttpStatusCode.Forbidden, await ZakenEndpointRequests.DescribeAsync(response));
    }

    [Theory]
    [MemberData(nameof(ZaakCreateVersions))]
    public async Task Zaak_above_the_clients_maximum_vertrouwelijkheidaanduiding_is_not_created(string apiVersion)
    {
        var clientId = ZrcClients.Authorized(_factory, ZaakTypes.A, BelowZaak, AuthorizationScopes.Zaken.Create);

        var response = await CreateZaakAsync(clientId, ZaakTypes.A, ZaakVertrouwelijkheid, apiVersion);

        Assert.True(response.StatusCode == HttpStatusCode.Forbidden, await ZakenEndpointRequests.DescribeAsync(response));
    }

    private async Task<HttpResponseMessage> SendAsync(string row, string clientZaakType, VertrouwelijkheidAanduiding clientMaximum)
    {
        var matrixRow = ScopeMatrix.Named(row);
        var seeds = Endpoints.Single(e => e.Row == row).Seeds ?? matrixRow.Seeds;

        var zaak = await ZakenSeed.SeedZaakAsync(_factory, ZaakTypes.A, ZaakVertrouwelijkheid, resource: seeds);
        var clientId = ZrcClients.Authorized(_factory, clientZaakType, clientMaximum, matrixRow.Scopes);

        return await _client.SendAsync(ZakenEndpointRequests.For(matrixRow, zaak, clientId));
    }

    private async Task<HttpResponseMessage> CreateZaakAsync(
        string clientId,
        string zaakType,
        VertrouwelijkheidAanduiding vertrouwelijkheidAanduiding,
        string apiVersion
    )
    {
        var body = ZakenEndpointRequests.Zaak(TestRsins.A, zaakType, vertrouwelijkheidAanduiding);

        return await _client.SendAsync(ZakenEndpointRequests.Create(HttpMethod.Post, "/api/v1/zaken", apiVersion, clientId, TestRsins.A, body));
    }
}
