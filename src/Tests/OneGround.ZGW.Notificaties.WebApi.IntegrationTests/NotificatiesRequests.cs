using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using OneGround.ZGW.IntegrationTests.Common.Authentication;
using OneGround.ZGW.Notificaties.Contracts.v1;
using OneGround.ZGW.Notificaties.Web.Controllers;

namespace OneGround.ZGW.Notificaties.WebApi.IntegrationTests;

/// <summary>
/// Well-formed requests on the v1 routes; a <c>null</c> client id and rsin send no Authorization header.
/// </summary>
internal static class NotificatiesRequests
{
    /// <summary>
    /// The request <paramref name="row"/> describes. <paramref name="resourceId"/> fills its <c>{id}</c> segment, and
    /// <paramref name="kanaalNaam"/> names the kanaal a body refers to.
    /// </summary>
    public static HttpRequestMessage For(ScopeMatrixRow row, Guid resourceId, string kanaalNaam, string clientId, string rsin)
    {
        var path = "/" + row.Route.Replace("{id}", resourceId.ToString());

        object body = row.Name switch
        {
            "KanaalController.CreateAsync" => NewKanaalBody(),
            "AbonnementController.CreateAsync" or "AbonnementController.UpdateAsync" => AbonnementBody(kanaalNaam),
            "AbonnementController.PartialUpdateAsync" => new { callbackUrl = NotificatiesSeed.CallbackUrl },
            "NotificatiesController.NotificeerAsync" => NotificatieBody(kanaalNaam),
            _ => null,
        };

        return Create(row.Method, path, clientId, rsin, body);
    }

    public static HttpRequestMessage GetAllAbonnementen(string clientId, string rsin) =>
        Create(HttpMethod.Get, "/" + ApiRoutes.Abonnement.GetAll, clientId, rsin);

    public static HttpRequestMessage GetAbonnement(Guid id, string clientId, string rsin) =>
        Create(HttpMethod.Get, "/" + ApiRoutes.Abonnement.Get.Replace("{id}", id.ToString()), clientId, rsin);

    public static HttpRequestMessage CreateAbonnement(string kanaalNaam, string auth, string clientId, string rsin) =>
        Create(HttpMethod.Post, "/" + ApiRoutes.Abonnement.Create, clientId, rsin, AbonnementBody(kanaalNaam, auth));

    public static HttpRequestMessage UpdateAbonnement(Guid id, string kanaalNaam, string auth, string clientId, string rsin) =>
        Create(HttpMethod.Put, "/" + ApiRoutes.Abonnement.Update.Replace("{id}", id.ToString()), clientId, rsin, AbonnementBody(kanaalNaam, auth));

    public static HttpRequestMessage PartialUpdateAbonnement(Guid id, string auth, string clientId, string rsin) =>
        Create(HttpMethod.Patch, "/" + ApiRoutes.Abonnement.Update.Replace("{id}", id.ToString()), clientId, rsin, new { auth });

    public static HttpRequestMessage DeleteAbonnement(Guid id, string clientId, string rsin) =>
        Create(HttpMethod.Delete, "/" + ApiRoutes.Abonnement.Delete.Replace("{id}", id.ToString()), clientId, rsin);

    public static HttpRequestMessage GetAllKanalen(string clientId, string rsin) =>
        Create(HttpMethod.Get, "/" + ApiRoutes.Kanaal.GetAll, clientId, rsin);

    public static HttpRequestMessage GetKanaal(Guid id, string clientId, string rsin) =>
        Create(HttpMethod.Get, "/" + ApiRoutes.Kanaal.Get.Replace("{id}", id.ToString()), clientId, rsin);

    public static HttpRequestMessage CreateKanaal(string naam, string clientId, string rsin) =>
        Create(HttpMethod.Post, "/" + ApiRoutes.Kanaal.Create, clientId, rsin, NewKanaalBody(naam));

    public static HttpRequestMessage Notify(string kanaalNaam, string clientId, string rsin) =>
        Create(HttpMethod.Post, "/" + ApiRoutes.Notificaties.Create, clientId, rsin, NotificatieBody(kanaalNaam));

    public static async Task<string> DescribeAsync(HttpResponseMessage response)
    {
        return $"{(int)response.StatusCode} {response.StatusCode}: {await response.Content.ReadAsStringAsync()}";
    }

    private static object NewKanaalBody(string naam = null)
    {
        return new
        {
            naam = naam ?? NotificatiesSeed.NewKanaalNaam(),
            documentatieLink = "https://docs.integrationtest.invalid/kanaal",
            filters = new[] { "bron" },
        };
    }

    private static object AbonnementBody(string kanaalNaam, string auth = null)
    {
        return new
        {
            callbackUrl = NotificatiesSeed.CallbackUrl,
            auth = auth ?? NotificatiesSeed.NewAuth(),
            kanalen = new[] { new { naam = kanaalNaam, filters = new { } } },
        };
    }

    private static object NotificatieBody(string kanaalNaam)
    {
        return new
        {
            kanaal = kanaalNaam,
            hoofdObject = "https://zaken.integrationtest.invalid/api/v1/zaken/3f1c2a5e-0001-4000-8000-000000000001",
            resource = "zaak",
            resourceUrl = "https://zaken.integrationtest.invalid/api/v1/zaken/3f1c2a5e-0001-4000-8000-000000000001",
            actie = "create",
            aanmaakdatum = "2026-01-01T12:00:00Z",
            kenmerken = new { bron = "integration-test" },
        };
    }

    private static HttpRequestMessage Create(HttpMethod method, string path, string clientId, string rsin, object body = null)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Add("Api-Version", Api.LatestVersion_1_0);

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
