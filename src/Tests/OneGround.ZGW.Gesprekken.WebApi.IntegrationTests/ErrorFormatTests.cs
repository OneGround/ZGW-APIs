using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using OneGround.ZGW.Gesprekken.Web.Controllers;
using OneGround.ZGW.IntegrationTests.Common;
using OneGround.ZGW.IntegrationTests.Common.Authentication;
using Xunit;

namespace OneGround.ZGW.Gesprekken.WebApi.IntegrationTests;

/// <summary>
/// Error responses are application/problem+json with the members of the shared Fout schema.
/// </summary>
[Collection(GesprekkenApiCollection.Name)]
public class ErrorFormatTests
{
    private readonly GesprekkenWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ErrorFormatTests(GesprekkenApiFixture fixture)
    {
        _factory = fixture.Factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task A_business_rule_violation_is_problem_json()
    {
        var response = await SendAsync(HttpMethod.Post, "/api/v1/gesprekken", AllowedClient(), FlowBody.NewGesprek(_factory.NewZaak(closed: true)));

        await AssertProblemAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task A_validation_error_is_problem_json()
    {
        var response = await SendAsync(HttpMethod.Post, "/api/v1/gesprekken", AllowedClient(), new JObject());

        var problem = await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.NotEmpty(problem["invalidParams"]!);
    }

    [Fact]
    public async Task An_unknown_gesprek_is_problem_json()
    {
        var response = await SendAsync(HttpMethod.Get, $"/api/v1/gesprekken/{Guid.NewGuid()}", AllowedClient());

        await AssertProblemAsync(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task A_missing_scope_is_problem_json()
    {
        var clientId = $"integration-test-{Guid.NewGuid():N}";
        _factory.AutorisatiesApi.GrantNoAuthorizations(clientId);

        var response = await SendAsync(HttpMethod.Get, "/api/v1/gesprekken", clientId);

        await AssertProblemAsync(response, HttpStatusCode.Forbidden);
    }

    private static async Task<JObject> AssertProblemAsync(HttpResponseMessage response, HttpStatusCode status)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        var problem = JObject.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal((int)status, problem.Value<int>("status"));
        Assert.NotNull(problem.Value<string>("title"));
        return problem;
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, string clientId, JObject body = null)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Add("Api-Version", Api.LatestVersion_1_0);
        request.Headers.Authorization = TestIdentity.Create(clientId, TestRsins.A);
        if (body != null)
            request.Content = new StringContent(body.ToString(), Encoding.UTF8, new MediaTypeHeaderValue("application/json"));
        return await _client.SendAsync(request);
    }

    private string AllowedClient()
    {
        var clientId = $"integration-test-{Guid.NewGuid():N}";
        _factory.AutorisatiesApi.GrantAllAuthorizations(clientId);
        return clientId;
    }
}
