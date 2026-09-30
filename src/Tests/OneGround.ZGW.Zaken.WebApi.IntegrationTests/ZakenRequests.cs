using System.Net.Http;
using OneGround.ZGW.IntegrationTests.Common.Authentication;
using OneGround.ZGW.Zaken.Web.Contracts.v1;
using OneGround.ZGW.Zaken.Web.Controllers;

namespace OneGround.ZGW.Zaken.WebApi.IntegrationTests;

internal static class ZakenRequests
{
    /// <summary>
    /// GET on the v1 <see cref="ApiRoutes.Zaken.GetAll"/> route, pinned to API version 1.0 so it is served by the v1 controller.
    /// Pass <c>null</c> for <paramref name="clientId"/> and <paramref name="rsin"/> to send no Authorization header at all.
    /// </summary>
    public static HttpRequestMessage GetAllZaken(string clientId, string rsin)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/" + ApiRoutes.Zaken.GetAll);
        request.Headers.Add("Api-Version", Api.LatestVersion_1_0);
        request.Headers.Add("Accept-Crs", "EPSG:4326");

        if (clientId != null || rsin != null)
        {
            request.Headers.Authorization = TestIdentity.Create(clientId, rsin);
        }

        return request;
    }
}
