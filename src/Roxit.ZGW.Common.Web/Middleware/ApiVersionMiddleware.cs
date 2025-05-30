using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Roxit.ZGW.Common.Web.Services;
using Roxit.ZGW.Common.Web.Versioning;

namespace Roxit.ZGW.Common.Web.Middleware;

public class ApiVersionMiddleware
{
    private readonly RequestDelegate _next;

    public ApiVersionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext httpContext, IApiMetaData apiMeta, ILogger<ApiVersionMiddleware> logger)
    {
        logger.LogDebug("API request url: {RequestPath}", httpContext.Request.Path);

        httpContext.Request.Headers["Api-Versions-Supported"] = string.Join(", ", apiMeta.SupportedVersions);

        // Note: ZGW should contain the ApiVersion (with patch number) in reponse but the ApiVersion library don't support this so we add it in the respsone headers
        httpContext.Response.OnStarting(
            _ =>
            {
                var apiVersion = httpContext.GetRequestedZgwApiVersion();
                if (apiVersion != null)
                {
                    if (apiMeta.SupportedVersions.Any(v => v == apiVersion.ToString()))
                    {
                        httpContext.Response.Headers["Api-Version"] = apiVersion.ToString();
                    }
                    httpContext.Response.Headers["Api-Versions-Supported"] = string.Join(", ", apiMeta.SupportedVersions);
                }

                return Task.FromResult(0);
            },
            httpContext
        );

        await _next(httpContext);
    }
}
