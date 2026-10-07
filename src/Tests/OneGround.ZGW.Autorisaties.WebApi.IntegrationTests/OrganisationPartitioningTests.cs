using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Autorisaties.DataModel;
using OneGround.ZGW.IntegrationTests.Common;
using Xunit;

namespace OneGround.ZGW.Autorisaties.WebApi.IntegrationTests;

/// <summary>
/// What a client authorized in one organisation sees of the applicaties of that organisation and of another one.
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
        var own = await SeedAsync(OwnRsin, clientId);
        var other = await SeedAsync(OtherRsin, NewClientId());

        var response = await _client.SendAsync(AutorisatiesRequests.GetAllApplicaties(clientId, OwnRsin));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var labels = await ReadLabelsAsync(response);
        Assert.Contains(own.Label, labels);
        Assert.DoesNotContain(other.Label, labels);
        AssertNoConsumerLookup(clientId);
    }

    [Fact]
    public async Task Applicatie_of_another_organisation_is_not_found()
    {
        var clientId = NewClientId();
        var own = await SeedAsync(OwnRsin, clientId);
        var other = await SeedAsync(OtherRsin, NewClientId());

        var ownResponse = await _client.SendAsync(AutorisatiesRequests.GetApplicatie(own.Id, clientId, OwnRsin));
        var otherResponse = await _client.SendAsync(AutorisatiesRequests.GetApplicatie(other.Id, clientId, OwnRsin));

        Assert.Equal(HttpStatusCode.OK, ownResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, otherResponse.StatusCode);
        AssertNoConsumerLookup(clientId);
    }

    private static async Task<List<string>> ReadLabelsAsync(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        return document.RootElement.GetProperty("results").EnumerateArray().Select(a => a.GetProperty("label").GetString()).ToList();
    }

    private void AssertNoConsumerLookup(string clientId)
    {
        Assert.DoesNotContain(
            _factory.OutboundHttp.Requests,
            r =>
                r.RequestUri!.AbsolutePath.EndsWith("/applicaties/consumer", StringComparison.OrdinalIgnoreCase)
                && r.RequestUri.Query.Contains($"clientId={clientId}", StringComparison.OrdinalIgnoreCase)
        );
    }

    private async Task<Applicatie> SeedAsync(string owner, string clientId)
    {
        var applicatie = new Applicatie
        {
            Id = Guid.NewGuid(),
            Owner = owner,
            Label = $"integration-test-{Guid.NewGuid():N}",
            CreationTime = DateTime.UtcNow,
            HeeftAlleAutorisaties = true,
            ClientIds =
            [
                new ApplicatieClient
                {
                    Id = Guid.NewGuid(),
                    ClientId = clientId,
                    CreationTime = DateTime.UtcNow,
                },
            ],
            Autorisaties = [],
        };

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AcDbContext>();
        context.Applicaties.Add(applicatie);
        await context.SaveChangesAsync();

        return applicatie;
    }

    private static string NewClientId() => $"integration-test-{Guid.NewGuid():N}";
}
