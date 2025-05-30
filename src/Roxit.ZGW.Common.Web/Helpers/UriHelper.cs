using System;
using System.Linq;

namespace Roxit.ZGW.Common.Web.Helpers;

public static class UriHelper
{
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

        if (!Guid.TryParse(new Uri(uri).Segments.Last(), out var guid))
        {
            return Guid.Empty;
        }

        return guid;
    }
}
