using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OneGround.ZGW.Common.Web.Configuration;

namespace OneGround.ZGW.Common.Web.Http;

/// <summary>
/// Fetches a JSON object from an untrusted, unauthenticated url. Never throws for a failing call: a refused url, a timeout, a non-success
/// status, an oversized response or a body that is not a JSON object all yield an empty object (and a warning in the log).
/// Registered scoped: within one incoming request the same url is fetched at most once, all calls together may take at most
/// <see cref="ExternalJsonClientSettings.TotalTimeout"/> (counted from the first EXTERNAL fetch of the request, so not only the time
/// spent waiting on external hosts).
/// The shared fetch is stopped when the incoming request is aborted (or by its own timeout). The token passed to
/// <see cref="GetJsonObjectAsync"/> only stops the caller from waiting: other callers may still be waiting on the same fetch, and a
/// caller without an incoming request (a background job) cannot abort a fetch that is already running, which ends at the latest at the timeout.
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
    private readonly IHttpContextAccessor _httpContextAccessor;

    // Note: per instance, and the instance is scoped to the incoming request. Failed fetches are cached too, so a broken url is not retried.
    private readonly ConcurrentDictionary<string, Lazy<Task<JObject>>> _cache = new(StringComparer.Ordinal);
    private readonly TimeProvider _timeProvider;
    private readonly object _budgetLock = new();
    private long? _budgetStart;
    private readonly ConcurrentDictionary<string, bool> _rejectedLogged = new(StringComparer.Ordinal);
    private bool _budgetExhaustedLogged;

    public ExternalJsonClient(
        IHttpClientFactory httpClientFactory,
        IOptions<ExternalJsonClientSettings> settings,
        ILogger<ExternalJsonClient> logger,
        IHttpContextAccessor httpContextAccessor = null,
        TimeProvider timeProvider = null
    )
    {
        _httpClientFactory = httpClientFactory;
        _settings = settings.Value;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<JObject> GetJsonObjectAsync(string url, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url))
            return new JObject();

        if (!ExternalUrlPolicy.IsAllowedUrl(url, _settings, out var uri))
        {
            // Note: once per host per request -- a list of zaken with the same refused url must not flood the log.
            var rejectedHost = Uri.TryCreate(url, UriKind.Absolute, out var rejected) ? rejected.Host : "<invalid>";

            return _rejectedLogged.TryAdd(rejectedHost, true) ? Failed(rejectedHost, "url is not allowed") : new JObject();
        }

        // Note: the shared fetch must not depend on the token of whichever caller happened to come first, so it only follows the
        // incoming request (the expand resolvers have no token of their own). A caller's own token just stops THAT caller waiting.
        var fetchToken = _httpContextAccessor?.HttpContext?.RequestAborted ?? default;

        var key = uri.GetComponents(UriComponents.HttpRequestUrl, UriFormat.UriEscaped);
        var entry = _cache.GetOrAdd(key, k => CreateEntry(k, uri, fetchToken));

        // Note: a copy, so callers never share (and cannot change) each other's object.
        var result = await entry.Value.WaitAsync(cancellationToken);

        return (JObject)result.DeepClone();
    }

    private Lazy<Task<JObject>> CreateEntry(string key, Uri uri, CancellationToken fetchToken)
    {
        Lazy<Task<JObject>> entry = null;
        entry = new Lazy<Task<JObject>>(() => FetchAndEvictOnFailureAsync(key, uri, fetchToken, entry));

        return entry;
    }

    // Note: a fetch that fails unexpectedly (or is cancelled) removes itself, whether or not anybody is still waiting for it, so the next
    // caller retries. Only if it is still OUR entry: a newer one may have replaced it. (An async method, so it can never throw synchronously
    // from inside the Lazy, which would cache that exception for good.)
    private async Task<JObject> FetchAndEvictOnFailureAsync(string key, Uri uri, CancellationToken fetchToken, Lazy<Task<JObject>> entry)
    {
        try
        {
            return await FetchAsync(uri, fetchToken);
        }
        catch
        {
            ((ICollection<KeyValuePair<string, Lazy<Task<JObject>>>>)_cache).Remove(new KeyValuePair<string, Lazy<Task<JObject>>>(key, entry));
            throw;
        }
    }

    private TimeSpan RemainingBudget()
    {
        lock (_budgetLock)
        {
            _budgetStart ??= _timeProvider.GetTimestamp();
            return _settings.TotalTimeout - _timeProvider.GetElapsedTime(_budgetStart.Value);
        }
    }

    private async Task<JObject> FetchAsync(Uri uri, CancellationToken cancellationToken)
    {
        var remaining = RemainingBudget();
        if (remaining <= TimeSpan.Zero)
        {
            lock (_budgetLock)
            {
                if (_budgetExhaustedLogged)
                    return new JObject();

                _budgetExhaustedLogged = true;
            }

            return Failed(uri.Host, "time budget for this request is used up, further external calls are skipped");
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(remaining < _settings.Timeout ? remaining : _settings.Timeout);

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
        catch (Exception ex) when (ex is BlockedAddressException || ex.InnerException is BlockedAddressException)
        {
            // Note: its own event, so a (possible) server-side request forgery attempt can be told from an outage.
            _logger.LogWarning(
                new EventId(4001, "ExternalJsonAddressBlocked"),
                "External JSON from host {Host} was blocked: the host resolves to a non-public address",
                uri.Host
            );
            return new JObject();
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or IOException)
        {
            _logger.LogDebug(ex, "External JSON from host {Host} failed", uri.Host);
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
