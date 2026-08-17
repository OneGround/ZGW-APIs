using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace OneGround.ZGW.Common.Web.Middleware;

public class SecurityHeadersMiddleware
{
    private const string XFrameOptionsHeader = "X-Frame-Options";
    private const string XFrameOptionsValue = "DENY";
    private const string ContentSecurityPolicyHeader = "Content-Security-Policy";
    private const string ContentSecurityPolicyValue = "frame-ancestors 'none'";

    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        SetSecurityHeaders(httpContext);

        await _next.Invoke(httpContext);
    }

    private static void SetSecurityHeaders(HttpContext httpContext)
    {
        // Register on OnStarting so the headers are (re)applied when the response actually starts. This
        // survives the response that UseExceptionHandler("/error") re-executes, which clears the earlier
        // headers. Assign (not Add) so a second registration cannot duplicate the value.
        httpContext.Response.OnStarting(() =>
        {
            httpContext.Response.Headers[XFrameOptionsHeader] = XFrameOptionsValue;
            httpContext.Response.Headers[ContentSecurityPolicyHeader] = ContentSecurityPolicyValue;

            return Task.CompletedTask;
        });
    }
}

public static class SecurityHeadersMiddlewareExtensions
{
    public static IApplicationBuilder UseZgwSecurityHeaders(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SecurityHeadersMiddleware>();
    }
}
