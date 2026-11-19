using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using OneGround.ZGW.IntegrationTests.Common.Authentication;
using OneGround.ZGW.Notificaties.Contracts.v1;
using OneGround.ZGW.Notificaties.Web.Controllers;
using OneGround.ZGW.Notificaties.Web.Controllers.v1;

namespace OneGround.ZGW.Notificaties.WebApi.IntegrationTests;

/// <summary>
/// Well-formed requests on the v1 routes; a <c>null</c> client id and rsin send no Authorization header.
/// </summary>
internal static class NotificatiesRequests
{
    /// <summary>
    /// The request <paramref name="row"/> describes, with <paramref name="resourceId"/> in its <c>{id}</c> segment and <paramref name="kanaalNaam"/> as the kanaal its body names.
    /// </summary>
    public static HttpRequestMessage For(ScopeMatrixRow row, Guid resourceId, string kanaalNaam, string clientId, string rsin)
    {
        var path = Path(row.Route, resourceId);

        object body = row.Name switch
        {
            var n when n == NameOf<KanaalController>(nameof(KanaalController.CreateAsync)) => NewKanaalBody(),
            var n
                when n == NameOf<AbonnementController>(nameof(AbonnementController.CreateAsync))
                    || n == NameOf<AbonnementController>(nameof(AbonnementController.UpdateAsync)) => AbonnementBody(kanaalNaam),
            var n when n == NameOf<AbonnementController>(nameof(AbonnementController.PartialUpdateAsync)) => new
            {
                callbackUrl = NotificatiesSeed.CallbackUrl,
            },
            var n when n == NameOf<NotificatiesController>(nameof(NotificatiesController.NotificeerAsync)) => NotificatieBody(kanaalNaam),
            _ => null,
        };

        return Create(row.Method, path, clientId, rsin, body);
    }

    public static HttpRequestMessage GetAllAbonnementen(string clientId, string rsin) =>
        Create(HttpMethod.Get, "/" + ApiRoutes.Abonnement.GetAll, clientId, rsin);

    public static HttpRequestMessage GetAbonnement(Guid id, string clientId, string rsin) =>
        Create(HttpMethod.Get, Path(ApiRoutes.Abonnement.Get, id), clientId, rsin);

    public static HttpRequestMessage CreateAbonnement(string kanaalNaam, string auth, string clientId, string rsin) =>
        Create(HttpMethod.Post, "/" + ApiRoutes.Abonnement.Create, clientId, rsin, AbonnementBody(kanaalNaam, auth));

    public static HttpRequestMessage UpdateAbonnement(Guid id, string kanaalNaam, string auth, string clientId, string rsin) =>
        Create(HttpMethod.Put, Path(ApiRoutes.Abonnement.Update, id), clientId, rsin, AbonnementBody(kanaalNaam, auth));

    public static HttpRequestMessage PartialUpdateAbonnement(Guid id, string auth, string clientId, string rsin) =>
        Create(HttpMethod.Patch, Path(ApiRoutes.Abonnement.Update, id), clientId, rsin, new { auth });

    public static HttpRequestMessage DeleteAbonnement(Guid id, string clientId, string rsin) =>
        Create(HttpMethod.Delete, Path(ApiRoutes.Abonnement.Delete, id), clientId, rsin);

    public static HttpRequestMessage GetAllKanalen(string clientId, string rsin) =>
        Create(HttpMethod.Get, "/" + ApiRoutes.Kanaal.GetAll, clientId, rsin);

    public static HttpRequestMessage GetKanaal(Guid id, string clientId, string rsin) =>
        Create(HttpMethod.Get, Path(ApiRoutes.Kanaal.Get, id), clientId, rsin);

    public static HttpRequestMessage CreateKanaal(string naam, string clientId, string rsin) =>
        Create(HttpMethod.Post, "/" + ApiRoutes.Kanaal.Create, clientId, rsin, NewKanaalBody(naam));

    public static HttpRequestMessage Notify(string kanaalNaam, string clientId, string rsin) =>
        Create(HttpMethod.Post, "/" + ApiRoutes.Notificaties.Create, clientId, rsin, NotificatieBody(kanaalNaam));

    public static async Task<string> DescribeAsync(HttpResponseMessage response)
    {
        return $"{(int)response.StatusCode} {response.StatusCode}: {await response.Content.ReadAsStringAsync()}";
    }

    private static string NameOf<TController>(string action) => ScopeMatrixRow.NameOf(typeof(TController), action);

    private static string Path(string route, Guid id) => "/" + route.Replace("{id}", id.ToString());

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
