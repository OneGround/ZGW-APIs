using Microsoft.AspNetCore.Http;
using OneGround.ZGW.Common.Web.Middleware;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.ExpandsTests;

internal static class AcceptCrsTestHelper
{
    // Note: a request that carries the given Accept-Crs header (or none at all when null), for an operation that requires Accept-Crs
    // (like the zaken operations) or, with requiresAcceptCrs false, for one that does not (like GET /statussen)
    public static IHttpContextAccessor AccessorWith(string acceptCrs, bool requiresAcceptCrs = true)
    {
        var httpContext = new DefaultHttpContext();
        if (acceptCrs != null)
            httpContext.Request.Headers["Accept-Crs"] = acceptCrs;

        if (requiresAcceptCrs)
            httpContext.SetEndpoint(new Endpoint(null, new EndpointMetadataCollection(new RequiresAcceptCrs()), "test"));

        return new HttpContextAccessor { HttpContext = httpContext };
    }
}
