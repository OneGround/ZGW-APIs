using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using OneGround.ZGW.Gesprekken.DataModel;

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
    private readonly ILogger<KlantinteractiesKlantcontactService> _logger;
    private readonly string _api;
    private readonly string _token;

    public KlantinteractiesKlantcontactService(HttpClient client, IConfiguration configuration, ILogger<KlantinteractiesKlantcontactService> logger)
    {
        _client = client;
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
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_api}/klantcontacten")
        {
            Content = JsonContent.Create(
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
            ),
        };

        if (!string.IsNullOrEmpty(_token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Token", _token);
        }

        try
        {
            using var response = await _client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Klantinteracties answered {Status}; no klantcontact for gesprek {Id}", (int)response.StatusCode, gesprek.Id);
                return null;
            }

            return JObject.Parse(await response.Content.ReadAsStringAsync(cancellationToken)).Value<string>("url");
        }
        catch (HttpRequestException e)
        {
            _logger.LogWarning(e, "Klantinteracties is not reachable; no klantcontact for gesprek {Id}", gesprek.Id);
            return null;
        }
    }
}
