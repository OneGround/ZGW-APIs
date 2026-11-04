using System;
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
using V1 = OneGround.ZGW.Zaken.Web.Controllers.v1;
using V1_2 = OneGround.ZGW.Zaken.Web.Controllers.v1._2;
using V1_5 = OneGround.ZGW.Zaken.Web.Controllers.v1._5;
using V1_7 = OneGround.ZGW.Zaken.Web.Controllers.v1._7;
using V1Routes = OneGround.ZGW.Zaken.Web.Contracts.v1.ApiRoutes;

namespace OneGround.ZGW.Zaken.WebApi.IntegrationTests;

/// <summary>
/// Actions on one seeded zaak (zaaktype A, <c>geheim</c>) are allowed by the client's zaaktype and maximum vertrouwelijkheidaanduiding.
/// </summary>
[Collection(ZakenApiCollection.Name)]
public class ZaakSubResourceAuthorizationTests
{
    private const VertrouwelijkheidAanduiding ZaakVertrouwelijkheid = VertrouwelijkheidAanduiding.geheim;
    private const VertrouwelijkheidAanduiding BelowZaak = VertrouwelijkheidAanduiding.vertrouwelijk;

    // Seeds overrides the row's own seeded resource.
    internal static readonly (string Row, ZaakResource? Seeds)[] Endpoints =
    [
        (ScopeMatrix.Name<V1.ZakenController>(nameof(V1.ZakenController.UpdateAsync)), null),
        (ScopeMatrix.Name<V1.ZakenController>(nameof(V1.ZakenController.PartialUpdateAsync)), null),
        (ScopeMatrix.Name<V1_5.ZakenController>(nameof(V1_5.ZakenController.UpdateAsync)), null),
        (ScopeMatrix.Name<V1_5.ZakenController>(nameof(V1_5.ZakenController.PartialUpdateAsync)), null),
        (ScopeMatrix.Name<V1.ZakenController>(nameof(V1.ZakenController.GetZaakAuditTrailRegelAsync)), null),
        (ScopeMatrix.Name<V1.ZaakStatussenController>(nameof(V1.ZaakStatussenController.GetAsync)), null),
        (ScopeMatrix.Name<V1_5.ZaakStatussenController>(nameof(V1_5.ZaakStatussenController.GetAsync)), null),
        // Statuses are added to a zaak that already has one.
        (ScopeMatrix.Name<V1.ZaakStatussenController>(nameof(V1.ZaakStatussenController.AddAsync)), ZaakResource.Status),
        (ScopeMatrix.Name<V1_5.ZaakStatussenController>(nameof(V1_5.ZaakStatussenController.AddAsync)), ZaakResource.Status),
        (ScopeMatrix.Name<V1.ZaakResultatenController>(nameof(V1.ZaakResultatenController.GetAsync)), null),
        (ScopeMatrix.Name<V1_5.ZaakResultatenController>(nameof(V1_5.ZaakResultatenController.GetAsync)), null),
        (ScopeMatrix.Name<V1.ZaakResultatenController>(nameof(V1.ZaakResultatenController.AddAsync)), null),
        (ScopeMatrix.Name<V1.ZaakResultatenController>(nameof(V1.ZaakResultatenController.UpdateAsync)), null),
        (ScopeMatrix.Name<V1.ZaakResultatenController>(nameof(V1.ZaakResultatenController.PartialUpdateAsync)), null),
        (ScopeMatrix.Name<V1.ZaakResultatenController>(nameof(V1.ZaakResultatenController.DeleteAsync)), null),
        (ScopeMatrix.Name<V1.ZaakRollenController>(nameof(V1.ZaakRollenController.GetAsync)), null),
        (ScopeMatrix.Name<V1_5.ZaakRollenController>(nameof(V1_5.ZaakRollenController.GetAsync)), null),
        (ScopeMatrix.Name<V1.ZaakRollenController>(nameof(V1.ZaakRollenController.AddAsync)), null),
        (ScopeMatrix.Name<V1_5.ZaakRollenController>(nameof(V1_5.ZaakRollenController.AddAsync)), null),
        (ScopeMatrix.Name<V1.ZaakRollenController>(nameof(V1.ZaakRollenController.DeleteAsync)), null),
        (ScopeMatrix.Name<V1_5.ZaakRollenController>(nameof(V1_5.ZaakRollenController.DeleteAsync)), null),
        (ScopeMatrix.Name<V1.ZaakObjectenController>(nameof(V1.ZaakObjectenController.GetAsync)), null),
        (ScopeMatrix.Name<V1_2.ZaakObjectenController>(nameof(V1_2.ZaakObjectenController.GetAsync)), null),
        (ScopeMatrix.Name<V1_5.ZaakObjectenController>(nameof(V1_5.ZaakObjectenController.GetAsync)), null),
        (ScopeMatrix.Name<V1.ZaakObjectenController>(nameof(V1.ZaakObjectenController.AddAsync)), null),
        (ScopeMatrix.Name<V1_2.ZaakObjectenController>(nameof(V1_2.ZaakObjectenController.AddAsync)), null),
        (ScopeMatrix.Name<V1_5.ZaakObjectenController>(nameof(V1_5.ZaakObjectenController.AddAsync)), null),
        (ScopeMatrix.Name<V1_2.ZaakObjectenController>(nameof(V1_2.ZaakObjectenController.UpdateAsync)), null),
        (ScopeMatrix.Name<V1_5.ZaakObjectenController>(nameof(V1_5.ZaakObjectenController.UpdateAsync)), null),
        (ScopeMatrix.Name<V1_2.ZaakObjectenController>(nameof(V1_2.ZaakObjectenController.PartialUpdateAsync)), null),
        (ScopeMatrix.Name<V1_5.ZaakObjectenController>(nameof(V1_5.ZaakObjectenController.PartialUpdateAsync)), null),
        (ScopeMatrix.Name<V1_2.ZaakObjectenController>(nameof(V1_2.ZaakObjectenController.DeleteAsync)), null),
        (ScopeMatrix.Name<V1_5.ZaakObjectenController>(nameof(V1_5.ZaakObjectenController.DeleteAsync)), null),
        (ScopeMatrix.Name<V1.ZakenController>(nameof(V1.ZakenController.GetZaakEigenschapAsync)), null),
        (ScopeMatrix.Name<V1_5.ZakenController>(nameof(V1_5.ZakenController.GetZaakEigenschapAsync)), null),
        (ScopeMatrix.Name<V1.ZakenController>(nameof(V1.ZakenController.AddZaakEigenschapAsync)), null),
        (ScopeMatrix.Name<V1_2.ZakenController>(nameof(V1_2.ZakenController.UpdateAsync)), null),
        (ScopeMatrix.Name<V1_2.ZakenController>(nameof(V1_2.ZakenController.PartialUpdateAsync)), null),
        (ScopeMatrix.Name<V1_2.ZakenController>(nameof(V1_2.ZakenController.DeleteAsync)), null),
        (ScopeMatrix.Name<V1.ZakenController>(nameof(V1.ZakenController.GetZaakBesluitAsync)), null),
        (ScopeMatrix.Name<V1.ZakenController>(nameof(V1.ZakenController.AddZaakBesluitenAsync)), null),
        (ScopeMatrix.Name<V1.ZakenController>(nameof(V1.ZakenController.DeleteZaakBesluitAsync)), null),
        (ScopeMatrix.Name<V1_5.ZaakContactmomentenController>(nameof(V1_5.ZaakContactmomentenController.GetAsync)), null),
        (ScopeMatrix.Name<V1_5.ZaakContactmomentenController>(nameof(V1_5.ZaakContactmomentenController.AddAsync)), null),
        (ScopeMatrix.Name<V1_5.ZaakContactmomentenController>(nameof(V1_5.ZaakContactmomentenController.DeleteAsync)), null),
        (ScopeMatrix.Name<V1_5.ZaakVerzoekenController>(nameof(V1_5.ZaakVerzoekenController.GetAsync)), null),
        (ScopeMatrix.Name<V1_5.ZaakVerzoekenController>(nameof(V1_5.ZaakVerzoekenController.AddAsync)), null),
        (ScopeMatrix.Name<V1_5.ZaakVerzoekenController>(nameof(V1_5.ZaakVerzoekenController.DeleteAsync)), null),
        (ScopeMatrix.Name<V1.ZaakInformatieObjectenController>(nameof(V1.ZaakInformatieObjectenController.AddAsync)), null),
        (ScopeMatrix.Name<V1_5.ZaakInformatieObjectenController>(nameof(V1_5.ZaakInformatieObjectenController.AddAsync)), null),
        (ScopeMatrix.Name<V1.ZaakInformatieObjectenController>(nameof(V1.ZaakInformatieObjectenController.GetAsync)), null),
        (ScopeMatrix.Name<V1.ZaakInformatieObjectenController>(nameof(V1.ZaakInformatieObjectenController.UpdateAsync)), null),
        (ScopeMatrix.Name<V1.ZaakInformatieObjectenController>(nameof(V1.ZaakInformatieObjectenController.PartialUpdateAsync)), null),
        (ScopeMatrix.Name<V1.ZaakInformatieObjectenController>(nameof(V1.ZaakInformatieObjectenController.DeleteAsync)), null),
        (ScopeMatrix.Name<V1_5.ZaakInformatieObjectenController>(nameof(V1_5.ZaakInformatieObjectenController.GetAsync)), null),
        (ScopeMatrix.Name<V1_5.ZaakInformatieObjectenController>(nameof(V1_5.ZaakInformatieObjectenController.UpdateAsync)), null),
        (ScopeMatrix.Name<V1_5.ZaakInformatieObjectenController>(nameof(V1_5.ZaakInformatieObjectenController.PartialUpdateAsync)), null),
        (ScopeMatrix.Name<V1_5.ZaakInformatieObjectenController>(nameof(V1_5.ZaakInformatieObjectenController.DeleteAsync)), null),
        (ScopeMatrix.Name<V1.KlantContactenController>(nameof(V1.KlantContactenController.GetAsync)), null),
        (ScopeMatrix.Name<V1.KlantContactenController>(nameof(V1.KlantContactenController.AddAsync)), null),
        (ScopeMatrix.Name<V1.ZakenController>(nameof(V1.ZakenController.DeleteAsync)), null),
        (ScopeMatrix.Name<V1_7.ZaakStatussenController>(nameof(V1_7.ZaakStatussenController.GetAsync)), null),
        // Statuses are added to a zaak that already has one.
        (ScopeMatrix.Name<V1_7.ZaakStatussenController>(nameof(V1_7.ZaakStatussenController.AddAsync)), ZaakResource.Status),
        (ScopeMatrix.Name<V1_7.ZaakResultatenController>(nameof(V1_7.ZaakResultatenController.GetAsync)), null),
        (ScopeMatrix.Name<V1_7.ZaakResultatenController>(nameof(V1_7.ZaakResultatenController.AddAsync)), null),
        (ScopeMatrix.Name<V1_7.ZaakResultatenController>(nameof(V1_7.ZaakResultatenController.UpdateAsync)), null),
        (ScopeMatrix.Name<V1_7.ZaakResultatenController>(nameof(V1_7.ZaakResultatenController.PartialUpdateAsync)), null),
        (ScopeMatrix.Name<V1_7.ZaakResultatenController>(nameof(V1_7.ZaakResultatenController.DeleteAsync)), null),
        (ScopeMatrix.Name<V1_7.ZaakRollenController>(nameof(V1_7.ZaakRollenController.GetAsync)), null),
        (ScopeMatrix.Name<V1_7.ZaakRollenController>(nameof(V1_7.ZaakRollenController.AddAsync)), null),
        (ScopeMatrix.Name<V1_7.ZaakRollenController>(nameof(V1_7.ZaakRollenController.DeleteAsync)), null),
        (ScopeMatrix.Name<V1_7.ZaakObjectenController>(nameof(V1_7.ZaakObjectenController.GetAsync)), null),
        (ScopeMatrix.Name<V1_7.ZaakObjectenController>(nameof(V1_7.ZaakObjectenController.AddAsync)), null),
        (ScopeMatrix.Name<V1_7.ZaakObjectenController>(nameof(V1_7.ZaakObjectenController.UpdateAsync)), null),
        (ScopeMatrix.Name<V1_7.ZaakObjectenController>(nameof(V1_7.ZaakObjectenController.PartialUpdateAsync)), null),
        (ScopeMatrix.Name<V1_7.ZaakObjectenController>(nameof(V1_7.ZaakObjectenController.DeleteAsync)), null),
        (ScopeMatrix.Name<V1_7.ZaakInformatieObjectenController>(nameof(V1_7.ZaakInformatieObjectenController.GetAsync)), null),
        (ScopeMatrix.Name<V1_7.ZaakInformatieObjectenController>(nameof(V1_7.ZaakInformatieObjectenController.AddAsync)), null),
        (ScopeMatrix.Name<V1_7.ZaakInformatieObjectenController>(nameof(V1_7.ZaakInformatieObjectenController.UpdateAsync)), null),
        (ScopeMatrix.Name<V1_7.ZaakInformatieObjectenController>(nameof(V1_7.ZaakInformatieObjectenController.PartialUpdateAsync)), null),
        (ScopeMatrix.Name<V1_7.ZaakInformatieObjectenController>(nameof(V1_7.ZaakInformatieObjectenController.DeleteAsync)), null),
        (ScopeMatrix.Name<V1_7.ZaakContactmomentenController>(nameof(V1_7.ZaakContactmomentenController.GetAsync)), null),
        (ScopeMatrix.Name<V1_7.ZaakContactmomentenController>(nameof(V1_7.ZaakContactmomentenController.AddAsync)), null),
        (ScopeMatrix.Name<V1_7.ZaakContactmomentenController>(nameof(V1_7.ZaakContactmomentenController.DeleteAsync)), null),
        (ScopeMatrix.Name<V1_7.ZakenController>(nameof(V1_7.ZakenController.GetZaakEigenschapAsync)), null),
        (ScopeMatrix.Name<V1_7.ZakenController>(nameof(V1_7.ZakenController.UpdateAsync)), null),
        (ScopeMatrix.Name<V1_7.ZakenController>(nameof(V1_7.ZakenController.PartialUpdateAsync)), null),
        (ScopeMatrix.Name<V1_7.ZakenController>(nameof(V1_7.ZakenController.GetZaakBesluitAsync)), null),
        (ScopeMatrix.Name<V1.ZakenController>(nameof(V1.ZakenController.GetZaakEigenschapV1_2Async)), null),
        (ScopeMatrix.Name<V1.ZakenController>(nameof(V1.ZakenController.HeadZaakEigenschapV1_2Async)), null),
        (ScopeMatrix.Name<V1_7.ZaakNotitiesController>(nameof(V1_7.ZaakNotitiesController.GetAsync)), null),
        (ScopeMatrix.Name<V1_7.ZaakNotitiesController>(nameof(V1_7.ZaakNotitiesController.AddAsync)), null),
        (ScopeMatrix.Name<V1_7.ZaakNotitiesController>(nameof(V1_7.ZaakNotitiesController.UpdateAsync)), null),
        (ScopeMatrix.Name<V1_7.ZaakNotitiesController>(nameof(V1_7.ZaakNotitiesController.PartialUpdateAsync)), null),
        (ScopeMatrix.Name<V1_7.ZaakNotitiesController>(nameof(V1_7.ZaakNotitiesController.DeleteAsync)), null),
        (ScopeMatrix.Name<V1_5.ZakenController>(nameof(V1_5.ZakenController.HeadAsync)), null),
        (ScopeMatrix.Name<V1_5.ZakenController>(nameof(V1_5.ZakenController.HeadZaakEigenschapAsync)), null),
        (ScopeMatrix.Name<V1_5.ZaakStatussenController>(nameof(V1_5.ZaakStatussenController.HeadAsync)), null),
        (ScopeMatrix.Name<V1_5.ZaakResultatenController>(nameof(V1_5.ZaakResultatenController.HeadAsync)), null),
        (ScopeMatrix.Name<V1_5.ZaakRollenController>(nameof(V1_5.ZaakRollenController.HeadAsync)), null),
        (ScopeMatrix.Name<V1_5.ZaakObjectenController>(nameof(V1_5.ZaakObjectenController.HeadAsync)), null),
        (ScopeMatrix.Name<V1_5.ZaakInformatieObjectenController>(nameof(V1_5.ZaakInformatieObjectenController.HeadAsync)), null),
        (ScopeMatrix.Name<V1_7.ZakenController>(nameof(V1_7.ZakenController.HeadAsync)), null),
        (ScopeMatrix.Name<V1_7.ZakenController>(nameof(V1_7.ZakenController.HeadZaakEigenschapAsync)), null),
        (ScopeMatrix.Name<V1_7.ZaakStatussenController>(nameof(V1_7.ZaakStatussenController.HeadAsync)), null),
        (ScopeMatrix.Name<V1_7.ZaakResultatenController>(nameof(V1_7.ZaakResultatenController.HeadAsync)), null),
        (ScopeMatrix.Name<V1_7.ZaakRollenController>(nameof(V1_7.ZaakRollenController.HeadAsync)), null),
        (ScopeMatrix.Name<V1_7.ZaakObjectenController>(nameof(V1_7.ZaakObjectenController.HeadAsync)), null),
        (ScopeMatrix.Name<V1_7.ZaakInformatieObjectenController>(nameof(V1_7.ZaakInformatieObjectenController.HeadAsync)), null),
    ];

    private readonly ZakenWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ZaakSubResourceAuthorizationTests(ZakenApiFixture fixture)
    {
        _factory = fixture.Factory;
        _client = _factory.CreateClient();

        CatalogiStub.InstallOn(_factory);
    }

    public static IEnumerable<object[]> EndpointRows() =>
        Endpoints.SelectMany(e => ScopeMatrix.Named(e.Row).ServedApiVersions.Select(v => new object[] { e.Row, v }));

    public static IEnumerable<object[]> ZaakCreateVersions() => VersionsServing(HttpMethod.Post, V1Routes.Zaken.Create);

    public static IEnumerable<object[]> StatusCreateVersions() => VersionsServing(HttpMethod.Post, V1Routes.ZaakStatussen.Create);

    public static IEnumerable<object[]> ZaakDeleteVersions() => VersionsServing(HttpMethod.Delete, V1Routes.Zaken.Delete);

    [Theory]
    [MemberData(nameof(EndpointRows))]
    public async Task Client_authorized_for_the_zaaktype_is_not_forbidden(string row, string apiVersion)
    {
        var response = await SendAsync(row, apiVersion, ZaakTypes.A, VertrouwelijkheidAanduiding.zeer_geheim);

        Assert.True(response.IsSuccessStatusCode, await ZakenEndpointRequests.DescribeAsync(response));
    }

    [Theory]
    [MemberData(nameof(EndpointRows))]
    public async Task Client_authorized_for_another_zaaktype_only_is_forbidden(string row, string apiVersion)
    {
        var response = await SendAsync(row, apiVersion, ZaakTypes.B, VertrouwelijkheidAanduiding.zeer_geheim);

        Assert.True(response.StatusCode == HttpStatusCode.Forbidden, await ZakenEndpointRequests.DescribeAsync(response));
    }

    [Theory]
    [MemberData(nameof(EndpointRows))]
    public async Task Client_whose_maximum_is_below_the_zaaks_vertrouwelijkheidaanduiding_is_forbidden(string row, string apiVersion)
    {
        var response = await SendAsync(row, apiVersion, ZaakTypes.A, BelowZaak);

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

    [Theory]
    [MemberData(nameof(StatusCreateVersions))]
    public async Task First_status_of_a_zaak_above_the_clients_maximum_vertrouwelijkheidaanduiding_is_not_set(string apiVersion)
    {
        var zaak = await ZakenSeed.SeedZaakAsync(_factory, ZaakTypes.A, ZaakVertrouwelijkheid);
        var clientId = ZrcClients.Authorized(_factory, ZaakTypes.A, BelowZaak, AuthorizationScopes.Zaken.Create);

        var response = await AddStatusAsync(zaak, clientId, apiVersion);

        Assert.True(response.StatusCode == HttpStatusCode.Forbidden, await ZakenEndpointRequests.DescribeAsync(response));
    }

    [Theory]
    [MemberData(nameof(StatusCreateVersions))]
    public async Task First_status_of_a_zaak_within_the_clients_maximum_vertrouwelijkheidaanduiding_is_not_forbidden(string apiVersion)
    {
        var zaak = await ZakenSeed.SeedZaakAsync(_factory, ZaakTypes.A, ZaakVertrouwelijkheid);
        var clientId = ZrcClients.Authorized(_factory, ZaakTypes.A, VertrouwelijkheidAanduiding.zeer_geheim, AuthorizationScopes.Zaken.Create);

        var response = await AddStatusAsync(zaak, clientId, apiVersion);

        Assert.True(response.StatusCode == HttpStatusCode.Created, await ZakenEndpointRequests.DescribeAsync(response));
    }

    [Theory]
    [MemberData(nameof(ZaakDeleteVersions))]
    public async Task Zaak_is_deleted_by_a_client_authorized_for_it_on_every_version(string apiVersion)
    {
        var (response, zaak) = await DeleteZaakAsync(ZaakTypes.A, VertrouwelijkheidAanduiding.zeer_geheim, apiVersion);

        Assert.True(response.StatusCode == HttpStatusCode.NoContent, await ZakenEndpointRequests.DescribeAsync(response));
        Assert.False(await ZakenSeed.ZaakExistsAsync(_factory, zaak.Id));
    }

    [Theory]
    [MemberData(nameof(ZaakDeleteVersions))]
    public async Task Zaak_of_another_zaaktype_is_not_deleted(string apiVersion)
    {
        var (response, zaak) = await DeleteZaakAsync(ZaakTypes.B, VertrouwelijkheidAanduiding.zeer_geheim, apiVersion);

        Assert.True(response.StatusCode == HttpStatusCode.Forbidden, await ZakenEndpointRequests.DescribeAsync(response));
        Assert.True(await ZakenSeed.ZaakExistsAsync(_factory, zaak.Id));
    }

    [Theory]
    [MemberData(nameof(ZaakDeleteVersions))]
    public async Task Zaak_above_the_clients_maximum_vertrouwelijkheidaanduiding_is_not_deleted(string apiVersion)
    {
        var (response, zaak) = await DeleteZaakAsync(ZaakTypes.A, BelowZaak, apiVersion);

        Assert.True(response.StatusCode == HttpStatusCode.Forbidden, await ZakenEndpointRequests.DescribeAsync(response));
        Assert.True(await ZakenSeed.ZaakExistsAsync(_factory, zaak.Id));
    }

    // Every version any controller serves the operation on, so a theory follows the routing instead of a hand-kept list.
    private static IEnumerable<object[]> VersionsServing(HttpMethod method, string route) =>
        ScopeMatrix
            .Rows.Where(r => r.Method == method && r.Route == route)
            .SelectMany(r => r.ServedApiVersions)
            .Distinct()
            .Order(StringComparer.Ordinal)
            .Select(v => new object[] { v });

    private async Task<(HttpResponseMessage Response, SeededZaak Zaak)> DeleteZaakAsync(
        string clientZaakType,
        VertrouwelijkheidAanduiding clientMaximum,
        string apiVersion
    )
    {
        var matrixRow = ScopeMatrix.Named(ScopeMatrix.Name<V1.ZakenController>(nameof(V1.ZakenController.DeleteAsync))) with
        {
            ApiVersion = apiVersion,
        };
        var zaak = await ZakenSeed.SeedZaakAsync(_factory, ZaakTypes.A, ZaakVertrouwelijkheid);
        var clientId = ZrcClients.Authorized(_factory, clientZaakType, clientMaximum, matrixRow.Scopes);

        return (await _client.SendAsync(ZakenEndpointRequests.For(matrixRow, zaak, clientId)), zaak);
    }

    private async Task<HttpResponseMessage> AddStatusAsync(SeededZaak zaak, string clientId, string apiVersion)
    {
        var body = ZakenEndpointRequests.Status(zaak, zaak.Catalogi.BeginStatusType);

        return await _client.SendAsync(
            ZakenEndpointRequests.Create(HttpMethod.Post, "/" + V1Routes.ZaakStatussen.Create, apiVersion, clientId, zaak.Rsin, body)
        );
    }

    private async Task<HttpResponseMessage> SendAsync(string row, string apiVersion, string clientZaakType, VertrouwelijkheidAanduiding clientMaximum)
    {
        var matrixRow = ScopeMatrix.Named(row) with { ApiVersion = apiVersion };
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

        return await _client.SendAsync(
            ZakenEndpointRequests.Create(HttpMethod.Post, "/" + V1Routes.Zaken.Create, apiVersion, clientId, TestRsins.A, body)
        );
    }
}
