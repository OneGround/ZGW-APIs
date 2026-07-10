using Microsoft.AspNetCore.Http;
using OneGround.ZGW.Referentielijsten.Web.MappingProfiles;
using Xunit;

namespace OneGround.ZGW.Referentielijsten.WebApi.UnitTests.MappingTests;

public class RequestUrlRewriterTests
{
    private static HttpRequest BuildRequest(string host, int? port, string scheme)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = scheme;
        httpContext.Request.Host = port.HasValue ? new HostString(host, port.Value) : new HostString(host);
        return httpContext.Request;
    }

    [Fact]
    public void Rewrite_rewrites_host_port_and_scheme_to_match_the_request()
    {
        var request = BuildRequest(host: "api.example.test", port: 8443, scheme: "https");

        var result = RequestUrlRewriter.Rewrite("http://upstream-source/api/v1/resultaten/abc", request);

        Assert.Equal("https://api.example.test:8443/api/v1/resultaten/abc", result);
    }

    [Fact]
    public void Rewrite_omits_the_port_when_it_is_the_scheme_default()
    {
        // Explicit HTTPS default port (443) — Host.Port.HasValue is true so the port is rewritten to
        // 443, which IS the default for the rewritten https scheme, so IsDefaultPort is true and the
        // port is reset to -1; UriBuilder then omits it entirely from the output (unlike the 8443
        // case above). Leaving the port unspecified does NOT exercise this branch — Host.Port.HasValue
        // would be false, the port would never be overwritten, and it would stay at the *source*
        // URL's own default (80 for http), which isn't the default for https either.
        var request = BuildRequest(host: "api.example.test", port: 443, scheme: "https");

        var result = RequestUrlRewriter.Rewrite("http://upstream-source/api/v1/resultaten/abc", request);

        Assert.Equal("https://api.example.test/api/v1/resultaten/abc", result);
    }

    [Fact]
    public void Rewrite_returns_the_source_url_unchanged_when_request_is_null()
    {
        // No request context (background job, direct Adapt, tests without an HTTP pipeline).
        var result = RequestUrlRewriter.Rewrite("http://upstream-source/api/v1/resultaten/abc", request: null);

        Assert.Equal("http://upstream-source/api/v1/resultaten/abc", result);
    }
}
