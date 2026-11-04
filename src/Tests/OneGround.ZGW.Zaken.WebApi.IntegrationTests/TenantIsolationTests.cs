using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using OneGround.ZGW.Common.DataModel;
using OneGround.ZGW.IntegrationTests.Common;
using OneGround.ZGW.IntegrationTests.Common.Authentication;
using Xunit;

namespace OneGround.ZGW.Zaken.WebApi.IntegrationTests;

/// <summary>
/// A client of another organisation, holding every scope for the zaak's zaaktype, can neither reach, change nor list a zaak of <see cref="TestRsins.A"/>.
/// </summary>
[Collection(ZakenApiCollection.Name)]
public class TenantIsolationTests(ZakenApiFixture fixture) : ZakenApiTestBase(fixture)
{
    private const string OtherRsin = TestRsins.B;

    public static IEnumerable<object[]> ActionsOnOneZaak() =>
        WithServedVersions(
            ZaakSubResourceAuthorizationTests
                .Endpoints.Select(e => e.Row)
                .Where(r => ScopeMatrix.Named(r).Method != HttpMethod.Post)
                .Concat(ZaakSubResourceListAuthorizationTests.ListsOfOneZaak)
        );

    public static IEnumerable<object[]> CreatesUnderOneZaak() =>
        WithServedVersions(ZaakSubResourceAuthorizationTests.Endpoints.Select(e => e.Row).Where(r => ScopeMatrix.Named(r).Method == HttpMethod.Post));

    public static IEnumerable<object[]> FilteredLists() => WithServedVersions(ZaakSubResourceListAuthorizationTests.FilteredLists);

    [Theory]
    [MemberData(nameof(ActionsOnOneZaak))]
    public async Task Action_on_a_zaak_of_another_organisation_is_not_found(string row, string apiVersion)
    {
        var (response, before, after) = await SendAsOtherOrganisationAsync(row, apiVersion);

        Assert.True(response.StatusCode == HttpStatusCode.NotFound, await ZakenEndpointRequests.DescribeAsync(response));
        Assert.Equal(before, after);
    }

    [Theory]
    [MemberData(nameof(CreatesUnderOneZaak))]
    public async Task Create_under_a_zaak_of_another_organisation_is_refused_as_an_unknown_zaak(string row, string apiVersion)
    {
        var (response, before, after) = await SendAsOtherOrganisationAsync(row, apiVersion);

        Assert.True(response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.NotFound, await ZakenEndpointRequests.DescribeAsync(response));
        Assert.Equal(before, after);
    }

    [Theory]
    [MemberData(nameof(FilteredLists))]
    public async Task Zaak_of_another_organisation_is_not_listed(string row, string apiVersion)
    {
        var (response, before, after, zaak) = await SendAsOtherOrganisationWithZaakAsync(row, apiVersion);
        var body = await response.Content.ReadAsStringAsync();

        Assert.True(response.StatusCode == HttpStatusCode.OK, await ZakenEndpointRequests.DescribeAsync(response));
        Assert.DoesNotContain(zaak.Id.ToString(), body);
        Assert.DoesNotContain(zaak.Identificatie, body);
        Assert.Equal(before, after);
    }

    private async Task<(HttpResponseMessage Response, string Before, string After)> SendAsOtherOrganisationAsync(string row, string apiVersion)
    {
        var (response, before, after, _) = await SendAsOtherOrganisationWithZaakAsync(row, apiVersion);

        return (response, before, after);
    }

    private async Task<(HttpResponseMessage Response, string Before, string After, SeededZaak Zaak)> SendAsOtherOrganisationWithZaakAsync(
        string row,
        string apiVersion
    )
    {
        var matrixRow = ScopeMatrix.Named(row) with { ApiVersion = apiVersion };
        var seeds = ZaakSubResourceAuthorizationTests.Endpoints.Where(e => e.Row == row).Select(e => e.Seeds).FirstOrDefault() ?? matrixRow.Seeds;

        var zaak = await ZakenSeed.SeedZaakAsync(Factory, resource: seeds);
        var clientId = ZrcClients.Authorized(Factory, zaak.ZaakType, VertrouwelijkheidAanduiding.zeer_geheim, ZrcClients.AllScopes);

        // The request targets the zaak's own URLs; only the organisation the client logs in for differs.
        var request = ZakenEndpointRequests.For(matrixRow, zaak, clientId);
        request.Headers.Authorization = TestIdentity.Create(clientId, OtherRsin);

        var before = await ZakenSeed.ZaakStateAsync(Factory, zaak.Id);
        var response = await Client.SendAsync(request);

        return (response, before, await ZakenSeed.ZaakStateAsync(Factory, zaak.Id), zaak);
    }

    private static IEnumerable<object[]> WithServedVersions(IEnumerable<string> rows) =>
        rows.SelectMany(r => ScopeMatrix.Named(r).ServedApiVersions.Select(v => new object[] { r, v }));
}
