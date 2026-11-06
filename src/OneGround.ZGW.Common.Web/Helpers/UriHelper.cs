using System;
using System.Linq;

namespace OneGround.ZGW.Common.Web.Helpers;

public static class UriHelper
{
    /// <summary>
    /// Get the resource id from a uri, without throwing on a malformed one.
    /// </summary>
    /// <param name="uri">Resource uri</param>
    /// <returns>Resource Guid, or null when the uri is empty, malformed or holds no Guid</returns>
    public static Guid? TryGetResourceId(string uri)
    {
        if (!Uri.TryCreate(uri, UriKind.Absolute, out _))
        {
            return null;
        }

        var id = GetResourceId(uri);

        return id == Guid.Empty ? null : id;
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
