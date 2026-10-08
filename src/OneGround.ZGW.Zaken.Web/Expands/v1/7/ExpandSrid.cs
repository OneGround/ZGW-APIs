using Microsoft.AspNetCore.Http;
using OneGround.ZGW.Common.Extensions;

namespace OneGround.ZGW.Zaken.Web.Expands.v1._7;

/// <summary>
/// The SRID in which a ZAAK that is fetched by an expand resolver must be returned: the one of the Accept-Crs header of the incoming
/// request, so an expanded zaak is in the same CRS as the main resource (the response states a single Content-Crs). Without such a header
/// (or without a request) the default of the query applies: 28992 (RD), the CRS in which the geometry is stored.
/// </summary>
internal static class ExpandSrid
{
    public const int Default = 28992;

    public static int From(IHttpContextAccessor httpContextAccessor) => httpContextAccessor?.HttpContext.TryGetAcceptCrsSrid() ?? Default;
}
