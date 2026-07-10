#nullable enable
using System;
using Microsoft.AspNetCore.Http;

namespace OneGround.ZGW.Referentielijsten.Web.MappingProfiles;

/// <summary>
/// Rewrites a URL's host, port and scheme to match an incoming HTTP request.
/// Pure and testable — the Mapster/DI boundary lives in <see cref="AdjustUrlMapster"/>.
/// </summary>
public static class RequestUrlRewriter
{
    public static string Rewrite(string sourceUrl, HttpRequest? request)
    {
        // No request context (background job, direct Adapt, tests): leave the URL untouched.
        if (request is null)
        {
            return sourceUrl;
        }

        var uriBuilder = new UriBuilder(sourceUrl);

        if (request.Host.HasValue)
        {
            uriBuilder.Host = request.Host.Host;
        }
        if (request.Host.Port.HasValue)
        {
            uriBuilder.Port = request.Host.Port.Value;
        }
        if (!string.IsNullOrEmpty(request.Scheme))
        {
            uriBuilder.Scheme = request.Scheme;
        }

        if (uriBuilder.Uri.IsDefaultPort)
        {
            uriBuilder.Port = -1;
        }

        return uriBuilder.ToString();
    }
}
