using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OneGround.ZGW.Common.Web.Configuration;

namespace OneGround.ZGW.Common.Web.Http;

/// <summary>
/// Fetches a JSON object from an untrusted, unauthenticated url. Never throws for a failing call: a refused url, a timeout, a non-success
/// status, an oversized response or a body that is not a JSON object all yield an empty object (and a warning in the log).
/// </summary>
public interface IExternalJsonClient
{
    Task<JObject> GetJsonObjectAsync(string url, CancellationToken cancellationToken = default);
}

public class ExternalJsonClient : IExternalJsonClient
{
    public const string HttpClientName = "ExternalJson";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ExternalJsonClientSettings _settings;
    private readonly ILogger<ExternalJsonClient> _logger;

    public ExternalJsonClient(IHttpClientFactory httpClientFactory, IOptions<ExternalJsonClientSettings> settings, ILogger<ExternalJsonClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _settings = settings.Value;
        _logger = logger;

        if (_settings.AllowPrivateAddresses)
            _logger.LogWarning("External JSON client: AllowPrivateAddresses is enabled, the protection against server-side request forgery is off");
    }

    public async Task<JObject> GetJsonObjectAsync(string url, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url))
            return new JObject();

        if (!ExternalUrlPolicy.IsAllowedUrl(url, _settings, out var uri))
            return Failed(Uri.TryCreate(url, UriKind.Absolute, out var rejected) ? rejected.Host : "<invalid>", "url is not allowed");

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(_settings.Timeout);

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // Note: this named client deliberately has no authentication handler; nothing of ours is ever sent to the external host.
            using var client = _httpClientFactory.CreateClient(HttpClientName);
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);

            if (!response.IsSuccessStatusCode)
                return Failed(uri.Host, $"status {(int)response.StatusCode}");

            var maxBytes = _settings.EffectiveMaxResponseBytes;

            if (response.Content.Headers.ContentLength > maxBytes)
                return Failed(uri.Host, "response too large");

            await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token);
            using var buffer = new MemoryStream();
            var chunk = new byte[8192];
            int read;
            while ((read = await stream.ReadAsync(chunk, timeout.Token)) > 0)
            {
                if (buffer.Length + read > maxBytes)
                    return Failed(uri.Host, "response too large");

                buffer.Write(chunk, 0, read);
            }

            buffer.Position = 0;
            using var reader = new JsonTextReader(new StreamReader(buffer)) { DateParseHandling = DateParseHandling.None };
            var token = await JToken.LoadAsync(reader, timeout.Token);

            return token as JObject ?? Failed(uri.Host, "response is not a JSON object");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            return Failed(uri.Host, "timeout");
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or IOException)
        {
            return Failed(uri.Host, ex.GetType().Name);
        }
    }

    // Note: only the host is logged -- the full url may carry a secret in its query string.
    private JObject Failed(string host, string reason)
    {
        _logger.LogWarning("External JSON from host {Host} is unavailable: {Reason}", host, reason);
        return new JObject();
    }
}
