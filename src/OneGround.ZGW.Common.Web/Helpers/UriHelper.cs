using System;
using System.Linq;

namespace OneGround.ZGW.Common.Web.Helpers;

public static class UriHelper
{
    /// <summary>
    /// Get the id of a resource from the url of exactly that resource (<c>.../{resourceName}/{id}</c>, optionally with a trailing slash),
    /// without throwing on a malformed url.
    /// </summary>
    /// <param name="uri">Absolute http(s) resource url</param>
    /// <param name="resourceName">Name of the resource in the path, e.g. "zaken" (case-insensitive)</param>
    /// <returns>Resource Guid, or null when the url is not an absolute http(s) url of that resource, e.g. a url of one of its sub-resources</returns>
    public static Guid? TryGetResourceId(string uri, string resourceName)
    {
        if (!Uri.TryCreate(uri, UriKind.Absolute, out var parsed) || (parsed.Scheme != Uri.UriSchemeHttp && parsed.Scheme != Uri.UriSchemeHttps))
        {
            return null;
        }

        var segments = parsed.Segments.Select(s => s.TrimEnd('/')).Where(s => s.Length > 0).ToArray();

        if (segments.Length < 2 || !string.Equals(segments[^2], resourceName, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return Guid.TryParseExact(segments[^1], "D", out var id) ? id : null;
    }

    /// <summary>
    /// Get the resource id from the uri last segment.
    /// </summary>
    /// <param name="uri">Resource uri</param>
    /// <returns>Resource Guid or Guid.Empty</returns>
    public static Guid GetResourceId(string uri)
    {
        if (string.IsNullOrEmpty(uri))
        {
            return Guid.Empty;
        }

        var segments = new Uri(uri).Segments;

        for (int i = segments.Length - 1; i >= 0; i--)
        {
            var segment = segments[i].TrimEnd('/');
            if (Guid.TryParse(segment, out var guid))
            {
                return guid;
            }
        }

        return Guid.Empty;
    }
}
