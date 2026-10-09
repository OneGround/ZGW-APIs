using Microsoft.AspNetCore.Http;
using OneGround.ZGW.Common.Extensions;
using OneGround.ZGW.Common.Web.Middleware;

namespace OneGround.ZGW.Zaken.Web.Expands.v1._7;

/// <summary>
/// The SRID in which a ZAAK that is fetched by an expand resolver must be returned: the one of the Accept-Crs header of the incoming
/// request, so an expanded zaak is in the same CRS as the main resource (the response states a single Content-Crs).
/// <para>
/// This only applies to an operation that requires Accept-Crs (<see cref="RequiresAcceptCrs"/>), because only those answer with a Content-Crs.
/// For every other operation (for instance GET /statussen?expand=zaak) the header is ignored and the default of the query applies: 28992 (RD),
/// the CRS in which the geometry is stored. Otherwise one response could hold zaken in two CRSs without saying so. The same default applies
/// without such a header, or without a request.
/// </para>
/// </summary>
internal static class ExpandSrid
{
    public const int Default = 28992;

    public static int From(IHttpContextAccessor httpContextAccessor)
    {
        var httpContext = httpContextAccessor?.HttpContext;

        if (httpContext?.GetEndpoint()?.Metadata.GetMetadata<RequiresAcceptCrs>() is null)
        {
            return Default;
        }

        return httpContext.TryGetAcceptCrsSrid() ?? Default;
    }
}
