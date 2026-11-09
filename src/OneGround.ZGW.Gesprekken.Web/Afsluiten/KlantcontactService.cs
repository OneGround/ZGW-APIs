using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.Gesprekken.DataModel;
using OneGround.ZGW.Gesprekken.Web.Helpers;

namespace OneGround.ZGW.Gesprekken.Web.Afsluiten;

public interface IKlantcontactService
{
    /// <summary>
    /// Registers the klantcontact a closed gesprek led to. Returns its URL, or null when Klantinteracties is not
    /// configured or did not accept it: closing a gesprek does not depend on another registration being available.
    /// </summary>
    Task<string> MaakAanAsync(Gesprek gesprek, CancellationToken cancellationToken);
}

public class KlantinteractiesKlantcontactService : IKlantcontactService
{
    private const string Kanaal = "MijnGesprekken";
    private const string Taal = "dut";

    private readonly HttpClient _client;
    private readonly IEntityUriService _uriService;
    private readonly ILogger<KlantinteractiesKlantcontactService> _logger;
    private readonly string _api;
    private readonly string _token;

    public KlantinteractiesKlantcontactService(
        HttpClient client,
        IConfiguration configuration,
        IEntityUriService uriService,
        ILogger<KlantinteractiesKlantcontactService> logger
    )
    {
        _client = client;
        _uriService = uriService;
        _logger = logger;
        _api = configuration["Klantinteracties:Api"]?.TrimEnd('/');
        _token = configuration["Klantinteracties:Token"];
    }

    public async Task<string> MaakAanAsync(Gesprek gesprek, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_api))
        {
            _logger.LogDebug("Klantinteracties is not configured; no klantcontact for gesprek {Id}", gesprek.Id);
            return null;
        }

        // The text of the bijdragen stays in this registration; the klantcontact only says that the contact took place
        using var request = NewRequest(
            "klantcontacten",
            new
            {
                kanaal = Kanaal,
                onderwerp = gesprek.Gespreksonderwerp,
                inhoud = $"Gesprek {gesprek.Referentie}",
                taal = Taal,
                vertrouwelijk = false,
                indicatieContactGelukt = true,
                plaatsgevondenOp = gesprek.MomentEindeGesprek,
            }
        );

        try
        {
            using var response = await _client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Klantinteracties answered {Status}; no klantcontact for gesprek {Id}", (int)response.StatusCode, gesprek.Id);
                return null;
            }

            var klantcontact = JObject.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            var uuid = klantcontact.Value<string>("uuid");
            if (uuid != null)
            {
                await KoppelAsync(gesprek, uuid, cancellationToken);
            }

            return klantcontact.Value<string>("url");
        }
        catch (Exception e) when (IsStoring(e, cancellationToken))
        {
            _logger.LogWarning(e, "Klantinteracties is not reachable; no klantcontact for gesprek {Id}", gesprek.Id);
            return null;
        }
    }

    /// <summary>
    /// Links the klantcontact to what it is about and who took part, so the customer view shows the context. A
    /// failure here keeps the klantcontact: it did take place.
    /// </summary>
    private async Task KoppelAsync(Gesprek gesprek, string klantcontactUuid, CancellationToken cancellationToken)
    {
        var klantcontact = new { uuid = klantcontactUuid };

        await PostAsync(
            "onderwerpobjecten",
            Onderwerpobject(klantcontact, _uriService.GetUri(gesprek), "gesprek", "mijngesprekken"),
            cancellationToken
        );
        if (gesprek.AanleidingZaak != null)
        {
            await PostAsync("onderwerpobjecten", Onderwerpobject(klantcontact, gesprek.AanleidingZaak, "zaak", "zaken"), cancellationToken);
        }

        foreach (var deelname in gesprek.Deelnames)
        {
            if (IsHier(deelname.Partij, "partijen", out var partij))
            {
                await PostAsync(
                    "betrokkenen",
                    new
                    {
                        hadKlantcontact = klantcontact,
                        wasPartij = new { uuid = partij },
                        rol = "klant",
                        initiator = false,
                        organisatienaam = "",
                        contactnaam = new { achternaam = deelname.Naam },
                        bezoekadres = (object)null,
                        correspondentieadres = (object)null,
                    },
                    cancellationToken
                );
            }
            else if (IsHier(deelname.Actor, "actoren", out var actor))
            {
                await PostAsync("actorklantcontacten", new { actor = new { uuid = actor }, klantcontact }, cancellationToken);
            }
        }
    }

    private static object Onderwerpobject(object klantcontact, string url, string objecttype, string register) =>
        new
        {
            klantcontact,
            wasKlantcontact = (object)null,
            onderwerpobjectidentificator = new
            {
                objectId = url,
                codeObjecttype = objecttype,
                codeRegister = register,
                codeSoortObjectId = "url",
            },
        };

    // A deelname can refer to another register; only references to this Klantinteracties can be linked
    private bool IsHier(string url, string collectie, out Guid id)
    {
        id = Guid.Empty;
        return url != null && url.StartsWith($"{_api}/{collectie}/", StringComparison.OrdinalIgnoreCase) && GrcUrl.TryGetId(url, collectie, out id);
    }

    private async Task PostAsync(string collectie, object body, CancellationToken cancellationToken)
    {
        try
        {
            using var request = NewRequest(collectie, body);
            using var response = await _client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Klantinteracties answered {Status} on {Collectie}", (int)response.StatusCode, collectie);
            }
        }
        catch (Exception e) when (IsStoring(e, cancellationToken))
        {
            _logger.LogWarning(e, "Klantinteracties is not reachable for {Collectie}", collectie);
        }
    }

    // A failure of Klantinteracties (unreachable, a time-out or an unreadable answer) must not stop closing the gesprek
    private static bool IsStoring(Exception e, CancellationToken cancellationToken) =>
        e is HttpRequestException or JsonReaderException || (e is TaskCanceledException && !cancellationToken.IsCancellationRequested);

    private HttpRequestMessage NewRequest(string collectie, object body)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"{_api}/{collectie}") { Content = JsonContent.Create(body, body.GetType()) };
        if (!string.IsNullOrEmpty(_token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Token", _token);
        }

        return request;
    }
}
