using System;
using System.Net.Http;
using System.Threading.Tasks;
using OneGround.ZGW.Common.DataModel;
using V1_2Routes = OneGround.ZGW.Zaken.Web.Contracts.v1._2.ApiRoutes;
using V1_5Routes = OneGround.ZGW.Zaken.Web.Contracts.v1._5.ApiRoutes;
using V1_7Routes = OneGround.ZGW.Zaken.Web.Contracts.v1._7.ApiRoutes;
using V1Routes = OneGround.ZGW.Zaken.Web.Contracts.v1.ApiRoutes;

namespace OneGround.ZGW.Zaken.WebApi.IntegrationTests;

/// <summary>
/// Builds a well-formed request for any <see cref="ScopeMatrixRow"/> on a <see cref="SeededZaak"/>, with list requests filtered down to that zaak.
/// </summary>
internal static class ZakenEndpointRequests
{
    // The in-process test server answers on this base address, and the API builds its resource URLs on it.
    private const string BaseAddress = "http://localhost/";

    public const string BesluitUrl = "https://besluiten.integrationtest.invalid/api/v1/besluiten/3f1c2a5e-0007-4000-8000-000000000001";
    public const string InformatieObjectUrl =
        "https://documenten.integrationtest.invalid/api/v1/enkelvoudiginformatieobjecten/3f1c2a5e-0008-4000-8000-000000000001";
    public const string ContactmomentUrl =
        "https://contactmomenten.integrationtest.invalid/api/v1/contactmomenten/3f1c2a5e-0009-4000-8000-000000000001";
    public const string VerzoekUrl = "https://verzoeken.integrationtest.invalid/api/v1/verzoeken/3f1c2a5e-0010-4000-8000-000000000001";

    public static string ZaakUrl(Guid id) => BaseAddress + V1Routes.Zaken.Get.Replace("{id}", id.ToString());

    /// <summary>
    /// The request <paramref name="row"/> describes, on <paramref name="zaak"/> and the resource seeded under it.
    /// </summary>
    public static HttpRequestMessage For(ScopeMatrixRow row, SeededZaak zaak, string clientId)
    {
        var path = row.Route.Replace("{zaak_uuid}", zaak.Id.ToString());

        if (row.Route == V1Routes.Zaken.Get)
            path = path.Replace("{id}", zaak.Id.ToString());

        if (path.Contains("{id}") || path.Contains("{uuid}"))
            path = path.Replace("{id}", ResourceIdOf(zaak).ToString()).Replace("{uuid}", ResourceIdOf(zaak).ToString());

        if (row.Method == HttpMethod.Get && !row.Route.Contains('{'))
        {
            path += row.Route switch
            {
                V1Routes.Zaken.GetAll => $"?identificatie={zaak.Identificatie}",
                V1_7Routes.ZaakNotities.GetAll => $"?gerelateerdAan={Uri.EscapeDataString(zaak.Url)}",
                _ => $"?zaak={Uri.EscapeDataString(zaak.Url)}",
            };
        }

        return Create(row.Method, "/" + path, row.ApiVersion, clientId, zaak.Rsin, BodyFor(row, zaak));
    }

    public static HttpRequestMessage Create(
        HttpMethod method,
        string pathAndQuery,
        string apiVersion,
        string clientId,
        string rsin,
        object body = null
    )
    {
        var request = ZakenRequests.Create(method, pathAndQuery, clientId, rsin, apiVersion);

        return body == null ? request : ZakenRequests.WithJson(request, body);
    }

    /// <summary>
    /// The request and the response's status and body, for an assertion message.
    /// </summary>
    public static async Task<string> DescribeAsync(HttpResponseMessage response)
    {
        var request = response.RequestMessage!;

        return $"{request.Method} {request.RequestUri} answered {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}";
    }

    /// <summary>
    /// A new zaak's body; <paramref name="identificatie"/> left <c>null</c> makes a fresh one.
    /// </summary>
    public static object Zaak(string rsin, string zaakType, VertrouwelijkheidAanduiding vertrouwelijkheidAanduiding, string identificatie = null)
    {
        var startdatum = DateTime.UtcNow.AddDays(-10).ToString("yyyy-MM-dd");
        identificatie ??= $"ITEST-{Guid.NewGuid():N}";

        return new
        {
            identificatie,
            bronorganisatie = rsin,
            omschrijving = "Integratietest",
            zaaktype = zaakType,
            verantwoordelijkeOrganisatie = rsin,
            startdatum,
            vertrouwelijkheidaanduiding = vertrouwelijkheidAanduiding.ToString(),
            communicatiekanaal = "",
            selectielijstklasse = "",
            productenOfDiensten = Array.Empty<string>(),
            relevanteAndereZaken = Array.Empty<object>(),
            kenmerken = Array.Empty<object>(),
        };
    }

    /// <summary>
    /// A status body; the time it was set lies in the past, which version 1.5 requires.
    /// </summary>
    public static object Status(SeededZaak zaak, string statusType, DateTime? datumStatusGezet = null)
    {
        return new
        {
            zaak = zaak.Url,
            statustype = statusType,
            datumStatusGezet = (datumStatusGezet ?? DateTime.UtcNow.AddMinutes(-1)).ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            statustoelichting = "",
        };
    }

    public static object Rol(SeededZaak zaak)
    {
        return new
        {
            zaak = zaak.Url,
            betrokkeneType = "medewerker",
            roltype = zaak.Catalogi.RolType,
            roltoelichting = "Integratietest",
            betrokkeneIdentificatie = new { identificatie = "itest-medewerker", achternaam = "Integratietest" },
        };
    }

    public static object ZaakObject(SeededZaak zaak, string relatieomschrijving = "")
    {
        return new
        {
            zaak = zaak.Url,
            objectType = "overige",
            objectTypeOverige = "integratietest",
            relatieomschrijving,
            objectIdentificatie = new { overigeData = new { kenmerk = "integratietest" } },
        };
    }

    private static object BodyFor(ScopeMatrixRow row, SeededZaak zaak)
    {
        if (row.Method == HttpMethod.Get || row.Method == HttpMethod.Head || row.Method == HttpMethod.Delete)
            return null;

        var isPatch = row.Method == HttpMethod.Patch;

        // A PUT writes a value the seed does not hold, so a saved replacement shows in ZakenSeed.ZaakStateAsync.
        var isPut = row.Method == HttpMethod.Put;

        return row.Route switch
        {
            V1Routes.Zaken.Search => new { identificatie = zaak.Identificatie },
            V1Routes.Zaken.Create when row.Method == HttpMethod.Post => Zaak(zaak.Rsin, zaak.ZaakType, zaak.VertrouwelijkheidAanduiding),
            V1Routes.Zaken.Update when isPatch => new { omschrijving = "Bijgewerkt" },
            V1Routes.Zaken.Update => Zaak(zaak.Rsin, zaak.ZaakType, zaak.VertrouwelijkheidAanduiding, zaak.Identificatie),
            V1Routes.ZaakBesluiten.Create => new { besluit = BesluitUrl },
            V1_2Routes.ZaakEigenschappen.Update when isPatch => new { waarde = "Bijgewerkt" },
            V1Routes.ZaakEigenschappen.Create or V1_2Routes.ZaakEigenschappen.Update => new
            {
                zaak = zaak.Url,
                eigenschap = zaak.Catalogi.Eigenschap,
                waarde = "Integratietest",
            },
            V1Routes.ZaakStatussen.Create => Status(zaak, zaak.Catalogi.BeginStatusType),
            V1Routes.ZaakResultaten.Update when isPatch => new { toelichting = "Bijgewerkt" },
            V1Routes.ZaakResultaten.Create or V1Routes.ZaakResultaten.Update => new
            {
                zaak = zaak.Url,
                resultaattype = zaak.Catalogi.ResultaatType,
                toelichting = isPut ? "Bijgewerkt" : "",
            },
            V1Routes.ZaakRollen.Create => Rol(zaak),
            V1_5Routes.ZaakObjecten.Update when isPatch => new { relatieomschrijving = "Bijgewerkt" },
            V1Routes.ZaakObjecten.Create or V1_5Routes.ZaakObjecten.Update => ZaakObject(zaak, isPut ? "Bijgewerkt" : ""),
            V1Routes.ZaakInformatieObjecten.Update when isPatch => new { titel = "Bijgewerkt" },
            V1Routes.ZaakInformatieObjecten.Create or V1Routes.ZaakInformatieObjecten.Update => new
            {
                informatieobject = InformatieObjectUrl,
                zaak = zaak.Url,
                titel = isPut ? "Bijgewerkt" : "Integratietest",
                beschrijving = "",
            },
            V1Routes.KlantContacten.Create => new
            {
                zaak = zaak.Url,
                identificatie = "ITEST",
                datumtijd = DateTime.UtcNow.AddMinutes(-1).ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                kanaal = "telefoon",
                onderwerp = "Integratietest",
                toelichting = "",
            },
            V1_5Routes.ZaakContactmomenten.Create => new { zaak = zaak.Url, contactmoment = ContactmomentUrl },
            V1_5Routes.ZaakVerzoeken.Create => new { zaak = zaak.Url, verzoek = VerzoekUrl },
            V1_7Routes.ZaaknummerReserveren.Create => new { bronorganisatie = zaak.Rsin, aantal = 1 },
            V1_7Routes.ZaakNotities.Update when isPatch => new { tekst = "Bijgewerkt" },
            V1_7Routes.ZaakNotities.Create or V1_7Routes.ZaakNotities.Update => new
            {
                gerelateerdAan = zaak.Url,
                onderwerp = "Integratietest",
                tekst = isPut ? "Bijgewerkt" : "Integratietest",
            },
            _ => throw new ArgumentOutOfRangeException(nameof(row), row.Name, $"No request body for {row.Method} {row.Route}."),
        };
    }

    private static Guid ResourceIdOf(SeededZaak zaak)
    {
        return zaak.ResourceId ?? throw new InvalidOperationException($"The route needs a seeded resource, but the zaak has {zaak.Resource}.");
    }
}
