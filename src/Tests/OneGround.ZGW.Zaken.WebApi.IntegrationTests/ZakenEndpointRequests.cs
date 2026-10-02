using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using OneGround.ZGW.Common.DataModel;
using OneGround.ZGW.IntegrationTests.Common.Authentication;

namespace OneGround.ZGW.Zaken.WebApi.IntegrationTests;

/// <summary>
/// Builds a well-formed request for any <see cref="ScopeMatrixRow"/> on a <see cref="SeededZaak"/>, with the identity headers the test
/// authentication reads. A list request is filtered down to the seeded zaak, so other tests' data never shows up in it.
/// </summary>
internal static class ZakenEndpointRequests
{
    // The in-process test server answers on this base address, and the API builds its resource URLs on it.
    private const string BaseAddress = "http://localhost/api/v1/";

    public const string BesluitUrl = "https://besluiten.integrationtest.invalid/api/v1/besluiten/3f1c2a5e-0007-4000-8000-000000000001";
    public const string InformatieObjectUrl =
        "https://documenten.integrationtest.invalid/api/v1/enkelvoudiginformatieobjecten/3f1c2a5e-0008-4000-8000-000000000001";
    public const string ContactmomentUrl =
        "https://contactmomenten.integrationtest.invalid/api/v1/contactmomenten/3f1c2a5e-0009-4000-8000-000000000001";
    public const string VerzoekUrl = "https://verzoeken.integrationtest.invalid/api/v1/verzoeken/3f1c2a5e-0010-4000-8000-000000000001";

    public static string Url(string relativePath) => BaseAddress + relativePath;

    /// <summary>
    /// The request <paramref name="row"/> describes, on <paramref name="zaak"/> and the resource seeded under it.
    /// </summary>
    public static HttpRequestMessage For(ScopeMatrixRow row, SeededZaak zaak, string clientId)
    {
        var path = row.Route.Replace("{zaak_uuid}", zaak.Id.ToString());

        if (path.StartsWith("api/v1/zaken/{id}", StringComparison.Ordinal))
            path = path.Replace("{id}", zaak.Id.ToString());

        if (path.Contains("{id}") || path.Contains("{uuid}"))
            path = path.Replace("{id}", ResourceIdOf(zaak).ToString()).Replace("{uuid}", ResourceIdOf(zaak).ToString());

        if (row.Method == HttpMethod.Get && !row.Route.Contains('{'))
        {
            path += row.Route == "api/v1/zaken" ? $"?identificatie={zaak.Identificatie}" : $"?zaak={Uri.EscapeDataString(zaak.Url)}";
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
        var request = new HttpRequestMessage(method, pathAndQuery);
        request.Headers.Add("Api-Version", apiVersion);
        request.Headers.Add("Accept-Crs", "EPSG:4326");
        request.Headers.Authorization = TestIdentity.Create(clientId, rsin);

        if (body != null)
        {
            request.Headers.Add("Content-Crs", "EPSG:4326");
            request.Content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
        }

        return request;
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

        var resource = row.Route.Replace("{zaak_uuid}/", "").Replace("/{uuid}", "").Replace("/{id}", "");
        var isPatch = row.Method == HttpMethod.Patch;

        return resource switch
        {
            "api/v1/zaken/_zoek" => new { identificatie = zaak.Identificatie },
            "api/v1/zaken" when row.Method == HttpMethod.Post => Zaak(zaak.Rsin, zaak.ZaakType, zaak.VertrouwelijkheidAanduiding),
            "api/v1/zaken" when isPatch => new { omschrijving = "Bijgewerkt" },
            "api/v1/zaken" => Zaak(zaak.Rsin, zaak.ZaakType, zaak.VertrouwelijkheidAanduiding, zaak.Identificatie),
            "api/v1/zaken/besluiten" => new { besluit = BesluitUrl },
            "api/v1/zaken/zaakeigenschappen" when isPatch => new { waarde = "Bijgewerkt" },
            "api/v1/zaken/zaakeigenschappen" => new
            {
                zaak = zaak.Url,
                eigenschap = zaak.Catalogi.Eigenschap,
                waarde = "Integratietest",
            },
            "api/v1/statussen" => Status(zaak, zaak.Catalogi.BeginStatusType),
            "api/v1/resultaten" when isPatch => new { toelichting = "Bijgewerkt" },
            "api/v1/resultaten" => new
            {
                zaak = zaak.Url,
                resultaattype = zaak.Catalogi.ResultaatType,
                toelichting = "",
            },
            "api/v1/rollen" => Rol(zaak),
            "api/v1/zaakobjecten" when isPatch => new { relatieomschrijving = "Bijgewerkt" },
            "api/v1/zaakobjecten" => ZaakObject(zaak),
            "api/v1/zaakinformatieobjecten" when isPatch => new { titel = "Bijgewerkt" },
            "api/v1/zaakinformatieobjecten" => new
            {
                informatieobject = InformatieObjectUrl,
                zaak = zaak.Url,
                titel = "Integratietest",
                beschrijving = "",
            },
            "api/v1/klantcontacten" => new
            {
                zaak = zaak.Url,
                identificatie = "ITEST",
                datumtijd = DateTime.UtcNow.AddMinutes(-1).ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                kanaal = "telefoon",
                onderwerp = "Integratietest",
                toelichting = "",
            },
            "api/v1/zaakcontactmomenten" => new { zaak = zaak.Url, contactmoment = ContactmomentUrl },
            "api/v1/zaakverzoeken" => new { zaak = zaak.Url, verzoek = VerzoekUrl },
            _ => throw new ArgumentOutOfRangeException(nameof(row), row.Name, $"No request body for {row.Method} {row.Route}."),
        };
    }

    private static Guid ResourceIdOf(SeededZaak zaak)
    {
        return zaak.ResourceId ?? throw new InvalidOperationException($"The route needs a seeded resource, but the zaak has {zaak.Resource}.");
    }
}
