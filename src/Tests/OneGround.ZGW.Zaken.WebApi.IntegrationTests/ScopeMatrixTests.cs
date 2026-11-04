using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using OneGround.ZGW.Common.DataModel;
using Xunit;

namespace OneGround.ZGW.Zaken.WebApi.IntegrationTests;

/// <summary>
/// Per <see cref="ScopeMatrix"/> row, the scope filter denies a client holding every scope but the action's and lets one holding a single accepted scope through.
/// </summary>
[Collection(ZakenApiCollection.Name)]
public class ScopeMatrixTests(ZakenApiFixture fixture) : ZakenApiTestBase(fixture)
{
    private const VertrouwelijkheidAanduiding Maximum = VertrouwelijkheidAanduiding.zeer_geheim;

    public static IEnumerable<object[]> Rows() => ScopeMatrix.Rows.SelectMany(r => r.ServedApiVersions.Select(v => new object[] { r.Name, v }));

    public static IEnumerable<object[]> AcceptedScopes() =>
        ScopeMatrix.Rows.SelectMany(r => r.ServedApiVersions.SelectMany(v => r.Scopes.Select(s => new object[] { r.Name, v, s })));

    [Theory]
    [MemberData(nameof(Rows))]
    public async Task Client_with_every_scope_except_the_actions_is_forbidden(string row, string apiVersion)
    {
        var matrixRow = ScopeMatrix.Named(row) with { ApiVersion = apiVersion };
        var clientId = ZrcClients.AuthorizedForAllScopesExcept(Factory, ZaakTypes.A, Maximum, matrixRow.Scopes);

        var response = await SendAsync(matrixRow, clientId);

        Assert.True(response.StatusCode == HttpStatusCode.Forbidden, await ZakenEndpointRequests.DescribeAsync(response));
    }

    [Theory]
    [MemberData(nameof(AcceptedScopes))]
    public async Task Client_with_only_one_of_the_actions_scopes_gets_past_the_scope_filter(string row, string apiVersion, string scope)
    {
        var matrixRow = ScopeMatrix.Named(row) with { ApiVersion = apiVersion };
        var clientId = ZrcClients.Authorized(Factory, ZaakTypes.A, Maximum, scope);

        var response = await SendAsync(matrixRow, clientId);

        Assert.True(
            response.StatusCode is not (HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden) && (int)response.StatusCode < 500,
            await ZakenEndpointRequests.DescribeAsync(response)
        );
    }

    // Request validation runs before the scope filter, so a 403 in the denied cell also shows the shared request is well-formed.
    private async Task<HttpResponseMessage> SendAsync(ScopeMatrixRow row, string clientId)
    {
        var zaak = await ZakenSeed.SeedZaakAsync(Factory, resource: row.Seeds);

        return await Client.SendAsync(ZakenEndpointRequests.For(row, zaak, clientId));
    }
}
