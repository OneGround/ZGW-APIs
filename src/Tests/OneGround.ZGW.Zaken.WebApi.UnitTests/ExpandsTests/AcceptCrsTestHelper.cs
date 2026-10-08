using Microsoft.AspNetCore.Http;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.ExpandsTests;

internal static class AcceptCrsTestHelper
{
    // Note: a request that carries the given Accept-Crs header (or none at all when null)
    public static IHttpContextAccessor AccessorWith(string acceptCrs)
    {
        var httpContext = new DefaultHttpContext();
        if (acceptCrs != null)
            httpContext.Request.Headers["Accept-Crs"] = acceptCrs;

        return new HttpContextAccessor { HttpContext = httpContext };
    }
}
