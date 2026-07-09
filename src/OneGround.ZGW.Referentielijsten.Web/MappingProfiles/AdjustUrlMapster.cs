using System;
using Mapster;
using Microsoft.AspNetCore.Http;

namespace OneGround.ZGW.Referentielijsten.Web.MappingProfiles;

/// <summary>
/// Mapster replacement for the AutoMapper <c>AdjustUrl</c> member value resolver: rewrites a URL
/// string's host, port and scheme to match the current HTTP request, resolving
/// <see cref="IHttpContextAccessor"/> from <see cref="MapContext"/>. Requires mapping to run
/// through ServiceMapper so the service provider is available on MapContext. Host-rewrite logic is
/// identical to the original resolver.
/// </summary>
public static class AdjustUrlMapster
{
    public static string Adjust(string sourceUrl)
    {
        var httpContextAccessor = MapContext.Current.GetService<IHttpContextAccessor>();

        var uriBuilder = new UriBuilder(sourceUrl);
        if (httpContextAccessor.HttpContext.Request.Host.HasValue)
        {
            uriBuilder.Host = httpContextAccessor.HttpContext.Request.Host.Host;
        }
        if (httpContextAccessor.HttpContext.Request.Host.Port.HasValue)
        {
            uriBuilder.Port = httpContextAccessor.HttpContext.Request.Host.Port.Value;
        }
        if (!string.IsNullOrEmpty(httpContextAccessor.HttpContext.Request.Scheme))
        {
            uriBuilder.Scheme = httpContextAccessor.HttpContext.Request.Scheme;
        }

        if (uriBuilder.Uri.IsDefaultPort)
        {
            uriBuilder.Port = -1;
        }

        return uriBuilder.ToString();
    }
}
