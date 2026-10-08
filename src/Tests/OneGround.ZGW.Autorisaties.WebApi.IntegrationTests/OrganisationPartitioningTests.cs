using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using OneGround.ZGW.Autorisaties.Web.Controllers;
using OneGround.ZGW.IntegrationTests.Common;
using Xunit;
using static OneGround.ZGW.Autorisaties.WebApi.IntegrationTests.AutorisatiesSeed;

namespace OneGround.ZGW.Autorisaties.WebApi.IntegrationTests;

/// <summary>
/// What a client authorized in one organisation sees and changes of the applicaties of that organisation and of another one.
/// </summary>
[Collection(AutorisatiesApiCollection.Name)]
public class OrganisationPartitioningTests
{
    private const string OwnRsin = TestRsins.B;
    private const string OtherRsin = TestRsins.C;

    private readonly AutorisatiesWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public OrganisationPartitioningTests(AutorisatiesApiFixture fixture)
    {
        _factory = fixture.Factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task List_contains_only_applicaties_owned_by_the_organisation_of_the_request()
    {
        var clientId = NewClientId();
        var otherClientId = NewClientId();
        var own = await ApplicatieAsync(_factory, OwnRsin, clientId, heeftAlleAutorisaties: true);
        await ApplicatieAsync(_factory, OtherRsin, otherClientId, heeftAlleAutorisaties: true);

        var response = await _client.SendAsync(
            AutorisatiesRequests.GetAllApplicaties(clientId, OwnRsin, query: $"?clientIds={clientId},{otherClientId}")
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var (count, labels) = await ReadListAsync(response);
        Assert.Equal(1, count);
        Assert.Equal([own.Label], labels);
        AssertNoConsumerLookup(_factory, clientId);
    }

    [Theory]
    [InlineData(Api.LatestVersion_1_0)]
    [InlineData(Api.LatestVersion_1_1)]
    public async Task Applicatie_of_another_organisation_is_not_found(string apiVersion)
    {
        var clientId = NewClientId();
        var own = await ApplicatieAsync(_factory, OwnRsin, clientId, heeftAlleAutorisaties: true);
        var other = await ApplicatieAsync(_factory, OtherRsin, NewClientId(), heeftAlleAutorisaties: true);

        var ownResponse = await _client.SendAsync(AutorisatiesRequests.GetApplicatie(own.Id, clientId, OwnRsin, apiVersion));
        var otherResponse = await _client.SendAsync(AutorisatiesRequests.GetApplicatie(other.Id, clientId, OwnRsin, apiVersion));

        Assert.Equal(HttpStatusCode.OK, ownResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, otherResponse.StatusCode);
        AssertNoConsumerLookup(_factory, clientId);
    }

    [Fact]
    public async Task Consumer_of_another_organisation_is_not_found()
    {
        var clientId = NewClientId();
        var otherClientId = NewClientId();
        await ApplicatieAsync(_factory, OwnRsin, clientId, heeftAlleAutorisaties: true);
        await ApplicatieAsync(_factory, OtherRsin, otherClientId, heeftAlleAutorisaties: true);

        var ownResponse = await _client.SendAsync(AutorisatiesRequests.GetApplicatieByConsumer(clientId, clientId, OwnRsin));
        var otherResponse = await _client.SendAsync(AutorisatiesRequests.GetApplicatieByConsumer(otherClientId, clientId, OwnRsin));

        Assert.Equal(HttpStatusCode.OK, ownResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, otherResponse.StatusCode);
    }

    [Theory]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    [InlineData("DELETE")]
    public async Task Applicatie_of_another_organisation_cannot_be_changed(string method)
    {
        var clientId = NewClientId();
        await ApplicatieAsync(_factory, OwnRsin, clientId, heeftAlleAutorisaties: true);
        var other = await ApplicatieAsync(_factory, OtherRsin, NewClientId(), heeftAlleAutorisaties: true);

        var response = await _client.SendAsync(AutorisatiesRequests.Write(method, other.Id, clientId, OwnRsin));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var stored = await FindAsync(_factory, other.Id);
        Assert.NotNull(stored);
        Assert.Equal(other.Label, stored.Label);
        Assert.Equal(other.ClientIds.Single().ClientId, stored.ClientIds.Single().ClientId);
    }

    private static async Task<(int Count, string[] Labels)> ReadListAsync(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        var labels = document.RootElement.GetProperty("results").EnumerateArray().Select(a => a.GetProperty("label").GetString()).ToArray();

        return (document.RootElement.GetProperty("count").GetInt32(), labels);
    }
}
