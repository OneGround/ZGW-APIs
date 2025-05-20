using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.CorrelationId;

namespace OneGround.ZGW.Common.Web.Middleware;

public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext httpContext, ICorrelationContextAccessor correlationContextAccessor)
    {
        var correlationId = GetCorrelationId(httpContext);
        httpContext.TraceIdentifier = correlationId;

        using var correlationIdScope = correlationContextAccessor.SetCorrelationId(correlationId);

        SetResponseHeader(httpContext, correlationId);

        await _next.Invoke(httpContext);
    }

    private static string GetCorrelationId(HttpContext httpContext)
    {
        string correlationId = null;
        if (httpContext.Request.Headers.TryGetValue(Headers.CorrelationHeader, out var stringValues))
        {
            correlationId = stringValues.FirstOrDefault();
        }
        else if (httpContext.Request.Headers.TryGetValue(Headers.CorrelationHeaderExternal, out var stringValuesExternal))
        {
            correlationId = stringValuesExternal.FirstOrDefault();
        }

        correlationId ??= Guid.NewGuid().ToString();
        return correlationId;
    }

    private static void SetResponseHeader(HttpContext httpContext, string correlationId)
    {
        httpContext.Response.OnStarting(() =>
        {
            if (!httpContext.Response.Headers.ContainsKey(Headers.CorrelationHeader))
            {
                httpContext.Response.Headers[Headers.CorrelationHeader] = correlationId;
            }

            return Task.CompletedTask;
        });
    }
}

public static class CorrelationIdMiddlewareExtensions
{
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<CorrelationIdMiddleware>();
    }
}
