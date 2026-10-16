using System;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using OneGround.ZGW.IntegrationTests.Common.Authentication;
using OneGround.ZGW.Zaken.Web.Contracts.v1;
using OneGround.ZGW.Zaken.Web.Controllers;

namespace OneGround.ZGW.Zaken.WebApi.IntegrationTests;

/// <summary>
/// Requests on the v1 routes, pinned to API version 1.0 unless given; a <c>null</c> client id and rsin send no Authorization header.
/// </summary>
internal static class ZakenRequests
{
    public static HttpRequestMessage GetAllZaken(string clientId, string rsin)
    {
        return Create(HttpMethod.Get, "/" + ApiRoutes.Zaken.GetAll, clientId, rsin);
    }

    public static HttpRequestMessage GetZaak(Guid id, string clientId, string rsin)
    {
        return Create(HttpMethod.Get, "/" + ApiRoutes.Zaken.Get.Replace("{id}", id.ToString()), clientId, rsin);
    }

    public static HttpRequestMessage DeleteZaak(Guid id, string clientId, string rsin, string apiVersion)
    {
        return Create(HttpMethod.Delete, "/" + ApiRoutes.Zaken.Delete.Replace("{id}", id.ToString()), clientId, rsin, apiVersion);
    }

    public static HttpRequestMessage CreateZaakStatus(object body, string clientId, string rsin, string apiVersion)
    {
        return WithJson(Create(HttpMethod.Post, "/" + ApiRoutes.ZaakStatussen.Create, clientId, rsin, apiVersion), body);
    }

    public static HttpRequestMessage CreateZaakObject(object body, string clientId, string rsin, string apiVersion)
    {
        return WithJson(Create(HttpMethod.Post, "/" + ApiRoutes.ZaakObjecten.Create, clientId, rsin, apiVersion), body);
    }

    private static HttpRequestMessage Create(HttpMethod method, string path, string clientId, string rsin, string apiVersion = Api.LatestVersion_1_0)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Add("Api-Version", apiVersion);
        request.Headers.Add("Accept-Crs", "EPSG:4326");

        if (clientId != null || rsin != null)
        {
            request.Headers.Authorization = TestIdentity.Create(clientId, rsin);
        }

        return request;
    }

    private static HttpRequestMessage WithJson(HttpRequestMessage request, object body)
    {
        request.Content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
        request.Content.Headers.Add("Content-Crs", "EPSG:4326");

        return request;
    }
}
