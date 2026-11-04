using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using OneGround.ZGW.Common.DataModel;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Zaken.Web.Controllers;
using Xunit;
using V1 = OneGround.ZGW.Zaken.Web.Controllers.v1;
using V1_5 = OneGround.ZGW.Zaken.Web.Controllers.v1._5;
using V1_7 = OneGround.ZGW.Zaken.Web.Controllers.v1._7;
using V1Routes = OneGround.ZGW.Zaken.Web.Contracts.v1.ApiRoutes;

namespace OneGround.ZGW.Zaken.WebApi.IntegrationTests;

/// <summary>
/// What a scope allows beyond the scope filter, decided in the handlers: modifying a closed zaak, setting statuses and reopening a zaak.
/// </summary>
[Collection(ZakenApiCollection.Name)]
public class ZaakScopeSemanticsTests
{
    private const VertrouwelijkheidAanduiding Maximum = VertrouwelijkheidAanduiding.zeer_geheim;

    private readonly ZakenWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ZaakScopeSemanticsTests(ZakenApiFixture fixture)
    {
        _factory = fixture.Factory;
        _client = _factory.CreateClient();

        CatalogiStub.InstallOn(_factory);
    }

    public static IEnumerable<object[]> ClosedZaakModifications() =>
        [
            [ScopeMatrix.Name<V1.ZakenController>(nameof(V1.ZakenController.UpdateAsync))],
            [ScopeMatrix.Name<V1.ZakenController>(nameof(V1.ZakenController.PartialUpdateAsync))],
            [ScopeMatrix.Name<V1.ZaakRollenController>(nameof(V1.ZaakRollenController.AddAsync))],
            [ScopeMatrix.Name<V1_5.ZakenController>(nameof(V1_5.ZakenController.UpdateAsync))],
            [ScopeMatrix.Name<V1_5.ZakenController>(nameof(V1_5.ZakenController.PartialUpdateAsync))],
            [ScopeMatrix.Name<V1_5.ZaakRollenController>(nameof(V1_5.ZaakRollenController.AddAsync))],
            [ScopeMatrix.Name<V1_7.ZakenController>(nameof(V1_7.ZakenController.UpdateAsync))],
            [ScopeMatrix.Name<V1_7.ZakenController>(nameof(V1_7.ZakenController.PartialUpdateAsync))],
            [ScopeMatrix.Name<V1_7.ZaakRollenController>(nameof(V1_7.ZaakRollenController.AddAsync))],
        ];

    [Theory]
    [MemberData(nameof(ClosedZaakModifications))]
    public async Task Closed_zaak_is_not_modified_with_only_the_update_scope(string row)
    {
        var response = await ModifyClosedZaakAsync(row, AuthorizationScopes.Zaken.Update);

        Assert.True(response.StatusCode == HttpStatusCode.Forbidden, await ZakenEndpointRequests.DescribeAsync(response));
    }

    [Theory]
    [MemberData(nameof(ClosedZaakModifications))]
    public async Task Closed_zaak_is_modified_with_the_forced_update_scope(string row)
    {
        var response = await ModifyClosedZaakAsync(row, AuthorizationScopes.Zaken.ForcedUpdate);

        Assert.True(response.IsSuccessStatusCode, await ZakenEndpointRequests.DescribeAsync(response));
    }

    [Theory]
    [MemberData(nameof(ZaakSubResourceAuthorizationTests.StatusCreateVersions), MemberType = typeof(ZaakSubResourceAuthorizationTests))]
    public async Task First_status_is_set_with_only_the_create_scope(string apiVersion)
    {
        var zaak = await ZakenSeed.SeedZaakAsync(_factory);
        var clientId = ZrcClients.Authorized(_factory, ZaakTypes.A, Maximum, AuthorizationScopes.Zaken.Create);

        var response = await AddStatusAsync(zaak, zaak.Catalogi.BeginStatusType, clientId, apiVersion);

        Assert.True(response.StatusCode == HttpStatusCode.Created, await ZakenEndpointRequests.DescribeAsync(response));
    }

    [Theory]
    [MemberData(nameof(ZaakSubResourceAuthorizationTests.StatusCreateVersions), MemberType = typeof(ZaakSubResourceAuthorizationTests))]
    public async Task Second_status_is_not_set_with_only_the_create_scope(string apiVersion)
    {
        var zaak = await ZakenSeed.SeedZaakAsync(_factory);
        var clientId = ZrcClients.Authorized(_factory, ZaakTypes.A, Maximum, AuthorizationScopes.Zaken.Create);

        var first = await AddStatusAsync(zaak, zaak.Catalogi.BeginStatusType, clientId, apiVersion, minutesAgo: 2);
        var second = await AddStatusAsync(zaak, zaak.Catalogi.BeginStatusType, clientId, apiVersion, minutesAgo: 1);

        Assert.True(first.StatusCode == HttpStatusCode.Created, await ZakenEndpointRequests.DescribeAsync(first));
        Assert.True(second.StatusCode == HttpStatusCode.Forbidden, await ZakenEndpointRequests.DescribeAsync(second));
        Assert.Equal(1, await ZakenSeed.ZaakStatusCountAsync(_factory, zaak.Id));
    }

    [Theory]
    [MemberData(nameof(ZaakSubResourceAuthorizationTests.StatusCreateVersions), MemberType = typeof(ZaakSubResourceAuthorizationTests))]
    public async Task Second_status_is_set_with_the_add_status_scope(string apiVersion)
    {
        var zaak = await ZakenSeed.SeedZaakAsync(_factory, resource: ZaakResource.Status);
        var clientId = ZrcClients.Authorized(_factory, ZaakTypes.A, Maximum, AuthorizationScopes.Zaken.Statuses.Add);

        var response = await AddStatusAsync(zaak, zaak.Catalogi.BeginStatusType, clientId, apiVersion);

        Assert.True(response.StatusCode == HttpStatusCode.Created, await ZakenEndpointRequests.DescribeAsync(response));
    }

    [Theory]
    [MemberData(nameof(ZaakSubResourceAuthorizationTests.StatusCreateVersions), MemberType = typeof(ZaakSubResourceAuthorizationTests))]
    public async Task Closed_zaak_is_not_reopened_with_only_the_add_status_scope(string apiVersion)
    {
        var zaak = await ZakenSeed.SeedZaakAsync(_factory, closed: true, resource: ZaakResource.Status);
        var clientId = ZrcClients.Authorized(_factory, ZaakTypes.A, Maximum, AuthorizationScopes.Zaken.Statuses.Add);

        var response = await AddStatusAsync(zaak, zaak.Catalogi.BeginStatusType, clientId, apiVersion);

        Assert.True(response.StatusCode == HttpStatusCode.Forbidden, await ZakenEndpointRequests.DescribeAsync(response));
    }

    [Theory]
    [MemberData(nameof(ZaakSubResourceAuthorizationTests.StatusCreateVersions), MemberType = typeof(ZaakSubResourceAuthorizationTests))]
    public async Task Closed_zaak_is_reopened_with_the_reopen_scope(string apiVersion)
    {
        var zaak = await ZakenSeed.SeedZaakAsync(_factory, closed: true, resource: ZaakResource.Status);
        var clientId = ZrcClients.Authorized(_factory, ZaakTypes.A, Maximum, AuthorizationScopes.Zaken.Reopen);

        var response = await AddStatusAsync(zaak, zaak.Catalogi.BeginStatusType, clientId, apiVersion);

        Assert.True(response.StatusCode == HttpStatusCode.Created, await ZakenEndpointRequests.DescribeAsync(response));
    }

    private async Task<HttpResponseMessage> ModifyClosedZaakAsync(string row, string scope)
    {
        var matrixRow = ScopeMatrix.Named(row);
        var zaak = await ZakenSeed.SeedZaakAsync(_factory, closed: true);
        var clientId = ZrcClients.Authorized(_factory, ZaakTypes.A, Maximum, scope);

        return await _client.SendAsync(ZakenEndpointRequests.For(matrixRow, zaak, clientId));
    }

    private async Task<HttpResponseMessage> AddStatusAsync(SeededZaak zaak, string statusType, string clientId, string apiVersion, int minutesAgo = 1)
    {
        var body = ZakenEndpointRequests.Status(zaak, statusType, DateTime.UtcNow.AddMinutes(-minutesAgo));

        return await _client.SendAsync(
            ZakenEndpointRequests.Create(HttpMethod.Post, "/" + V1Routes.ZaakStatussen.Create, apiVersion, clientId, zaak.Rsin, body)
        );
    }
}
