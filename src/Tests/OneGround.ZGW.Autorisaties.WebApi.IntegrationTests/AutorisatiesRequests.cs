using System;
using System.Net.Http;
using OneGround.ZGW.Autorisaties.Web.Contracts.v1;
using OneGround.ZGW.Autorisaties.Web.Controllers;
using OneGround.ZGW.IntegrationTests.Common.Authentication;

namespace OneGround.ZGW.Autorisaties.WebApi.IntegrationTests;

/// <summary>
/// Requests on the v1 routes, pinned to API version 1.0; a <c>null</c> client id and rsin send no Authorization header.
/// </summary>
internal static class AutorisatiesRequests
{
    public static HttpRequestMessage GetAllApplicaties(string clientId, string rsin)
    {
        return Create("/" + ApiRoutes.Applicaties.GetAll, clientId, rsin);
    }

    public static HttpRequestMessage GetApplicatie(Guid id, string clientId, string rsin)
    {
        return Create("/" + ApiRoutes.Applicaties.Get.Replace("{id}", id.ToString()), clientId, rsin);
    }

    private static HttpRequestMessage Create(string path, string clientId, string rsin)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add("Api-Version", Api.LatestVersion_1_0);

        if (clientId != null || rsin != null)
        {
            request.Headers.Authorization = TestIdentity.Create(clientId, rsin);
        }

        return request;
    }
}
