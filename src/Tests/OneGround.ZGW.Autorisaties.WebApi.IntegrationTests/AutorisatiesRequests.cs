using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using OneGround.ZGW.Autorisaties.Web.Contracts.v1;
using OneGround.ZGW.Autorisaties.Web.Controllers;
using OneGround.ZGW.IntegrationTests.Common.Authentication;

namespace OneGround.ZGW.Autorisaties.WebApi.IntegrationTests;

/// <summary>
/// Requests on the v1 routes, pinned to API version 1.0 unless given; a <c>null</c> client id and rsin send no Authorization header.
/// </summary>
internal static class AutorisatiesRequests
{
    public static HttpRequestMessage GetAllApplicaties(string clientId, string rsin, string apiVersion = Api.LatestVersion_1_0, string query = "")
    {
        return Create(HttpMethod.Get, "/" + ApiRoutes.Applicaties.GetAll + query, clientId, rsin, apiVersion);
    }

    public static HttpRequestMessage GetApplicatie(Guid id, string clientId, string rsin, string apiVersion = Api.LatestVersion_1_0)
    {
        return Create(HttpMethod.Get, ItemPath(id), clientId, rsin, apiVersion);
    }

    public static HttpRequestMessage GetApplicatieByConsumer(string consumerClientId, string clientId, string rsin)
    {
        return Create(HttpMethod.Get, $"/{ApiRoutes.Applicaties.GetByConsumer}?clientId={Uri.EscapeDataString(consumerClientId)}", clientId, rsin);
    }

    public static HttpRequestMessage CreateApplicatie(string clientId, string rsin)
    {
        return Create(HttpMethod.Post, "/" + ApiRoutes.Applicaties.Create, clientId, rsin, body: NewApplicatieBody());
    }

    public static HttpRequestMessage UpdateApplicatie(Guid id, string clientId, string rsin)
    {
        return Create(HttpMethod.Put, ItemPath(id), clientId, rsin, body: NewApplicatieBody());
    }

    public static HttpRequestMessage PartialUpdateApplicatie(Guid id, string clientId, string rsin)
    {
        return Create(HttpMethod.Patch, ItemPath(id), clientId, rsin, body: new { label = $"integration-test-{Guid.NewGuid():N}" });
    }

    public static HttpRequestMessage DeleteApplicatie(Guid id, string clientId, string rsin)
    {
        return Create(HttpMethod.Delete, ItemPath(id), clientId, rsin);
    }

    public static HttpRequestMessage Write(string method, Guid id, string clientId, string rsin)
    {
        return method switch
        {
            "POST" => CreateApplicatie(clientId, rsin),
            "PUT" => UpdateApplicatie(id, clientId, rsin),
            "PATCH" => PartialUpdateApplicatie(id, clientId, rsin),
            "DELETE" => DeleteApplicatie(id, clientId, rsin),
            _ => throw new ArgumentOutOfRangeException(nameof(method), method, null),
        };
    }

    private static string ItemPath(Guid id) => "/" + ApiRoutes.Applicaties.Get.Replace("{id}", id.ToString());

    private static object NewApplicatieBody()
    {
        return new
        {
            clientIds = new[] { AutorisatiesSeed.NewClientId() },
            label = $"integration-test-{Guid.NewGuid():N}",
            heeftAlleAutorisaties = true,
            autorisaties = Array.Empty<object>(),
        };
    }

    private static HttpRequestMessage Create(
        HttpMethod method,
        string path,
        string clientId,
        string rsin,
        string apiVersion = Api.LatestVersion_1_0,
        object body = null
    )
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Add("Api-Version", apiVersion);

        if (body != null)
        {
            request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        }

        if (clientId != null || rsin != null)
        {
            request.Headers.Authorization = TestIdentity.Create(clientId, rsin);
        }

        return request;
    }
}
