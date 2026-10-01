using System;
using System.Net.Http;
using OneGround.ZGW.IntegrationTests.Common.Authentication;
using OneGround.ZGW.Zaken.Web.Contracts.v1;
using OneGround.ZGW.Zaken.Web.Controllers;

namespace OneGround.ZGW.Zaken.WebApi.IntegrationTests;

/// <summary>
/// Requests on the v1 routes, pinned to API version 1.0; a <c>null</c> client id and rsin send no Authorization header.
/// </summary>
internal static class ZakenRequests
{
    public static HttpRequestMessage GetAllZaken(string clientId, string rsin)
    {
        return Create("/" + ApiRoutes.Zaken.GetAll, clientId, rsin);
    }

    public static HttpRequestMessage GetZaak(Guid id, string clientId, string rsin)
    {
        return Create("/" + ApiRoutes.Zaken.Get.Replace("{id}", id.ToString()), clientId, rsin);
    }

    private static HttpRequestMessage Create(string path, string clientId, string rsin)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add("Api-Version", Api.LatestVersion_1_0);
        request.Headers.Add("Accept-Crs", "EPSG:4326");

        if (clientId != null || rsin != null)
        {
            request.Headers.Authorization = TestIdentity.Create(clientId, rsin);
        }

        return request;
    }
}
