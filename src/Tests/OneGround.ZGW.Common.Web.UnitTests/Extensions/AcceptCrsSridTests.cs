using Microsoft.AspNetCore.Http;
using OneGround.ZGW.Common.Extensions;
using Xunit;

namespace OneGround.ZGW.Common.Web.UnitTests.Extensions;

public class AcceptCrsSridTests
{
    [Theory]
    [InlineData("EPSG:4326", 4326)]
    [InlineData("EPSG:28992", 28992)]
    [InlineData("EPSG:4937", 4937)]
    [InlineData("EPSG:9999", null)]
    [InlineData("4326", null)]
    [InlineData(null, null)]
    public void TryGetAcceptCrsSrid_MapsTheHeaderToItsSrid(string acceptCrs, int? expected)
    {
        var httpContext = new DefaultHttpContext();
        if (acceptCrs != null)
            httpContext.Request.Headers["Accept-Crs"] = acceptCrs;

        Assert.Equal(expected, httpContext.TryGetAcceptCrsSrid());
    }

    [Fact]
    public void TryGetAcceptCrsSrid_WithoutHttpContext_IsNull()
    {
        HttpContext httpContext = null;

        Assert.Null(httpContext.TryGetAcceptCrsSrid());
    }
}
