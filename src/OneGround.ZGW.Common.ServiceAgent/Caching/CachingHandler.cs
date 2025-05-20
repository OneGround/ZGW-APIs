using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Common.Authentication;
using OneGround.ZGW.Common.Caching;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.CorrelationId;
using OneGround.ZGW.Common.ServiceAgent.Extensions;

namespace OneGround.ZGW.Common.ServiceAgent.Caching;

public class CachingHandler<T> : DelegatingHandler
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<CachingHandler<T>> _logger;
    private readonly ICorrelationContextAccessor _correlationContextAccessor;
    private readonly IOrganisationContextAccessor _organisationContextAccessor;
    private readonly CachingConfiguration<T> _configuration;

    public CachingHandler(
        ILogger<CachingHandler<T>> logger,
        ICorrelationContextAccessor correlationContextAccessor,
        IOrganisationContextAccessor organisationContextAccessor,
        IDistributedCache cache,
        CachingConfiguration<T> configuration
    )
    {
        _logger = logger;
        _correlationContextAccessor = correlationContextAccessor;
        _organisationContextAccessor = organisationContextAccessor;
        _cache = cache;
        _configuration = configuration;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // only cache get requests
        if (request.Method != HttpMethod.Get)
        {
            return await base.SendAsync(request, cancellationToken);
        }

        string apiVersion =
            request.Headers.SingleOrDefault(h => string.Equals(h.Key, "Api-Version", StringComparison.OrdinalIgnoreCase)).Value?.FirstOrDefault()
            ?? "1.0";

        // only cache configured requests
        var rsin = _organisationContextAccessor.OrganisationContext.Rsin;
        var key = _configuration.GetKey(request.RequestUri, rsin, apiVersion);
        if (key == null)
        {
            return await base.SendAsync(request, cancellationToken);
        }

        var cachedResponse = await _cache.GetAsync(key, cancellationToken);
        if (cachedResponse != null)
        {
            _logger.LogDebug("Response cache hit: '{key}'.", key);
            var response = JsonSerializer.Deserialize<CachedResponse>(cachedResponse);
            var httpResponse = response.ToHttpResponseMessage();

            CorrelateCachedResponse(httpResponse);

            return httpResponse;
        }
        else
        {
            _logger.LogDebug("Response cache miss: '{key}'.", key);
        }

        var baseResponse = await base.SendAsync(request, cancellationToken);
        if (baseResponse.StatusCode == HttpStatusCode.OK)
        {
            var entry = await baseResponse.ToCachedResponse();
            var serialized = JsonSerializer.SerializeToUtf8Bytes(entry);

            var cacheExpirationOption = new DistributedCacheEntryOptions { AbsoluteExpiration = GetCacheExpiration() };

            await _cache.SetAsync(key, serialized, cacheExpirationOption, cancellationToken);
            _logger.LogDebug("Added response to cache with key: '{key}'.", key);
        }

        return baseResponse;
    }

    private static DateTimeOffset GetCacheExpiration()
    {
        var now = new DateTimeOffset(DateTime.Now);
        if (now.Hour < 21)
        {
            // Note: Let cache-entry be expire after 3 hours from being set
            return now.AddHours(3);
        }

        // Note: Midnight within 3 hours: Be sure (at least ZTC) cache-entry expires at midnight (0:00) with an extra margin to be sure cache-entry will be valid (with a minimum of 20 sec).
        //  The reason for this is cached versions of zaakttypen, besluittypen or zaakinformatieobjectypen can be invalid at the next day!
        return now.Date.AddDays(1).AddSeconds(20); // Example of: 2024-07-16 23:39:17 => expires at 2024-07-17 00:00:20
    }

    private void CorrelateCachedResponse(HttpResponseMessage response)
    {
        const string correlationHeader = Headers.CorrelationHeader;

        response.Headers.Remove(correlationHeader);

        var correlationId = _correlationContextAccessor?.CorrelationId;
        if (correlationId != null)
        {
            response.Headers.Add(correlationHeader, correlationId);
        }
    }
}
