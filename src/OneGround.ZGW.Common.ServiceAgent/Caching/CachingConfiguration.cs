using System;
using System.Collections.Generic;
using System.Web;
using OneGround.ZGW.Common.Caching;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.Services;

namespace OneGround.ZGW.Common.ServiceAgent.Caching;

public class CachingConfiguration<T>
{
    private readonly IServiceDiscovery _cachedServiceEndpoints;
    private readonly List<CacheEntity> _cacheEntities = [];

    /// <summary>
    /// Configure what uri needs to be cached.
    /// </summary>
    public List<CacheEntity> CacheEntities => _cacheEntities;

    public CachingConfiguration(IServiceDiscovery serviceEndpoints)
    {
        _cachedServiceEndpoints = serviceEndpoints;
    }

    public string GetKey(Uri requestUri, string rsin, string apiVersion)
    {
        foreach (var cachedUri in _cacheEntities)
        {
            // match the pattern
            var match = cachedUri.Pattern.Match(requestUri.AbsolutePath.TrimEnd('/'));
            if (match.Success)
            {
                // match the service endpoint
                var serviceUri = _cachedServiceEndpoints.GetApi(cachedUri.Service);
                if (serviceUri.IsBaseOf(requestUri))
                {
                    var entity = match.Groups["entity"];
                    var uuid = match.Groups["uuid"];

                    if (cachedUri.Service == ServiceRoleName.AC)
                    {
                        var clientId = HttpUtility.ParseQueryString(requestUri.Query).Get("clientId");
                        // example key without rsin: ZGW:AC:applicaties:ALK-zKlhYoyq41Zp
                        return $"ZGW:{cachedUri.Service}:{entity}:{clientId}:{apiVersion}";
                    }

                    // example key: ZGW:ZTC:zaaktypen:123456789:12aee180-581c-47de-afd6-ea43fb3c1cd1
                    return $"ZGW:{cachedUri.Service}:{entity}:{rsin}:{uuid}:{apiVersion}";
                }
            }
        }

        return null;
    }
}
